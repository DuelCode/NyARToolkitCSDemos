using System;
using System.Collections;
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
using jp.nyatla.nyartoolkit.cs.markersystem;
using NyARToolkitCSUtils.Capture;
using NyARToolkitCSUtils.Common;
using NyARToolkitCSUtils.Direct3d;

namespace Test_WPFSimple_TwoCamera.Main
{
    /// <summary>
    /// CameraPnl1.xaml 的交互逻辑
    /// </summary>
    public partial class CameraPnl1 : UserControl, CaptureListener
    {
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

        private NyARD3dMarkerSystem ARMarkerSystem;
        private NyARDirectShowCamera ARCameraSensor;
        private CaptureDevice MyCaptureDevice;

        private double CameraWidth;
        private double CameraHeight;

        public CameraPnl1()
        {
            InitializeComponent();

            this.CameraWidth = this.Width;
            this.CameraHeight = this.Height;

            CaptureDeviceList oCaptureDeviceList = new CaptureDeviceList();
            MyCaptureDevice = oCaptureDeviceList[0];
            MyCaptureDevice.SetCaptureListener(this);
            MyCaptureDevice.PrepareCapture((int)this.CameraWidth, (int)this.CameraHeight, 30.0f);

            INyARMarkerSystemConfig oMarkerSystemConfig = new NyARMarkerSystemConfig((int)this.CameraWidth, (int)this.CameraHeight);
            this.ARMarkerSystem = new NyARD3dMarkerSystem(oMarkerSystemConfig);
            this.ARCameraSensor = new NyARDirectShowCamera(MyCaptureDevice);

            this.MarkerID_Hiro = this.ARMarkerSystem.addARMarker(AR_CODE_FILE1, 16, 25, 80);
            this.MarkerID_KanJi = this.ARMarkerSystem.addARMarker(AR_CODE_FILE2, 16, 25, 80);
            this.MarkerID_VTT = this.ARMarkerSystem.addARMarker(AR_CODE_FILE3, 16, 25, 80);
            this.MarkerID_ABB = this.ARMarkerSystem.addARMarker(AR_CODE_FILE4, 16, 25, 80);
            this.Marker_Hello = this.ARMarkerSystem.addARMarker(AR_CODE_FILE5, 16, 25, 80);

            this.Loaded += CameraPnl1_Loaded;
        }

        private void CameraPnl1_Loaded(object sender, RoutedEventArgs e)
        {
            MyCaptureDevice.StartCapture();
        }

        void CaptureListener.OnBuffer(CaptureDevice oCaptureDevice, double i_sample_time, IntPtr i_buffer,
            int i_buffer_len)
        {
            Dispatcher.Invoke(new Action(delegate ()
            {
              
                    TransformedBitmap b = new TransformedBitmap();
                    b.BeginInit();
                    b.Source = BitmapSource.Create(oCaptureDevice.video_width, oCaptureDevice.video_height, 96.0, 96.0,
                        PixelFormats.Bgr32, BitmapPalettes.WebPalette, i_buffer,
                        i_buffer_len, oCaptureDevice.video_width * (oCaptureDevice.video_bit_count / 8));
                    b.SetValue(TransformedBitmap.TransformProperty, new ScaleTransform(-1, -1));
                    b.EndInit();
                    this.ImgCameraZm.SetValue(Image.SourceProperty, b);

                this.ARMarkerSystem.update(this.ARCameraSensor);
                this.CvMainZm.Children.Clear();

                if (this.ARMarkerSystem.isExistMarker(this.MarkerID_VTT))
                    this.DrawARDetectInfo(this.MarkerID_VTT, "VTT");
                if (this.ARMarkerSystem.isExistMarker(this.MarkerID_ABB))
                    this.DrawARDetectInfo(this.MarkerID_ABB, "ABB");
                if (this.ARMarkerSystem.isExistMarker(this.Marker_Hello))
                    this.DrawARDetectInfo(this.Marker_Hello, "Hello");
            }));
        }


        private TextBlock GetVertexPointLabel()
        {
            TextBlock oTbk = new TextBlock();
            oTbk.FontSize = 16;
            return oTbk;
        }

        private Hashtable MarkerInfos = new Hashtable();

        private void DrawARDetectInfo(int nMarkerID, string sMarkerName)
        {
            double dConfidence = Math.Round(this.ARMarkerSystem.getConfidence(nMarkerID), 5);
            NyARIntPoint2d oCenterPoint = this.ARMarkerSystem.getCenter(nMarkerID);
            NyARIntPoint2d[] oPoints = this.ARMarkerSystem.getVertex2D(nMarkerID);

            if (MarkerInfos.Contains(nMarkerID))
            {
                MarkerInfo oMarkerInfo = this.MarkerInfos[nMarkerID] as MarkerInfo;
                oMarkerInfo.Update(oPoints, oCenterPoint, dConfidence);
                this.ShowMarkerInfo(oMarkerInfo);
            }
            else
            {
                MarkerInfo oMarkerInfo = new MarkerInfo(nMarkerID, oPoints, oCenterPoint, dConfidence);
                this.MarkerInfos.Add(nMarkerID, oMarkerInfo);
                this.ShowMarkerInfo(oMarkerInfo);
            }
        }

