using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PlcMonitor.Desktop.ViewModels;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PlcMonitor.Desktop.Data;
using OfficeOpenXml;
using NLog; // 日志功能：引入 NLog 命名空间


namespace PlcMonitor.Desktop
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // 日志功能：本窗口的日志器，名字自动取类名 MainWindow，方便日后定位"谁记的"
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
              private void Window_Loaded(object sender, RoutedEventArgs e)
      {
         // 日志功能：程序窗口加载完即记一笔，证明界面已起来
         _logger.Info("程序窗口已加载，开始加载历史曲线并订阅采集事件");
         var vm = DataContext as MainViewModel; // 拿到 ViewModel
          if (vm == null) return;                // 安全判断，拿不到就不画
 
          // 第一次画历史曲线
          vm.DrawHistory(HistoryPlot.Plot);
          HistoryPlot.Refresh();                 // 刷新控件显示

          // 每次 ViewModel 采到新数据，就重画一次曲线
          vm.DataUpdated += (s, ev) =>
          {
              vm.DrawHistory(HistoryPlot.Plot);
              HistoryPlot.Refresh();
          };
      }
    private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try // 日志功能：用 try-catch 兜住整个导出过程，任何异常都能记日志并弹窗
            {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // EPPlus 5.x 必须声明非商业授权（学习/演示免费）

            var repo = new HistoryRepository();      // 新建历史仓库实例
            var all = repo.GetAllHistory();          // 查出全部历史记录（设备名, 时间, 温度）

            // 配置功能：导出目录从 App.config 读；没配或不合法就回退到桌面
            string exportDir = ConfigHelper.ExportDirectory();
            string fileName = "PLC历史温度_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx"; // 文件名带时间戳
            string filePath = Path.Combine(exportDir, fileName); // 拼成完整路径

            using (var package = new ExcelPackage())  // 创建 Excel 工作簿，用完自动释放
            {
                var ws = package.Workbook.Worksheets.Add("历史数据"); // 新建工作表，标签名"历史数据"

                ws.Cells[1, 1].Value = "设备名";   // 第1行第1列：表头
                ws.Cells[1, 2].Value = "记录时间"; // 第1行第2列：表头
                ws.Cells[1, 3].Value = "温度(℃)";  // 第1行第3列：表头
                ws.Cells[1, 1, 1, 3].Style.Font.Bold = true; // 表头三列加粗

                int row = 2;                        // 数据从第2行开始
                foreach (var item in all)           // 遍历每条记录
                {
                    ws.Cells[row, 1].Value = item.Item1; // 第1列：设备名
                    ws.Cells[row, 2].Value = item.Item2; // 第2列：时间
                    ws.Cells[row, 3].Value = item.Item3; // 第3列：温度
                    row++;                          // 行号下移
                }

                ws.Column(2).Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss"; // 时间列设为日期显示格式
                ws.Cells.AutoFitColumns();          // 自动调整列宽
                package.SaveAs(new FileInfo(filePath)); // 保存成 .xlsx 文件
            }

            // 日志功能：导出成功记一笔 Info（会写文件、也会进 VS 输出窗口）
            _logger.Info($"导出成功，共 {all.Count} 条，文件：{filePath}");
            MessageBox.Show("已导出 " + all.Count + " 条记录到：\n" + filePath, // 弹窗告知结果
                "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) // 日志功能：兜底捕获异常
            {
                // 日志功能：把异常完整写进日志（含堆栈），比只看弹窗有用得多
                _logger.Error(ex, "导出 Excel 失败");
                MessageBox.Show("导出失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
