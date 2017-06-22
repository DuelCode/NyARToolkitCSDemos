using System;
using System.Collections.Generic;
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

namespace WPFSimpleLite_Bydll2._5._2
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, CaptureListener
    {
        private string AR_CODE_FILE = AppDomain.CurrentDomain.BaseDirectory + "Data/hiro.patt";
        private string AR_CAMERA_FILE = AppDomain.CurrentDomain.BaseDirectory + "Data/camera_para.dat";

        private CaptureDevice m_cap;
        private NyARDetectMarker m_ar;

        private const double dpiX = 96.0;
        private const double dpiY = 96.0;

        private const int cameraResX = 800;
        private const int cameraResY = 600;

        private int m_threshold = 100;

        private NyARRgbRaster m_raster;

        static int v_top;
        static int v_left;
        static int v_top_old = -100;
        static int v_left_old = -100;
        static bool v_change = false;



        public MainWindow()
        {
            InitializeComponent();

            CaptureDeviceList cl = new CaptureDeviceList();
            m_cap = cl[0];
            m_cap.SetCaptureListener(this);
            m_cap.PrepareCapture(cameraResX, cameraResY, 30); // 800x600 resolution, 30 fps

            NyARParam ap = new NyARParam();
            ap.loadARParamFromFile(AR_CAMERA_FILE);
            ap.changeScreenSize(cameraResX, cameraResY);

            this.m_raster = new NyARRgbRaster(m_cap.video_width, m_cap.video_height, NyARBufferType.BYTE1D_R8G8B8_24,
                false);

            NyARCode code = new NyARCode(16, 16); // 16 pixels to detect within black box
            code.loadARPattFromFile(AR_CODE_FILE);
            this.m_ar = new NyARDetectMarker(ap, new NyARCode[] { code }, new double[] { 80.0 }, 1,
                NyARBufferType.BYTE1D_B8G8R8_24);
            this.m_ar.setContinueMode(false);

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.m_cap.StartCapture();
        }

        void CaptureListener.OnBuffer(CaptureDevice i_sender, double i_sample_time, IntPtr i_buffer, int i_buffer_len)
        {
            // calculate size of the frame bitmap
            int w = i_sender.video_width;
            int h = i_sender.video_height;
            int s = w * (i_sender.video_bit_count / 8); // stride            


            AForge.Imaging.Filters.FiltersSequence seq = new AForge.Imaging.Filters.FiltersSequence();
            seq.Add(new AForge.Imaging.Filters.Grayscale(0.2125, 0.7154, 0.0721));
            seq.Add(new AForge.Imaging.Filters.Threshold(127));
            seq.Add(new AForge.Imaging.Filters.GrayscaleToRGB());
            AForge.Imaging.UnmanagedImage srcImg = new AForge.Imaging.UnmanagedImage(i_buffer, w, h, s, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            AForge.Imaging.UnmanagedImage outputImg = seq.Apply(srcImg);
            byte[] destArr = new byte[outputImg.Stride * outputImg.Height];
            System.Runtime.InteropServices.Marshal.Copy(outputImg.ImageData, destArr, 0, outputImg.Stride * outputImg.Height);
            this.m_raster.wrapBuffer(destArr);

            try
            {
                int detectedMkrs = this.m_ar.detectMarkerLite(this.m_raster, m_threshold);
                NyARSquare square = null;
                if (detectedMkrs > 0)
                {
                    NyARTransMatResult transMat = new NyARTransMatResult();
                    NyARDoublePoint2d[] points = m_ar.getCorners(0); // RichF added this method
                    square = new NyARSquare();
                    square.sqvertex = points;
                }


                Dispatcher.BeginInvoke(new Action(delegate ()
                {
                    TransformedBitmap b = new TransformedBitmap();
                    b.BeginInit();
                    b.Source = BitmapSource.Create(w, h, dpiX, dpiY, PixelFormats.Bgr32, BitmapPalettes.WebPalette, i_buffer, i_buffer_len, s);
                    b.SetValue(TransformedBitmap.TransformProperty, new ScaleTransform(-1, -1));
                    b.EndInit();
                    image1.SetValue(Image.SourceProperty, b);

                    if (square != null)
                    {
                        recognizedTag.Points = new PointCollection(new Point[] {
                                new Point(cameraResX - square.sqvertex[0].x, cameraResY - square.sqvertex[0].y),
                                new Point(cameraResX - square.sqvertex[1].x, cameraResY - square.sqvertex[1].y),
                                new Point(cameraResX - square.sqvertex[2].x, cameraResY - square.sqvertex[2].y),
                                new Point(cameraResX - square.sqvertex[3].x, cameraResY - square.sqvertex[3].y)});

                        recognizedTag.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        recognizedTag.Visibility = System.Windows.Visibility.Hidden;
                    }
                }), null);
            }
            catch
            {
            }
        }
    }
}