        private void ShowMarkerInfo(MarkerInfo oMarkerInfo)
        {
            if (oMarkerInfo == null) return;

            this.ShowMarkerRect(oMarkerInfo);
            this.ShowMarkerVertex(oMarkerInfo);
            this.ShowMarkerCenter(oMarkerInfo);
            this.ShowMarkerLayoutDir(oMarkerInfo);
            this.ShowMarkerRotate(oMarkerInfo);
        }

        private void ShowMarkerRect(MarkerInfo oMarkerInfo)
        {
            int nMarkerID = oMarkerInfo.MarkerID;
            NyARIntPoint2d[] oPoints = oMarkerInfo.Vertex;
            Polygon oPolygon = new Polygon()
            {
                SnapsToDevicePixels = true,
                Opacity = 0.8,
                Stroke = new SolidColorBrush(Colors.Green)
            };

            if (nMarkerID == this.MarkerID_VTT)
                oPolygon.Fill = new SolidColorBrush(Colors.Violet);
            else if (nMarkerID == this.MarkerID_Hiro)
                oPolygon.Fill = new SolidColorBrush(Colors.Aqua);
            else if (nMarkerID == this.MarkerID_KanJi)
                oPolygon.Fill = new SolidColorBrush(Colors.DarkKhaki);
            else if (nMarkerID == this.MarkerID_ABB)
                oPolygon.Fill = new SolidColorBrush(Colors.Coral);
            else if (nMarkerID == this.Marker_Hello)
                oPolygon.Fill = new SolidColorBrush(Colors.Aquamarine);
            else
                oPolygon.Fill = new SolidColorBrush(Colors.OrangeRed);

            oPolygon.Points = new PointCollection(new Point[]
            {
                new Point(oPoints[0].x, oPoints[0].y),
                new Point(oPoints[1].x, oPoints[1].y),
                new Point(oPoints[2].x, oPoints[2].y),
                new Point(oPoints[3].x, oPoints[3].y)
            });

            this.CvMainZm.Children.Add(oPolygon);
        }

        private void ShowMarkerCenter(MarkerInfo oMarkerInfo)
        {
            NyARIntPoint2d oCenterPoint = oMarkerInfo.Center;
            Ellipse oEllipse = new Ellipse()
            {
                Width = 5,
                Height = 5,
                Fill = new SolidColorBrush(Colors.Green),
            };
            Canvas.SetLeft(oEllipse, oCenterPoint.x);
            Canvas.SetTop(oEllipse, oCenterPoint.y);
            this.CvMainZm.Children.Add(oEllipse);
        }

        private void ShowMarkerVertex(MarkerInfo oMarkerInfo)
        {
            NyARIntPoint2d[] oPoints = oMarkerInfo.Vertex;
            for (int i = 0; i < oPoints.Length; i++)
            {
                TextBlock oTbk = this.GetVertexPointLabel();
                Canvas.SetLeft(oTbk, oPoints[i].x);
                Canvas.SetTop(oTbk, oPoints[i].y);

                if (i == 0)
                    oTbk.Text = "A";
                else if (i == 1)
                    oTbk.Text = "B";
                else if (i == 2)
                    oTbk.Text = "C";
                else if (i == 3)
                    oTbk.Text = "D";

                this.CvMainZm.Children.Add(oTbk);
            }
        }

        private void ShowMarkerLayoutDir(MarkerInfo oMarkerInfo)
        {
            NyARIntPoint2d oCenterPoint = oMarkerInfo.Center;

            string sDir = "方向:  未检测出";
            TextBlock oTbkDir = this.GetVertexPointLabel();
            Canvas.SetLeft(oTbkDir, oCenterPoint.x - 30);
            Canvas.SetTop(oTbkDir, oCenterPoint.y - 60);

            if (oMarkerInfo.CurLayoutDir == LayoutDirType.ldtUp)
                sDir = "方向:  上";
            else if (oMarkerInfo.CurLayoutDir == LayoutDirType.ldtLeft)
                sDir = "方向:  左";
            else if (oMarkerInfo.CurLayoutDir == LayoutDirType.ldtRight)
                sDir = "方向:  右";
            else if (oMarkerInfo.CurLayoutDir == LayoutDirType.ldtDown)
                sDir = "方向:  下";

            oTbkDir.Text = sDir;
            this.CvMainZm.Children.Add(oTbkDir);
        }

        private void ShowMarkerRotate(MarkerInfo oMarkerInfo)
        {
            NyARIntPoint2d oCenterPoint = oMarkerInfo.Center;

            TextBlock oTbkRotate = this.GetVertexPointLabel();
            Canvas.SetLeft(oTbkRotate, oCenterPoint.x - 60);
            Canvas.SetTop(oTbkRotate, oCenterPoint.y + 20);

            oTbkRotate.Text = "旋转:  " + Math.Round(oMarkerInfo.Rotate, 3);
            this.CvMainZm.Children.Add(oTbkRotate);
        }
    }
}
