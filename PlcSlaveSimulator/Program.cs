using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Modbus.Device;
using Modbus.Data;
using System.Net.Sockets;

namespace PlcSlaveSimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 502);
            listener.Start();

            DataStore dataStore = DataStoreFactory.CreateDefaultDataStore();

            dataStore.HoldingRegisters[1] = 635;
            dataStore.HoldingRegisters[2] = 502;
            dataStore.HoldingRegisters[3] = 610;
            dataStore.HoldingRegisters[4] = 554;

            ModbusTcpSlave slave = ModbusTcpSlave.CreateTcp(0, listener);
            slave.DataStore = dataStore;

            Task.Run(() => slave.Listen());

            Task.Run(() =>
            {

                Random rnd = new Random();
                while (true) {
                    Thread.Sleep(1000);
                    for (int i = 1; i <= 4; i++)
                    {
                        int baseTemp = dataStore.HoldingRegisters[i];
                        int delta = rnd.Next(-5, 6);
                        int newTemp = baseTemp + delta;
                        if (newTemp < 0) newTemp = 0;
                        if (newTemp > 1500) newTemp = 1500;
                        dataStore.HoldingRegisters[i] = (ushort)newTemp;
                    }
                }
            });
            Console.WriteLine("假 PLC 从站已启动，监听 502 端口。按 Enter 停止。");  // 提示用户
            Console.ReadLine();
        }
    }
}
