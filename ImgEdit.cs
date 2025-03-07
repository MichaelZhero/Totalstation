using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp.Extensions;
using OpenCvSharp;
using Point = OpenCvSharp.Point;

namespace PaddleOCR.TotalStation
{
    internal class ImgEdit
    {
        //显示图像
        public void ShowBitmapWithOpenCV(Bitmap bitmap)
        {
            // 1. 转换 Bitmap 到 Mat
            Mat mat = BitmapConverter.ToMat(bitmap);

            // 2. 使用 OpenCV 显示 Mat
            Cv2.ImShow("OpenCV 显示窗口", mat);
            Cv2.WaitKey(0); // 等待按键
            Cv2.DestroyAllWindows(); // 关闭窗口
        }


        public Point maxLoc(Bitmap bitmapImage)
        {
            Point centerPoint = new Point();

            // 读取灰度图
            Mat matImage = OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmapImage);
            Mat grayImage = new Mat();
            Cv2.CvtColor(matImage, grayImage, ColorConversionCodes.BGR2GRAY);
            if (grayImage.Empty())
            {
                Console.WriteLine("图像加载失败！");
                return centerPoint;
            }

            // 二值化，提取亮度 > 250 的区域
            Mat mask = new Mat();
            Cv2.Threshold(grayImage, mask, 250, 255, ThresholdTypes.Binary);

            // 查找所有连通区域
            Cv2.FindContours(mask, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            if (contours.Length == 0)
            {
                Console.WriteLine("未找到高亮区域");
                return centerPoint;
            }

            // 找到像素最多的区域（即轮廓面积最大）
            int maxIndex = 0;
            double maxArea = 0;
            for (int i = 0; i < contours.Length; i++)
            {
                double area = Cv2.ContourArea(contours[i]);
                if (area > maxArea)
                {
                    maxArea = area;
                    maxIndex = i;
                }
            }

            // 计算最大区域的质心（中心坐标）
            Moments moment = Cv2.Moments(contours[maxIndex]);
            int centerX = (int)(moment.M10 / moment.M00);
            int centerY = (int)(moment.M01 / moment.M00);
            centerPoint = new(centerX, centerY);

            // 复制原图，用于标记
            Mat outputImage = grayImage.CvtColor(ColorConversionCodes.GRAY2BGR);

            // 画出最大区域轮廓
            Cv2.DrawContours(outputImage, contours, maxIndex, new Scalar(0, 0, 255), 2);

            // 计算并画出包围矩形
            Rect boundingBox = Cv2.BoundingRect(contours[maxIndex]);
            Cv2.Rectangle(outputImage, boundingBox, new Scalar(0, 255, 0), 2);

            // 画出中心点
            Cv2.Circle(outputImage, centerPoint, 5, new Scalar(255, 0, 0), -1);

            // 显示结果
            //Cv2.ImShow("img", outputImage);
            //Cv2.WaitKey(0);
            //Cv2.DestroyAllWindows();


            //MessageBox.Show($"中心坐标: ({centerX}, {centerY})");
            return centerPoint;
        }


        public Point maxLoc(string imagePath)
        {
            Point centerPoint = new Point();

            // 读取灰度图
            Mat grayImage = Cv2.ImRead(imagePath, ImreadModes.Grayscale);
            if (grayImage.Empty())
            {
                Console.WriteLine("图像加载失败！");
                return centerPoint;
            }

            // 二值化，提取亮度 > 250 的区域
            Mat mask = new Mat();
            Cv2.Threshold(grayImage, mask, 250, 255, ThresholdTypes.Binary);

            // 查找所有连通区域
            Cv2.FindContours(mask, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            if (contours.Length == 0)
            {
                Console.WriteLine("未找到高亮区域");
                return centerPoint;
            }

            // 找到像素最多的区域（即轮廓面积最大）
            int maxIndex = 0;
            double maxArea = 0;
            for (int i = 0; i < contours.Length; i++)
            {
                double area = Cv2.ContourArea(contours[i]);
                if (area > maxArea)
                {
                    maxArea = area;
                    maxIndex = i;
                }
            }

            // 计算最大区域的质心（中心坐标）
            Moments moment = Cv2.Moments(contours[maxIndex]);
            int centerX = (int)(moment.M10 / moment.M00);
            int centerY = (int)(moment.M01 / moment.M00);
            centerPoint = new (centerX, centerY);

            // 复制原图，用于标记
            Mat outputImage = grayImage.CvtColor(ColorConversionCodes.GRAY2BGR);

            // 画出最大区域轮廓
            Cv2.DrawContours(outputImage, contours, maxIndex, new Scalar(0, 0, 255), 2);

            // 计算并画出包围矩形
            Rect boundingBox = Cv2.BoundingRect(contours[maxIndex]);
            Cv2.Rectangle(outputImage, boundingBox, new Scalar(0, 255, 0), 2);

            // 画出中心点
            Cv2.Circle(outputImage, centerPoint, 5, new Scalar(255, 0, 0), -1);

            // 显示结果
            Cv2.ImShow("img", outputImage);
            Cv2.WaitKey(0);
            Cv2.DestroyAllWindows();

           
            MessageBox.Show($"中心坐标: ({centerX}, {centerY})");
            return centerPoint;
        }
    }
}
