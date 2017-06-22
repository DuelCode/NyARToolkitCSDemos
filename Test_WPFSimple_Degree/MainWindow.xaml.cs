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
using jp.nyatla.nyartoolkit.cs.markersystem;
using NyARToolkitCSUtils.Capture;
using NyARToolkitCSUtils.Direct3d;

namespace Test_WPFSimple_Degree
{
    /// <summary>
    /// 计算某个标签的旋转角度，（未实现正向还是反向旋转判读， 计算旋转角度算法精确度不足、有待改进）
    /// </summary>
    public partial class MainWindow : Window, CaptureListener
    {
        private string AR_CODE_FILE1 = AppDomain.CurrentDomain.BaseDirectory + "Data/hiro.patt";
        private string AR_CODE_FILE2 = AppDomain.CurrentDomain.BaseDirectory + "Data/kanji.patt";
        private string AR_CODE_FILE3 = AppDomain.CurrentDomain.BaseDirectory + "Data/vtt.patt";

        private int MarkerID_Hiro;
        private int MarkerID_KanJi;
        private int MarkerID_VTT;

        private NyARD3dMarkerSystem ARMarkerSystem;
        private NyARDirectShowCamera ARCameraSensor;
        private CaptureDevice MyCaptureDevice;

        public MainWindow()
        {
            InitializeComponent();

            CaptureDeviceList oCaptureDeviceList = new CaptureDeviceList();
            MyCaptureDevice = oCaptureDeviceList[0];
            MyCaptureDevice.SetCaptureListener(this);
            MyCaptureDevice.PrepareCapture(800, 600, 30.0f);

            INyARMarkerSystemConfig oMarkerSystemConfig = new NyARMarkerSystemConfig(800, 600);
            this.ARMarkerSystem = new NyARD3dMarkerSystem(oMarkerSystemConfig);
            this.ARCameraSensor = new NyARDirectShowCamera(MyCaptureDevice);

            this.MarkerID_Hiro = this.ARMarkerSystem.addARMarker(AR_CODE_FILE1, 16, 25, 80);
            this.MarkerID_KanJi = this.ARMarkerSystem.addARMarker(AR_CODE_FILE2, 16, 25, 80);
            this.MarkerID_VTT = this.ARMarkerSystem.addARMarker(AR_CODE_FILE3, 16, 25, 80);

            this.Loaded += MainWindow_Loaded;

            this.BtnPauseZm.Click += BtnPauseZm_Click;
            this.BtnStartZm.Click += BtnStartZm_Click;
        }

        private bool IsMarkering = true;

        private void BtnPauseZm_Click(object sender, RoutedEventArgs e)
        {
            this.IsMarkering = false;
            this.CvMainZm.Children.Clear();
        }

        private void BtnStartZm_Click(object sender, RoutedEventArgs e)
        {
            this.IsMarkering = true;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            MyCaptureDevice.StartCapture();
        }

        private int CacheItemCount = 200;

        private int CurItemCount = -1;

        private void ShowARDetectInfo(string sResult)
        {
            CurItemCount++;
            Border oBorder = new Border() { Height = 16 };
            TextBlock oTextBlock = new TextBlock() { VerticalAlignment = VerticalAlignment.Center };

            oBorder.Child = oTextBlock;
            oTextBlock.Text = DateTime.Now.ToLongTimeString() + "： " + sResult;
            this.LbxResultZm.Items.Add(oBorder);
            this.SvMainZm.ScrollToEnd();

            if (this.CurItemCount >= CacheItemCount)
            {
                this.CurItemCount = -1;
                this.LbxResultZm.Items.Clear();
            }
        }

        private double ConvertToDegree(double dRadValue)
        {
            return dRadValue * 180.0 / Math.PI;
        }

        private void CalcRotateDegree(NyARIntPoint2d[] oPoints)
        {
            double dIntervalY = (oPoints[3].y - oPoints[0].y)/2d + (oPoints[2].y - oPoints[1].y)/2d;
            double dIntervalX = (oPoints[1].x - oPoints[0].x)/2d + (oPoints[2].x - oPoints[3].x)/2d;

            if (dIntervalX > dIntervalY)
                this.BdrAnchorZm.Angle = 0;
            else
            {
                //double dPercent = dIntervalX/dIntervalY;
                //this.BdrAnchorZm.Angle = 90*(1 - dPercent);

                this.BdrAnchorZm.Angle = ConvertToDegree(Math.Acos(dIntervalX / dIntervalY));
            }

            this.TbkRotateZm.Text = Math.Round(this.BdrAnchorZm.Angle, 4).ToString();
        }

