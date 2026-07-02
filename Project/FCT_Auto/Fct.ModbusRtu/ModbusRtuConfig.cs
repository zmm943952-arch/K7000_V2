using System;
using System.IO;
using System.IO.Ports;
using Newtonsoft.Json;

namespace Fct.ModbusRtu
{
    public sealed class ModbusRtuConfig
    {
        public string PortName { get; set; } = "COM1";
        public int BaudRate { get; set; } = 9600;
        public string Parity { get; set; } = "None";
        public int DataBits { get; set; } = 8;
        public string StopBits { get; set; } = "One";
        public int ReadTimeoutMs { get; set; } = 1000;
        public int WriteTimeoutMs { get; set; } = 1000;
        public int Retries { get; set; } = 2;
        public int RetryDelayMs { get; set; } = 200;
        public byte SlaveId { get; set; } = 1;
        public int AddressBase { get; set; } = 0;

        public static ModbusRtuConfig Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Config path is empty.", nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Config file not found.", path);
            }

            var json = File.ReadAllText(path);
            var config = JsonConvert.DeserializeObject<ModbusRtuConfig>(json);
            if (config == null)
            {
                throw new InvalidOperationException("Failed to parse config file.");
            }

            config.Validate();
            return config;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(PortName))
            {
                throw new InvalidOperationException("PortName is required.");
            }

            if (BaudRate <= 0)
            {
                throw new InvalidOperationException("BaudRate must be positive.");
            }

            if (DataBits < 5 || DataBits > 8)
            {
                throw new InvalidOperationException("DataBits must be between 5 and 8.");
            }

            if (ReadTimeoutMs < 0 || WriteTimeoutMs < 0)
            {
                throw new InvalidOperationException("Timeouts must be >= 0.");
            }

            if (Retries < 0)
            {
                throw new InvalidOperationException("Retries must be >= 0.");
            }

            if (RetryDelayMs < 0)
            {
                throw new InvalidOperationException("RetryDelayMs must be >= 0.");
            }

            if (AddressBase != 0 && AddressBase != 1)
            {
                throw new InvalidOperationException("AddressBase must be 0 or 1.");
            }
        }

        public SerialPort CreateSerialPort()
        {
            var port = new SerialPort
            {
                PortName = PortName,
                BaudRate = BaudRate,
                Parity = ParseParity(Parity),
                DataBits = DataBits,
                StopBits = ParseStopBits(StopBits),
                ReadTimeout = ReadTimeoutMs,
                WriteTimeout = WriteTimeoutMs
            };

            return port;
        }

        private static Parity ParseParity(string parity)
        {
            if (Enum.TryParse(parity, true, out Parity parsed))
            {
                return parsed;
            }

            throw new InvalidOperationException($"Invalid Parity: {parity}");
        }

        private static StopBits ParseStopBits(string stopBits)
        {
            if (Enum.TryParse(stopBits, true, out StopBits parsed))
            {
                return parsed;
            }

            throw new InvalidOperationException($"Invalid StopBits: {stopBits}");
        }
    }
}
