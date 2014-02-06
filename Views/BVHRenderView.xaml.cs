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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VMDToBVH.Models;
using VMDToBVH.ViewModels;

namespace VMDToBVH.Views
{
    /// <summary>
    /// BVHRenderView.xaml の相互作用ロジック
    /// </summary>
    public partial class BVHRenderView : UserControl
    {
        public BVHRenderView()
        {
            InitializeComponent();

            var vm = (BVHRenderViewModel)viewport.DataContext;
            vm.ModelRoot = modelRoot;
        }

        public static readonly DependencyProperty BVHProperty =
            DependencyProperty.Register("BVH", typeof(BVH), typeof(BVHRenderView), new PropertyMetadata(OnBVHChanged));

        private static void OnBVHChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (BVHRenderView)d;
            var vm = (BVHRenderViewModel)self.viewport.DataContext;
            vm.BVH = (BVH)e.NewValue;
        }

        public BVH BVH
        {
            get
            {
                return (BVH)this.GetValue(BVHProperty);
            }
            set
            {
                this.SetValue(BVHProperty, value);
            }
        }

        public static readonly DependencyProperty CurrentFrameProperty =
            DependencyProperty.Register("CurrentFrame", typeof(int), typeof(BVHRenderView), new PropertyMetadata(OnCurrentFrameChanged));

        private static void OnCurrentFrameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (BVHRenderView)d;
            var vm = (BVHRenderViewModel)self.viewport.DataContext;
            vm.CurrentFrame = (int)e.NewValue;
        }

        public int CurrentFrame
        {
            get
            {
                return (int)this.GetValue(CurrentFrameProperty);
            }
            set
            {
                this.SetValue(CurrentFrameProperty, value);
            }
        }

        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register("Scale", typeof(double), typeof(BVHRenderView), new PropertyMetadata(OnScaleChanged));

        private static void OnScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (BVHRenderView)d;
            var vm = (BVHRenderViewModel)self.viewport.DataContext;
            vm.Scale = (double)e.NewValue;
        }

        public double Scale
        {
            get
            {
                return (double)this.GetValue(ScaleProperty);
            }
            set
            {
                this.SetValue(ScaleProperty, value);
            }
        }

        public static readonly DependencyProperty OffsetProperty =
            DependencyProperty.Register("Offset", typeof(Vector3D), typeof(BVHRenderView), new PropertyMetadata(OnOffsetChanged));

        private static void OnOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (BVHRenderView)d;
            var vm = (BVHRenderViewModel)self.viewport.DataContext;
            vm.Offset = (Vector3D)e.NewValue;            
        }

        public Vector3D Offset
        {
            get
            {
                return (Vector3D)this.GetValue(OffsetProperty);
            }
            set
            {
                this.SetValue(OffsetProperty, value);
            }
        }

        private void viewport_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            camera.FieldOfView = 45.0 * viewport.RenderSize.Width / viewport.RenderSize.Height;
        }
    }
}
