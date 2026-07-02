using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using Modbus.Device;

namespace Fct.ModbusRtu
{
    public sealed class ModbusRtuClient : IDisposable
    {
        private readonly object _sync = new object();
        private readonly object _ioLock = new object();
        private readonly ModbusRtuConfig _config;
        private SerialPort? _port;
        private IModbusSerialMaster? _master;
        private bool _disposed;
        private bool _closing;
        private int _ioActive;
        private static int _loggedStartup;
        private static readonly string _logPath = Path.Combine(Path.GetTempPath(), "Fct.ModbusRtu.log");

        public ModbusRtuClient(ModbusRtuConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _config.Validate();
            LogStartupOnce();
        }

        // Convenience factory for TestStand (no config file).
        public static ModbusRtuClient Create(
            string portName)
        {
            return Create(
                portName,
                baudRate: 38400,
                parity: "None",
                dataBits: 8,
                stopBits: "One",
                readTimeoutMs: 1000,
                writeTimeoutMs: 1000,
                retries: 2,
                retryDelayMs: 200,
                slaveId: 1,
                addressBase: 0);
        }

        // Full settings version (kept for compatibility).
        public static ModbusRtuClient Create(
            string portName,
            int baudRate = 9600,
            string parity = "None",
            int dataBits = 8,
            string stopBits = "One",
            int readTimeoutMs = 1000,
            int writeTimeoutMs = 1000,
            int retries = 2,
            int retryDelayMs = 200,
            byte slaveId = 1,
            int addressBase = 0)
        {
            var config = new ModbusRtuConfig
            {
                PortName = portName,
                BaudRate = baudRate,
                Parity = parity,
                DataBits = dataBits,
                StopBits = stopBits,
                ReadTimeoutMs = readTimeoutMs,
                WriteTimeoutMs = writeTimeoutMs,
                Retries = retries,
                RetryDelayMs = retryDelayMs,
                SlaveId = slaveId,
                AddressBase = addressBase
            };

            return new ModbusRtuClient(config);
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

        public static ModbusRtuClient FromConfig(string? configPath = null)
        {
            string path = string.IsNullOrWhiteSpace(configPath)
                ? GetDefaultConfigPath()
                : configPath!;

            var config = ModbusRtuConfig.Load(path);
            return new ModbusRtuClient(config);
        }

        private static string GetDefaultConfigPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var exePath = Path.Combine(baseDir, "ModbusRtu.config.json");
            if (File.Exists(exePath))
            {
                return exePath;
            }

            var dllDir = Path.GetDirectoryName(typeof(ModbusRtuClient).Assembly.Location);
            if (!string.IsNullOrWhiteSpace(dllDir))
            {
                var dllPath = Path.Combine(dllDir, "ModbusRtu.config.json");
                if (File.Exists(dllPath))
                {
                    return dllPath;
                }
            }

            return exePath;
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

                _port = _config.CreateSerialPort();
                _port.Open();

                _master = ModbusSerialMaster.CreateRtu(_port);
                _master.Transport.ReadTimeout = _config.ReadTimeoutMs;
                _master.Transport.WriteTimeout = _config.WriteTimeoutMs;
                _master.Transport.Retries = _config.Retries;
                Log("Connect: success.");
            }
        }

        public void Disconnect()
        {
            Dispose();
        }

        public bool[] ReadCoils(ushort startAddress, ushort numberOfPoints, byte? slaveId = null)
        {
            EnsureCount(numberOfPoints);
            return ExecuteWithRetry(master => master.ReadCoils(ResolveSlaveId(slaveId), ConvertAddress(startAddress), numberOfPoints));
        }

        public bool[] ReadDiscreteInputs(ushort startAddress, ushort numberOfPoints, byte? slaveId = null)
        {
            EnsureCount(numberOfPoints);
            return ExecuteWithRetry(master => master.ReadInputs(ResolveSlaveId(slaveId), ConvertAddress(startAddress), numberOfPoints));
        }

        public ushort[] ReadHoldingRegisters(ushort startAddress, ushort numberOfPoints, byte? slaveId = null)
        {
            EnsureCount(numberOfPoints);
            return ExecuteWithRetry(master => master.ReadHoldingRegisters(ResolveSlaveId(slaveId), ConvertAddress(startAddress), numberOfPoints));
        }

        public ushort[] ReadInputRegisters(ushort startAddress, ushort numberOfPoints, byte? slaveId = null)
        {
            EnsureCount(numberOfPoints);
            return ExecuteWithRetry(master => master.ReadInputRegisters(ResolveSlaveId(slaveId), ConvertAddress(startAddress), numberOfPoints));
        }

