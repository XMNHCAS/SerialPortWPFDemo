using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SerialPortWPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //串口实例
        SerialPort serialPort = new SerialPort();

        //是否发送文本
        bool sendText = true;

        /// <summary>
        /// 窗体构造函数
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            //获取串口列表
            cbxSerialPortList.DataContext = SerialPort.GetPortNames();
            //设置可选波特率
            cbxBaudRate.DataContext = new object[] { 9600, 19200 };
            //设置可选奇偶校验
            cbxParity.DataContext = new object[] { "None", "Odd", "Even", "Mark", "Space" };
            //设置可选数据位
            cbxDataBits.DataContext = new object[] { 5, 6, 7, 8 };
            //设置可选停止位
            cbxStopBits.DataContext = new object[] { 1, 1.5, 2 };
            //设置发送模式
            cbxSendStatus.DataContext = new object[] { "文本", "字节" };

            //设置默认选中项
            cbxSerialPortList.SelectedIndex = 0;
            cbxBaudRate.SelectedIndex = 0;
            cbxParity.SelectedIndex = 0;
            cbxDataBits.SelectedIndex = 3;
            cbxStopBits.SelectedIndex = 0;
            cbxSendStatus.SelectedIndex = 0;

            rbxReceiveMsg.Document.Blocks.Clear();
            btnSend.IsEnabled = false;

            //注册串口接收到数据事件的回调函数
            serialPort.DataReceived += GetReceiveMsg;
        }

        /// <summary>
        /// 打开或关闭串口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (!serialPort.IsOpen)
            {
                //设定参数
                serialPort.PortName = cbxSerialPortList.SelectedItem.ToString();
                serialPort.BaudRate = (int)cbxBaudRate.SelectedItem;
                serialPort.Parity = GetSelectedParity();
                serialPort.DataBits = (int)cbxDataBits.SelectedItem;
                serialPort.StopBits = GetSelectedStopBits();

                try
                {
                    //打开串口
                    serialPort.Open();
                }
                catch (Exception)
                {
                    MessageBox.Show("无法打开此串口，请检查是否被占用");
                    return;
                }

                //切换文本
                tbxStatus.Text = "已连接";
                btnSwitch.Content = "关闭串口";

                //切换可用状态
                cbxSerialPortList.IsEnabled = false;
                cbxBaudRate.IsEnabled = false;
                cbxParity.IsEnabled = false;
                cbxDataBits.IsEnabled = false;
                cbxStopBits.IsEnabled = false;

                btnSend.IsEnabled = true;
            }
            else
            {
                if (serialPort != null)
                {
                    //关闭串口
                    serialPort.Close();
                }

                //切换文本
                tbxStatus.Text = "未连接";
                btnSwitch.Content = "打开串口";

                //切换可用状态
                cbxSerialPortList.IsEnabled = true;
                cbxBaudRate.IsEnabled = true;
                cbxParity.IsEnabled = true;
                cbxDataBits.IsEnabled = true;
                cbxStopBits.IsEnabled = true;

                btnSend.IsEnabled = false;
            }
        }

        /// <summary>
        /// 切换读写模式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbxSendStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            sendText = cbxSendStatus.SelectedItem.ToString() == "文本" ? true : false;
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            //获取RichTextBox上的文本
            string str = new TextRange(rbxSendMsg.Document.ContentStart, rbxSendMsg.Document.ContentEnd).Text;
            if (string.IsNullOrEmpty(str.Replace("\r\n", "")))
            {
                MessageBox.Show("未输入消息");
                return;
            }

            //判断读写模式
            if (sendText)
            {
                //发送字符串
                serialPort.Write(str);
            }
            else
            {
                str = str.Replace(" ", "").Replace("\r\n", "");

                //将输入的16进制字符串两两分割为字符串集合
                var strArr = Regex.Matches(str, ".{2}").Cast<Match>().Select(m => m.Value);

                //需要发送的字节数组
                byte[] data = new byte[strArr.Count()];
                
                //循环索引
                int temp = 0;

                //将字符串集合转换为字节数组
                foreach (string item in strArr)
                {
                    data[temp] = Convert.ToByte(item, 16);
                    temp++;
                }

                //发送字节
                serialPort.Write(data, 0, data.Length);
            }

            MessageBox.Show("发送成功");
        }

        /// <summary>
        /// 清空发送框的文本
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClearSendText_Click(object sender, RoutedEventArgs e)
        {
            rbxSendMsg.Document.Blocks.Clear();
        }

        /// <summary>
        /// 获取接收到的数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetReceiveMsg(object sender, SerialDataReceivedEventArgs e)
        {
            //读取串口缓冲区的字节数据
            byte[] result = new byte[serialPort.BytesToRead];
            serialPort.Read(result, 0, serialPort.BytesToRead);

            string str = $"{DateTime.Now}：\n";

            //判断读写模式
            //将接收到的字节数组转换为指定的消息格式
            if (sendText)
            {
                str += $"{Encoding.UTF8.GetString(result)}";
            }
            else
            {
                for (int i = 0; i < result.Length; i++)
                {
                    str += $"{result[i].ToString("X2")} ";
                }
            }

            //在窗体中显示接收到的消息
            SetRecMsgRbx(str.Trim());
        }

        /// <summary>
        /// 为显示接收消息的富文本框添加文本
        /// </summary>
        /// <param name="str"></param>
        private void SetRecMsgRbx(string str)
        {
            rbxReceiveMsg.Dispatcher.BeginInvoke(new Action(() =>
            {
                Run run = new Run(str);
                Paragraph p = new Paragraph();
                p.Inlines.Add(run);
                rbxReceiveMsg.Document.Blocks.Add(p);
            }));
        }

        /// <summary>
        /// 获取窗体选中的奇偶校验
        /// </summary>
        /// <returns></returns>
        private Parity GetSelectedParity()
        {
            switch (cbxParity.SelectedItem.ToString())
            {
                case "Odd":
                    return Parity.Odd;
                case "Even":
                    return Parity.Even;
                case "Mark":
                    return Parity.Mark;
                case "Space":
                    return Parity.Space;
                case "None":
                default:
                    return Parity.None;
            }
        }

        /// <summary>
        /// 获取窗体选中的停止位
        /// </summary>
        /// <returns></returns>
        private StopBits GetSelectedStopBits()
        {
            switch (Convert.ToDouble(cbxStopBits.SelectedItem))
            {
                case 1:
                    return StopBits.One;
                case 1.5:
                    return StopBits.OnePointFive;
                case 2:
                    return StopBits.Two;
                default:
                    return StopBits.One;
            }
        }
    }
}
