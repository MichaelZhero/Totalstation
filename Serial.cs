using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PaddleOCR.TotalStation
{
    internal class Serial
    {
        private SerialPort _serialPort =new SerialPort();

        public string SeriPortDataBuffer = "";
        public string ThisNumber = "";

        private static readonly Dictionary<Command, CommandInfo> CommandDictionary = new Dictionary<Command, CommandInfo>
    {
        { Command.On, new CommandInfo { Description = "On", AdditionalInfo = "%R1Q,111:1" } },
        { Command.Off, new CommandInfo { Description = "Off", AdditionalInfo = "%R1Q,112:1" } },
        { Command.Laser, new CommandInfo { Description = "Laser", AdditionalInfo = "%R1Q,1004:{0}" } },
        { Command.Move, new CommandInfo { Description = "Move", AdditionalInfo = "%R1Q,50003:{0},{1},0,0,0,{2},{3}" } },
        { Command.Measure, new CommandInfo { Description = "Measure", AdditionalInfo = "%R1Q,50013:2" } },
        { Command.CurrentValue, new CommandInfo { Description = "CurrentValue", AdditionalInfo = "%R1Q,2003:1" } }
    };

        public enum Command
        {
            On,
            Off,
            Laser,
            Move,
            Measure,
            CurrentValue
        }

        public class CommandInfo
        {
            public string Description { get; set; }
            public string AdditionalInfo { get; set; }
        }

        public Serial(string portName = "COM3", int baudRate = 115200, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One) //构造函数，初始化打开串口
        {
            Open();

            // 注册接收数据事件
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);      //串口数据接收事件
        }

        private void Open()   //打开串口
        {
            //串口如果打开就关闭
            if (_serialPort.IsOpen)
            {
                try
                {
                    _serialPort.Close();
                }
                catch { }

            }
            else
            {
                try
                {
                    _serialPort.BaudRate = 115200;//波特率
                    _serialPort.PortName = SearchAndAddSerialToComboBox();//端口号             
                    _serialPort.DataBits = 8;//停止位
                    _serialPort.StopBits = StopBits.One;//数据位
                    _serialPort.Parity = Parity.None;//校验位


                    _serialPort.Open();//打开端口

                }
                catch
                {
                    MessageBox.Show("串口打开失败", "错误");
                }
            }
        } 

        private string SearchAndAddSerialToComboBox(String portName = "COM3")   //默认返回第一个Com口
        {
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();
            if (ports.Length == 0)
            {
                Console.WriteLine("未找到可用的串口!");
                // 抛出异常或者返回空
                throw new Exception("未找到可用串口");
            }

            return string.IsNullOrEmpty(portName) ? ports[0] : portName;
        }

        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)             //串口接收事件
        {


            try
            {
                ThisNumber = _serialPort.ReadExisting();
            }
            catch { }
            if (ThisNumber != "") //传递非空数据。
            {
                SeriPortDataBuffer = SeriPortDataBuffer + ThisNumber;
                //this.BeginInvoke(new Action(() =>
                //{
                //    richTextBox2.AppendText(ThisNumber);     //串口类会自动处理汉字，所以不需要特别转换
                //}));
            }

        }

        public void SendCommand(string name, int isSwitch = 1, double dHA = 0, double dVA = 0, int hDir = 0, int vDir = 0)
        {
            const string endLine = "\r\n";  // 结尾符
            const int delay = 4000;        // 延迟时间

            if (!_serialPort.IsOpen)
            {
                MessageBox.Show("请打开串口");
                return;
            }

            try
            {
                // 处理用户输入命令
                string userCommand = name.Trim().ToLower();  // 去除空格并转为小写

                // 尝试将输入转换为枚举
                if (Enum.TryParse(userCommand, true, out Command command))
                {
                    // 查找命令对应的附加信息
                    if (CommandDictionary.TryGetValue(command, out var commandInfo))
                    {
                        string additionalInfo = commandInfo.AdditionalInfo;

                        // 根据不同命令格式化附加信息
                        switch (command)
                        {
                            case Command.Laser:
                                additionalInfo = string.Format(additionalInfo, isSwitch);
                                break;

                            case Command.Move:
                                additionalInfo = string.Format(additionalInfo, dHA, dVA, hDir, vDir);
                                break;

                            default:
                                break;
                        }

                        // 发送处理后的命令
                        _serialPort.Write(additionalInfo + endLine);
                        Thread.Sleep(delay); // 延迟三秒
                    }
                    else
                    {
                        Console.WriteLine("命令在字典中未找到: {0}", userCommand);
                    }
                }
                else
                {
                    Console.WriteLine("无效命令: {0}", userCommand);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("发送命令时发生错误: " + ex.Message);
            }
        }



    }
}
