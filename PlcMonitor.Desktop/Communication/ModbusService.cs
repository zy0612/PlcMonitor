using System;
using System.Net.Sockets;
using Modbus.Device;
using NLog;

namespace PlcMonitor.Desktop.Communication
{
    internal class ModbusService
    {
        private TcpClient _tcpClient;
        private ModbusIpMaster _master;
        private bool _connected=false;
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        public void EnsureConnected(string ip,int port)
        {
            if(!_connected)
            {
                _tcpClient = new TcpClient(ip,port);
                _master = ModbusIpMaster.CreateIp(_tcpClient);
                _connected = true;
                _logger.Info($"已连接从站{ip}:{port}");
            }
        }
         
        public double[] ReadTemperatures(ushort startAddress,ushort count)
        {
            ushort[] regs = _master.ReadHoldingRegisters(startAddress, count);
            double[] temps = new double[regs.Length];
            for (int i = 0; i < regs.Length; i++)
                temps[i] = regs[i] / 10.0;
            return temps;
           
        }
        public void MarkDisconnected() { _connected = false; }

        public void Disconnect()
        {
            _master?.Dispose();
            _tcpClient?.Close();
            _connected = false;
        }
    }
}
