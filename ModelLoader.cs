using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using OpenCvSharp.Dnn;
using OpenCvSharp;
using static System.Windows.Forms.Design.AxImporter;
using Point = OpenCvSharp.Point;

namespace PaddleOCR.TotalStation
{
    internal class ModelLoader
    {
        private string modelPath = @"C:\Users\lenovo\Desktop\PaddleOCR-1\PaddleOCR\PaddleOCR\TotalStation\qzy_2_8.onnx";
        private float ConfidenceThreshold = 0.5f; // 置信度阈值
        private float IoUThreshold = 0.45f; // IoU 阈值
        private InferenceSession session;

        

        public (Bitmap resultBitmap, PointF? centerPoint) ProcessAndDrawOnBitmap(Bitmap bitmapImage)
        {
            var options = new SessionOptions();
            session = new InferenceSession(modelPath, options);

            // 将Bitmap转换为Mat格式
            Mat image = OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmapImage);

            // 获取原始图片的尺寸
            int origHeight = image.Rows;
            int origWidth = image.Cols;

            // 模型输入尺寸
            int inputWidth = 640;
            int inputHeight = 640;

            // 等比例缩放到 640 × 360
            Mat resizedImage = new Mat();
            Cv2.Resize(image, resizedImage, new OpenCvSharp.Size(inputWidth, 360));

            // 在上方和下方填充黑边，使其变为 640 × 640
            int paddingTop = (inputHeight - 360) / 2;
            int paddingBottom = inputHeight - 360 - paddingTop;
            Cv2.CopyMakeBorder(resizedImage, resizedImage, paddingTop, paddingBottom, 0, 0, BorderTypes.Constant, Scalar.Black);

            // 转换为 RGB 格式
            Mat resizedImageRgb = new Mat();
            Cv2.CvtColor(resizedImage, resizedImageRgb, ColorConversionCodes.BGR2RGB);

            // 归一化
            resizedImageRgb.ConvertTo(resizedImageRgb, MatType.CV_32FC3, 1.0 / 255.0);

            // 创建输入张量，形状为 [1, 3, 640, 640]
            var inputTensor4D = new DenseTensor<float>(new[] { 1, 3, inputHeight, inputWidth });

            // 填充输入张量数据
            for (int h = 0; h < inputHeight; h++)
            {
                for (int w = 0; w < inputWidth; w++)
                {
                    Vec3f pixel = resizedImageRgb.Get<Vec3f>(h, w); // 获取每个像素的RGB值
                    inputTensor4D[0, 0, h, w] = pixel.Item0; // R通道
                    inputTensor4D[0, 1, h, w] = pixel.Item1; // G通道
                    inputTensor4D[0, 2, h, w] = pixel.Item2; // B通道
                }
            }

            // 执行推理
            var inputName = session.InputMetadata.Keys.First();
            var outputName = session.OutputMetadata.Keys.First();
            var inputs = new List<NamedOnnxValue>
    {
        NamedOnnxValue.CreateFromTensor(inputName, inputTensor4D)
    };

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);
            var output = results.First(v => v.Name == outputName).AsTensor<float>();

            // 解析输出
            var detections = output.ToArray();
            int channels = 6;
            int numDetections = 8400;
            float[,] reshapedDetections = new float[channels, numDetections];

            for (int c = 0; c < channels; c++)
            {
                for (int i = 0; i < numDetections; i++)
                {
                    reshapedDetections[c, i] = detections[c * numDetections + i];
                }
            }

            // 解析输出并收集所有有效检测框、常规矩形及置信度
            List<RotatedRect> rotatedBoxes = new List<RotatedRect>();
            List<Rect> boundingBoxes = new List<Rect>();
            List<float> confidences = new List<float>();

