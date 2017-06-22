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
using jp.nyatla.nyartoolkit.cs.markersystem;
using NyARToolkitCSUtils.Capture;
using WPFMediaKit.DirectShow.Controls;

namespace Test_WPFSimple3
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private int MarkerID;

        private NyARMarkerSystem MyMarkerSystem;
        private NyARSingleDetectMarker MySingleDetectMarker;
        private DsRgbRaster MyArRaster;
      
        //private string AR_CODE_FILE1 = AppDomain.CurrentDomain.BaseDirectory + "Data/hiro.patt";
        private string AR_CODE_FILE2 = AppDomain.CurrentDomain.BaseDirectory + "Data/vtt.patt";
        private string AR_CAMERA_FILE = AppDomain.CurrentDomain.BaseDirectory + "Data/camera_para.dat";

        public MainWindow()
        {
            InitializeComponent();

            this.InitCamera();
            this.InitARConfigs();

            this.Loaded += MainWindow_Loaded;
        }

        private void InitCamera()
        {
            this.GdMainZm.Height = 600;
            this.CameraZm.Height = 600;
            this.CameraZm.DesiredPixelHeight = 600;

            this.GdMainZm.Width = 800;
            this.CameraZm.Width = 800;
            this.CameraZm.DesiredPixelWidth = 800;

            this.CameraZm.EnableSampleGrabbing = true;
            this.CameraZm.NewVideoSample += CameraZm_NewVideoSample;
        }

        private void LoadCameta()
        {
            try
            {
                this.CameraZm.VideoCaptureDevice = MultimediaUtil.VideoInputDevices[0];
                this.CameraZm.Visibility = Visibility.Visible;
            }
            catch
            {
                string sError = "未查找到摄像头，程序无法正常加载，请检查摄像头是否正常，若仍无法正常运行，请与本软件供应商联系！";
                MessageBox.Show(sError, "提示信息", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private byte[] ConvertBitmapToBytes(System.Drawing.Bitmap oNewFrame)
        {
            var bitmapData = oNewFrame.LockBits(
               new System.Drawing.Rectangle(0, 0, oNewFrame.Width, oNewFrame.Height),
               System.Drawing.Imaging.ImageLockMode.ReadOnly, oNewFrame.PixelFormat);

            byte[] destArr = new byte[bitmapData.Stride * bitmapData.Height];
            System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, destArr, 0,
                bitmapData.Stride * bitmapData.Height);
            oNewFrame.UnlockBits(bitmapData);

            return destArr;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
           this.LoadCameta();
        }

        private void CameraZm_NewVideoSample(object sender, WPFMediaKit.DirectShow.MediaPlayers.VideoSampleArgs e)
        {
            Dispatcher.Invoke(new Action(delegate ()
            {
                System.Drawing.Bitmap oNewFrame = e.VideoFrame;
                //byte[] oFrameMomeryStream = this.ConvertBitmapToBytes(oNewFrame);
                //IntPtr ptr = oNewFrame.GetHbitmap();
                this.MyArRaster.wrapBuffer(oNewFrame);
                
                if (this.MySingleDetectMarker.detectMarkerLite(this.MyArRaster, 127))
                {
                    this.CvMainZm.Children.Clear();
                    NyARDoubleMatrix44 oResultMat = new NyARDoubleMatrix44();
                    this.MySingleDetectMarker.getTransmationMatrix(oResultMat);

                    NyARSquare square = this.MySingleDetectMarker.refSquare();

                    Polygon oPolygon = new Polygon()
                    {
                        SnapsToDevicePixels = true,
                        Fill = new SolidColorBrush(Colors.Violet),
                        Opacity = 0.8,
                        Stroke = new SolidColorBrush(Colors.Red)
                    };
                    oPolygon.Points = new PointCollection(new Point[] {
                                new Point(square.sqvertex[0].x, 600 - square.sqvertex[0].y),
                                new Point(square.sqvertex[1].x, 600 - square.sqvertex[1].y),
                                new Point(square.sqvertex[2].x, 600 - square.sqvertex[2].y),
                                new Point(square.sqvertex[3].x, 600 - square.sqvertex[3].y)});
                    this.CvMainZm.Children.Add(oPolygon);
                }
            }));
        }


        private void InitARConfigs()
        {
            NyARParam ap = NyARParam.loadFromARParamFile(File.OpenRead(AR_CAMERA_FILE), 800, 600);
            NyARCode code = NyARCode.loadFromARPattFile(File.OpenRead(AR_CODE_FILE2), 16, 16);

            this.MyArRaster = new DsRgbRaster(800, 600);
            this.MySingleDetectMarker = NyARSingleDetectMarker.createInstance(ap, code, 80.0);
            this.MySingleDetectMarker.setContinueMode(false);
        }















        //private NyARDetectMarker m_ar;
        //private NyARRgbRaster m_raster;

        //private NyARMarkerSystem MyMarkerSystem;

        //private string AR_CODE_FILE1 = AppDomain.CurrentDomain.BaseDirectory + "Data/hiro.patt";
        //private string AR_CODE_FILE2 = AppDomain.CurrentDomain.BaseDirectory + "Data/vtt.patt";
        //private string AR_CAMERA_FILE = AppDomain.CurrentDomain.BaseDirectory + "Data/camera_para.dat";

        //public MainWindow()
        //{
        //    InitializeComponent();

        //    this.GdMainZm.Height = 600;
        //    this.CameraZm.Height = 600;
        //    this.CameraZm.DesiredPixelHeight = 600;

        //    this.GdMainZm.Width = 800;
        //    this.CameraZm.Width = 800;
        //    this.CameraZm.DesiredPixelWidth = 800;

        //    this.CameraZm.EnableSampleGrabbing = true;
        //    this.CameraZm.NewVideoSample += CameraZm_NewVideoSample;

        //    NyARParam ap = NyARParam.loadFromARParamFile(File.OpenRead(AR_CAMERA_FILE), 800, 600);
        //    this.m_raster = new NyARRgbRaster_BYTE1D_B8G8R8_24(800, 600, false);

        //    NyARCode code = NyARCode.loadFromARPattFile(File.OpenRead(AR_CODE_FILE1), 16, 16);
        //    this.m_ar = new NyARDetectMarker(ap, new NyARCode[] { code }, new double[] { 80.0 }, 1);
        //    this.m_ar.setContinueMode(true);
        //    this.Loaded += MainWindow_Loaded;

        //    INyARMarkerSystemConfig oMarkerSystemConfig = new NyARMarkerSystemConfig(800, 600);
        //    this.MyMarkerSystem = new NyARMarkerSystem(oMarkerSystemConfig);
        //    MarkerID = this.MyMarkerSystem.addARMarker(AR_CODE_FILE1, 16, 25, 80); // value = 0
        //    //MarkerID = this.MyMarkerSystem.addARMarker(AR_CODE_FILE2, 16, 25, 80); // Value = 1
        //}

        //private int MarkerID;

        //private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        this.CameraZm.VideoCaptureDevice = MultimediaUtil.VideoInputDevices[0];
        //        this.CameraZm.Visibility = Visibility.Visible;
        //    }
        //    catch
        //    {
        //        string sError = "未查找到摄像头，程序无法正常加载，请检查摄像头是否正常，若仍无法正常运行，请与本软件供应商联系！";
        //        MessageBox.Show(sError, "提示信息", MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        //private int m_threshold = 100;
        //private const int cameraResY = 600;

        //private void CameraZm_NewVideoSample(object sender, WPFMediaKit.DirectShow.MediaPlayers.VideoSampleArgs e)
        //{
        //    Dispatcher.Invoke(new Action(delegate ()
        //    {
        //        System.Drawing.Bitmap oNewFrame = e.VideoFrame;
        //        var bitmapData = oNewFrame.LockBits(
        //        new System.Drawing.Rectangle(0, 0, oNewFrame.Width, oNewFrame.Height),
        //        System.Drawing.Imaging.ImageLockMode.ReadOnly, oNewFrame.PixelFormat);

        //        byte[] destArr = new byte[bitmapData.Stride * bitmapData.Height];
        //        System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, destArr, 0,
        //            bitmapData.Stride * bitmapData.Height);
        //        oNewFrame.UnlockBits(bitmapData);
        //        this.m_raster.wrapBuffer(destArr);

        //        try
        //        {
        //            int detectedMkrs = this.m_ar.detectMarkerLite(this.m_raster, m_threshold);
        //            NyARDoublePoint2d[] points = null;
        //            List<NyARDoublePoint2d[]> ltPoints = new List<NyARDoublePoint2d[]>();
        //            if (detectedMkrs > 0)
        //            {
        //                points = m_ar.getCorners(0);

        //                ltPoints.Add(points);
        //                for (int i = 0; i < detectedMkrs; i++)
        //                {
        //                    NyARDoublePoint2d[] oMarkerPoints = m_ar.getCorners(i);
        //                    ltPoints.Add(oMarkerPoints);
        //                }
        //            }
        //            Dispatcher.BeginInvoke(new Action(delegate ()
        //            {
        //                try
        //                {
        //                    this.CvMainZm.Children.Clear();
        //                    for (int i = 0; i < ltPoints.Count; i++)
        //                    {
        //                        NyARDoublePoint2d[] oMarkerPoints = ltPoints[i];
        //                        Polygon oPolygon = new Polygon()
        //                        {
        //                            SnapsToDevicePixels = true,
        //                            Fill = new SolidColorBrush(Colors.Violet),
        //                            Opacity = 0.8,
        //                            Stroke = new SolidColorBrush(Colors.Red)
        //                        };

        //                        oPolygon.Points = new PointCollection(new Point[]
        //                        {
        //                              new Point(oMarkerPoints[0].x, cameraResY - oMarkerPoints[0].y),
        //                              new Point(oMarkerPoints[1].x, cameraResY - oMarkerPoints[1].y),
        //                              new Point(oMarkerPoints[2].x, cameraResY - oMarkerPoints[2].y),
        //                              new Point(oMarkerPoints[3].x, cameraResY - oMarkerPoints[3].y)
        //                        });
        //                        this.CvMainZm.Children.Add(oPolygon);
        //                    }
        //                }
        //                catch
        //                { }
        //            }), null);
        //        }
        //        catch
        //        { }
        //    }));
        //}
    }
}
