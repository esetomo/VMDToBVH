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

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            var vm = DataContext as MainViewModel;
            vm.RenderContext = renderControl.RenderContext;
            vm.WorldSpace = renderControl.WorldSpace;
            vm.TextureContext = renderControl.TextureContext;

            renderControl.Background = new Color4(1, 0, 0, 0);
        }
    }
}
