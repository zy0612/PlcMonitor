using System;
using System.Net.Sockets;
using Modbus.Device;

namespace PlcMonitor.Desktop.Communication
{
    internal class ModbusTcpClient
    {
        private TcpClient _tcpClient;
        private ModbusIpMaster _master;

        public void Connect(string ip,int port)
        {
            _tcpClient = new TcpClient(ip, port);
            _master = ModbusIpMaster.CreateIp(_tcpClient);
        }

        public ushort[] ReadHoldingRegisters(ushort startAddress, ushort count)
        {
            return _master.ReadHoldingRegisters(startAddress, count);
        }

        public void Disconnect()
        {
            _master?.Dispose();
            _tcpClient?.Close();

        }
    }
}
