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
using System.Windows.Threading;
using jp.nyatla.nyartoolkit.cs.core;
using jp.nyatla.nyartoolkit.cs.markersystem;
using NyARToolkitCSUtils.Capture;
using NyARToolkitCSUtils.Direct3d;

namespace Test_WPFSimple5
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, CaptureListener
    {
        private string AR_CODE_FILE = AppDomain.CurrentDomain.BaseDirectory + "Data/hiro.patt";
        private int mid;
        private NyARD3dMarkerSystem _ms;
        private NyARDirectShowCamera _ss;

        private CaptureDevice MyCaptureDevice;

        public MainWindow()
        {
            InitializeComponent();

            CaptureDeviceList oCaptureDeviceList = new CaptureDeviceList();
            MyCaptureDevice = oCaptureDeviceList[0];
            MyCaptureDevice.SetCaptureListener(this);
            MyCaptureDevice.PrepareCapture(800, 600, 30.0f);

            INyARMarkerSystemConfig cf = new NyARMarkerSystemConfig(800, 600);
            this._ms = new NyARD3dMarkerSystem(cf);
            this.mid = this._ms.addARMarker(AR_CODE_FILE, 16, 25, 80);

            this._ss = new NyARDirectShowCamera(MyCaptureDevice);

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            MyCaptureDevice.StartCapture();
        }

        void CaptureListener.OnBuffer(CaptureDevice i_sender, double i_sample_time, IntPtr i_buffer, int i_buffer_len)
        {
            int w = i_sender.video_width;
            int h = i_sender.video_height;
            int s = w*(i_sender.video_bit_count/8);

            Dispatcher.Invoke(new Action(delegate()
            {
                TransformedBitmap b = new TransformedBitmap();
                b.BeginInit();
                b.Source = BitmapSource.Create(w, h, 96.0, 96.0, PixelFormats.Bgr32, BitmapPalettes.WebPalette, i_buffer,
                    i_buffer_len, s);
                b.SetValue(TransformedBitmap.TransformProperty, new ScaleTransform(-1, -1));
                b.EndInit();
                image1.SetValue(Image.SourceProperty, b);

                this._ms.update(this._ss);
                this.CvMainZm.Children.Clear();
                if (this._ms.isExistMarker(this.mid))
                {
                    double d = this._ms.getConfidence(this.mid);
                    NyARIntPoint2d[] oPoints = this._ms.getVertex2D(this.mid);

                    Polygon oPolygon = new Polygon()
                    {
                        SnapsToDevicePixels = true,
                        Fill = new SolidColorBrush(Colors.Violet),
                        Opacity = 0.8,
                        Stroke = new SolidColorBrush(Colors.Red)
                    };
                    oPolygon.Points = new PointCollection(new Point[]
                    {
                        new Point(oPoints[0].x, oPoints[0].y),
                        new Point(oPoints[1].x, oPoints[1].y),
                        new Point(oPoints[2].x, oPoints[2].y),
                        new Point(oPoints[3].x, oPoints[3].y)
                    });
                    this.CvMainZm.Children.Add(oPolygon);
                }
                else
                {
                    Console.WriteLine("Checked Nothing");
                }
            }));
        }
    }
}
