using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp.Extensions;
using OpenCvSharp;
using Point = OpenCvSharp.Point;
using NVRCsharpDemo;
using Microsoft.VisualBasic.Logging;
using static NVRCsharpDemo.CHCNetSDK;
using Microsoft.ML.OnnxRuntime;
using System.Runtime.InteropServices;
using Emgu.CV.CvEnum;
using Emgu.CV;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Timers;
using static System.Net.Mime.MediaTypeNames;
using YamlDotNet.Core.Tokens;
using Mat = OpenCvSharp.Mat;
using Emgu.CV.Reg;
using PaddleOCR.Main;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.IO.Ports;
using Emgu.CV.Structure;
using Scalar = OpenCvSharp.Scalar;
using System.Text.RegularExpressions;
using static System.Collections.Specialized.BitVector32;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using HalconDotNet;
using Microsoft.VisualBasic.Devices;

namespace PaddleOCR.TotalStation
{
    public partial class Test : Form
    {
        public Test()
        {
            InitializeComponent();

            m_bInitSDK = CHCNetSDK.NET_DVR_Init();

            if (m_bInitSDK == false)
            {
                MessageBox.Show("NET_DVR_Init error!");
                return;
            }

            Login();

            InitializePreview();
            RealPlayWnd.MouseClick += RealPlayWnd_MouseClick; // 绑定鼠标点击事件
        }

        private bool m_bInitSDK = false;
        private bool m_bRecord = false;
        private uint iLastErr = 0;
        private Int32 m_lUserID = -1;
        private Int32 m_lRealHandle = -1;
        private Int32 i = 0;
        private string str;
        private long iSelIndex = 0;
        private uint dwAChanTotalNum = 0;
        private uint dwDChanTotalNum = 0;
        private Int32 m_lPort = -1;
        private IntPtr m_ptrRealHandle;
        private int[] iIPDevID = new int[96];
        private int[] iChannelNum = new int[96];

        private bool captureRunning = false;

        private CHCNetSDK.REALDATACALLBACK RealData = null;
        public CHCNetSDK.NET_DVR_DEVICEINFO_V30 DeviceInfo;
        public CHCNetSDK.NET_DVR_IPPARACFG_V40 m_struIpParaCfgV40;
        public CHCNetSDK.NET_DVR_STREAM_MODE m_struStreamMode;
        public CHCNetSDK.NET_DVR_IPCHANINFO m_struChanInfo;
        public CHCNetSDK.NET_DVR_PU_STREAM_URL m_struStreamURL;
        public CHCNetSDK.NET_DVR_IPCHANINFO_V40 m_struChanInfoV40;
        private PlayCtrl.DECCBFUN m_fDisplayFun = null;
        public delegate void MyDebugInfo(string str);

        //目标检测模块
        private string modelPath = @"./Onnx/model_jjq_1.onnx";
        private string outputFolderPath = @"./images/"; // 输出文件夹路径
        private float ConfidenceThreshold = 0.8f; // 置信度阈值
        private float IoUThreshold = 0.45f; // IoU 阈值
        private InferenceSession session;

        //OCR识别模块
        //OCR识别模块
        private static InferenceSession recsion;
        private static InferenceSession detsion;
        private static string ModelPath = @"./Onnx/rec.onnx";
        private static string detPath = @"./Onnx/det.onnx";
        private const string LabelsPath = @"./Onnx/ppocr_keys_v3.txt";
        private static List<string> _labels;

        private System.Timers.Timer resetTimer; // 定时器，用于检测未检测到目标的时间

        private bool islook = false;//计价器寻找标志位
        private bool hasMoved = false;
        private bool hasZoomed = false;

        public int m_lChannel = 1;
        Bitmap bmplk;

        ModelLoader modelLoader = new ModelLoader();
        private readonly float x; // 当前窗体的宽度
        private readonly float y; // 当前窗体的高度
        ImgEdit ie = new ImgEdit();
        PreSet dlg = new PreSet();

        Serial serial = new Serial();
        //Serial serial = new Serial();

