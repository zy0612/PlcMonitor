using PlcMonitor.Desktop.Communication; // Modbus TCP 客户端
using PlcMonitor.Desktop.Data;          // 历史数据库
using PlcMonitor.Desktop.Models;        // PlcTag 模型
using System;                           // 基础类型
using System.Collections.ObjectModel;   // ObservableCollection
using System.Linq;                      // Select / ToArray
using System.Windows;                   // MessageBox
using System.Windows.Threading;         // DispatcherTimer
using System.Threading.Tasks;                              // 日志功能：引入 NLog 命名空间
using PlcMonitor.Desktop;
using System.Data;              // 配置功能：使用 ConfigHelper

namespace PlcMonitor.Desktop.ViewModels
{
    internal class MainViewModel
    {
        public ObservableCollection<PlcTag> Tags { get; set; } // 4 台设备集合
        private DispatcherTimer _timer;                         // 每秒采集一次的定时器
        private HistoryRepository _history;                     // SQLite 历史记录仓库
        private ModbusService _modbus;
        private bool _isPolling= false;
        private DateTime _lastErrorLogTime = DateTime.MinValue;
        private const int ErrorLogIntervalSeconds = 10;

        // 日志功能：本 ViewModel 的日志器，名字自动取类名 MainViewModel，方便定位"谁记的"
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        // 数据更新事件：主窗口订阅后用来刷新 ScottPlot 曲线
        public event EventHandler DataUpdated;

        public MainViewModel()
        {
            // 日志功能：ViewModel 构造即记一笔，证明采集即将开始
            _logger.Info("MainViewModel 启动，开始初始化设备并启动采集定时器");
            Tags = new ObservableCollection<PlcTag>
            {
                // 9.4：1、2 号归 A 线，3、4 号归 B 线
                new PlcTag { Name = "1号注塑机", Line = "A线", Value = 0, Unit = "℃", IsRunning = false, AlarmThreshold = 70 },
                new PlcTag { Name = "2号注塑机", Line = "A线", Value = 0, Unit = "℃", IsRunning = false, AlarmThreshold = 70 },
                new PlcTag { Name = "3号注塑机", Line = "B线", Value = 0, Unit = "℃", IsRunning = false, AlarmThreshold = 70 },
                new PlcTag { Name = "4号注塑机", Line = "B线", Value = 0, Unit = "℃", IsRunning = false, AlarmThreshold = 70 }
            };

            _modbus = new ModbusService();
            _history = new HistoryRepository();

            _timer = new DispatcherTimer()
            {
                // 配置功能：采集周期从 App.config 读，默认 1 秒
                Interval = TimeSpan.FromSeconds(ConfigHelper.AcquisitionIntervalSeconds())
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        // 9.3：每秒采集一次，但把阻塞的"连设备+读数据"放到后台线程，UI 不卡
        private async void Timer_Tick(object sender, EventArgs e)
        {
            if (_isPolling) return;                    // 9.3：上一次还没采完就跳过这一拍，防止重入叠加
            _isPolling = true;                         // 9.3：打上"采集中"标记，直到本次结束
            try
            {
                // 9.3：await Task.Run 把下面阻塞操作丢到线程池，await 期间 UI 线程空闲不卡界面
                double[] temps = await Task.Run(() =>
                {
                    _modbus.EnsureConnected(ConfigHelper.ModbusSlaveIp(), ConfigHelper.ModbusSlavePort()); // 后台连从站
                    return _modbus.ReadTemperatures(0, 4); // 后台读 4 台温度（服务内部已转 ℃）
                });

                // 以下代码在 await 之后自动回到 UI 线程，更新 Tags 绑定是安全的
                for (int i = 0; i < 4; i++)                      // 遍历 4 台设备，逐台更新界面与入库
                {
                    double val = temps[i];                  // 取出第 i 台设备的温度值
                    Tags[i].Value = val;                    // 更新界面绑定的温度，卡片自动刷新
                    Tags[i].IsRunning = true;               // 标记该设备在线（运行中）
                    _history.InsertHistory(Tags[i].Name, val); // 把这条温度写入 SQLite 历史表
                    _logger.Debug($"采集成功：{Tags[i].Name} 温度 {val:F1}℃"); // 日志功能：采集命中记 Debug

                    bool wasAlarm = Tags[i].IsAlarm;                              // 记录上一拍是否报警
                    bool nowAlarm = val > Tags[i].AlarmThreshold;                 // 判断这一拍是否超阈值
                    Tags[i].IsAlarm = nowAlarm;                                  // 更新当前报警状态

                    if (nowAlarm && !wasAlarm)                                   // 仅在刚越线那一刻弹窗
                    {
                        _logger.Warn($"[{Tags[i].Line}] 报警：{Tags[i].Name} 温度 {val:F1}℃ 超阈值 {Tags[i].AlarmThreshold}℃");
                        MessageBox.Show(                                         // 弹出报警提示框
                            Tags[i].Name + " 温度超限！当前 " + val.ToString("F,1") + Tags[i].Unit // 内容：设备名+当前温度
                            + "，阈值 " + Tags[i].AlarmThreshold + Tags[i].Unit,                     // 内容续：阈值
                            "温度报警", MessageBoxButton.OK, MessageBoxImage.Warning);              // 标题与图标
                    }
                }
                DataUpdated?.Invoke(this, EventArgs.Empty); // 通知主窗口刷新曲线
            }
            catch (Exception ex)
            {
                LogErrorThrottled("采集定时器执行异常", ex); // 9.1：限频记日志，断连时不会每秒刷屏
                _modbus.MarkDisconnected();                 // 9.2：让服务重置连接，下一轮重连
                for (int i = 0; i < 4; i++)                 // 遍历 4 台设备，统一置离线
                {
                    Tags[i].IsRunning = false;              // 标记离线
                    Tags[i].IsAlarm = false;                // 清除报警
                }
            }
            finally
            {
                _isPolling = false;                         // 9.3：无论成功失败，本拍结束都解除"采集中"标记
            }
        }


        private void LogErrorThrottled(string where, Exception ex)
        {
            TimeSpan gap = DateTime.Now - _lastErrorLogTime;
            if (gap.TotalSeconds>=ErrorLogIntervalSeconds)
            {
                _logger.Error(ex, where);
                _lastErrorLogTime = DateTime.Now;
            }
        }

        // 把 4 台设备的历史温度曲线画到传入的 ScottPlot.Plot 上
        public void DrawHistory(ScottPlot.Plot plt)
        {
            plt.Clear(); // 先清空旧曲线，避免重绘时叠成乱线

            // 遍历每台设备，各画一条曲线
            foreach (var tag in Tags)
            {
                // 从数据库读这台设备最近 100 条历史记录
                var history = _history.GetHistory(tag.Name, 100);
                if (history.Count == 0) continue; // 没数据就跳过，不画空线

                // X 轴：时间转成 OLE 自动化日期，ScottPlot 才能按日期显示
                double[] xs = history.Select(h => h.Item1.ToOADate()).ToArray();
                // Y 轴：温度值
                double[] ys = history.Select(h => h.Item2).ToArray();

                // 添加一条散点折线，并命名（用于图例）
                plt.AddScatter(xs, ys, label: tag.Name);
            }

            plt.Legend();                  // 显示图例（右上角列出设备名）
            plt.XAxis.TickLabelFormat("g", dateTimeFormat: true); // X 轴显示成日期时间格式
            plt.Title("历史温度曲线");      // 图表标题
            plt.YLabel("温度 (℃)");         // Y 轴标签
        }
    }
}
