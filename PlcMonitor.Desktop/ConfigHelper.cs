using System;                          // 基础类型
using System.Configuration;           // 配置功能：读取 App.config 的 appSettings

namespace PlcMonitor.Desktop
{
    /// <summary>
    /// 配置功能：集中读取 App.config 里的 appSettings，把"写死的常量"变成"可改的配置"。
    /// </summary>
    internal static class ConfigHelper
    {
        // 配置功能：读 SQLite 连接串，默认落到 C 盘根目录(保持和原来一致)
        public static string DbConnectionString()
        {
            string cfg = ConfigurationManager.AppSettings["DbConnectionString"];
            return string.IsNullOrWhiteSpace(cfg)
                ? "Data Source=C:\\plcdata.db;Version=3;"
                : cfg;
        }

        // 配置功能：读采集周期(秒)，默认 1 秒，保持和原来一致
        public static int AcquisitionIntervalSeconds()
        {
            string cfg = ConfigurationManager.AppSettings["AcquisitionIntervalSeconds"];
            if (int.TryParse(cfg, out int seconds) && seconds > 0)
                return seconds;
            return 1; // 解析失败或没配，就用默认值
        }

        // 配置功能：读导出目录；空字符串或不合法就用桌面
        public static string ExportDirectory()
        {
            string cfg = ConfigurationManager.AppSettings["ExportDirectory"];
            if (!string.IsNullOrWhiteSpace(cfg))
            {
                try
                {
                    // 把配置里的相对路径/环境变量展开
                    string expanded = Environment.ExpandEnvironmentVariables(cfg);
                    // 如果目录存在或路径合法，就用它
                    if (System.IO.Directory.Exists(expanded))
                        return expanded;
                }
                catch { /* 出错了就回退桌面 */ }
            }
            // 默认：桌面
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        // 配置功能：读 Modbus 从站 IP，默认 127.0.0.1
        public static string ModbusSlaveIp()
        {
            string cfg = ConfigurationManager.AppSettings["ModbusSlaveIp"];
            return string.IsNullOrWhiteSpace(cfg) ? "127.0.0.1" : cfg.Trim();
        }

        // 配置功能：读 Modbus 从站端口，默认 502
        public static int ModbusSlavePort()
        {
            string cfg = ConfigurationManager.AppSettings["ModbusSlavePort"];
            if (int.TryParse(cfg, out int port) && port > 0)
                return port;
            return 502;
        }
    }
}