        double Maxangle = 51.2;
        double Minangle = 3;
        int ScreenWidth = 2560;
        int ScreenHigh = 1440;
        private void RealPlayWnd_MouseClick(object sender, MouseEventArgs e)
        {

            Get_Currentimg(out Bitmap img);
            // 1. 获取 PictureBox 点击的坐标
            int clickX = e.X;
            int clickY = e.Y;

            int hdir = 0;
            int vdir = 0;

            // 2. 原始图像尺寸（2560x1440）
            double originalWidth = 2560;
            double originalHeight = 1440;

            // 3. PictureBox 显示尺寸（1200x675）
            double displayWidth = RealPlayWnd.Width;
            double displayHeight = RealPlayWnd.Height;

            // 4. 计算缩放比例
            double scaleX = originalWidth / displayWidth;  // X 方向缩放比例
            double scaleY = originalHeight / displayHeight; // Y 方向缩放比例

            // 5. 计算原始图像中的坐标
            double imgX = clickX * scaleX;
            double imgY = clickY * scaleY;
            DebugInfo("目标点坐标: ({ imgX: F2}, { imgY: F2})");
            serial.SendCommand("Measure");
            string sensorData = serial.ThisNumber;
            // 获取数据并将其存储到二维数组中
            string[] sensorValues = GetDataFromSensor(sensorData);
            double L = Convert.ToDouble(sensorValues[3]) * 100;
            double width = GetMsg(L);

            double msg = width / originalWidth;

            System.Drawing.Point maxpoint = ie.maxLoc(img);

            while (true)
            {



                double y = Math.Abs(Math.Atan2(msg * (imgY - (maxpoint.Y)), L));
                double x = Math.Abs(Math.Atan2(msg * (imgX - (maxpoint.X)), L));

                if (imgX - maxpoint.X < 0)
                {
                    hdir = 1;
                }
                else
                {
                    hdir = 0;
                }

                if (imgY - maxpoint.Y > 0)
                {
                    vdir = 1;
                }
                else
                {
                    vdir = 0;
                }



                DebugInfo("移动水平弧度值" + x.ToString() + "垂直弧度值" + y.ToString());
                serial.SendCommand("Move", dHA: x, dVA: y, hDir: hdir, vDir: vdir);

                Thread.Sleep(1000);

                Get_Currentimg(out Bitmap img1);
                maxpoint = ie.maxLoc(img1);

                double num = Math.Abs(imgX - (maxpoint.X));

                if (num < 5)
                {
                    string str = "像素偏差 " + num.ToString() + "移动到目标点"; //登录失败，输出错误号 Failed to login and output the error code

                    break;
                }
                else
                {
                    string str = "像素偏差" + num.ToString() + "继续移动"; //登录失败，输出错误号 Failed to login and output the error code

                }
                DebugInfo(str);
            }










            // 6. 显示坐标信息
            MessageBox.Show($"PictureBox 点击坐标: ({clickX}, {clickY})\n"
                          + $"转换后原始图像坐标: ({imgX:F2}, {imgY:F2})",
                          "坐标转换结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        private void button1_Click(object sender, EventArgs e)
        {
            Thread ProcessThread = new Thread(Process);
            ProcessThread.Start();
        }



        private void OnTimedEvent2()
        {
            if (m_lPort >= 0)
            {
                int iWidth = 0, iHeight = 0;
                uint iActualSize = 0;

                if (!PlayCtrl.PlayM4_GetPictureSize(m_lPort, ref iWidth, ref iHeight))
                {
                    iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                    str = "PlayM4_GetPictureSize failed, error code= " + iLastErr;
                    return;
                }

                uint nBufSize = (uint)(iWidth * iHeight) * 8;
                byte[] pBitmap = new byte[nBufSize];

                if (!PlayCtrl.PlayM4_GetBMP(m_lPort, pBitmap, nBufSize, ref iActualSize))
                {
                    iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                    str = "PlayM4_GetBMP failed, error code= " + iLastErr;
                    return;
                }
                else
                {
                    // 将 BMP 数据转换为 Bitmap
                    Bitmap bmp;
                    using (MemoryStream ms = new MemoryStream(pBitmap, 0, (int)iActualSize))
                    {
                        bmp = new Bitmap(ms);
                    }

                    var (resultBitmap, centerPoint, rect) = modelLoader.ProcessAndDrawOnBitmap(bmp);

                    if (resultBitmap != null)
                    {

                        if (centerPoint.HasValue)
                        {
                            DebugInfo("目标参数位置信息" + centerPoint);
                            System.Drawing.Point point = new System.Drawing.Point((int)centerPoint.Value.X, (int)centerPoint.Value.Y);

                            Move(point);


                        }

                    }
                }
            }
        }

        private void OnTimedEvent3()
        {
            if (m_lPort >= 0)
            {
                int iWidth = 0, iHeight = 0;
                uint iActualSize = 0;

                if (!PlayCtrl.PlayM4_GetPictureSize(m_lPort, ref iWidth, ref iHeight))
                {
                    iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                    str = "PlayM4_GetPictureSize failed, error code= " + iLastErr;
                    return;
                }

                uint nBufSize = (uint)(iWidth * iHeight) * 8;
                byte[] pBitmap = new byte[nBufSize];

                if (!PlayCtrl.PlayM4_GetBMP(m_lPort, pBitmap, nBufSize, ref iActualSize))
                {
                    iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                    str = "PlayM4_GetBMP failed, error code= " + iLastErr;
                    return;
                }
                else
                {
                    // 将 BMP 数据转换为 Bitmap
                    Bitmap bmp;
                    using (MemoryStream ms = new MemoryStream(pBitmap, 0, (int)iActualSize))
                    {
                        bmp = new Bitmap(ms);
                    }

                    var (resultBitmap, centerPoint, rect) = modelLoader.ProcessAndDrawOnBitmap(bmp);
                    System.Drawing.Point maxpoint = ie.maxLoc(bmp);
                    if (resultBitmap != null)
                    {

                        if (centerPoint.HasValue)
                        {
                            int x1 = (int)centerPoint.Value.X;
                            int y1 = (int)centerPoint.Value.Y;
                            int x2 = (int)maxpoint.X;
                            int y2 = (int)maxpoint.Y;
                            int x3 = x1 - x2;
                            int y3 = y1 - y2;

                            LaserMove(x3, y3);


                        }

                    }
                }
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            Thread aa = new Thread(gongneng);
            aa.Start();

            //xxxx();
            //GetC_Radian(-2.0778,-3.8338,0.0594,-2.0447,-3.8645,0.0593,4.3606, out double dis, out double slop,out double c_huduzhi);

            //拿求出的弧度值-当前弧度值
        }

        private void gongneng()
        {
            string[,] First = Scan(20);
            int first_num = GetCentPoint(First, out double[] arr1);
            drawPoint(arr1, pictureBox1);

            string distance1 = First[first_num, 3];
            double.TryParse(distance1, out double r1);//全站仪到中线的距离

            Thread.Sleep(3000);

            double threePoint = 0.3 / r1;
            serial.SendCommand("Move", dHA: threePoint, dVA: 0.014, vDir: 0);
            DebugInfo("向右移动0.3m");

            Thread.Sleep(3000);
            string[,] Second = Scan(20);
            int second_num = GetCentPoint(Second, out double[] arr2);
            drawPoint(arr2, pictureBox2);

            string distance2 = First[first_num, 3];
            double.TryParse(distance1, out double r2);//全站仪到B点的距离

            double r3 = Math.Abs(r2 - r1);

            //拿r1和B点的弧度值求C点的坐标

            Thread.Sleep(3000);
            RadianToPoint(First[first_num, 1], First[first_num, 2], First[first_num, 3], out double first_N, out double firs_E, out double first_Z);
            RadianToPoint(Second[second_num, 1], Second[second_num, 2], Second[second_num, 3], out double second_N, out double second_E, out double second_Z);
            GetC_Radian(first_N, firs_E, first_Z, second_N, second_E, second_Z, r1, out double dis, out double slop, out double c_huduzhi);
            double.TryParse(First[first_num, 1], out double o_huduzhi);

            double a_huduzhi = o_huduzhi + (c_huduzhi - o_huduzhi) / 3;
            double b_huduzhi = o_huduzhi + (c_huduzhi - o_huduzhi) / 3 * 2;
            DebugInfo("第一个点位置在" + a_huduzhi);
            DebugInfo("第二个点位置在" + b_huduzhi);
        }

        //登录
        private void Login()
        {
            if (m_lUserID < 0)
            {
                string DVRIPAddress = IpPort.IP; //设备IP地址或者域名 Device IP
                Int16 DVRPortNumber = Int16.Parse(IpPort.Port); //设备服务端口号 Device Port
                string DVRUserName = IpPort.User; //设备登录用户名 User name to login
                string DVRPassword = IpPort.Password; //设备登录密码 Password to login

                //登录设备 Login the device
                m_lUserID = CHCNetSDK.NET_DVR_Login_V30(DVRIPAddress, DVRPortNumber, DVRUserName, DVRPassword, ref DeviceInfo);
                if (m_lUserID < 0)
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    string str = "相机登录失败，错误代码： " + iLastErr; //登录失败，输出错误号 Failed to login and output the error code
                    DebugInfo(str);
                    return;
                }
                else
                {
                    //登录成功
                    DebugInfo("相机登录成功");
                    dwAChanTotalNum = (uint)DeviceInfo.byChanNum;
                    dwDChanTotalNum = (uint)DeviceInfo.byIPChanNum + 256 * (uint)DeviceInfo.byHighDChanNum;
                    if (dwDChanTotalNum > 0)
                    {
                        //InfoIPChannel();
                    }
                    else
                    {
                        for (i = 0; i < dwAChanTotalNum; i++)
                        {
                            iChannelNum[i] = i + (int)DeviceInfo.byStartChan;
                        }
                        //comboBoxView.SelectedItem = 1;
                        // MessageBox.Show("This device has no IP channel!");
                    }
                }
            }
            else
            {
                //注销登录 Logout the device
                if (!CHCNetSDK.NET_DVR_Logout(m_lUserID))
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    string str = "相机注销失败，错误代码：" + iLastErr;
                    DebugInfo(str);
                    return;
                }
                m_lUserID = -1;
            }
            return;
        }

        private void InitializePreview()
        {

            if (m_lUserID < 0)
            {
                MessageBox.Show("Please login the device firstly!");
                return;
            }

            if (m_bRecord)
            {
                MessageBox.Show("Please stop recording firstly!");
                return;
            }

            if (m_lRealHandle < 0)
            {
                CHCNetSDK.NET_DVR_PREVIEWINFO lpPreviewInfo = new CHCNetSDK.NET_DVR_PREVIEWINFO();
                lpPreviewInfo.hPlayWnd = RealPlayWnd.Handle; ;//预览窗口 live view window
                lpPreviewInfo.lChannel = iChannelNum[(int)iSelIndex];//预览的设备通道 the device channel number
                lpPreviewInfo.dwStreamType = 0;//码流类型：0-主码流，1-子码流，2-码流3，3-码流4，以此类推
                lpPreviewInfo.dwLinkMode = 0;//连接方式：0- TCP方式，1- UDP方式，2- 多播方式，3- RTP方式，4-RTP/RTSP，5-RSTP/HTTP 
                lpPreviewInfo.bBlocked = false; //0- 非阻塞取流，1- 阻塞取流
                lpPreviewInfo.dwDisplayBufNum = 2; //播放库显示缓冲区最大帧数

                IntPtr pUser = IntPtr.Zero;//用户数据 user data 

                // 打开预览 Start live view 
                //m_lRealHandle = CHCNetSDK.NET_DVR_RealPlay_V40(m_lUserID, ref lpPreviewInfo, null, pUser);

                //回调函数执行预览
                lpPreviewInfo.hPlayWnd = IntPtr.Zero;//预览窗口 live view window
                m_ptrRealHandle = RealPlayWnd.Handle;
                RealData = new CHCNetSDK.REALDATACALLBACK(RealDataCallBack);//预览实时流回调函数 real-time stream callback function 
                m_lRealHandle = CHCNetSDK.NET_DVR_RealPlay_V40(m_lUserID, ref lpPreviewInfo, RealData, pUser);

                if (m_lRealHandle < 0)
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str = "NET_DVR_RealPlay_V40 failed, error code= " + iLastErr; //预览失败，输出错误号 failed to start live view, and output the error code.
                    DebugInfo(str);
                    return;
                }
                else
                {
                    //预览成功
                    DebugInfo("NET_DVR_RealPlay_V40 succ!");


                    //计时器启用
                }
            }
            else
            {
                //停止预览 Stop live view 
                if (!CHCNetSDK.NET_DVR_StopRealPlay(m_lRealHandle))
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str = "NET_DVR_StopRealPlay failed, error code= " + iLastErr;
                    DebugInfo(str);
                    return;
                }

                m_lRealHandle = -1;


                //计时器关闭
                RealPlayWnd.Invalidate();//刷新窗口 refresh the window
            }
            return;
        }

        public void RealDataCallBack(Int32 lRealHandle, UInt32 dwDataType, IntPtr pBuffer, UInt32 dwBufSize, IntPtr pUser)
        {
            MyDebugInfo AlarmInfo = new MyDebugInfo(DebugInfo);
            switch (dwDataType)
            {
                case CHCNetSDK.NET_DVR_SYSHEAD:     // sys head
                    if (dwBufSize > 0)
                    {
                        if (m_lPort >= 0)
                        {
                            return; //同一路码流不需要多次调用开流接口
                        }

                        if (!PlayCtrl.PlayM4_GetPort(ref m_lPort))
                        {
                            iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                            str = "PlayM4_GetPort failed, error code= " + iLastErr;
                            this.BeginInvoke(AlarmInfo, str);
                            break;
                        }


                        if (!PlayCtrl.PlayM4_SetStreamOpenMode(m_lPort, PlayCtrl.STREAME_REALTIME))
                        {
                            iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                            str = "Set STREAME_REALTIME mode failed, error code= " + iLastErr;
                            this.BeginInvoke(AlarmInfo, str);
                        }

                        if (!PlayCtrl.PlayM4_OpenStream(m_lPort, pBuffer, dwBufSize, 4 * 1024 * 1024))
                        {
                            iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                            str = "PlayM4_OpenStream failed, error code= " + iLastErr;
                            this.BeginInvoke(AlarmInfo, str);
                            break;
                        }

                        if (!PlayCtrl.PlayM4_SetDisplayBuf(m_lPort, 1)) // Set buffer to 1 for minimal latency
                        {
                            iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                            str = "PlayM4_SetDisplayBuf failed, error code= " + iLastErr;
                            this.BeginInvoke(AlarmInfo, str);
                        }

                        if (!PlayCtrl.PlayM4_SetOverlayMode(m_lPort, 0, 0))
                        {
                            iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                            str = "PlayM4_SetOverlayMode failed, error code= " + iLastErr;
                            this.BeginInvoke(AlarmInfo, str);
                        }

                        m_fDisplayFun = new PlayCtrl.DECCBFUN(DecCallbackFUN);
                        if (!PlayCtrl.PlayM4_SetDecCallBackEx(m_lPort, m_fDisplayFun, IntPtr.Zero, 0))
                        {
                            this.BeginInvoke(AlarmInfo, "PlayM4_SetDisplayCallBack fail");
                        }

                        if (!PlayCtrl.PlayM4_Play(m_lPort, m_ptrRealHandle))
                        {
                            iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                            str = "PlayM4_Play failed, error code= " + iLastErr;
                            this.BeginInvoke(AlarmInfo, str);
                            break;
                        }
                    }
                    break;
                case CHCNetSDK.NET_DVR_STREAMDATA:     // video stream data
                    if (dwBufSize > 0 && m_lPort != -1)
                    {
                        //送入码流数据进行解码 Input the stream data to decode
                        if (!PlayCtrl.PlayM4_InputData(m_lPort, pBuffer, dwBufSize))
                        {
                            iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                            str = "PlayM4_InputData failed, error code11= " + iLastErr;
                            this.BeginInvoke(AlarmInfo, str);
                        }
                    }
                    break;
                default:
                    if (dwBufSize > 0 && m_lPort != -1)
                    {
                        if (!PlayCtrl.PlayM4_InputData(m_lPort, pBuffer, dwBufSize))
                        {
                            iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                            str = "PlayM4_InputData failed, error code= " + iLastErr;
                            this.BeginInvoke(AlarmInfo, str);
                        }
                    }
                    break;
            }
        }

