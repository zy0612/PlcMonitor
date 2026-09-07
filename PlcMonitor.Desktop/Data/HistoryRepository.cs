using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Data;
using NLog; // 日志功能：引入 NLog 命名空间
using PlcMonitor.Desktop; // 配置功能：使用 ConfigHelper

namespace PlcMonitor.Desktop.Data
{
    internal class HistoryRepository
    {
        // 配置功能：连接串从 App.config 读，不用写死在代码里
        private string _connStr = ConfigHelper.DbConnectionString();
        // 日志功能：本仓库的日志器，名字自动取类名 HistoryRepository，方便定位"谁记的"
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        public HistoryRepository() 
        {
            CreateTable();
        }

        private void CreateTable()
        {
            using(var conn = new SQLiteConnection(_connStr))
            {
                conn.Open();                // 打开连接（此时才真正创建/打开 db 文件）
                new SQLiteCommand(@"
                    CREATE TABLE IF NOT EXISTS DeviceHistory (
                        Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                        DeviceName  TEXT    NOT NULL,
                        Value       REAL    NOT NULL,
                        RecordTime  TEXT    NOT NULL
                    )", conn).ExecuteNonQuery(); // ExecuteNonQuery：执行"不返回结果集"的语句（建表/插入用）
            }
        }
        public void InsertHistory(string deviceName,double value)
        {
            try // 日志功能：用 try-catch 兜住入库过程
            {
            using (var conn = new SQLiteConnection(_connStr))
            {
                conn.Open();
                var cmd = new SQLiteCommand(
                    "INSERT INTO DeviceHistory (DeviceName, Value, RecordTime) VALUES (@name, @val, @time)",
                    conn);
                cmd.Parameters.AddWithValue("@name", deviceName);
                cmd.Parameters.AddWithValue("@val", value);
                cmd.Parameters.AddWithValue("@time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.ExecuteNonQuery();
            }
            }
            catch (Exception ex) // 日志功能：入库失败记日志
            {
                // 日志功能：把异常完整写进日志（含堆栈）
                _logger.Error(ex, $"写入历史数据失败：{deviceName}");
                throw; // 继续往外抛，让上层（MainViewModel 的采集 catch）也知道出错了
            }
        }
        public List<Tuple<DateTime, double>> GetHistory(string deviceName, int limit)
        {
            var result = new List<Tuple<DateTime, double>>();
            using (var conn = new SQLiteConnection(_connStr))
            {
                conn.Open();

                string sql = "SELECT Value, RecordTime FROM DeviceHistory WHERE DeviceName=@name ORDER BY RecordTime DESC LIMIT @limit";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@name", deviceName);
                    cmd.Parameters.AddWithValue("@limit", limit);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            double value = Convert.ToDouble(reader["Value"]);
                            DateTime time = Convert.ToDateTime(reader["RecordTime"]);
                            result.Add(Tuple.Create(time, value));
                        }
                    }
                }
            }
            result.Reverse();
            return result;
        }

        public List<Tuple<string, DateTime, double>> GetAllHistory()
        {
            var result = new List<Tuple<string, DateTime, double>>(); // 三列：设备名、时间、温度
            using (var conn = new SQLiteConnection(_connStr))           // 打开数据库连接
            {
                conn.Open();
                // SQL：查出全部记录，按时间升序（从早到晚）排列
                string sql = "SELECT DeviceName, Value, RecordTime FROM DeviceHistory ORDER BY RecordTime ASC";
                using (var cmd = new SQLiteCommand(sql, conn))          // 创建命令对象
                using (var reader = cmd.ExecuteReader())              // 执行查询
                {
                    while (reader.Read())                              // 逐行读取
                    {
                        string name = reader["DeviceName"].ToString();       // 取出设备名
                        double value = Convert.ToDouble(reader["Value"]);    // 取出温度值
                        DateTime time = Convert.ToDateTime(reader["RecordTime"]); // 取出时间
                        result.Add(Tuple.Create(name, time, value));         // 加入结果列表
                    }
                }
            }
            return result; // 返回完整列表
        }
    }
}
