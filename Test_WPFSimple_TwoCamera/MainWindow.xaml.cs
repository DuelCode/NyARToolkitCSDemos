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
using Test_WPFSimple_TwoCamera.Main;

namespace Test_WPFSimple_TwoCamera
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private CameraPnl1 CameraPanel1;
        private CameraPnl2 CameraPanel2;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
            this.Unloaded += MainWindow_Unloaded;
        }

        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.CameraPanel1 = new CameraPnl1();
            this.GdCamera1Zm.Children.Add(this.CameraPanel1);
            this.CameraPanel2 = new CameraPnl2();
            this.GdCamera2Zm.Children.Add(this.CameraPanel2);
        }
    }
}
