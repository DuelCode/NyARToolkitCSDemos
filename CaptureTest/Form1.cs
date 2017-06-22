using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using jp.nyatla.nyartoolkit.cs.core;
using jp.nyatla.nyartoolkit.cs.detector;
using NyARToolkitCSUtils.Capture;
using System.IO;

namespace CaptureTest
{
    public partial class Form1 : Form, CaptureListener
    {
        private CaptureDevice m_cap;
        private string AR_CODE_FILE = AppDomain.CurrentDomain.BaseDirectory + "Data/hiro.patt";
        private string AR_CAMERA_FILE = AppDomain.CurrentDomain.BaseDirectory + "Data/camera_para.dat";
        private NyARSingleDetectMarker m_ar;
        private DsRgbRaster m_raster;

        public Form1()
        {
            InitializeComponent();

            NyARParam ap = NyARParam.loadFromARParamFile(File.OpenRead(AR_CAMERA_FILE), 640, 480);


            NyARCode code = NyARCode.loadFromARPattFile(File.OpenRead(AR_CODE_FILE), 16, 16);

            NyARDoubleMatrix44 result_mat = new NyARDoubleMatrix44();
            CaptureDeviceList cl = new CaptureDeviceList();
            CaptureDevice cap = cl[0];
            cap.SetCaptureListener(this);
            cap.PrepareCapture(640, 480, 30);
            this.m_cap = cap;
            
            this.m_raster = new DsRgbRaster(cap.video_width, cap.video_height);
            
            this.m_ar = NyARSingleDetectMarker.createInstance(ap, code, 80.0);
            this.m_ar.setContinueMode(false);

           
        }

        public void OnBuffer(CaptureDevice i_sender, double i_sample_time, IntPtr i_buffer, int i_buffer_len)
        {
            int w = i_sender.video_width;
            int h = i_sender.video_height;
            int s = w*(i_sender.video_bit_count/8);


            Bitmap b = new Bitmap(w, h, s, PixelFormat.Format32bppRgb, i_buffer);
            b.RotateFlip(RotateFlipType.RotateNoneFlipY);
            pictureBox1.Image = b;

            //ARの計算
            this.m_raster.setBuffer(i_buffer, i_buffer_len, i_sender.video_vertical_flip);
            if (this.m_ar.detectMarkerLite(this.m_raster, 100))
            {
                NyARDoubleMatrix44 result_mat = new NyARDoubleMatrix44();
                this.m_ar.getTransmationMatrix(result_mat);
                try
                {
                    this.Invoke(
                        (MethodInvoker) delegate()
                        {
                            
                            label1.Text = this.m_ar.getConfidence().ToString();
                            label3.Text = result_mat.m00.ToString();
                            label4.Text = result_mat.m01.ToString();
                            label5.Text = result_mat.m02.ToString();
                            label6.Text = result_mat.m03.ToString();
                            label7.Text = result_mat.m10.ToString();
                            label8.Text = result_mat.m11.ToString();
                            label9.Text = result_mat.m12.ToString();
                            label10.Text = result_mat.m13.ToString();
                            label11.Text = result_mat.m20.ToString();
                            label12.Text = result_mat.m21.ToString();
                            label13.Text = result_mat.m22.ToString();
                            label14.Text = result_mat.m23.ToString();
                        }
                        );
                }
                catch
                {
                }
            }
            else
            {
                try
                {
                    this.Invoke(
                        (MethodInvoker) delegate()
                        {
                            label1.Text = "未检出";
                            label2.Text = "-";
                            label3.Text = "-";
                            label4.Text = "-";
                            label5.Text = "-";
                            label6.Text = "-";
                            label7.Text = "-";
                            label8.Text = "-";

                            label9.Text = "-";
                            label10.Text = "-";
                            label11.Text = "-";
                            label12.Text = "-";
                            label13.Text = "-";
                            label14.Text = "-";
                        }
                        );
                }
                catch
                {
                }
            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            this.m_cap.StartCapture();
        }
    }
}
