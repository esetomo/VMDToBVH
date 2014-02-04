using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using VMDToBVH.Models;

namespace VMDToBVH.ViewModels
{
    public class BVHRenderViewModel : ViewModelBase
    {
        private BVH bvh;
        public BVH BVH
        {
            get
            {
                return bvh;
            }
            set
            {
                bvh = value;

                if (bvh != null)
                {
                    ModelRoot.Children.Clear();
                    ModelRoot.Children.Add(CreateMarkerTree(BVH.Root));
                }

                RaisePropertyChanged();
            }
        }

        private int currentFrame;
        public int CurrentFrame
        {
            get
            {
                return currentFrame;
            }
            set
            {
                currentFrame = value;
                UpdateFrame();

                RaisePropertyChanged();
            }
        }

        public ModelVisual3D ModelRoot { get; set; }

        private Visual3D CreateMarkerTree(CompositeElement joint)
        {
            ContainerUIElement3D marker = CreateMarker(1, 0.05);
            joint.Visual = marker;

            Transform3DGroup transforms = new Transform3DGroup();
            marker.Transform = transforms;

            transforms.Children.Add(new MatrixTransform3D());

            Vector3D offset = joint.Offset.Value;
            transforms.Children.Add(new TranslateTransform3D(offset));

            foreach (CompositeElement child in joint.JointList)
            {
                marker.Children.Add(CreateMarkerTree(child));
            }

            return marker;
        }

        private ContainerUIElement3D CreateMarker(double length, double width)
        {
            ContainerUIElement3D visual = new ContainerUIElement3D();
            visual.Children.Add(new ModelUIElement3D()
            {
                Model = new GeometryModel3D()
                {
                    Geometry = new MeshGeometry3D()
                    {
                        Positions = new Point3DCollection(){
                            new Point3D(length, 0, 0),
                            new Point3D(0, width, 0),
                            new Point3D(0, -width, 0),
                            new Point3D(length, 0, 0),
                            new Point3D(0, 0, width),
                            new Point3D(0, 0, -width),
                        },
                    },
                    Material = new DiffuseMaterial()
                    {
                        Brush = new SolidColorBrush(Colors.Red),
                    },
                    BackMaterial = new DiffuseMaterial()
                    {
                        Brush = new SolidColorBrush(Colors.Red),
                    },
                },
            });
            visual.Children.Add(new ModelUIElement3D()
            {
                Model = new GeometryModel3D()
                {
                    Geometry = new MeshGeometry3D()
                    {
                        Positions = new Point3DCollection(){
                            new Point3D(0, length, 0),
                            new Point3D(width, 0, 0),
                            new Point3D(-width, 0, 0),
                            new Point3D(0, length, 0),
                            new Point3D(0, 0, width),
                            new Point3D(0, 0, -width),
                        },
                    },
                    Material = new DiffuseMaterial()
                    {
                        Brush = new SolidColorBrush(Colors.Lime),
                    },
                    BackMaterial = new DiffuseMaterial()
                    {
                        Brush = new SolidColorBrush(Colors.Lime),
                    },
                },
            });
            visual.Children.Add(new ModelUIElement3D()
            {
                Model = new GeometryModel3D()
                {
                    Geometry = new MeshGeometry3D()
                    {
                        Positions = new Point3DCollection(){
                            new Point3D(0, 0, length),
                            new Point3D(width, 0, 0),
                            new Point3D(-width, 0, 0),
                            new Point3D(0, 0, length),
                            new Point3D(0, width, 0),
                            new Point3D(0, -width, 0),
                        },
                    },
                    Material = new DiffuseMaterial()
                    {
                        Brush = new SolidColorBrush(Colors.Blue),
                    },
                    BackMaterial = new DiffuseMaterial()
                    {
                        Brush = new SolidColorBrush(Colors.Blue),
                    },
                },
            });

            return visual;
        }

        private void UpdateFrame()
        {
            if (BVH == null)
                return;

            if (CurrentFrame >= BVH.FrameList.Count)
                return;

            FrameElement frame = BVH.FrameList[CurrentFrame];
            foreach (CompositeElement joint in BVH.JointList)
            {
                JointFrame jf = frame.GetJointFrame(joint.Name);
                Transform3DGroup tg = (Transform3DGroup)joint.Visual.Transform;
                MatrixTransform3D transform = (MatrixTransform3D)tg.Children[0];
                transform.Matrix = jf.Matrix;
            }
        }
    }
}
