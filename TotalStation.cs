using System;
using System.IO.Ports;
using System.Linq;
using System.Threading;

class SerialPortCommunication
{
    static void Main(string[] args)
    {
        // 查找所有可用的串口
        string[] availablePorts = SerialPort.GetPortNames();
        if (availablePorts.Length == 0)
        {
            Console.WriteLine("未找到可用的串口!");
            return;
        }

        // 显示找到的串口
        Console.WriteLine("找到以下串口:");
        foreach (string port in availablePorts)
        {
            Console.WriteLine(port);
        }

        // 选择第一个串口进行连接
        string selectedPort = availablePorts[0];
        Console.WriteLine($"选择串口: {selectedPort}");

        // 创建串口对象并配置
        using (SerialPort serialPort = new SerialPort(selectedPort))
        {
            try
            {
                // 配置串口参数
                serialPort.BaudRate = 9600;  // 波特率
                serialPort.Parity = Parity.None;  // 校验位
                serialPort.DataBits = 8;  // 数据位
                serialPort.StopBits = StopBits.One;  // 停止位
                serialPort.ReadTimeout = 500;  // 读取超时
                serialPort.WriteTimeout = 500;  // 写入超时

                // 打开串口
                serialPort.Open();
                Console.WriteLine("串口已连接.");

                // 发送命令
                string command = "%R1Q,50003:0,0.005,0,0,0,1,1";
                Console.WriteLine($"发送命令: {command}");

                serialPort.WriteLine(command);

                // 等待命令执行完毕
                Thread.Sleep(1000);
                Console.WriteLine("命令已发送.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"串口连接失败: {ex.Message}");
            }
        }
    }
}
