using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using jp.nyatla.nyartoolkit.cs.core;
using jp.nyatla.nyartoolkit.cs.detector;
using WPFMediaKit.DirectShow.Controls;

namespace Test_WPFSimple2
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private NyARDetectMarker m_ar;
        private NyARRgbRaster m_raster;

        private string AR_CODE_FILE = AppDomain.CurrentDomain.BaseDirectory + "Data/hiro.patt";
        private string AR_CAMERA_FILE = AppDomain.CurrentDomain.BaseDirectory + "Data/camera_para.dat";

        public MainWindow()
        {
            InitializeComponent();

            this.GdMainZm.Height = 600;
            this.CameraZm.Height = 600;
            this.CameraZm.DesiredPixelHeight = 600;

            this.GdMainZm.Width = 800;
            this.CameraZm.Width = 800;
            this.CameraZm.DesiredPixelWidth = 800;

            this.CameraZm.EnableSampleGrabbing = true;
            this.CameraZm.NewVideoSample += CameraZm_NewVideoSample;

            NyARParam ap = NyARParam.loadFromARParamFile(File.OpenRead(AR_CAMERA_FILE), 800, 600);
            this.m_raster = new NyARRgbRaster_BYTE1D_B8G8R8_24(800, 600, false);

            NyARCode code = NyARCode.loadFromARPattFile(File.OpenRead(AR_CODE_FILE), 16, 16);
            this.m_ar = new NyARDetectMarker(ap, new NyARCode[] { code }, new double[] { 80.0 }, 1);
            this.m_ar.setContinueMode(false);

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.CameraZm.VideoCaptureDevice = MultimediaUtil.VideoInputDevices[0];
                this.CameraZm.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                string sError = "未查找到摄像头，程序无法正常加载，请检查摄像头是否正常，若仍无法正常运行，请与本软件供应商联系！";
                MessageBox.Show(sError, "提示信息", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int m_threshold = 127;
        private const int cameraResY = 600;

        public BitmapSource ConvertBitmapToBiamapSource(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height, 96, 96, PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }

        private void CameraZm_NewVideoSample(object sender, WPFMediaKit.DirectShow.MediaPlayers.VideoSampleArgs e)
        {
            Dispatcher.Invoke(new Action(delegate ()
            {
                System.Drawing.Bitmap oNewFrame = e.VideoFrame;
                
                AForge.Imaging.Filters.FiltersSequence seq = new AForge.Imaging.Filters.FiltersSequence();
                seq.Add(new AForge.Imaging.Filters.Grayscale(0.2125, 0.7154, 0.0721));
                seq.Add(new AForge.Imaging.Filters.Threshold(127));
                seq.Add(new AForge.Imaging.Filters.GrayscaleToRGB());
                System.Drawing.Bitmap oFilterBitmap = seq.Apply(oNewFrame);

                var bitmapData = oFilterBitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, oFilterBitmap.Width, oFilterBitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, oFilterBitmap.PixelFormat);

                byte[] destArr = new byte[bitmapData.Stride * bitmapData.Height];

                System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, destArr, 0,
                    bitmapData.Stride * bitmapData.Height);

                var bitmapSource = BitmapSource.Create(
               bitmapData.Width, bitmapData.Height, 96, 96, PixelFormats.Bgr24, null,
               bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

                oFilterBitmap.UnlockBits(bitmapData);

                this.m_raster.wrapBuffer(destArr);

                try
                {
                    int detectedMkrs = this.m_ar.detectMarkerLite(this.m_raster, m_threshold);
                    NyARDoublePoint2d[] points = null;
                    List<NyARDoublePoint2d[]> ltPoints = new List<NyARDoublePoint2d[]>();
                    if (detectedMkrs > 0)
                    {
                        points = m_ar.getCorners(0);

                        ltPoints.Add(points);
                        for (int i = 0; i < detectedMkrs; i++)
                        {
                            NyARDoublePoint2d[] oMarkerPoints = m_ar.getCorners(i);
                            ltPoints.Add(oMarkerPoints);
                        }
                    }
                    Dispatcher.BeginInvoke(new Action(delegate ()
                    {
                        try
                        {
                            this.ImgMainZm.Source = bitmapSource;

                            this.CvMainZm.Children.Clear();
                            for (int i = 0; i < ltPoints.Count; i++)
                            {
                                NyARDoublePoint2d[] oMarkerPoints = ltPoints[i];
                                Polygon oPolygon = new Polygon()
                                {
                                    SnapsToDevicePixels = true,
                                    Fill = new SolidColorBrush(Colors.Violet),
                                    Opacity = 0.8,
                                    Stroke = new SolidColorBrush(Colors.Red)
                                };

                                oPolygon.Points = new PointCollection(new Point[]
                                {
                                      new Point(oMarkerPoints[0].x, cameraResY - oMarkerPoints[0].y),
                                      new Point(oMarkerPoints[1].x, cameraResY - oMarkerPoints[1].y),
                                      new Point(oMarkerPoints[2].x, cameraResY - oMarkerPoints[2].y),
                                      new Point(oMarkerPoints[3].x, cameraResY - oMarkerPoints[3].y)
                                });
                                this.CvMainZm.Children.Add(oPolygon);
                            }
                        }
                        catch
                        { }
                    }), null);
                }
                catch
                { }
            }));
        }
    }
}
