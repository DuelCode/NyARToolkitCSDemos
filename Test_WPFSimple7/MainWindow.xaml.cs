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

namespace Test_WPFSimple7
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private NyARDetectMarker ARDetectMarker;
        private NyARRgbRaster ARRgbRaster;

        private string AR_CAMERA_FILE = AppDomain.CurrentDomain.BaseDirectory + "Data/camera_para.dat";
        private string AR_CODE_FILE1 = AppDomain.CurrentDomain.BaseDirectory + "Data/hiro.patt";
        private string AR_CODE_FILE2 = AppDomain.CurrentDomain.BaseDirectory + "Data/kanji.patt";
        private string AR_CODE_FILE3 = AppDomain.CurrentDomain.BaseDirectory + "Data/vtt.patt";
        private string AR_CODE_FILE4 = AppDomain.CurrentDomain.BaseDirectory + "Data/abb.patt";
        private string AR_CODE_FILE5 = AppDomain.CurrentDomain.BaseDirectory + "Data/hello.patt";

        private int MarkerID_Hiro;
        private int MarkerID_KanJi;
        private int MarkerID_VTT;
        private int MarkerID_ABB;
        private int Marker_Hello;

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
            this.ARRgbRaster = new NyARRgbRaster_BYTE1D_B8G8R8_24(800, 600, false);
            
            NyARCode[] arrMarkers = new NyARCode[4];
            arrMarkers[0] = NyARCode.loadFromARPattFile(File.OpenRead(AR_CODE_FILE1), 16, 16);
            arrMarkers[1] = NyARCode.loadFromARPattFile(File.OpenRead(AR_CODE_FILE3), 16, 16);
            arrMarkers[2] = NyARCode.loadFromARPattFile(File.OpenRead(AR_CODE_FILE4), 16, 16);
            arrMarkers[3] = NyARCode.loadFromARPattFile(File.OpenRead(AR_CODE_FILE5), 16, 16);

            this.ARDetectMarker = new NyARDetectMarker(ap, arrMarkers, new double[] { 80.0 }, 1);
            this.ARDetectMarker.setContinueMode(false);

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

        private void CameraZm_NewVideoSample(object sender, WPFMediaKit.DirectShow.MediaPlayers.VideoSampleArgs e)
        {
            Dispatcher.Invoke(new Action(delegate()
            {
                System.Drawing.Bitmap oNewFrame = e.VideoFrame;
                var bitmapData = oNewFrame.LockBits(
                new System.Drawing.Rectangle(0, 0, oNewFrame.Width, oNewFrame.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, oNewFrame.PixelFormat);

                byte[] oFrameBytes = new byte[bitmapData.Stride * bitmapData.Height];
                System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, oFrameBytes, 0,
                    bitmapData.Stride * bitmapData.Height);
                oNewFrame.UnlockBits(bitmapData);

                this.ARRgbRaster.wrapBuffer(oFrameBytes);
                int detectedMkrs = this.ARDetectMarker.detectMarkerLite(this.ARRgbRaster, 100);

                double d = this.ARDetectMarker.getConfidence(3);
                int n = this.ARDetectMarker.getARCodeIndex(3);

                this.TbkInfoZm.Text = detectedMkrs.ToString();
                //NyARDoublePoint2d[] points = null;
                //List<NyARDoublePoint2d[]> ltPoints = new List<NyARDoublePoint2d[]>();
                //if (detectedMkrs > 0)
                //{
                //    //points = ARDetectMarker.getCorners(0);

                //    //ltPoints.Add(points);
                //    for (int i = 0; i < detectedMkrs; i++)
                //    {
                //        NyARDoublePoint2d[] oMarkerPoints = ARDetectMarker.getCorners(i);
                //        ltPoints.Add(oMarkerPoints);
                //    }
                //}
                //Dispatcher.BeginInvoke(new Action(delegate ()
                //{
                //    try
                //    {
                //        this.CvMainZm.Children.Clear();
                //        for (int i = 0; i < ltPoints.Count; i++)
                //        {
                //            NyARDoublePoint2d[] oMarkerPoints = ltPoints[i];
                //            Polygon oPolygon = new Polygon()
                //            {
                //                SnapsToDevicePixels = true,
                //                Fill = new SolidColorBrush(Colors.Violet),
                //                Opacity = 0.8,
                //                Stroke = new SolidColorBrush(Colors.Red)
                //            };

                //            oPolygon.Points = new PointCollection(new Point[]
                //            {
                //                      new Point(oMarkerPoints[0].x, 600 - oMarkerPoints[0].y),
                //                      new Point(oMarkerPoints[1].x, 600 - oMarkerPoints[1].y),
                //                      new Point(oMarkerPoints[2].x, 600 - oMarkerPoints[2].y),
                //                      new Point(oMarkerPoints[3].x, 600 - oMarkerPoints[3].y)
                //            });
                //            this.CvMainZm.Children.Add(oPolygon);
                //        }
                //    }
                //    catch
                //    { }
                //}), null);
            }));
        }

    }
}
