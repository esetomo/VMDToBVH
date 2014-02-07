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

        private double scale = 1.0;
        public double Scale
        {
            get
            {
                return scale;
            }
            set
            {
                scale = value;
                RaisePropertyChanged();
            }
        }

        private Vector3D offset = new Vector3D();
        public Vector3D Offset
        {
            get
            {
                return offset;
            }
            set
            {
                offset = value;
                RaisePropertyChanged();
            }
        }

        public ModelVisual3D ModelRoot { get; set; }

        private Visual3D CreateMarkerTree(CompositeElement joint)
        {
            ContainerUIElement3D marker = CreateMarker(1, 0.05);
            joint.Visual = marker;

            Matrix3D m = Matrix3D.Identity;
            m.Translate(joint.Offset.Value);

            marker.Transform = new MatrixTransform3D(m);

            foreach (CompositeElement child in joint.JointList)
            {
                marker.Children.Add(CreateMarkerTree(child));
            }

            return marker;
        }

        private ContainerUIElement3D CreateMarker(double length, double width)
        {
            length /= scale;
            width /= scale;

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
                var m = jf.Matrix;

                if (joint.Channels.ChannelList.Count() == 3)
                {
                    m.OffsetX = joint.Offset.Value.X;
                    m.OffsetY = joint.Offset.Value.Y;
                    m.OffsetZ = joint.Offset.Value.Z;
                }

                var transform = (MatrixTransform3D)joint.Visual.Transform;
                transform.Matrix = m;
            }
        }
    }
}
