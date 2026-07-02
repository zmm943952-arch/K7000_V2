using System;
using Fct.ModbusRtu;

namespace Fct.ModbusRtu.Example
{
    public static class TestStandExample
    {
        // This method is designed for TestStand .NET Adapter to call directly.
        public static void RunOnce(string? configPath = null)
        {
            using (var client = ModbusRtuClient.FromConfig(configPath))
            {
                client.Connect();

                // Read 2 holding registers starting at address 100 (F=03)
                var regs = client.ReadHoldingRegisters(100, 2);

                // Write single register
                client.WriteSingleRegister(100, 1);

                client.Disconnect();
            }
        }

        // Explicit path version for TestStand to avoid config lookup issues.
        public static void RunOnceWithPath(string configPath)
        {
            RunOnce(configPath);
        }

        // Return holding registers for TestStand to capture as Return Value.
        public static ushort[] ReadHolding(string configPath, ushort startAddress, ushort count)
        {
            using (var client = ModbusRtuClient.FromConfig(configPath))
            {
                client.Connect();
                var regs = client.ReadHoldingRegisters(startAddress, count);
                client.Disconnect();
                return regs;
            }
        }
    }
}