        public void DebugInfo(string str)
        {
            if (!string.IsNullOrWhiteSpace(str))
            {
                // 添加时间戳并确保在行末添加换行符
                string output = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {str}\r\n";


                // 如果不是在UI线程，使用Invoke确保在UI线程中执行
                if (richTextBoxInfo.InvokeRequired)
                {
                    richTextBoxInfo.Invoke(new Action(() =>
                    {
                        richTextBoxInfo.AppendText(output);
                    }));
                }
                else
                {
                    // 如果已经在UI线程，直接修改控件
                    richTextBoxInfo.AppendText(output);
                }
            }
        }

        private void DecCallbackFUN(int nPort, IntPtr pBuf, int nSize, ref PlayCtrl.FRAME_INFO pFrameInfo, int nReserved1, int nReserved2)
        {
            // 将pBuf解码后视频输入写入文件中（解码后YUV数据量极大，尤其是高清码流，不建议在回调函数中处理）
            if (pFrameInfo.nType == 3) //#define T_YV12	3
            {
                //frameCount++; // 统计帧数
                // 处理实时数据

                //    FileStream fs = null;
                //    BinaryWriter bw = null;
                //    try
                //    {
                //        fs = new FileStream("DecodedVideo.yuv", FileMode.Append);
                //        bw = new BinaryWriter(fs);
                //        byte[] byteBuf = new byte[nSize];
                //        Marshal.Copy(pBuf, byteBuf, 0, nSize);
                //        bw.Write(byteBuf);
                //        bw.Flush();
                //    }
                //    catch (System.Exception ex)
                //    {
                //        MessageBox.Show(ex.ToString());
                //    }
                //    finally
                //    {
                //        bw.Close();
                //        fs.Close();
                //    }
            }
        }



