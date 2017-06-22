using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using jp.nyatla.nyartoolkit.cs.markersystem;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using NyARToolkitCSUtils.Capture;
using NyARToolkitCSUtils.Direct3d;

namespace WinformSimpleLite
{
    

    public partial class Mainfrm : Form
    {
        private const int SCREEN_WIDTH = 640;
        private const int SCREEN_HEIGHT = 480;
        private string AR_CODE_FILE = AppDomain.CurrentDomain.BaseDirectory + "Data/tu.patt";
        //private string AR_CODE_FILE = AppDomain.CurrentDomain.BaseDirectory + "Data/vtt.patt";
        //private string AR_CODE_FILE = AppDomain.CurrentDomain.BaseDirectory + "Data/myhiro.patt";


        private NyARD3dMarkerSystem _ms;
        private NyARDirectShowCamera _ss;
        private NyARD3dRender _rs;
        private int mid;
        private PresentParameters _dpp = new PresentParameters();
        private Device _d3d;

        public Mainfrm()
        {
            InitializeComponent();
            
            CaptureDeviceList capture_device_list = new CaptureDeviceList();
            if (capture_device_list.count < 1)
            {
                MessageBox.Show("The capture system is not found.");
                return;
            }
            CaptureDevice capture_device = capture_device_list[0];
            this.setup(capture_device);
            if (this._d3d == null)
            {
                this._d3d = prepareD3dDevice(this, this._dpp);
            }

            this.Shown += Mainfrm_Shown;
        }

        private void Mainfrm_Shown(object sender, EventArgs e)
        {
            while (this.Created)
            {
                this.loop(this._d3d);
                Application.DoEvents();
            }
        }

        private void loop(Device i_d3d)
        {
            lock (this._ss)
            {
                this._ms.update(this._ss);
                this._rs.drawBackground(i_d3d, this._ss.getSourceImage());
                i_d3d.BeginScene();
                i_d3d.Clear(ClearFlags.ZBuffer, Color.DarkBlue, 1.0f, 0);
                if (this._ms.isExistMarker(this.mid))
                {
                    Matrix transform_mat2 = Matrix.Translation(0, 0, 20.0f);
                    transform_mat2 *= this._ms.getD3dMarkerMatrix(this.mid);
                    i_d3d.SetTransform(TransformType.World, transform_mat2);
                    this._rs.colorCube(i_d3d, 40);
                }
                i_d3d.EndScene();
            }
            i_d3d.Present();
        }

        private void setup(CaptureDevice i_cap)
        {
            Device d3d = this.size(SCREEN_WIDTH, SCREEN_HEIGHT);
            i_cap.PrepareCapture(SCREEN_WIDTH, SCREEN_HEIGHT, 30.0f);
            INyARMarkerSystemConfig cf = new NyARMarkerSystemConfig(SCREEN_WIDTH, SCREEN_HEIGHT);
            d3d.RenderState.ZBufferEnable = true;
            d3d.RenderState.Lighting = false;
            d3d.RenderState.CullMode = Cull.CounterClockwise;
            this._ms = new NyARD3dMarkerSystem(cf);
            this._ss = new NyARDirectShowCamera(i_cap);
            this._rs = new NyARD3dRender(d3d, this._ms);
            this.mid = this._ms.addARMarker(AR_CODE_FILE, 16, 25, 80);
            this._rs.loadARViewMatrix(d3d);
            this._rs.loadARViewPort(d3d);
            this._rs.loadARProjectionMatrix(d3d);
            this._ss.start();
        }

        public Device size(int i_width, int i_height)
        {
            Debug.Assert(this._d3d == null);
            this.ClientSize = new System.Drawing.Size(i_width, i_height);
            Device d = prepareD3dDevice(this, this._dpp);
            this._d3d = d;
            return d;
        }

        private Device prepareD3dDevice(Control i_window, PresentParameters pp)
        {
            pp.Windowed = true;
            pp.SwapEffect = SwapEffect.Flip;
            pp.BackBufferFormat = Format.X8R8G8B8;
            pp.BackBufferCount = 1;
            pp.EnableAutoDepthStencil = true;
            pp.AutoDepthStencilFormat = DepthFormat.D16;
            CreateFlags fl_base = CreateFlags.FpuPreserve;
            try
            {
                return new Device(0, DeviceType.Hardware, i_window.Handle, fl_base | CreateFlags.HardwareVertexProcessing, pp);
            }
            catch (Exception ex1)
            {
                Debug.WriteLine(ex1.ToString());
                try
                {
                    return new Device(0, DeviceType.Hardware, i_window.Handle, fl_base | CreateFlags.SoftwareVertexProcessing, pp);
                }
                catch (Exception ex2)
                {
                    // 作成に失敗
                    Debug.WriteLine(ex2.ToString());
                    try
                    {
                        return new Device(0, DeviceType.Reference, i_window.Handle, fl_base | CreateFlags.SoftwareVertexProcessing, pp);
                    }
                    catch (Exception ex3)
                    {
                        throw ex3;
                    }
                }
            }
        }
    }
}
