using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

internal static class Program
{
    // ===== ExitCode 约定（数字错误码）=====
    private const int CODE_OK = 0;            // 成功
    private const int CODE_FAIL = 2;          // 烧录失败（CH1=2）
    private const int CODE_START_ERROR = 1001;// AT+START 返回 ERROR
    private const int CODE_TIMEOUT = 1002;    // 总超时
    private const int CODE_PROTOCOL = 1003;   // 协议/通信/解析异常
    private const int CODE_CFG_ERROR = 1004;  // AT+FILE/AT+MODE 返回 ERROR

    // AT+Q 查询周期建议 >= 100ms
    private const int POLL_INTERVAL_MS = 120;
    private const string EOL_CRLF = "\r\n";
    private const string EOL_CR = "\r";
    private const string EOL_LF = "\n";

    public static int Main(string[] args)
    {
        // args: <COMx> <luaRelativePath> [timeoutSec]
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: H7ToolAtFlash.exe <COMx> <luaRelativePath> [timeoutSec]");
            Console.WriteLine("Example: H7ToolAtFlash.exe COM5 \"Demo/测试程序_IS25LP016.lua\" 180");
            Console.WriteLine("[FAIL] Flash failed.");
            return CODE_PROTOCOL;
        }

        string portName = args[0];
        string luaRelativePath = args[1];
        int t;
        int timeoutSec = (args.Length >= 3 && int.TryParse(args[2], out t) && t > 0) ? t : 180;

        // RS232 常用：115200 8N1
        int baudRate = 115200;

        Encoding gbk;
        try { gbk = Encoding.GetEncoding("GBK"); }
        catch { gbk = Encoding.UTF8; }

        using (var sp = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
        {
            NewLine = "\r\n",
            ReadTimeout = 200,
            WriteTimeout = 500,
            Encoding = gbk,
            DtrEnable = false,
            RtsEnable = false
        })
        {

            try
            {
                sp.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] Open " + portName + " failed: " + ex.Message);
                Console.WriteLine("[FAIL] Flash failed.");
                return CODE_PROTOCOL;
            }

        Console.WriteLine("[INFO] Port=" + portName + ", " + baudRate + " 8N1, Lua=\"" + luaRelativePath + "\", Timeout=" + timeoutSec + "s");
        LogLuaFileContent(luaRelativePath, gbk);