        private void Get_Currentimg(out Bitmap bmp)
        {
            bmp = null;
            dlg.m_lUserID = m_lUserID;
            dlg.m_lChannel = 1;
            dlg.m_lRealHandle = m_lRealHandle;
            List<System.Drawing.Point> doubleList = new List<System.Drawing.Point>();
            List<System.Drawing.Point> TotalStationToFourpoints = new List<System.Drawing.Point>(); //
            System.Drawing.Point Camera = new System.Drawing.Point(0, 0);
            if (m_lPort >= 0)
            {
                int iWidth = 0, iHeight = 0;
                uint iActualSize = 0;

                if (!PlayCtrl.PlayM4_GetPictureSize(m_lPort, ref iWidth, ref iHeight))
                {
                    iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                    str = "PlayM4_GetPictureSize failed, error code= " + iLastErr;
                    return;
                }

                uint nBufSize = (uint)(iWidth * iHeight) * 8;
                byte[] pBitmap = new byte[nBufSize];

                if (!PlayCtrl.PlayM4_GetBMP(m_lPort, pBitmap, nBufSize, ref iActualSize))
                {
                    iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                    str = "PlayM4_GetBMP failed, error code= " + iLastErr;
                    return;
                }
                else
                {
                    // 将 BMP 数据转换为 Bitmap

                    using (MemoryStream ms = new MemoryStream(pBitmap, 0, (int)iActualSize))
                    {
                        bmp = new Bitmap(ms);
                    }

                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            PreSet dlg = new PreSet();
            dlg.m_lUserID = m_lUserID;
            dlg.m_lChannel = 1;
            dlg.m_lRealHandle = m_lRealHandle;

            // 设置窗口启动位置为手动
            dlg.StartPosition = FormStartPosition.Manual;
            // 设置窗口位置为屏幕左侧
            //dlg.Location = new System.Drawing.Point(0, Screen.PrimaryScreen.WorkingArea.Height / 2 - dlg.Height / 2);

            dlg.ShowDialog();
        }


        // 定义类级变量保存上次鼠标位置
        private System.Drawing.Point? lastMousePosition = null;
        private Point lastMousePosition1;
        // 定义允许的最大移动范围阈值（像素值）
        private const int allowedDeltaX = 0;
        private const int allowedDeltaY = 0;
        // 定位追踪方法
        public void Move(System.Drawing.Point mousePositio)
        {
            // 检查是否有上一次的位置记录
            if (lastMousePosition.HasValue)
            {
                // 计算当前和上一次位置之间的差异
                int deltaX = Math.Abs(mousePositio.X - lastMousePosition.Value.X);
                int deltaY = Math.Abs(mousePositio.Y - lastMousePosition.Value.Y);

                // 如果差异在允许范围内，则不进行移动
                if (deltaX <= allowedDeltaX && deltaY <= allowedDeltaY)
                {
                    DebugInfo("位置变化在允许范围内，无需移动。");
                    return;
                }
            }

            // 如果没有上一次记录或差异超过阈值，则进行移动操作
            CHCNetSDK.NET_DVR_POINT_FRAME posbean = new CHCNetSDK.NET_DVR_POINT_FRAME();

            // 根据实际使用场景设置缩放因子，此处示例使用固定值3200和1800进行映射
            posbean.xTop = mousePositio.X * 255 / 2560;
            posbean.yTop = mousePositio.Y * 255 / 1440;
            posbean.xBottom = posbean.xTop;
            posbean.yBottom = posbean.yTop;
            posbean.bCounter = 1;


            if (!CHCNetSDK.NET_DVR_PTZSelZoomIn_EX(0, 1, ref posbean))
            {
                uint errorCode = CHCNetSDK.NET_DVR_GetLastError();
                string errorMsg = $"定位追踪失败，错误代码: {errorCode}，参数: {posbean}";
                DebugInfo(errorMsg);
            }
            else
            {
                DebugInfo("移动操作成功执行。");
            }

            // 更新最后一次位置为当前的位置
            lastMousePosition = mousePositio;
        }
        private void RealPlayWnd_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // 获取鼠标双击位置相对于PictureBox控件的坐标
            System.Drawing.Point clickPoint = e.Location;

            // 打印坐标
            MessageBox.Show($"鼠标双击位置: X = {clickPoint.X}, Y = {clickPoint.Y}");
        }


        private void OnTimedEvent()
        {
            if (m_lPort >= 0)
            {
                int iWidth = 0, iHeight = 0;
                uint iActualSize = 0;

                if (!PlayCtrl.PlayM4_GetPictureSize(m_lPort, ref iWidth, ref iHeight))
                {
                    iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                    str = "PlayM4_GetPictureSize failed, error code= " + iLastErr;
                    return;
                }

                uint nBufSize = (uint)(iWidth * iHeight) * 8;
                byte[] pBitmap = new byte[nBufSize];

                if (!PlayCtrl.PlayM4_GetBMP(m_lPort, pBitmap, nBufSize, ref iActualSize))
                {
                    iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                    str = "PlayM4_GetBMP failed, error code= " + iLastErr;
                    return;
                }
                else
                {
                    // 将 BMP 数据转换为 Bitmap
                    Bitmap bmp;
                    using (MemoryStream ms = new MemoryStream(pBitmap, 0, (int)iActualSize))
                    {
                        bmp = new Bitmap(ms);
                    }
                    OpenCvSharp.Mat currentImage = BitmapConverter.ToMat(bmp);

                    bmplk = bmp;
                    //ImgEdit imgEdit = new ImgEdit();
                    //imgEdit.ShowBitmapWithOpenCV(bmp);

                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OnTimedEvent2();


        }

        public void LaserMove(int x, int y)
        {
            int hDir = x < 0 ? 1 : 0;
            int vDir = y > 0 ? 1 : 0;

            //string aa = cmd.Setcmd("Move", dHA: x * 0.000556, dVA: y * 0.00056, hDir: hDir, vDir: vDir);
            serial.SendCommand("Move", dHA: Math.Abs(y) * 0.000238, dVA: Math.Abs(x) * 0.000238, hDir: hDir, vDir: vDir);

            string aa = serial.ThisNumber;
            //toatl.WriteLine(aa);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            OnTimedEvent3();
            //LensZoom(15.0f).Wait();
        }

        async Task LensZoom(float map)
        {
            // 创建 PreSet 窗体的实例
            PreSet presetForm = new PreSet
            {
                m_lUserID = m_lUserID,
                m_lRealHandle = m_lRealHandle,
                m_lChannel = 1
            };

            // 调用 ZoomInWithLimit 方法
            await presetForm.Zoom(map); // 将 100.0f 替换为所需的最大缩放值
        }





        public string[,] Scan(int dataCount)
        {

            // 创建一个二维数组来存储采集的数据
            string[,] collectedData = new string[dataCount, 10];  // 假设每次采集的数据有10个字段

            // 循环执行移动采集并将数据保存在数组中
            for (int i = 0; i < dataCount; i++)
            {
                serial.SendCommand("Move", dVA: 0.0007, vDir: 1);
                // 获取传感器数据
                serial.SendCommand("Measure");

                string sensorData = serial.ThisNumber;
                // 获取数据并将其存储到二维数组中
                string[] sensorValues = GetDataFromSensor(sensorData);
                if (sensorValues.Length > 4)
                {
                    int sppd = i + 1;
                    DebugInfo("第" + sppd + "次提取的值: " + sensorValues[3]);
                }
                // 将每次采集的数据存储到二维数组中的每一行
                for (int j = 0; j < sensorValues.Length; j++)
                {
                    collectedData[i, j] = sensorValues[j];

                }
            }

            // 返回采集的数据二维数组
            return collectedData;
        }

        // 全站仪距离测量数据解析
        private string[] GetDataFromSensor(string sensorData)
        {
            string[] parts = null;
            string[] targetValues = null;  // 用于存储多个提取的值

            // 检查数据是否有效
            if (!string.IsNullOrEmpty(sensorData))
            {
                parts = sensorData.Substring(5).Split(",");

                if (parts.Length > 1)
                {
                    // 将所有值存储到数组中，或者按需处理
                    targetValues = parts;  // 如果你想要提取所有值

                }
                else
                {
                    Console.WriteLine("传感器返回的数据格式不正确");
                    DebugInfo("传感器返回的数据为null");
                }
            }
            else
            {
                Console.WriteLine("传感器返回的数据为空");
                DebugInfo("传感器返回的数据为空");
            }

            // 如果数据有问题，返回空数组或默认值
            return targetValues ?? new string[] { "0.0" };  // 默认返回0.0
        }




        public void FourMove(Point mousePositio)
        {


            // 如果没有上一次记录或差异超过阈值，则进行移动操作
            CHCNetSDK.NET_DVR_POINT_FRAME posbean = new CHCNetSDK.NET_DVR_POINT_FRAME();

            int X = posbean.xTop;
            int Y = posbean.yTop;


            // 根据实际使用场景设置缩放因子，此处示例使用固定值3200和1800进行映射
            posbean.xTop = mousePositio.X * 255 / 2560;
            posbean.yTop = mousePositio.Y * 255 / 1440;
            posbean.xBottom = posbean.xTop;
            posbean.yBottom = posbean.yTop;
            posbean.bCounter = 1;


            if (!CHCNetSDK.NET_DVR_PTZSelZoomIn_EX(0, 1, ref posbean))
            {
                uint errorCode = CHCNetSDK.NET_DVR_GetLastError();
                string errorMsg = $"定位追踪失败，错误代码: {errorCode}，参数: {posbean}";
                DebugInfo(errorMsg);
            }
            else
            {
                DebugInfo("移动操作成功执行。");
                // LensZoom(15.0f).Wait();
            }

            Thread.Sleep(5000);
            //LensZoom(1).Wait();
            posbean.xTop = X;
            posbean.yTop = Y;
            posbean.xBottom = posbean.xTop;
            posbean.yBottom = posbean.yTop;
            posbean.bCounter = 1;
            if (!CHCNetSDK.NET_DVR_PTZSelZoomIn_EX(0, 1, ref posbean))
            {
                uint errorCode = CHCNetSDK.NET_DVR_GetLastError();
                string errorMsg = $"定位追踪失败，错误代码: {errorCode}，参数: {posbean}";
                DebugInfo(errorMsg);
            }
            else
            {
                DebugInfo("移动操作成功执行。");

            }

        }
        public struct PointD
        {
            public double X { get; set; }
            public double Y { get; set; }

            public PointD(double x, double y)
            {
                X = x;
                Y = y;
            }

            public override string ToString()
            {
                return $"({X}, {Y})";
            }
        }

        private void Process()
        {

            dlg.m_lUserID = m_lUserID;
            dlg.m_lChannel = 1;
            dlg.m_lRealHandle = m_lRealHandle;
            List<System.Drawing.Point> doubleList = new List<System.Drawing.Point>();
            List<System.Drawing.Point> TotalStationToFourpoints = new List<System.Drawing.Point>(); //
            System.Drawing.Point Camera = new System.Drawing.Point(0, 0);
            if (m_lPort >= 0)
            {
                int iWidth = 0, iHeight = 0;
                uint iActualSize = 0;

                if (!PlayCtrl.PlayM4_GetPictureSize(m_lPort, ref iWidth, ref iHeight))
                {
                    iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                    str = "PlayM4_GetPictureSize failed, error code= " + iLastErr;
                    return;
                }

                uint nBufSize = (uint)(iWidth * iHeight) * 8;
                byte[] pBitmap = new byte[nBufSize];

                if (!PlayCtrl.PlayM4_GetBMP(m_lPort, pBitmap, nBufSize, ref iActualSize))
                {
                    iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                    str = "PlayM4_GetBMP failed, error code= " + iLastErr;
                    return;
                }
                else
                {
                    // 将 BMP 数据转换为 Bitmap
                    Bitmap bmp;
                    using (MemoryStream ms = new MemoryStream(pBitmap, 0, (int)iActualSize))
                    {
                        bmp = new Bitmap(ms);
                    }

                    var (resultBitmap, centerPoint, rect) = modelLoader.ProcessAndDrawOnBitmap1(bmp);//对图片进行物体识别，返回裁切图像，中心点，宽和高
                    var (resultBitmap1, centerPoint1, rect1) = modelLoader.ProcessAndDrawOnBitmap(bmp);
                    System.Drawing.Point maxpoint = ie.maxLoc(bmp);
                    if (resultBitmap != null)
                    {
                        DebugInfo("相机启动，开始寻找槽道");

                        double mag = 1500 / (rect[0] + 0.001);//假定模型被测物体为1500mm
                        double L = 4900;//假定被测物体距离为4900mm！！！后期修改
                        var (WPanPos, WTiltPos, WZoomPos) = dlg.GetCameraBasisVectors();//获取当前相机的坐标轴
                        var (HzValue, VValue) = GetTotalStationCurrenRadian(); //获取当前全站仪的水平弧度值和垂直弧度值 Hz为水平 V为垂直
                        var (CenterlineX, CenterlineY) = FindCentralAxisOfTunnel(resultBitmap); //寻找隧道中线与槽道的两个交点  需要返回4个参数 2个坐标！！！要修改

                        int index = 0;//计数
                        double der1 = 0, der2 = 0, der3 = 0, der4 = 0;//点B和点D坐标
                        foreach (var point in centerPoint)//循环遍历模型获取四个点的坐标，将相机和激光点移动过去！！！后期需要增加图像算法，现在大致估算位置
                        {
                            double angleY = Math.Atan2(mag * (point.Y - (ScreenHigh / 2)), L) * (180.0 / Math.PI);
                            double angleX = Math.Atan2(mag * (point.X - (ScreenWidth / 2)), L) * (180.0 / Math.PI);

                            double pointangleY = Math.Abs(Math.Atan2(mag * (point.Y - (maxpoint.Y)), L));//绝对值是暂时的！！！要修改
                            double pointangleX = Math.Abs(Math.Atan2(mag * (point.X - (maxpoint.X)), L));


                            System.Drawing.Point point2 = new System.Drawing.Point(Convert.ToInt16(angleX), Convert.ToInt16(angleY));
                            switch (index)
                            {
                                case 1: // 点2的坐标
                                    der1 = pointangleX;
                                    der3 = pointangleY;
                                    break;
                                case 3: // 点4的坐标
                                    der2 = pointangleX;
                                    der4 = pointangleY;
                                    break;
                            }

                            TotalStationToFourpoints.Add(point2);

                            //计算相机在四个角的PTZ坐标
                            System.Drawing.Point CameraPoint = new System.Drawing.Point(angleX < 0 ? Convert.ToInt16(WPanPos - angleX) : Convert.ToInt16(WPanPos - angleX), angleY < 0 ? Convert.ToInt16(WTiltPos - angleY) : Convert.ToInt16(WTiltPos - angleY));
                            Camera = CalculatingAngle(CameraPoint);//纠正计算！！！有误判，要修改
                            doubleList.Add(Camera);
                            index++;
                        }

                        int index1 = 0;


                        //double derx = 0, dery = 0;
                        //int derh = 0, derv = 0;
                        //if (der1 <0)
                        //{
                        //    int xzuobiao = 1;
                        //}
                        //if (der2 < 0)
                        //{
                        //    int yzuobiao = 0;
                        //}

                        //移动相机
                        foreach (var point in doubleList)
                        {

                            int zmmpos = 8;
                            dlg.PtzSet_R(point, zmmpos);
                            Thread.Sleep(1000);
                            switch (index1)
                            {
                                case 0:
                                    serial.SendCommand("Move", dHA: der1, dVA: der4, hDir: 1, vDir: 1);
                                    DebugInfo("移动到目标点A");
                                    if (!PlayCtrl.PlayM4_GetBMP(m_lPort, pBitmap, nBufSize, ref iActualSize))
                                    { }
                                    else
                                    {
                                        Bitmap bmp1;
                                        using (MemoryStream ms = new MemoryStream(pBitmap, 0, (int)iActualSize))
                                        {
                                            bmp1 = new Bitmap(ms);
                                        }
                                        Mat image = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp1);
                                        string currentTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
                                        string filePath = @$"E:\img\{currentTime}.jpg"; ;  // 修改为你希望保存的路径
                                        Cv2.ImWrite(filePath, image);
                                    }
                                    Thread.Sleep(2000);
                                    break;
                                case 1:
                                    serial.SendCommand("Move", dHA: 0, dVA: der3 + der4, hDir: 0, vDir: 0);
                                    DebugInfo("移动到目标点B");
                                    if (!PlayCtrl.PlayM4_GetBMP(m_lPort, pBitmap, nBufSize, ref iActualSize))
                                    { }
                                    else
                                    {
                                        Bitmap bmp1;
                                        using (MemoryStream ms = new MemoryStream(pBitmap, 0, (int)iActualSize))
                                        {
                                            bmp1 = new Bitmap(ms);
                                        }
                                        Mat image = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp1);
                                        string currentTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
                                        string filePath = @$"E:\img\{currentTime}.jpg"; ;  // 修改为你希望保存的路径
                                        Cv2.ImWrite(filePath, image);
                                    }
                                    Thread.Sleep(2000);
                                    break;
                                case 2:
                                    serial.SendCommand("Move", dHA: der1 + der2, dVA: 0, hDir: 0, vDir: 0);
                                    DebugInfo("移动到目标点C");
                                    if (!PlayCtrl.PlayM4_GetBMP(m_lPort, pBitmap, nBufSize, ref iActualSize))
                                    { }
                                    else
                                    {
                                        Bitmap bmp1;
                                        using (MemoryStream ms = new MemoryStream(pBitmap, 0, (int)iActualSize))
                                        {
                                            bmp1 = new Bitmap(ms);
                                        }
                                        Mat image = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp1);
                                        string currentTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
                                        string filePath = @$"E:\img\{currentTime}.jpg"; ;  // 修改为你希望保存的路径
                                        Cv2.ImWrite(filePath, image);
                                    }
                                    Thread.Sleep(2000);
                                    break;
                                default:
                                    serial.SendCommand("Move", dHA: 0, dVA: der3 + der4, hDir: 0, vDir: 1);
                                    DebugInfo("移动到目标点D");
                                    if (!PlayCtrl.PlayM4_GetBMP(m_lPort, pBitmap, nBufSize, ref iActualSize))
                                    { }
                                    else
                                    {
                                        Bitmap bmp1;
                                        using (MemoryStream ms = new MemoryStream(pBitmap, 0, (int)iActualSize))
                                        {
                                            bmp1 = new Bitmap(ms);
                                        }
                                        Mat image = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp1);
                                        string currentTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
                                        string filePath = @$"E:\img\{currentTime}.jpg"; ;  // 修改为你希望保存的路径
                                        Cv2.ImWrite(filePath, image);
                                    }
                                    Thread.Sleep(2000);
                                    break;
                            }


                            index1++;
                        }

                        Thread.Sleep(2000);

                        //寻找隧道中线

                        System.Drawing.Point point111 = new System.Drawing.Point((int)(centerPoint1.Value.X - (rect[0] / 2) + CenterlineX.X), (int)(centerPoint1.Value.Y - (rect[1] / 2) + CenterlineX.Y));
                        int x1 = (int)point111.X;
                        int y1 = (int)point111.Y;
                        int x2 = (int)(centerPoint1.Value.X + (rect[0] / 2));
                        int y2 = (int)(centerPoint1.Value.Y + (rect[1] / 2));
                        double angleY1 = Math.Atan2(mag * (point111.Y - y2), L) * (180.0 / Math.PI);
                        double angleX1 = Math.Atan2(mag * (point111.X - x2), L) * (180.0 / Math.PI);
                        double pointangleY1 = Math.Atan2(mag * (point111.Y - y2), L);
                        double pointangleX1 = Math.Atan2(mag * (point111.X - x2), L);
                        System.Drawing.Point CameraPoint1 = new System.Drawing.Point((Camera.X + Convert.ToInt16(Math.Abs(angleX1))), (Camera.Y + Convert.ToInt16(Math.Abs(angleY1))));
                        Camera = CalculatingAngle(CameraPoint1);
                        dlg.PtzSet_R(Camera, 8);
                        serial.SendCommand("Move", dHA: Math.Abs(pointangleX1), dVA: Math.Abs(pointangleY1), hDir: 1, vDir: 0);
                        DebugInfo("移动至隧道中线顶点");
                        Thread.Sleep(5000);

                        System.Drawing.Point CameraPoint2 = new System.Drawing.Point((Camera.X - 4), (Camera.Y));

                        string[,] First = Scan(20);//开始对隧道中线进行扫描
                        int first_num = GetCentPoint(First, out double[] arr1);
                        drawPoint(arr1, pictureBox1);

                        string distance1 = First[first_num, 3];
                        double.TryParse(distance1, out double distance);

                        Thread.Sleep(3000);

                        double threePoint = 0.3 / distance;
                        serial.SendCommand("Move", dHA: threePoint, dVA: 0.014, vDir: 0);
                        Camera = CalculatingAngle(CameraPoint2);
                        dlg.PtzSet_R(Camera, 8);
                        DebugInfo("向右移动0.3m");

                        Thread.Sleep(3000);
                        string[,] Second = Scan(20); //开始对第三个螺栓位置进行扫描 但实际移动位置不对
                        int second_num = GetCentPoint(Second, out double[] arr2);
                        drawPoint(arr2, pictureBox2);

                        Thread.Sleep(3000);
                        RadianToPoint(First[first_num, 1], First[first_num, 2], First[first_num, 3], out double first_E, out double first_N, out double first_Z);//转化坐标 enz  E为x N为y 
                        RadianToPoint(Second[second_num, 1], Second[second_num, 2], Second[second_num, 3], out double second_E, out double second_N, out double second_Z);

                        GetC_Radian(first_E, first_N, first_Z, second_E, second_N, second_Z, distance, out double dis, out double slop, out double c_huduzhi);//根据两个点求取第三个点的坐标
                        double t = 0.3 / dis;
                        var (x, y, z) = GetPointOnLine(first_E, first_N, first_Z, second_E, second_N, second_Z, t);
                        var (h, v) = GetVradiansAndHRradians(x, y, z, 4.4409); //求出第三个点真实的弧度值
                        DebugInfo("水平弧度值为" + h + "垂直弧度值为  " + v);


                        var (HzValue1, VValue1) = GetTotalStationCurrenRadian();
                        double up_h = HzValue1 - h;
                        double up_v = VValue1 - v;
                        int aaa = 0, bbb = 0;
                        if (up_h > 0)
                        {
                            aaa = 1;
                        }
                        else
                        {
                            up_h = Math.Abs(up_h);
                        }
                        if (up_v > 0)
                        {
                            bbb = 1;
                        }
                        else
                        {
                            up_v = Math.Abs(up_v);
                        }
                        serial.SendCommand("Move", dHA: up_h, dVA: up_v, hDir: aaa, vDir: bbb);


                    }

                }
            }


        }


        private (double HzValue, double VValue) GetTotalStationCurrenRadian()  //全站仪当前水平和垂直弧度值解析
        {
            string str1, str2 = "";
            string[] parts = null;


            // 获取传感器数据
            serial.SendCommand("CurrentValue");
            string sensorData = serial.ThisNumber;


            if (!string.IsNullOrEmpty(sensorData))
            {
                parts = sensorData.Split(",");
                if (parts.Length > 4)
                {
                    str1 = parts[2];
                    str2 = parts[3];


                    // 尝试将提取的值转换为数字
                    if (double.TryParse(str1, out double HzValue) && double.TryParse(str2, out double VValue))
                    {
                        // 成功转换
                        return (HzValue, VValue);
                    }
                    else
                    {
                        Console.WriteLine("无法转换提取的值为数字");
                        return (0, 0);
                    }


                }
                else
                {
                    Console.WriteLine("传感器返回的数据格式不正确");
                    return (0, 0);
                }
            }
            else
            {
                Console.WriteLine("传感器返回的数据为空");
                return (0, 0);
            }

            // 如果数据有问题，返回0.0作为默认值
            return (0, 0);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            SetMsg();
        }

        static (double, double, double) GetPointOnLine(double x1, double y1, double z1, double x2, double y2, double z2, double t)
        {
            // 计算向量 AB
            double dx = x2 - x1;
            double dy = y2 - y1;
            double dz = z2 - z1;

            // 根据 t 计算点 P 的坐标
            double x = x1 + t * dx;
            double y = y1 + t * dy;
            double z = z1 + t * dz;

            return (x, y, z);
        }

        private void drawPoint(double[] arr, PictureBox pictureBox)
        {
            // 创建一个空白的图像，背景为白色
            Mat img = new Mat(2000, 2000, MatType.CV_8UC3, new Scalar(255, 255, 255));
            arr = arr.Where(x => x != 0).ToArray();
            // 定义数组的数据
            //double[] arr = new double[] { 428.99, 429.12, 429.20, 429.25, 432.11, 432.64, 432.54, 432.48, 432.34, 432.33, 432.26, 432.31, 432.21, 431.85, 429.51 };

            // 定义Y轴的范围：从4.2到4.5
            double minY = arr.Min() - 1;
            double maxY = arr.Max() + 1;

            // 图像的高度范围
            int imgHeight = 2000;
            int imgWidth = 2000;

            // 计算缩放因子，确保值在图像高度范围内
            double scaleFactor = imgHeight / (maxY - minY);

            // 用于存储图表中所有点的列表
            List<Point> points = new List<Point>();

            // 遍历数组，为每个值创建相应的点
            for (int i = 0; i < arr.Length; i++)
            {
                // 将数组的值映射到合适的Y轴位置
                int x = 100 + i * 80; // 设置X轴的位置，保证每个点之间有适当的间距
                int y = (int)(imgHeight - (arr[i] - minY) * scaleFactor); // 映射Y轴值到图像的像素值

                points.Add(new Point(x, y));
            }

            // 画出红色的线段，连接各个点
            for (int i = 1; i < points.Count; i++)
            {
                Cv2.Line(img, points[i - 1], points[i], new Scalar(0, 0, 255), 2); // 红色线条
            }

            // 绘制蓝色的圆点，标记每个数据点
            foreach (var point in points)
            {
                Cv2.Circle(img, point, 5, new Scalar(255, 0, 0), -1); // 蓝色圆点
            }

            // 绘制X轴
            Cv2.Line(img, new Point(15, imgHeight - 20), new Point(imgWidth - 15, imgHeight - 20), new Scalar(0, 0, 0), 2); // X轴

            // 绘制Y轴
            Cv2.Line(img, new Point(20, 15), new Point(20, imgHeight - 20), new Scalar(0, 0, 0), 2); // Y轴

            // 添加X轴和Y轴的标签
            for (int i = 0; i < arr.Length; i++)
            {
                int x = 100 + i * 80;
                Cv2.PutText(img, (i + 1).ToString(), new Point(x, imgHeight - 5), HersheyFonts.HersheySimplex, 1, new Scalar(0, 0, 0), 1); // X轴标签
            }

            // 添加Y轴标签
            for (double yValue = minY; yValue <= maxY; yValue += 1)
            {
                int y = (int)(imgHeight - (yValue - minY) * scaleFactor);
                Cv2.PutText(img, yValue.ToString("0.1"), new Point(5, y), HersheyFonts.HersheySimplex, 1, new Scalar(0, 0, 0), 1); // Y轴标签
            }

            // 转换 Mat 为 Bitmap
            Bitmap bitmap = BitmapConverter.ToBitmap(img);
            pictureBox.Image = bitmap;

        }



        public (System.Drawing.Point point1, System.Drawing.Point point2) FindCentralAxisOfTunnel(Bitmap resultbitmap)  //寻找隧道中线，需要修改！！！
        {

            Mat image = BitmapConverter.ToMat(resultbitmap);


            //Mat image = Cv2.ImRead("C:\\Users\\lenovo\\Desktop\\20250211\\20250211\\隧道中心.bmp", OpenCvSharp.ImreadModes.Color);
            // 获取图像的宽度和高度
            int width = image.Width;
            int height = image.Height;

            // 设置裁剪区域的大小（例如裁剪图像的中心区域，大小为原图的一半）
            int cropWidth = width / 7;
            int cropHeight = height / 2;

            // 计算裁剪区域的左上角坐标
            int xStart = (width - (5 * cropWidth));
            int yStart = (height - cropHeight) / 2;

            // 使用矩形区域裁剪图像
            Rect cropRegion = new Rect(xStart, 0, cropWidth, height);
            Mat croppedImage = new Mat(image, cropRegion);

            // 转换为灰度图像
            Mat gray = new Mat();
            Cv2.CvtColor(croppedImage, gray, ColorConversionCodes.BGR2GRAY);

            // 使用高斯模糊减少噪声
            Mat blurred = new Mat();
            Cv2.GaussianBlur(gray, blurred, new OpenCvSharp.Size(5, 5), 0);

            // 边缘检测（Canny）
            Mat edges = new Mat();
            Cv2.Canny(blurred, edges, 50, 150);


            // 使用霍夫变换检测直线
            OpenCvSharp.LineSegmentPoint[] lines = Cv2.HoughLinesP(edges, 1, Math.PI / 180, threshold: 50, minLineLength: 50, maxLineGap: 20);

            // 提取竖直方向的直线
            List<LineSegmentPoint> verticalLines = new List<LineSegmentPoint>();
            foreach (var line in lines)
            {
                // 计算直线的斜率
                double slope = (line.P2.Y - line.P1.Y) / (double)(line.P2.X - line.P1.X + 1e-6); // 防止除以零

                // 判断是否接近竖直（斜率绝对值大于某个阈值）
                if (Math.Abs(slope) > 10) // 阈值可以根据实际情况调整
                {
                    verticalLines.Add(line);
                }
            }

            int avgX = 0;
            // 如果检测到竖直直线
            if (verticalLines.Count > 0)
            {
                // 计算所有竖直直线的平均 X 坐标
                int sumX = 0;
                foreach (var line in verticalLines)
                {
                    sumX += (line.P1.X + line.P2.X) / 2; // 取每条直线的中点 X 坐标
                }
                avgX = sumX / verticalLines.Count; // 平均 X 坐标

                // 设置直线的起点和终点（延长至整个图像高度）
                Point p1 = new Point(avgX, 0); // 起点：图像顶部
                Point p2 = new Point(avgX, height); // 终点：图像底部

                // 在原图上绘制延长后的竖直直线
                Cv2.Line(croppedImage, p1, p2, new OpenCvSharp.Scalar(0, 0, 255), 2); // 红色线条
            }


            //Cv2.Line(croppedImage, verticalLines[0].P1, verticalLines[0].P2, new OpenCvSharp.Scalar(0, 0, 255), 2); // 红色线条
            System.Drawing.Point point1 = new System.Drawing.Point((int)avgX + xStart, 0);
            System.Drawing.Point point2 = new System.Drawing.Point((int)avgX + xStart, 0);


            // 显示结果
            //Cv2.ImShow("Vertical Lines", croppedImage);
            //Cv2.WaitKey(0);
            //Cv2.DestroyAllWindows();
            return (point1, point2);

        }


        public System.Drawing.Point CalculatingAngle(System.Drawing.Point Ptz)
        {


            int xResult = (Ptz.X % 360 + 360) % 360;  // X轴规范化，确保在0到360之间
            int yResult;

            if (Ptz.Y >= 0 && Ptz.Y <= 90)
            {
                yResult = Ptz.Y;  // 如果Y轴在0到90之间，直接使用Ptz.Y
            }
            else if (Ptz.Y > 90 && Ptz.Y < 355)
            {
                // 如果Y轴大于90，从90开始往下调整，且X轴增加180
                yResult = 90 - (Ptz.Y - 90);  // 从90开始调整
                xResult = (xResult + 180) % 360;  // X轴增加180并确保其在0到360之间
            }
            else if (Ptz.Y >= 355 && Ptz.Y <= 360)
            {
                // Y轴小于0时，从360开始调整，且确保最低355
                yResult = Math.Max(355, (360 + Ptz.Y % 360) % 360);  // Y轴小于0的情况
            }
            else
            {
                yResult = (Ptz.Y % 360 + 360) % 360;
            }

            return new System.Drawing.Point(xResult, yResult);

        }


        private void RadianToPoint(string V, string HR, string D, out double E, out double N, out double Z)  //弧度值转角度,根据距离求坐标
        {
            double.TryParse(V, out double Vradians);
            double.TryParse(HR, out double HRradians);
            double.TryParse(D, out double Distance);
            //double Vradians = 1.107777;
            //double HRradians = 4.700869;
            //double Distance = 4.9473;

            double Vdegree = Vradians * (180 / Math.PI);
            double HRdegree = HRradians * (180 / Math.PI);



            // 提取度数
            int VdegreePart = (int)Vdegree;
            int HRdegreePart = (int)HRdegree;

            // 提取分钟（去掉度数后的小数部分，乘以60）
            double VfractionalPart = Vdegree - VdegreePart;
            double HRfractionalPart = HRdegree - HRdegreePart;
            int VminutePart = (int)(VfractionalPart * 60);
            int HRminutePart = (int)(HRfractionalPart * 60);

            // 提取秒数（去掉分钟后的小数部分，乘以60）
            double VsecondPart = (VfractionalPart * 60 - VminutePart) * 60;
            double HRsecondPart = (HRfractionalPart * 60 - HRminutePart) * 60;

            //double E = Math.Round(Distance * Math.Sin(Math.PI * 2 - Vradians), 4);
            //double N = Math.Round(Distance * Math.Cos(Math.PI - Vradians), 4);
            //double Z = Math.Round(Distance * Math.Cos(HRradians), 4);

            E = Math.Round(Distance * Math.Sin(Math.PI * 2 - Vradians), 4);
            N = Math.Round(Distance * Math.Cos(Math.PI - Vradians), 4);
            double E1 = Math.Round(-Distance * Math.Sin(Vradians), 4);
            double N1 = Math.Round(-Distance * Math.Cos(Vradians), 4);
            Z = Math.Round(Distance * Math.Cos(HRradians), 4);

            // 输出结果
            //MessageBox.Show($"弧度 {Vradians} 转换为度分秒为: {VdegreePart}° {VminutePart}' {VsecondPart:F2} 弧度 {HRradians} 转换为度分秒为: {HRdegreePart}° {HRminutePart}' {HRsecondPart:F2}  北坐标{N} 东坐标{E} 高程：{Z}\"");
        }

        // 反推 Vradians
        static (double, double) GetVradiansAndHRradians(double E, double N, double Z, double Distance)
        {
            // 先计算 Vradians
            double cosHRradians = -N / Distance;
            double sinHRradians = -E / Distance;
            double HRradians = Math.Acos(cosHRradians);
            double HRradians2 = Math.Asin(sinHRradians);

            double Vradians = Math.PI * 2 - Math.Acos(Z / Distance);
            // 验证 E 和 Vradians 之间的关系

            if (Math.Round(E, 4) == Math.Round(Distance * sinHRradians, 4))
            {
                return (HRradians, Vradians);
            }
            else
            {
                // 如果计算的 Vradians 不符合 E 的条件，可以进行调整
                return (HRradians, Vradians); // 假设一个负的值
            }
        }

        private void GetC_Radian(double A_E, double A_N, double A_Z, double B_E, double B_N, double B_Z, double A_distance, out double distanceXY, out double slope, out double C_hudu, double distanceToMove = 0.3) //求距离和倾斜率 //计算第三个点的N和E坐标
        {
            // 假设A点和B点的三维坐标
            //double A_E =  - 3.5555, A_N = -2.3373, A_Z = -0.0734;  // A点坐标
            //double B_E = -4.3601, B_N = - 1.4526, B_Z = -0.0789;  // B点坐标

            // 1. 计算三维距离
            double distance = Math.Sqrt(Math.Pow(B_E - A_E, 2) + Math.Pow(B_N - A_N, 2) + Math.Pow(B_Z - A_Z, 2));

            // 2. 计算水平距离
            distanceXY = Math.Sqrt(Math.Pow(B_E - A_E, 2) + Math.Pow(B_N - A_N, 2));

            // 3. 计算高度差
            double deltaZ = B_Z - A_Z;

            // 4. 计算倾斜率（坡度）
            slope = deltaZ / distanceXY;  // 倾斜率（坡度）

            // 5. 计算倾斜角度（以弧度和度数表示）
            double slopeAngle = Math.Atan(slope);  // 弧度
            double slopeAngleDegrees = slopeAngle * (180 / Math.PI);  // 转换为度数

            // 计算方向向量
            double delta_E = B_E - A_E;
            double delta_N = B_N - A_N;

            // 归一化方向向量
            double magnitude = Math.Sqrt(delta_E * delta_E + delta_N * delta_N);
            double u_x = delta_E / magnitude;
            double u_y = delta_N / magnitude;

            // 计算点C的坐标
            double C_E = A_E + distanceToMove * u_x;
            double C_N = B_N + distanceToMove * u_y;


            double C_distance = ((Math.Abs(slope) / distance * 0.3) + 1) * A_distance;
            C_hudu = Math.Asin(C_N / C_distance / -1);
        }

        private int GetCentPoint(string[,] data, out double[] arr) //求凹槽的中心点  
        {


            //double[] arr = new double[] { 4.2899, 4.2912, 4.2920, 4.2925, 4.3211, 4.3264, 4.3254, 4.3248, 4.3234, 4.3233, 4.3226, 4.3231, 4.3221, 4.3185, 4.2951 };
            // 获取data数组的行数
            int rowCount = data.GetLength(0);

            // 创建一个arr数组来存储第四列的double类型值
            arr = new double[rowCount];

            // 1. 将data的每行的第四列（索引为3）转换为double类型并存入arr数组
            for (int i = 0; i < rowCount; i++)
            {
                // 尝试将data[i, 3]转换为double
                if (double.TryParse(data[i, 3], out double value))
                {
                    arr[i] = value * 100; // 存储在arr数组中
                }
                else
                {
                    Console.WriteLine($"无法将 {data[i, 3]} 转换为double");
                }
            }

            // 2. 计算arr的平均值
            double average = arr.Average();
            Console.WriteLine($"数组的平均值: {average}");

            // 3. 将大于平均值的元素的索引添加到maximaIndexes中
            var maximaIndexes = new System.Collections.Generic.List<int>();

            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] > average && arr[i] < 500)//小于6是为了测试要删除！！
                {
                    maximaIndexes.Add(i);
                }
            }

            // 4. 找到maximaIndexes的中位数索引
            if (maximaIndexes.Count == 0)
            {
                Console.WriteLine("没有大于平均值的元素");
                return -1;
            }

            int medianIndex;
            int count = maximaIndexes.Count;
            if (count % 2 == 1)
            {
                // 如果数量为奇数，取中间位置的索引
                medianIndex = maximaIndexes[count / 2];
            }
            else
            {
                // 如果数量为偶数，取中间两个索引的前一个
                medianIndex = maximaIndexes[count / 2 - 1];
            }

            // 输出中位数索引对应的arr值
            Console.WriteLine($"maximaIndexes中位数对应的arr索引为: {medianIndex}, 值为: {arr[medianIndex]}");
            return medianIndex;

        }


        public void GetCircleCenter(double x1, double y1, double z1, double x2, double y2, double z2, double x3, double y3, double z3, out double X, out double Y, out double Z, out double R)
        {
            //获取三点坐标
            //根据三点坐标求圆心
            // 计算D
            double D = 2 * ((x1 - x3) * (y2 - y3) - (x2 - x3) * (y1 - y3));

            // 计算圆心X
            X = ((x1 * x1 + y1 * y1 + z1 * z1) * (y2 - y3) +
                       (x2 * x2 + y2 * y2 + z2 * z2) * (y3 - y1) +
                       (x3 * x3 + y3 * y3 + z3 * z3) * (y1 - y2)) / D;

            // 计算圆心Y
            Y = ((x1 * x1 + y1 * y1 + z1 * z1) * (x3 - x2) +
                       (x2 * x2 + y2 * y2 + z2 * z2) * (x1 - x3) +
                       (x3 * x3 + y3 * y3 + z3 * z3) * (x2 - x1)) / D;

            // 计算圆心Z
            Z = ((x1 * x1 + y1 * y1 + z1 * z1) * (z2 - z3) +
                       (x2 * x2 + y2 * y2 + z2 * z2) * (z3 - z1) +
                       (x3 * x3 + y3 * y3 + z3 * z3) * (z1 - z2)) / D;
            //获取半径
            R = Math.Sqrt((x1 - X) * (x1 - X) + (y1 - Y) * (y1 - Y) + (z1 - Z) * (z1 - Z));
        }





        // 计算单位向量
        public static (double X, double Y, double Z) Normalize(double x, double y, double z)
        {
            double length = Math.Sqrt(x * x + y * y + z * z);
            return (x / length, y / length, z / length);
        }

        // 计算旋转后的点
        public static (double X, double Y, double Z) RotatePoint(double x, double y, double z, double uX, double uY, double uZ, double theta)
        {
            // 计算旋转后的向量
            var cosTheta = Math.Cos(theta);
            var sinTheta = Math.Sin(theta);

            var dot = DotProduct(x, y, z, uX, uY, uZ);
            var cross = CrossProduct(x, y, z, uX, uY, uZ);

            double rotatedX = x * cosTheta + cross.X * sinTheta + uX * dot * (1 - cosTheta);
            double rotatedY = y * cosTheta + cross.Y * sinTheta + uY * dot * (1 - cosTheta);
            double rotatedZ = z * cosTheta + cross.Z * sinTheta + uZ * dot * (1 - cosTheta);

            return (rotatedX, rotatedY, rotatedZ);
        }

        public (double X, double Y, double Z) GetCirclePoint(double X, double Y, double Z, double R, double x1, double y1, double z1)
        {

            // 计算弧长所对应的角度
            double L = 0.3;  // 弧长
            double theta = L / R;  // 角度

            // 计算单位向量
            var (unitX, unitY, unitZ) = Normalize(x1 - X, y1 - Y, z1 - Z);

            // 选择旋转轴，这里选择与点P1方向垂直的轴，例如简单选择Z轴
            var (uX, uY, uZ) = (0, 0, 1);  // Z轴为旋转轴

            // 旋转后的点P2
            var (x2, y2, z2) = RotatePoint(x1 - X, y1 - Y, z1 - Z, uX, uY, uZ, theta);

            // 输出结果
            Console.WriteLine($"点P2坐标: X = {x2 + X}, Y = {y2 + Y}, Z = {z2 + Z}");
            return (x2 + X, y2 + Y, z2 + Z);

        }


        public static void xxxx()
        {
            // 点A, B, C的坐标
            double x1 = -3.5537, y1 = -2.3484, z1 = -0.1237;
            double x2 = -3.9475, y2 = -1.9459, z2 = -0.1277;
            double x3 = -4.358, y3 = -1.4588, z3 = -0.1344;

            // 计算外接圆圆心
            var (X, Y, Z) = GetCircumcenter(x1, y1, z1, x2, y2, z2, x3, y3, z3);

            // 计算半径（圆心到点A的距离）
            double radius = Distance(X, Y, Z, x1, y1, z1);





        }

        // 计算两点的向量
        public static (double X, double Y, double Z) VectorBetween(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return (x2 - x1, y2 - y1, z2 - z1);
        }

        // 向量的叉积
        public static (double X, double Y, double Z) CrossProduct(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            double cx = y1 * z2 - z1 * y2;
            double cy = z1 * x2 - x1 * z2;
            double cz = x1 * y2 - y1 * x2;
            return (cx, cy, cz);
        }

        // 向量的点积
        public static double DotProduct(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return x1 * x2 + y1 * y2 + z1 * z2;
        }

        // 计算两点之间的距离
        public static double Distance(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2) + Math.Pow(z2 - z1, 2));
        }

        // 计算三维空间中三点的外接圆圆心
        public static (double X, double Y, double Z) GetCircumcenter(double x1, double y1, double z1,
                                                                      double x2, double y2, double z2,
                                                                      double x3, double y3, double z3)
        {
            // 计算边AB和AC的向量
            var (vx1, vy1, vz1) = VectorBetween(x1, y1, z1, x2, y2, z2); // AB向量
            var (vx2, vy2, vz2) = VectorBetween(x1, y1, z1, x3, y3, z3); // AC向量

            // 计算AB和AC的叉积，得到法向量
            var (nx, ny, nz) = CrossProduct(vx1, vy1, vz1, vx2, vy2, vz2);

            // 计算中点
            var (mx1, my1, mz1) = ((x1 + x2) / 2, (y1 + y2) / 2, (z1 + z2) / 2);  // AB的中点
            var (mx2, my2, mz2) = ((x2 + x3) / 2, (y2 + y3) / 2, (z2 + z3) / 2);  // BC的中点

            // 计算垂直平分线的方向
            // 计算 AB 的垂直平分线
            var (perp1X, perp1Y, perp1Z) = CrossProduct(nx, ny, nz, vx1, vy1, vz1);
            var (perp2X, perp2Y, perp2Z) = CrossProduct(nx, ny, nz, vx2, vy2, vz2);

            // 解线性方程，求得圆心
            // 由于空间限制，这里使用近似方法。直接采用叉积法得到近似值
            double X = mx1 + (mx2 - mx1) / 2;
            double Y = my1 + (my2 - my1) / 2;
            double Z = mz1 + (mz2 - mz1) / 2;

            return (X, Y, Z);
        }

        private void action(HObject image, out System.Drawing.Point Point)
        {

            // Local iconic variables 

            HObject ho_TemplateRoi, ho_ModelContours, ho_TransContours;
            HObject ho_Image, ho_Highpass, ho_Image1, ho_Image2, ho_Image3;
            HObject ho_ConnectedRegions1, ho_UnionContours, ho_SelectedXLD;
            HObject ho_Rectangle, ho_ImageReduced, ho_Regions, ho_ConnectedRegions;
            HObject ho_SelectedRegions, ho_RegionFillUp, ho_RegionTrans;
            HObject ho_ImageReduced1, ho_OutEdges, ho_UnionContours1;

            // Local control variables 

            HTuple hv_ModelID = new HTuple(), hv_ModelRegionArea = new HTuple();
            HTuple hv_RefRow = new HTuple(), hv_RefColumn = new HTuple();
            HTuple hv_HomMat2D = new HTuple(), hv_Row = new HTuple();
            HTuple hv_Column = new HTuple(), hv_Angle = new HTuple();
            HTuple hv_Score = new HTuple(), hv_Area = new HTuple();
            HTuple hv_Row1 = new HTuple(), hv_Column1 = new HTuple();
            HTuple hv_PointOrder = new HTuple(), hv_Column1_Max = new HTuple();
            HTuple hv_Row1_Max = new HTuple(), hv_Column1_Min = new HTuple();
            HTuple hv_Row1_Min = new HTuple(), hv_Row11 = new HTuple();
            HTuple hv_Column11 = new HTuple(), hv_Row2 = new HTuple();
            HTuple hv_Column2 = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_TemplateRoi);
            HOperatorSet.GenEmptyObj(out ho_ModelContours);
            HOperatorSet.GenEmptyObj(out ho_TransContours);
            HOperatorSet.GenEmptyObj(out ho_Image);
            HOperatorSet.GenEmptyObj(out ho_Highpass);
            HOperatorSet.GenEmptyObj(out ho_Image1);
            HOperatorSet.GenEmptyObj(out ho_Image2);
            HOperatorSet.GenEmptyObj(out ho_Image3);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions1);
            HOperatorSet.GenEmptyObj(out ho_UnionContours);
            HOperatorSet.GenEmptyObj(out ho_SelectedXLD);
            HOperatorSet.GenEmptyObj(out ho_Rectangle);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced);
            HOperatorSet.GenEmptyObj(out ho_Regions);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions);
            HOperatorSet.GenEmptyObj(out ho_RegionFillUp);
            HOperatorSet.GenEmptyObj(out ho_RegionTrans);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced1);
            HOperatorSet.GenEmptyObj(out ho_OutEdges);
            HOperatorSet.GenEmptyObj(out ho_UnionContours1);
            ho_TemplateRoi.Dispose();
            HOperatorSet.ReadImage(out ho_TemplateRoi, "C:/Users/27703/Desktop/Extract_Image_feature-main/1.bmp");

            HOperatorSet.ReadShapeModel("C:/Users/27703/Desktop/Extract_Image_feature-main/model1.sbm", out hv_ModelID);
            ho_ModelContours.Dispose();
            HOperatorSet.GetShapeModelContours(out ho_ModelContours, hv_ModelID, 1);


            HOperatorSet.AreaCenter(image, out hv_ModelRegionArea, out hv_RefRow,
                out hv_RefColumn);

            HOperatorSet.VectorAngleToRigid(0, 0, 0, hv_RefRow, hv_RefColumn, 0, out hv_HomMat2D);
            ho_TransContours.Dispose();
            HOperatorSet.AffineTransContourXld(ho_ModelContours, out ho_TransContours, hv_HomMat2D);

            ho_Image.Dispose();
            HOperatorSet.ReadImage(out ho_Image, "C:/Users/27703/Desktop/Extract_Image_feature-main/1.bmp");
            ho_Highpass.Dispose();
            HOperatorSet.HighpassImage(ho_Image, out ho_Highpass, 3, 3);
            ho_Image1.Dispose(); ho_Image2.Dispose(); ho_Image3.Dispose();
            HOperatorSet.Decompose3(ho_Highpass, out ho_Image1, out ho_Image2, out ho_Image3
                );
            ho_ConnectedRegions1.Dispose();
            HOperatorSet.Connection(ho_Image3, out ho_ConnectedRegions1);

            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {

                HOperatorSet.FindShapeModel(ho_Image, hv_ModelID, (new HTuple(0)).TupleRad(),
                    (new HTuple(360)).TupleRad(), 0.2, 1, 0.3, "least_squares", (new HTuple(7)).TupleConcat(
                    2), 0.74, out hv_Row, out hv_Column, out hv_Angle, out hv_Score);
            }



            HOperatorSet.HomMat2dIdentity(out hv_HomMat2D);
            {
                HTuple ExpTmpOutVar_0;
                HOperatorSet.HomMat2dRotate(hv_HomMat2D, hv_Angle, 0, 0, out ExpTmpOutVar_0);

                hv_HomMat2D = ExpTmpOutVar_0;
            }
            {
                HTuple ExpTmpOutVar_0;
                HOperatorSet.HomMat2dTranslate(hv_HomMat2D, hv_Row, hv_Column, out ExpTmpOutVar_0);

                hv_HomMat2D = ExpTmpOutVar_0;
            }
            //ho_TransContours.Dispose();
            //HOperatorSet.AffineTransContourXld(ho_ModelContours, out ho_TransContours, hv_HomMat2D);
            //ho_UnionContours.Dispose();
            //HOperatorSet.UnionAdjacentContoursXld(ho_TransContours, out ho_UnionContours,
            //    20, 250, "attr_keep");
            //ho_SelectedXLD.Dispose();
            //HOperatorSet.SelectShapeXld(ho_UnionContours, out ho_SelectedXLD, "contlength",
            //    "and", 500, 891.67);



            //HOperatorSet.AreaCenterXld(ho_SelectedXLD, out hv_Area, out hv_Row1, out hv_Column1,
            //    out hv_PointOrder);


            //HOperatorSet.TupleMax(hv_Column1, out hv_Column1_Max);

            //HOperatorSet.TupleMax(hv_Row1, out hv_Row1_Max);

            //HOperatorSet.TupleMin(hv_Column1, out hv_Column1_Min);

            //HOperatorSet.TupleMin(hv_Row1, out hv_Row1_Min);

            //using (HDevDisposeHelper dh = new HDevDisposeHelper())
            //{
            //    ho_Rectangle.Dispose();
            //    HOperatorSet.GenRectangle1(out ho_Rectangle, hv_Row1_Min - 20, hv_Column1_Min - 200,
            //        hv_Row1_Max + 20, hv_Column1_Max + 1000);
            //}
            ////gen_rectangle1 (Rectangle1, Row1_Min-50, Column1_Min, Row1_Max+50, Column1_Max)
            //ho_ImageReduced.Dispose();
            //HOperatorSet.ReduceDomain(ho_Image3, ho_Rectangle, out ho_ImageReduced);

            //ho_Regions.Dispose();
            //HOperatorSet.Threshold(ho_ImageReduced, out ho_Regions, 36, 107);
            //ho_ConnectedRegions.Dispose();
            //HOperatorSet.Connection(ho_Regions, out ho_ConnectedRegions);
            //ho_SelectedRegions.Dispose();
            //HOperatorSet.SelectShape(ho_ConnectedRegions, out ho_SelectedRegions, "area",
            //    "and", 109413, 200000);
            //ho_RegionFillUp.Dispose();
            //HOperatorSet.FillUp(ho_SelectedRegions, out ho_RegionFillUp);
            //ho_RegionTrans.Dispose();
            //HOperatorSet.ShapeTrans(ho_RegionFillUp, out ho_RegionTrans, "rectangle1");

            //HOperatorSet.SmallestRectangle1(ho_RegionTrans, out hv_Row11, out hv_Column11,
            //    out hv_Row2, out hv_Column2);
            //ho_ImageReduced1.Dispose();
            //HOperatorSet.ReduceDomain(ho_ImageReduced, ho_RegionTrans, out ho_ImageReduced1
            //    );
            ////detect_image_features(ImageReduced1, Edges, Corners, ['edge','corner'], 40, 20, 0.8, 3, 0.04, 0.5)
            //ho_OutEdges.Dispose();
            //HOperatorSet.EdgesSubPix(ho_ImageReduced1, out ho_OutEdges, "canny", 1.5, 50,
            //    90);
            //ho_UnionContours1.Dispose();
            //HOperatorSet.UnionAdjacentContoursXld(ho_OutEdges, out ho_UnionContours1, 10,
            //    1, "attr_keep");
            //if (HDevWindowStack.IsOpen())
            //{
            //    HOperatorSet.ClearWindow(HDevWindowStack.GetActive());
            //}
            //if (HDevWindowStack.IsOpen())
            //{
            //    HOperatorSet.DispObj(ho_ImageReduced1, HDevWindowStack.GetActive());
            //}
            //if (HDevWindowStack.IsOpen())
            //{
            //    HOperatorSet.SetColor(HDevWindowStack.GetActive(), "green");
            //}

            Point = new System.Drawing.Point(hv_Column.TupleInt(), hv_Row.TupleInt());

            ho_TemplateRoi.Dispose();
            ho_ModelContours.Dispose();
            ho_TransContours.Dispose();
            ho_Image.Dispose();
            ho_Highpass.Dispose();
            ho_Image1.Dispose();
            ho_Image2.Dispose();
            ho_Image3.Dispose();
            ho_ConnectedRegions1.Dispose();
            ho_UnionContours.Dispose();
            ho_SelectedXLD.Dispose();
            ho_Rectangle.Dispose();
            ho_ImageReduced.Dispose();
            ho_Regions.Dispose();
            ho_ConnectedRegions.Dispose();
            ho_SelectedRegions.Dispose();
            ho_RegionFillUp.Dispose();
            ho_RegionTrans.Dispose();
            ho_ImageReduced1.Dispose();
            ho_OutEdges.Dispose();
            ho_UnionContours1.Dispose();



        }

        private double GetMsg(double distance) //获取当前相机的倍率计算像素值
        {
            var (WPanPos, WTiltPos, WZoomPos) = dlg.GetCameraBasisVectors();

            double HFOV_1X = 51.2; // 1倍变焦时的水平视场角（度）
            double HFOV = HFOV_1X / WZoomPos; // 经验公式计算当前变倍的视场角

            // 计算水平视野宽度 W = 2 * D * tan(HFOV / 2)
            double width = 2 * distance * Math.Tan(HFOV / 2 * Math.PI / 180);
            return width;
        }


        private void SetMsg()
        {
            int hdir = 0;
            int vdir = 0;

            Get_Currentimg(out Bitmap img);
            Bitmap2HObjectBpp24(img, out HObject image);
            action(image, out System.Drawing.Point p);
            // 2. 原始图像尺寸（2560x1440）
            double originalWidth = 2560;
            double originalHeight = 1440;


            serial.SendCommand("Measure");
            string sensorData = serial.ThisNumber;
            // 获取数据并将其存储到二维数组中
            string[] sensorValues = GetDataFromSensor(sensorData);
            double L = Convert.ToDouble(sensorValues[3]) * 100;
            double width = GetMsg(L);

            double msg = width / originalWidth;

            System.Drawing.Point maxpoint = ie.maxLoc(img);

            while (true)
            {



                double y = Math.Abs(Math.Atan2(msg * (p.Y - (maxpoint.Y)), L));
                double x = Math.Abs(Math.Atan2(msg * (p.X - (maxpoint.X)), L));

                if (p.X - maxpoint.X < 0)
                {
                    hdir = 1;
                }
                else
                {
                    hdir = 0;
                }

                if (p.X - maxpoint.Y > 0)
                {
                    vdir = 1;
                }
                else
                {
                    vdir = 0;
                }




                serial.SendCommand("Move", dHA: x, dVA: y, hDir: hdir, vDir: vdir);

                Thread.Sleep(1000);

                Get_Currentimg(out Bitmap img1);
                maxpoint = ie.maxLoc(img1);

                double num = Math.Abs(p.X - (maxpoint.X));

                if (num < 5)
                {
                    string str = "像素偏差 " + num.ToString() + "移动到目标点"; //登录失败，输出错误号 Failed to login and output the error code

                    break;
                }
                else
                {
                    string str = "像素偏差" + num.ToString() + "继续移动"; //登录失败，输出错误号 Failed to login and output the error code

                }

            }
        }


        public void Bitmap2HObjectBpp24(Bitmap bmp, out HObject image)
        {
            try
            {
                Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

                BitmapData srcBmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                HOperatorSet.GenImageInterleaved(out image, srcBmpData.Scan0, "bgr", bmp.Width, bmp.Height, 0, "byte", 0, 0, 0, 0, -1, 0);
                bmp.UnlockBits(srcBmpData);

            }
            catch (Exception ex)
            {
                image = null;
            }
        }
    }
}