        /// <summary>
        ///  计算标签摆放的方向、绘制顶点坐标
        /// </summary>
        /// <param name="oPoints"></param>
        private void CalcVertex(NyARIntPoint2d[] oPoints)
        {
            int nPointMinXCount_0X = 0;
            if (oPoints[0].x < oPoints[1].x)
                nPointMinXCount_0X++;
            if (oPoints[0].x < oPoints[2].x)
                nPointMinXCount_0X++;
            if (oPoints[0].x < oPoints[3].x)
                nPointMinXCount_0X++;

            int nPointMinYCount_0Y = 0;
            if (oPoints[0].y < oPoints[1].y)
                nPointMinYCount_0Y++;
            if (oPoints[0].y < oPoints[2].y)
                nPointMinYCount_0Y++;
            if (oPoints[0].y < oPoints[3].y)
                nPointMinYCount_0Y++;

            string sDir = "标签方向:  未检测出";
            if (nPointMinXCount_0X >= 2 && nPointMinYCount_0Y >= 2)
            {
                // A----B
                // |    |
                // D----C
                sDir = "标签方向:  上";
            }
            else if (nPointMinXCount_0X >= 2 && nPointMinYCount_0Y < 2)
            {
                //B---C
                //|   |
                //A---D
                sDir = "标签方向:  左";
            }
            else if (nPointMinXCount_0X < 2 && nPointMinYCount_0Y >= 2)
            {
                //D---A
                //|   |
                //C---B
                sDir = "标签方向:  右";
            }
            else if (nPointMinXCount_0X < 2 && nPointMinYCount_0Y < 2)
            {
                sDir = "标签方向:  下";
                //C---D
                //|   |
                //B---A
            }

            this.TbkDirZm.Text = sDir;

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


        private TextBlock GetVertexPointLabel()
        {
            TextBlock oTbk = new TextBlock();
            oTbk.FontSize = 16;
            return oTbk;
        }

        private void DrawARDetectInfo(int nMarkerID, string sMarkerName)
        {
            //double dConfidence = Math.Round(this.ARMarkerSystem.getConfidence(nMarkerID), 5);
            double dConfidence = this.ARMarkerSystem.getConfidence(nMarkerID);
            NyARIntPoint2d oCenterPoint = this.ARMarkerSystem.getCenter(nMarkerID);
            NyARIntPoint2d[] oPoints = this.ARMarkerSystem.getVertex2D(nMarkerID);

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

            oPolygon.Points = new PointCollection(new Point[]
            {
                new Point(oPoints[0].x, oPoints[0].y),
                new Point(oPoints[1].x, oPoints[1].y),
                new Point(oPoints[2].x, oPoints[2].y),
                new Point(oPoints[3].x, oPoints[3].y)
            });

            this.CalcRotateDegree(oPoints);

            this.TbkPoint1Zm.Text = oPoints[0].x + "   " + oPoints[0].y;
            this.TbkPoint2Zm.Text = oPoints[1].x + "   " + oPoints[1].y;
            this.TbkPoint3Zm.Text = oPoints[2].x + "   " + oPoints[2].y;
            this.TbkPoint4Zm.Text = oPoints[3].x + "   " + oPoints[3].y;

            Ellipse oEllipse = new Ellipse()
            {
                Width = 5,
                Height = 5,
                Fill = new SolidColorBrush(Colors.Green),
            };
            Canvas.SetLeft(oEllipse, oCenterPoint.x);
            Canvas.SetTop(oEllipse, oCenterPoint.y);

            this.CvMainZm.Children.Add(oPolygon);
            this.CvMainZm.Children.Add(oEllipse);

            string sResult = "识别结果:  " + sMarkerName + "    匹配度:" + dConfidence;
            this.ShowARDetectInfo(sResult);

            this.CalcVertex(oPoints);
        }

        void CaptureListener.OnBuffer(CaptureDevice oCaptureDevice, double i_sample_time, IntPtr i_buffer,
            int i_buffer_len)
        {
            Dispatcher.Invoke(new Action(delegate ()
            {
                if (this.IsMarkering)
                {
                    TransformedBitmap b = new TransformedBitmap();
                    b.BeginInit();
                    b.Source = BitmapSource.Create(oCaptureDevice.video_width, oCaptureDevice.video_height, 96.0, 96.0,
                        PixelFormats.Bgr32, BitmapPalettes.WebPalette, i_buffer,
                        i_buffer_len, oCaptureDevice.video_width*(oCaptureDevice.video_bit_count/8));
                    b.SetValue(TransformedBitmap.TransformProperty, new ScaleTransform(-1, -1));
                    b.EndInit();
                    this.ImgMainZm.SetValue(Image.SourceProperty, b);

                    this.ARMarkerSystem.update(this.ARCameraSensor);
                    this.CvMainZm.Children.Clear();

                    //if (this.ARMarkerSystem.isExistMarker(this.MarkerID_Hiro))
                    //    this.DrawARDetectInfo(this.MarkerID_Hiro, "Hiro");
                    //if (this.ARMarkerSystem.isExistMarker(this.MarkerID_KanJi))
                    //    this.DrawARDetectInfo(this.MarkerID_KanJi, "KanJi");
                    if (this.ARMarkerSystem.isExistMarker(this.MarkerID_VTT))
                        this.DrawARDetectInfo(this.MarkerID_VTT, "VTT");
                }
            }));
        }
    }
}
