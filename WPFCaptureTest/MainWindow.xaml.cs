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
using NyARToolkitCSUtils.Capture;

namespace WPFCaptureTest
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, CaptureListener
    {
        private CaptureDevice m_cap;
        public MainWindow()
        {
            InitializeComponent();


            CaptureDeviceList cl = new CaptureDeviceList();
            m_cap = cl[0];
            m_cap.SetCaptureListener(this);
            m_cap.PrepareCapture(800, 600, 30);

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Closing -= MainWindow_Closing;

            this.m_cap.StopCapture();
            this.m_cap.Dispose();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= MainWindow_Loaded;

            this.m_cap.StartCapture();
        }

        void CaptureListener.OnBuffer(CaptureDevice i_sender, double i_sample_time, IntPtr i_buffer, int i_buffer_len)
        {
            int w = i_sender.video_width;
            int h = i_sender.video_height;
            int s = w * (i_sender.video_bit_count / 8);
            try
            {
                Dispatcher.BeginInvoke(new Action(delegate ()
                {
                    TransformedBitmap b = new TransformedBitmap();
                    b.BeginInit();
                    b.Source = BitmapSource.Create(w, h, 96.0, 96.0, PixelFormats.Bgr32, BitmapPalettes.WebPalette, i_buffer, i_buffer_len, s);
                    b.SetValue(TransformedBitmap.TransformProperty, new ScaleTransform(-1, -1));
                    b.EndInit();
                    image1.SetValue(Image.SourceProperty, b);
                }), null);
            }
            catch {}
        }
    }
}
