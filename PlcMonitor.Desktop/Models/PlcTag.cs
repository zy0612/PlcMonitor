using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace PlcMonitor.Desktop.Models
{
    internal class PlcTag : INotifyPropertyChanged
    {
        private string _name;
        private double _value;
        private string _unit;
        private bool _isRunning;
        private bool _isAlarm;
        private double _alarmThreshold;

        public string Name                            // 名称属性
        {
            get { return _name; }                     // 获取时返回私有字段
            set                                       // 设置时
            {
                _name = value;                        // 先更新字段
                OnPropertyChanged(nameof(Name));      // 再通知界面"Name变了，请刷新"
            }
        }

        public double Value                           // 数值属性
        {
            get { return _value; }
            set
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        public string Unit                            // 单位属性
        {
            get { return _unit; }
            set
            {
                _unit = value;
                OnPropertyChanged(nameof(Unit));
            }
        }

        public bool IsRunning                         // 运行状态属性
        {
            get { return _isRunning; }
            set
            {
                _isRunning = value;
                OnPropertyChanged(nameof(IsRunning));
            }
        }

        public bool IsAlarm
        {
            get { return _isAlarm; }
            set
            {
                _isAlarm = value;
                OnPropertyChanged(nameof(IsAlarm));
            }
        }
         
        public double AlarmThreshold
        {
            get { return _alarmThreshold; }
            set
            {
                _alarmThreshold = value;
                OnPropertyChanged(nameof(AlarmThreshold));
            }
        }
        private string _line;                          // 9.4：所属产线私有字段，如"A线""B线"

        public string Line                             // 9.4：所属产线属性，用于分组与报警前缀
        {
            get { return _line; }                      // 获取时返回私有字段
            set                                       // 设置时
            {
                _line = value;                         // 先更新字段
                OnPropertyChanged(nameof(Line));       // 再通知界面"Line 变了，请刷新"
            }
        }


        // ========== 通知事件 ==========
        public event PropertyChangedEventHandler PropertyChanged;  // 界面订阅这个事件来感知属性变化

        protected void OnPropertyChanged(string propertyName)      // 触发通知的方法
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            // ↑ 如果界面订阅了事件（PropertyChanged 不为 null），就发送"某某属性变了"的消息
        }
    }
}
