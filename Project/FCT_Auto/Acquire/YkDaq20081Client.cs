using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using Modbus.Device;

namespace Fct.Acquire
{
    // YK-DAQ20081 RS485 Modbus RTU client (±100V range for all channels).
    public sealed class YkDaq20081Client : IDisposable
    {
        private readonly object _sync = new object();
        private SerialPort? _port;
        private IModbusSerialMaster? _master;
        private bool _disposed;
        private bool _closing;
        private int _ioActive;
        private static int _loggedStartup;
        private static readonly string _logPath = Path.Combine(Path.GetTempPath(), "YK_DAQ20081.log");

        public string PortName { get; }
        public int BaudRate { get; }
        public byte SlaveId { get; }

        public YkDaq20081Client(string portName, byte slaveId = 1)
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                throw new ArgumentException("PortName is required.", nameof(portName));
            }

            PortName = portName;
            BaudRate = 9600;
            SlaveId = slaveId;
            LogStartupOnce();
        }

        public bool IsConnected
        {
            get
            {
                lock (_sync)
                {
                    return _port != null && _port.IsOpen;
                }
            }
        }

        public void Connect()
        {
            ThrowIfDisposed();

            lock (_sync)
            {
                if (_master != null && _port != null && _port.IsOpen)
                {
                    return;
                }

                Log("Connect: begin.");
                CloseInternal();

                _port = new SerialPort(PortName)
                {
                    BaudRate = BaudRate,
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    ReadTimeout = 1000,
                    WriteTimeout = 1000,
                    DtrEnable = true,
                    RtsEnable = true
                };

                _port.Open();
                _port.DiscardInBuffer();
                _port.DiscardOutBuffer();
                Thread.Sleep(200);

                _master = ModbusSerialMaster.CreateRtu(_port);
                _master.Transport.Retries = 1;
                _master.Transport.ReadTimeout = 1000;
                Log("Connect: success.");
            }
        }

        public void Disconnect()
        {
            Dispose();
        }

        // Read voltage for channel (1-8). Range: ±100V, scale = 0.001 V per count.
        public double ReadVoltage(int channelIndex)
        {
            if (channelIndex < 1 || channelIndex > 48)
            {
                throw new ArgumentOutOfRangeException(nameof(channelIndex), "channelIndex must be between 1 and 8.");
            }

            EnsureConnected();
            EnterIo();
            try
            {
                ushort startAddress = (ushort)((channelIndex - 1) * 2);
                ushort[] regs = ReadWithRetry(startAddress, 2, 3, 200);
                int raw = CombineToInt32(regs[0], regs[1]);

                // ±100V: 1 mV per count.
                return raw * 0.001;
            }
            finally
            {
                ExitIo();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            lock (_sync)
            {
                if (_disposed) return;
                _disposed = true;
                Log("Dispose: begin.");
                CloseInternal();
                Log("Dispose: end.");
            }
        }

        private ushort[] ReadWithRetry(ushort start, ushort count, int retries, int delayMs)
        {
            Exception? last = null;
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    return _master!.ReadHoldingRegisters(SlaveId, start, count);
                }
                catch (Exception ex) when (ex is TimeoutException || ex is IOException || ex is InvalidOperationException || ex is NotImplementedException)
                {
                    last = ex;
                    try
                    {
                        if (_port != null && _port.IsOpen)
                        {
                            _port.DiscardInBuffer();
                            _port.DiscardOutBuffer();
                        }
                    }
                    catch { }
                    Thread.Sleep(delayMs);
                }
            }

            throw new TimeoutException($"Read timeout after {retries} attempts.", last);
        }

        private static int CombineToInt32(ushort high, ushort low)
        {
            uint combined = ((uint)high << 16) | low;
            return unchecked((int)combined);
        }

        private void EnsureConnected()
        {
            if (_master != null && _port != null && _port.IsOpen)
            {
                return;
            }

            Connect();
        }

        private void CloseInternal()
        {
            _closing = true;
            var deadline = unchecked(Environment.TickCount + 1000);
            while (_ioActive > 0)
            {
                var remaining = unchecked(deadline - Environment.TickCount);
                if (remaining <= 0)
                {
                    Log("CloseInternal: timeout waiting for active I/O, proceeding to close.");
                    break;
                }

                Monitor.Wait(_sync, (int)Math.Min(remaining, 200));
            }

            try
            {
                _master?.Dispose();
            }
            catch { }
            finally
            {
                _master = null;
            }

            try
            {
                if (_port != null)
                {
                    if (_port.IsOpen)
                    {
                        _port.Close();
                        Thread.Sleep(50);
                    }

                    _port.Dispose();
                }
            }
            catch { }
            finally
            {
                _port = null;
            }

            _closing = false;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(YkDaq20081Client));
            }
        }

        private void EnterIo()
        {
            lock (_sync)
            {
                if (_disposed || _closing)
                {
                    throw new ObjectDisposedException(nameof(YkDaq20081Client));
                }

                _ioActive++;
            }
        }

        private void ExitIo()
        {
            lock (_sync)
            {
                _ioActive--;
                if (_ioActive == 0)
                {
                    Monitor.PulseAll(_sync);
                }
            }
        }

        private static void LogStartupOnce()
        {
            if (Interlocked.Exchange(ref _loggedStartup, 1) == 1)
            {
                return;
            }

            Log($"Startup: Assembly={typeof(YkDaq20081Client).Assembly.Location}, Is64BitProcess={Environment.Is64BitProcess}.");
        }

        private static void Log(string message)
        {
            try
            {
                File.AppendAllText(_logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
            }
            catch
            {
            }
        }
    }
}