            try
            {
            // 0) 可选握手（不强制要求一定OK）
            var hs = SendAt(sp, "AT", 800, EOL_CRLF);
            LogResp("AT", hs);

            // 1) 选择烧录脚本文件
            var rFile = SendAt(sp, $"AT+FILE=\"{luaRelativePath}\"", 1200, EOL_CRLF);
            LogResp("AT+FILE", rFile);
            if (ContainsError(rFile)) { Console.WriteLine("[FAIL] Flash failed."); return CODE_CFG_ERROR; }

            // 2) 读回当前配置（可选）
            var rRead = SendAt(sp, "AT+READFILE", 1200, EOL_CRLF);
            LogResp("AT+READFILE", rRead);

            // 3) 单工位模式
            var rMode = SendAt(sp, "AT+MODE=0,0", 800, EOL_CRLF);
            LogResp("AT+MODE", rMode);
            if (ContainsError(rMode)) { Console.WriteLine("[FAIL] Flash failed."); return CODE_CFG_ERROR; }
            Thread.Sleep(400);

            // 4) 开始烧录
            // 观察串口助手显示 SEND ASCII/10，意味着设备可能只认 LF
            var rStart = SendAt(sp, "AT+START", 1500, EOL_LF);
            LogResp("AT+START", rStart);
            if (ContainsError(rStart)) { Console.WriteLine("[FAIL] Flash failed."); return CODE_START_ERROR; }
            Thread.Sleep(200);

            // 5) 轮询 AT+Q 直到结束
            var sw = Stopwatch.StartNew();
            List<string> lastQ = new List<string>();

            int parseFailCount = 0;

            while (sw.Elapsed.TotalSeconds < timeoutSec)
            {
                var q = SendAt(sp, "AT+Q", 800, EOL_CRLF);
                lastQ = q;

                int total;
                int ch1;
                if (TryParseQ(q, out total, out ch1))
                {
                    parseFailCount = 0;
                    Console.WriteLine("[Q] total=" + total + ", ch1=" + ch1);

                    if (total == 2) // 结束
                    {
                        if (ch1 == 1)
                        {
                            Console.WriteLine("[SUCCESS] Flash done.");
                            return CODE_OK;
                        }
                        if (ch1 == 2) { Console.WriteLine("[FAIL] Flash failed."); return CODE_FAIL; }

                        Console.WriteLine("[ERROR] Finished but CH1 status is unknown.");
                        Console.WriteLine("[FAIL] Flash failed.");
                        return CODE_PROTOCOL;
                    }
                }
                else
                {
                    parseFailCount++;
                    Console.WriteLine("[WARN] Cannot parse +Q response.");

                    // 连续多次解析失败，认为协议异常（避免一直卡住）
                    if (parseFailCount >= 10)
                    {
                        Console.WriteLine("[ERROR] Too many parse failures.");
                        Console.WriteLine("[LAST Q]\n" + JoinLines(lastQ));
                        Console.WriteLine("[FAIL] Flash failed.");
                        return CODE_PROTOCOL;
                    }
                }

                Thread.Sleep(POLL_INTERVAL_MS);
            }

            Console.WriteLine("[ERROR] TIMEOUT.");
            Console.WriteLine("[LAST Q]\n" + JoinLines(lastQ));
            Console.WriteLine("[FAIL] Flash failed.");
            return CODE_TIMEOUT;
        }
        catch (Exception ex)
        {
                Console.WriteLine("[ERROR] Exception:\n" + ex);
                Console.WriteLine("[FAIL] Flash failed.");
                return CODE_PROTOCOL;
            }
            finally
            {
                try { sp.Close(); } catch { /* ignore */ }
            }
        }
    }

    /// <summary>
    /// 发送 AT 命令并读取响应（容忍无 CRLF 的回包），直到超时或命中终止条件。
    /// </summary>
    private static List<string> SendAt(SerialPort sp, string cmd, int timeoutMs, string lineEnding)
    {
        try { sp.DiscardInBuffer(); } catch { /* ignore */ }

        // 默认 CRLF，部分设备要求只用 CR
        sp.Write(cmd + lineEnding);

        var lines = new List<string>();
        var sw = Stopwatch.StartNew();
        var sb = new StringBuilder();
        const int QUIET_MS = 120;
        long lastDataMs = 0;

        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                int toRead = sp.BytesToRead;
                if (toRead <= 0)
                {
                    if (lines.Count > 0 && (sw.ElapsedMilliseconds - lastDataMs) >= QUIET_MS)
                        break;

                    Thread.Sleep(10);
                    continue;
                }

                var buf = new byte[toRead];
                int n = sp.Read(buf, 0, buf.Length);
                if (n <= 0) continue;

                lastDataMs = sw.ElapsedMilliseconds;

                sb.Append(sp.Encoding.GetString(buf, 0, n));
                DrainLines(sb, lines);

                if (HasTerminalLine(lines)) break;
            }
            catch (TimeoutException)
            {
                // 继续等到 timeoutMs
            }
        }

        // 末尾无换行的残留数据
        string tail = sb.ToString().Trim();
        if (tail.Length > 0) lines.Add(tail);

        return lines;
    }

    private static void DrainLines(StringBuilder sb, List<string> lines)
    {
        int start = 0;
        for (int i = 0; i < sb.Length; i++)
        {
            char c = sb[i];
            if (c != '\r' && c != '\n') continue;

            if (i > start)
            {
                string line = sb.ToString(start, i - start).Trim();
                if (line.Length > 0) lines.Add(line);
            }

            // skip consecutive \r\n
            while (i + 1 < sb.Length && (sb[i + 1] == '\r' || sb[i + 1] == '\n'))
                i++;

            start = i + 1;
        }

        if (start > 0) sb.Remove(0, start);
    }

    private static bool HasTerminalLine(List<string> lines)
    {
        foreach (var line in lines)
        {
            // 终止条件：OK / ERROR
            if (line.Equals("OK", StringComparison.OrdinalIgnoreCase) ||
                line.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
                return true;

            // AT+Q 返回行
            if (line.StartsWith("+Q:", StringComparison.OrdinalIgnoreCase))
                return true;

            // AT+READFILE 可能直接回路径
            if (line.IndexOf("0:/H7-TOOL/Programmer/", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private static bool ContainsError(List<string> resp)
        => resp.Any(l => l.Equals("ERROR", StringComparison.OrdinalIgnoreCase));

    private static void LogResp(string tag, List<string> resp)
    {
        Console.WriteLine($"[{tag}]");
        if (resp.Count == 0)
        {
            Console.WriteLine("  <no response>");
            return;
        }
        foreach (var l in resp) Console.WriteLine("  " + l);
    }

    private static string JoinLines(List<string> lines)
        => string.Join(Environment.NewLine, lines);

    /// <summary>
    /// 解析 AT+Q 回包：+Q:总进度,CH1状态,CH2状态...
    /// 单工位只取 total 和 ch1
    /// </summary>
    private static bool TryParseQ(List<string> resp, out int total, out int ch1)
    {
        total = -1;
        ch1 = -1;

        foreach (var line in resp)
        {
            if (!line.StartsWith("+Q:", StringComparison.OrdinalIgnoreCase))
                continue;

            // "+Q:" 后面的内容
            var s = line.Substring(3).Trim();
            if (s.StartsWith(":", StringComparison.Ordinal)) s = s.Substring(1).Trim();

            // 取前两个字段：total,ch1
            var m = Regex.Match(s, @"^\s*(\d+)\s*,\s*(\d+)\s*(,|$)");
            if (!m.Success) return false;

            total = int.Parse(m.Groups[1].Value);
            ch1 = int.Parse(m.Groups[2].Value);
            return true;
        }

        return false;
    }

    private static void LogLuaFileContent(string luaPath, Encoding gbk)
    {
        try
        {
            // Try absolute path first; then relative to current working directory.
            string fullPath = Path.IsPathRooted(luaPath)
                ? luaPath
                : Path.GetFullPath(luaPath);

            if (!System.IO.File.Exists(fullPath))
            {
                Console.WriteLine("[INFO] LUA file not found on PC: " + fullPath);
                return;
            }

            string content;
            try
            {
                content = System.IO.File.ReadAllText(fullPath, gbk);
            }
            catch
            {
                content = System.IO.File.ReadAllText(fullPath, Encoding.UTF8);
            }

            Console.WriteLine("[LUA-BEGIN] " + fullPath);
            Console.WriteLine(content);
            Console.WriteLine("[LUA-END]");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[WARN] Cannot read LUA file content: " + ex.Message);
        }
    }
}