            for (int i = 0; i < numDetections; i++)
            {
                float x_center = reshapedDetections[0, i];
                float y_center = reshapedDetections[1, i];
                float width = reshapedDetections[2, i];
                float height = reshapedDetections[3, i];
                float confidence = reshapedDetections[4, i];
                float theta_radians = reshapedDetections[5, i];

                if (confidence > ConfidenceThreshold)
                {
                    // 将检测框从 640 × 640 (含黑边) 还原到 640 × 360 (有效区域)
                    float x_center_no_padding = x_center;  // X轴无需调整
                    float y_center_no_padding = y_center - paddingTop;  // 去除顶部的黑边
                    if (y_center_no_padding < 0 || y_center_no_padding > 360) continue;  // 忽略超出有效区域的检测框

                    // 将检测框从 640 × 360 映射回原始尺寸
                    float x_center_orig = x_center_no_padding * origWidth / inputWidth;
                    float y_center_orig = y_center_no_padding * origHeight / 360;  // 映射到原始高度
                    float width_orig = width * origWidth / inputWidth;
                    float height_orig = height * origHeight / 360;  // 高度基于有效区域缩放

                    // 计算旋转角度
                    float theta_degrees = theta_radians * (180.0f / (float)Math.PI);




                    // 在原始图片上构建旋转框
                    RotatedRect rotatedBox = new RotatedRect(
                        new Point2f(x_center_orig, y_center_orig),
                        new Size2f(width_orig, height_orig), theta_degrees);

                    rotatedBoxes.Add(rotatedBox);

                    confidences.Add(confidence);

                    // 也存一下常规Rect（给NMS用）
                    Rect boundingBox = new Rect(
                        (int)(x_center_orig - width_orig / 2),
                        (int)(y_center_orig - height_orig / 2),
                        (int)width_orig,
                        (int)height_orig
                    );
                    boundingBoxes.Add(boundingBox);
                }
            }


            if (boundingBoxes.Count == 0)
            {
                return (null, null); // 未检测到任何框
            }

            // 使用 NMS 筛选最佳检测框
            float[] confArray = confidences.ToArray();
            Rect[] boxArray = boundingBoxes.ToArray();
            CvDnn.NMSBoxes(boxArray, confArray, ConfidenceThreshold, IoUThreshold, out int[] selectedIndices);

            if (selectedIndices.Length == 0)
            {
                return (null, null);
            }

            // 取 NMS 筛选后的第一个检测框
            int bestIdx = selectedIndices[0];
            RotatedRect bestRotatedBox = rotatedBoxes[bestIdx];

            //复制原图

            int x1 = (int)bestRotatedBox.Center.X - ((int)bestRotatedBox.Size.Width / 2);
            int y1 = (int)bestRotatedBox.Center.Y- ((int)bestRotatedBox.Size.Height / 2);
            int x2 = (int)bestRotatedBox.Center.X + ((int)bestRotatedBox.Size.Width / 2);
            int y2 = (int)bestRotatedBox.Center.Y + ((int)bestRotatedBox.Size.Height / 2);
            Cv2.Rectangle(image, new Point(x1,y1), new Point(x2,y2), new Scalar(0, 0, 255), 2);
            Cv2.ImShow("img", image);
            Cv2.WaitKey(0);
            Cv2.DestroyAllWindows();

            // 更新中心点
            PointF? centerPoint = new PointF(bestRotatedBox.Center.X, bestRotatedBox.Center.Y);


            // 使用与之前相同的透视变换逻辑裁剪出目标区域
            Point2f[] rectPoints = bestRotatedBox.Points();
            Point2f[] dstPoints = new Point2f[4];
            dstPoints[0] = new Point2f(0, bestRotatedBox.Size.Height - 1);
            dstPoints[1] = new Point2f(0, 0);
            dstPoints[2] = new Point2f(bestRotatedBox.Size.Width - 1, 0);
            dstPoints[3] = new Point2f(bestRotatedBox.Size.Width - 1, bestRotatedBox.Size.Height - 1);

            Mat M = Cv2.GetPerspectiveTransform(rectPoints, dstPoints);
            Mat croppedImage = new Mat();
            Cv2.WarpPerspective(image, croppedImage, M,
                new OpenCvSharp.Size((int)bestRotatedBox.Size.Width, (int)bestRotatedBox.Size.Height));

            Bitmap croppedBitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(croppedImage);

            return (croppedBitmap, centerPoint);


        }

    }
}
