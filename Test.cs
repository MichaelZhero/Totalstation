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

namespace PaddleOCR.TotalStation
{
    public partial class Test : Form
    {
        public Test()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {



            ModelLoader modelLoader = new ModelLoader();
            ImgEdit imgEdit = new ImgEdit();

            Point acenterPoint = new Point();
            Point LaserPoint = new Point();

            int iWidth = 0, iHeight = 0;
            uint iActualSize = 0;
            uint nBufSize = (uint)(iWidth * iHeight) * 8;
           

            string imagePath = @"C:\Users\lenovo\Desktop\PaddleOCR-1\PaddleOCR\PaddleOCR\TotalStation\test\23.bmp";
            byte[] pBitmap = LoadBitmapToByteArray(imagePath);
            Bitmap bmp = ConvertByteArrayToBitmap(pBitmap);
            var (resultBitmap, centerPoint) = modelLoader.ProcessAndDrawOnBitmap(bmp);
            if (resultBitmap != null & centerPoint != null)
            {
                MessageBox.Show(centerPoint.Value.X.ToString() + ":" + centerPoint.Value.Y.ToString());
                imgEdit.ShowBitmapWithOpenCV(resultBitmap);
            }

            LaserPoint = imgEdit.maxLoc(imagePath);
            // 计算距离
            double distance = Math.Sqrt(Math.Pow(centerPoint.Value.X - LaserPoint.X, 2) + Math.Pow(centerPoint.Value.Y - LaserPoint.Y, 2));
            MessageBox.Show($"LaserPoint 与 centerPoint 的距离: {distance}");

            // 计算与图像中心的偏差值
            int imageCenterX = bmp.Width / 2;
            int imageCenterY = bmp.Height / 2;
            int offsetX = (int)(centerPoint.Value.X - imageCenterX);
            int offsetY = (int)(centerPoint.Value.Y - imageCenterY);
            MessageBox.Show($"与图像中心的偏差值: X = {offsetX}, Y = {offsetY}");

            // 计算角度
            double angleX = Math.Atan2(offsetY, imageCenterX) * (180 / Math.PI);
            double angleY = Math.Atan2(offsetX, imageCenterY) * (180 / Math.PI);
            MessageBox.Show($"旋转角度: X轴 = {angleX}度, Y轴 = {angleY}度");
            // 计算角度

            



        }

        static byte[] LoadBitmapToByteArray(string path)
        {
            try
            {
                // 加载位图
                using (Bitmap bitmap = new Bitmap(path))
                {
                    // 使用 MemoryStream 将 Bitmap 转换为字节数组
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmap.Save(ms, ImageFormat.Bmp);
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生错误: {ex.Message}");
                return null;
            }
        }

        public static Bitmap ConvertByteArrayToBitmap(byte[] imageBytes)
        {
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                return new Bitmap(ms);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "C:\\";
                openFileDialog.Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.gif";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string imagePath = openFileDialog.FileName;

                    ModelLoader modelLoader = new ModelLoader();
                    ImgEdit imgEdit = new ImgEdit();

                    Point acenterPoint = new Point();

                    int iWidth = 0, iHeight = 0;
                    uint iActualSize = 0;
                    uint nBufSize = (uint)(iWidth * iHeight) * 8;
                    for (int i = 0; i < 23; i++)
                    {

                    }

                    byte[] pBitmap = LoadBitmapToByteArray(imagePath);
                    Bitmap bmp = ConvertByteArrayToBitmap(pBitmap);
                    var (resultBitmap, centerPoint) = modelLoader.ProcessAndDrawOnBitmap(bmp);
                    if (resultBitmap != null & centerPoint != null)
                    {
                        MessageBox.Show(centerPoint.Value.X.ToString() + ":" + centerPoint.Value.Y.ToString());
                        imgEdit.ShowBitmapWithOpenCV(resultBitmap);
                    }

                    acenterPoint = imgEdit.maxLoc(imagePath);
                }
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    string folderPath = folderBrowserDialog.SelectedPath;
                    string[] imageFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(s => s.EndsWith(".bmp") || s.EndsWith(".jpg") || s.EndsWith(".jpeg") || s.EndsWith(".png") || s.EndsWith(".gif"))
                        .ToArray();

                    ModelLoader modelLoader = new ModelLoader();
                    ImgEdit imgEdit = new ImgEdit();

                    foreach (string imagePath in imageFiles)
                    {
                        byte[] pBitmap = LoadBitmapToByteArray(imagePath);
                        Bitmap bmp = ConvertByteArrayToBitmap(pBitmap);
                        var (resultBitmap, centerPoint) = modelLoader.ProcessAndDrawOnBitmap(bmp);
                        if (resultBitmap != null & centerPoint != null)
                        {
                            MessageBox.Show(centerPoint.Value.X.ToString() + ":" + centerPoint.Value.Y.ToString());
                            imgEdit.ShowBitmapWithOpenCV(resultBitmap);
                        }

                        Point acenterPoint = imgEdit.maxLoc(imagePath);
                    }
                }
            }
        }
    } 
}