        // Reads all 48 input registers (Function 04) and returns the requested channel (1-48).
        public bool ReadInputChannel(int channelIndex, byte? slaveId = null)
        {
            if (channelIndex < 1 || channelIndex > 48)
            {
                throw new ArgumentOutOfRangeException(nameof(channelIndex), "channelIndex must be between 1 and 48.");
            }

            var startAddress = (ushort)(_config.AddressBase == 1 ? 1 : 0);
            var regs = ReadInputRegisters(startAddress, 48, slaveId);
            return regs[channelIndex - 1] != 0;
        }

        public void WriteSingleCoil(ushort address, bool value, byte? slaveId = null)
        {
            ExecuteWithRetry(master =>
            {
                master.WriteSingleCoil(ResolveSlaveId(slaveId), ConvertAddress(address), value);
                return true;
            });
        }

        public void WriteSingleRegister(ushort address, ushort value, byte? slaveId = null)
        {
            ExecuteWithRetry(master =>
            {
                master.WriteSingleRegister(ResolveSlaveId(slaveId), ConvertAddress(address), value);
                return true;
            });
        }

        public void WriteMultipleCoils(ushort startAddress, bool[] data, byte? slaveId = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            ExecuteWithRetry(master =>
            {
                master.WriteMultipleCoils(ResolveSlaveId(slaveId), ConvertAddress(startAddress), data);
                return true;
            });
        }

        public void WriteMultipleRegisters(ushort startAddress, ushort[] data, byte? slaveId = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            ExecuteWithRetry(master =>
            {
                master.WriteMultipleRegisters(ResolveSlaveId(slaveId), ConvertAddress(startAddress), data);
                return true;
            });
        }

        // Write single holding register for output channel (1-48).
        public void WriteOutputChannel(int channelIndex, bool value)
        {
            WriteOutputChannel(channelIndex, value, null);
        }

        // Write single holding register for output channel (1-48).
        public void WriteOutputChannel(int channelIndex, bool value, byte? slaveId = null)
        {
            if (channelIndex < 1 || channelIndex > 48)
            {
                throw new ArgumentOutOfRangeException(nameof(channelIndex), "channelIndex must be between 1 and 48.");
            }

            var address = _config.AddressBase == 1
                ? (ushort)channelIndex
                : (ushort)(channelIndex - 1);

            var regValue = (ushort)(value ? 1 : 0);
            WriteSingleRegister(address, regValue, slaveId);
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

        private T ExecuteWithRetry<T>(Func<IModbusSerialMaster, T> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            ThrowIfDisposed();

            var attempt = 0;
            while (true)
            {
                try
                {
                    EnsureConnected();
                    EnterIo();
                    try
                    {
                        lock (_ioLock)
                        {
                            return action(_master!);
                        }
                    }
                    finally
                    {
                        ExitIo();
                    }
                }
                catch (Exception) when (attempt < _config.Retries)
                {
                    attempt++;
                    SafeReconnect();
                    if (_config.RetryDelayMs > 0)
                    {
                        Thread.Sleep(_config.RetryDelayMs);
                    }
                }
            }
        }

        private void EnsureConnected()
        {
            if (_master != null && _port != null && _port.IsOpen)
            {
                return;
            }

            Connect();
        }

        private void SafeReconnect()
        {
            lock (_sync)
            {
                CloseInternal();
                _port = _config.CreateSerialPort();
                _port.Open();
                _master = ModbusSerialMaster.CreateRtu(_port);
                _master.Transport.ReadTimeout = _config.ReadTimeoutMs;
                _master.Transport.WriteTimeout = _config.WriteTimeoutMs;
                _master.Transport.Retries = _config.Retries;
            }
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

        private byte ResolveSlaveId(byte? slaveId)
        {
            return slaveId ?? _config.SlaveId;
        }

        private ushort ConvertAddress(ushort address)
        {
            if (_config.AddressBase == 0)
            {
                return address;
            }

            if (address == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(address), "Address cannot be 0 when AddressBase=1.");
            }

            return (ushort)(address - 1);
        }

        private static void EnsureCount(ushort numberOfPoints)
        {
            if (numberOfPoints == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfPoints), "numberOfPoints must be > 0.");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ModbusRtuClient));
            }
        }

        private void EnterIo()
        {
            lock (_sync)
            {
                if (_disposed || _closing)
                {
                    throw new ObjectDisposedException(nameof(ModbusRtuClient));
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

            Log($"Startup: Assembly={typeof(ModbusRtuClient).Assembly.Location}, Is64BitProcess={Environment.Is64BitProcess}.");
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
