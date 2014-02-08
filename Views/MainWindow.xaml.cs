using MMF.CG.Model.Grid;
using MMF.CG.Model.MMD;
using SlimDX;
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
using VMDToBVH.ViewModels;

namespace VMDToBVH.Views
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private Point basePoint;
        private double baseCameraAngleX;
        private double baseCameraAngleY;
        private double cameraAngleX;
        private double cameraAngleY;

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            var vm = DataContext as MainViewModel;
            vm.RenderContext = renderControl.RenderContext;
            vm.WorldSpace = renderControl.WorldSpace;
            vm.TextureContext = renderControl.TextureContext;

            BasicGrid grid = new BasicGrid();
            grid.Load(renderControl.RenderContext);
            renderControl.WorldSpace.AddResource(grid);
            renderControl.Background = new Color4(1, 0, 0, 0);

            UpdateCamera();
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            basePoint = e.GetPosition((IInputElement)sender);
            baseCameraAngleX = cameraAngleX;
            baseCameraAngleY = cameraAngleY;
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
                return;

            Point p = e.GetPosition((IInputElement)sender);

            cameraAngleY = baseCameraAngleY + basePoint.X - p.X;
            cameraAngleX = baseCameraAngleX + basePoint.Y - p.Y;
            if (cameraAngleX < -89.0)
                cameraAngleX = -89.0;
            if (cameraAngleX > 89.0)
                cameraAngleX = 89.0;

            UpdateCamera();
        }

        private void UpdateCamera()
        {
            var pos = new Vector3(0, 0, -25);
            var m = SlimDX.Matrix.RotationX((float)(-cameraAngleX * Math.PI / 180.0)) * SlimDX.Matrix.RotationY((float)(-cameraAngleY * Math.PI / 180.0));
            pos = SlimDX.Vector3.TransformCoordinate(pos, m);
            pos += new Vector3(0, 10, 0);
            renderControl.TextureContext.MatrixManager.ViewMatrixManager.CameraPosition = pos;
            renderControl.TextureContext.MatrixManager.ViewMatrixManager.CameraLookAt = new Vector3(0, 10, 0);

            bvhRenderView.CameraAngleX = cameraAngleX;
            bvhRenderView.CameraAngleY = cameraAngleY;
        }
    }
}
