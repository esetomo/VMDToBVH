using MMF.CG;
using MMF.CG.DeviceManager;
using MMF.CG.Model.MMD;
using MMF.CG.Motion;
using SlimDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using VMDToBVH.Models;

namespace VMDToBVH.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            convertCommand = new DelegateCommand(ConvertCommandExecute);
        }

        private void ConvertCommandExecute(object param)
        {
            BVH = new BVH(model, motion, renderContext);
        }

        private RenderContext renderContext;
        public RenderContext RenderContext
        {
            get
            {
                return renderContext;
            }
            set
            {
                renderContext = value;
                Model = MMDModel.OpenLoad(@"X:\mmd\models\a.pmx", renderContext);                

                RaisePropertyChanged();
                RaisePropertyChanged(() => Model);
            }
        }

        private WorldSpace worldSpace;
        public WorldSpace WorldSpace
        {
            get
            {
                return worldSpace;
            }
            set
            {
                worldSpace = value;
                worldSpace.AddResource(model);
                RaisePropertyChanged();
            }
        }

        private TextureTargetContext textureContext;
        public TextureTargetContext TextureContext
        {
            get
            {
                return textureContext;
            }
            set
            {
                textureContext = value;
                textureContext.MatrixManager.ViewMatrixManager.CameraPosition = new Vector3(0, 10, -25);
                textureContext.MatrixManager.ViewMatrixManager.CameraLookAt = new Vector3(0, 10, 0);
                RaisePropertyChanged();
            }
        }

        private MMDModel model;
        public MMDModel Model
        {
            get
            {
                return model;
            }
            set
            {
                model = value;
                Motion = model.MotionManager.AddMotionFromFile(@"X:\mmd\motions\test.vmd", true);

                RaisePropertyChanged();
            }
        }

        private IMotionProvider motion;
        public IMotionProvider Motion
        {
            get
            {
                return motion;
            }
            set
            {
                motion = value;

                model.MotionManager.ApplyMotion(motion, 0);
                motion.Stop();
                // BVH = new BVH(model, motion, renderContext);

                RaisePropertyChanged();
                RaisePropertyChanged(() => FinalFrame);
                RaisePropertyChanged(() => CurrentFrame);
            }
        }

        public float CurrentFrame
        {
            get
            {
                if (motion == null)
                    return float.NaN;
                return motion.CurrentFrame;
            }
            set
            {
                motion.CurrentFrame = value;
                RaisePropertyChanged();
            }
        }

        public int FinalFrame
        {
            get
            {
                if (motion == null)
                    return 0;
                return motion.FinalFrame;
            }
        }

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
                RaisePropertyChanged();
            }
        }

        private readonly ICommand convertCommand;
        public ICommand ConvertCommand
        {
            get
            {
                return convertCommand;
            }
        }
    }
}
