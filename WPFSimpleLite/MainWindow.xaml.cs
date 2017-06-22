using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using NyARToolkitCSUtils.Capture;

namespace WPFSimpleLite
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, CaptureListener
    {
        private CaptureDevice m_cap;
        private NyARDetectMarker m_ar;
        private NyARRgbRaster m_raster;

        //private string AR_CODE_FILE = AppDomain.CurrentDomain.BaseDirectory + "Data/hiro.patt";
        private string AR_CODE_FILE = AppDomain.CurrentDomain.BaseDirectory + "Data/vtt.patt";
        private string AR_CAMERA_FILE = AppDomain.CurrentDomain.BaseDirectory + "Data/camera_para.dat";

        public MainWindow()
        {
            InitializeComponent();

            CaptureDeviceList cl = new CaptureDeviceList();
            m_cap = cl[0];
            m_cap.SetCaptureListener(this);
            m_cap.PrepareCapture(800, 600, 30);

            NyARParam ap = NyARParam.loadFromARParamFile(File.OpenRead(AR_CAMERA_FILE), 800, 600);
            this.m_raster = new NyARRgbRaster_BYTE1D_B8G8R8_24(m_cap.video_width, m_cap.video_height, false);

            NyARCode code = NyARCode.loadFromARPattFile(File.OpenRead(AR_CODE_FILE), 16, 16);
            this.m_ar = new NyARDetectMarker(ap, new NyARCode[] { code }, new double[] { 80.0 }, 1);
            this.m_ar.setContinueMode(false);

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }


        private int m_threshold = 100;
        private const int cameraResX = 800;
        private const int cameraResY = 600;
        static int v_top;
        static int v_left;
        static int v_top_old = -100;
        static int v_left_old = -100;
        static bool v_change = false;

        void CaptureListener.OnBuffer(CaptureDevice i_sender, double i_sample_time, IntPtr i_buffer, int i_buffer_len)
        {
            int w = i_sender.video_width;
            int h = i_sender.video_height;
            int s = w*(i_sender.video_bit_count/8);
            AForge.Imaging.Filters.FiltersSequence seq = new AForge.Imaging.Filters.FiltersSequence();
            seq.Add(new AForge.Imaging.Filters.Grayscale(0.2125, 0.7154, 0.0721));
            seq.Add(new AForge.Imaging.Filters.Threshold(127));
            seq.Add(new AForge.Imaging.Filters.GrayscaleToRGB());

            AForge.Imaging.UnmanagedImage srcImg = new AForge.Imaging.UnmanagedImage(i_buffer, w, h, s,
                System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            AForge.Imaging.UnmanagedImage outputImg = seq.Apply(srcImg);

            byte[] destArr = new byte[outputImg.Stride*outputImg.Height];
            System.Runtime.InteropServices.Marshal.Copy(outputImg.ImageData, destArr, 0,
                outputImg.Stride*outputImg.Height);
            this.m_raster.wrapBuffer(destArr);

            try
            {
                //this.m_ar.

                int detectedMkrs = this.m_ar.detectMarkerLite(this.m_raster, m_threshold);
                NyARDoublePoint2d[] points = null;
                List<NyARDoublePoint2d[]> ltPoints = new List<NyARDoublePoint2d[]>();
                if (detectedMkrs > 0)
                {
                    points = m_ar.getCorners(0);

                    ltPoints.Add(points);
                    for (int i = 1; i < detectedMkrs; i++)
                    {
                        NyARDoublePoint2d[] oMarkerPoints = m_ar.getCorners(i);
                        ltPoints.Add(oMarkerPoints);
                    }
                }

                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    try
                    {
                        TransformedBitmap b = new TransformedBitmap();
                        b.BeginInit();
                        b.Source = BitmapSource.Create(w, h, 96.0, 96.0, PixelFormats.Bgr32, BitmapPalettes.WebPalette,
                            i_buffer, i_buffer_len, s);
                        b.SetValue(TransformedBitmap.TransformProperty, new ScaleTransform(-1, -1));
                        b.EndInit();
                        image1.SetValue(Image.SourceProperty, b);

                        if (points != null && points.Length == 4)
                        {
                            recognizedTag.Points = new PointCollection(new Point[]
                            {
                                new Point(cameraResX - points[0].x, cameraResY - points[0].y),
                                new Point(cameraResX - points[1].x, cameraResY - points[1].y),
                                new Point(cameraResX - points[2].x, cameraResY - points[2].y),
                                new Point(cameraResX - points[3].x, cameraResY - points[3].y)
                            });
                            recognizedTag.Visibility = System.Windows.Visibility.Visible;
                        }
                        else
                            recognizedTag.Visibility = System.Windows.Visibility.Hidden;

                        this.CvMainZm.Children.Clear();
                        for (int i = 1; i < ltPoints.Count; i++)
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
                                new Point(cameraResX - oMarkerPoints[0].x, cameraResY - oMarkerPoints[0].y),
                                new Point(cameraResX - oMarkerPoints[1].x, cameraResY - oMarkerPoints[1].y),
                                new Point(cameraResX - oMarkerPoints[2].x, cameraResY - oMarkerPoints[2].y),
                                new Point(cameraResX - oMarkerPoints[3].x, cameraResY - oMarkerPoints[3].y)
                            });
                            this.CvMainZm.Children.Add(oPolygon);
                        }
                    }
                    catch
                    {
                    }
                }), null);
            }
            catch
            {
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.m_cap.StopCapture();
            this.m_cap.Dispose();
            App.Current.Shutdown();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.m_cap.StartCapture();
        }
    }
}
