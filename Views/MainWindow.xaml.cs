using MMF.Grid;
using MMF.Matricies.Camera.CameraMotion;
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

            renderControl.TextureContext.CameraMotionProvider = new WPFBasicCameraControllerMotionProvider(this);

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            var pos = renderControl.TextureContext.MatrixManager.ViewMatrixManager.CameraPosition;
            var look = renderControl.TextureContext.MatrixManager.ViewMatrixManager.CameraLookAt;

            bvhRenderView.camera.Position = new System.Windows.Media.Media3D.Point3D(pos.X, pos.Y, -pos.Z);
            bvhRenderView.camera.LookDirection = new System.Windows.Media.Media3D.Vector3D(look.X - pos.X, look.Y - pos.Y, -look.Z + pos.Z);
        }
    }
}
