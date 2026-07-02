using System;
using System.Diagnostics;

namespace RfpTestStation.Adapters.Flashing
{
    public sealed class WindowsProcessTerminator : IProcessTerminator
    {
        public void TerminateTree(Process process)
        {
            if (process == null)
            {
                throw new ArgumentNullException(nameof(process));
            }

            try
            {
                if (process.HasExited)
                {
                    return;
                }

                using (var taskkill = Process.Start(new ProcessStartInfo
                {
                    FileName = "taskkill.exe",
                    Arguments = "/PID " + process.Id + " /T /F",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }))
                {
                    taskkill?.WaitForExit(5000);
                }
            }
            catch
            {
                TryKill(process);
            }
        }

        private static void TryKill(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
