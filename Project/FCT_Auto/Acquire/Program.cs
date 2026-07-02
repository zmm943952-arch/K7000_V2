using System;
using System.IO.Ports;
using Modbus.Device;
using System.IO;

// Demo: Read CH1 voltage from YK-DAQ20081 via Modbus RTU (RS485)
// Register 0 = CH1 high word, Register 1 = CH1 low word (32-bit signed)
// Unit scaling depends on the voltage range model:
//  ±1V  -> 0.01 mV per count  (0.00001 V)
//  ±10V -> 0.1  mV per count  (0.0001  V)
//  ±100V-> 1    mV per count  (0.001   V)

var portName = "COM5";   // TODO: change to your COM port
var slaveId = (byte)1;   // default address = 1
var baudRate = 9600;     // default baud = 9600

var voltageRange = VoltageRange.V100; // TODO: set to your board's CH1 voltage range
var scale = GetScale(voltageRange);

using var serialPort = new SerialPort(portName)
{
    BaudRate = baudRate,
    DataBits = 8,
    Parity = Parity.None,
    StopBits = StopBits.One,
    ReadTimeout = 1000,
    WriteTimeout = 1000,
    DtrEnable = true,
    RtsEnable = true
};

serialPort.Open();
serialPort.DiscardInBuffer();
serialPort.DiscardOutBuffer();
System.Threading.Thread.Sleep(200);

var master = ModbusSerialMaster.CreateRtu(serialPort);
master.Transport.Retries = 1;
master.Transport.ReadTimeout = 1000;

// Read 2 registers starting at address 0 (CH1 high/low), retry on timeout
ushort startAddress = 0x0000;
ushort numberOfPoints = 2;

ushort[] regs = ReadWithRetry(master, serialPort, slaveId, startAddress, numberOfPoints, 3, 200);
int raw = CombineToInt32(regs[0], regs[1]);

// Convert to volts
var volts = raw * scale;

Console.WriteLine($"CH1 raw = {raw}, voltage = {volts:F6} V");

static int CombineToInt32(ushort high, ushort low)
{
    uint combined = ((uint)high << 16) | low;
    return unchecked((int)combined);
}

static ushort[] ReadWithRetry(IModbusSerialMaster master, SerialPort port, byte slaveId, ushort start, ushort count, int retries, int delayMs)
{
    Exception? last = null;
    for (int i = 0; i < retries; i++)
    {
        try
        {
            return master.ReadHoldingRegisters(slaveId, start, count);
        }
        catch (Exception ex) when (ex is TimeoutException || ex is IOException || ex is InvalidOperationException || ex is NotImplementedException)
        {
            last = ex;
            try
            {
                if (port.IsOpen)
                {
                    port.DiscardInBuffer();
                    port.DiscardOutBuffer();
                }
            }
            catch { }
            System.Threading.Thread.Sleep(delayMs);
        }
    }

    throw new TimeoutException($"Read timeout after {retries} attempts.", last);
}

static double GetScale(VoltageRange range)
{
    return range switch
    {
        VoltageRange.V1 => 0.00001,   // 0.01 mV
        VoltageRange.V10 => 0.0001,   // 0.1 mV
        VoltageRange.V100 => 0.001,   // 1 mV
        _ => 0.0001
    };
}

enum VoltageRange
{
    V1,
    V10,
    V100
}
