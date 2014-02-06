using Microsoft.Win32;
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
using System.Windows.Media.Media3D;
using VMDToBVH.Models;

namespace VMDToBVH.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            openPmxCommand = new DelegateCommand(OpenPmxCommandExecute, OpenPmxCommandCanExecute);
            openVmdCommand = new DelegateCommand(OpenVmdCommandExecute, OpenVmdCommandCanExecute);
            calcBvhCommand = new DelegateCommand(CalcBvhCommandExecute, CalcBvhCommandCanExecute);
            saveBvhCommand = new DelegateCommand(SaveBvhCommandExecute, SaveBvhCommandCanExecute);
            toggleRunningCommand = new DelegateCommand(ToggleRunningCommandExecute, ToggleRunningCommandCanExecute);
            cancelCommand = new DelegateCommand(CancelCommandExecute, CancelCommandCanExecute);
            convertToSlCommand = new DelegateCommand(ConvertToSlCommandExecute, ConvertToSlCommandCanExecute);
        }

        private bool OpenPmxCommandCanExecute(object arg)
        {
            return renderContext != null && !isConverting;
        }

        private void OpenPmxCommandExecute(object obj)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "PMXモデルファイル(*.pmx)|*.pmx";
            if (ofd.ShowDialog() == true)
            {
                Model = MMDModel.OpenLoad(ofd.FileName, renderContext);
            }
        }

        private bool OpenVmdCommandCanExecute(object arg)
        {
            return model != null && !isConverting;
        }

        private void OpenVmdCommandExecute(object obj)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "VMDモーションファイル(*.vmd)|*.vmd";
            if (ofd.ShowDialog() == true)
            {
                Motion = model.MotionManager.AddMotionFromFile(ofd.FileName, true);
            }
        }

        private bool CalcBvhCommandCanExecute(object arg)
        {
            return motion != null && !isConverting;
        }

        private void CalcBvhCommandExecute(object param)
        {
            IsConverting = true;
            var bvh = new BVH(model, motion, (frame) => { CurrentFrame = frame; return IsConverting; });
            if (IsConverting)
            {
                Scale = 1.0;
                Offset = new Vector3D();
                BVH = bvh;
            }
            IsConverting = false;
        }

        private bool ConvertToSlCommandCanExecute(object arg)
        {
            return bvh != null;
        }

        private void ConvertToSlCommandExecute(object obj)
        {
            var converter = new BVHConverter(bvh);
            Scale = 0.20;
            Offset = new Vector3D(0, 6.5, 0);
            BVH = converter.Convert();
        }

        private bool SaveBvhCommandCanExecute(object arg)
        {
            return bvh != null && !isConverting;
        }

        private void SaveBvhCommandExecute(object obj)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "BVHファイル(*.bvh)|*.bvh";
            if (sfd.ShowDialog() == true)
            {
                using (var stream = sfd.OpenFile())
                    bvh.Save(stream);
            }
        }

        private bool ToggleRunningCommandCanExecute(object arg)
        {
            return motion != null && !isConverting;
        }

        private void ToggleRunningCommandExecute(object obj)
        {
            IsRunning = !isRunning;
        }

        private bool CancelCommandCanExecute(object arg)
        {
            return isConverting;
        }

        private void CancelCommandExecute(object obj)
        {
            IsConverting = false;
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
                worldSpace.AddResource(model);
                Motion = null;

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

                if (motion != null)
                {
                    motion.FrameTicked += (_, e) => RaisePropertyChanged(() => CurrentFrame);
                    model.MotionManager.ApplyMotion(motion, 0);
                    motion.Stop();
                }

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

        private bool isRunning = false;
        public bool IsRunning
        {
            get
            {
                return isRunning;
            }
            set
            {
                if (isRunning == value)
                    return;

                isRunning = value;
                if (isRunning)
                {
                    motion.Start(CurrentFrame, ActionAfterMotion.Replay);
                }
                else
                {
                    motion.Stop();
                }

                RaisePropertyChanged();
            }
        }

        private bool isConverting = false;
        public bool IsConverting
        {
            get
            {
                return isConverting;
            }
            set
            {
                isConverting = value;
                RaisePropertyChanged();
            }
        }

        private readonly ICommand calcBvhCommand;
        public ICommand CalcBvhCommand
        {
            get
            {
                return calcBvhCommand;
            }
        }

        private readonly ICommand openPmxCommand;
        public ICommand OpenPmxCommand
        {
            get
            {
                return openPmxCommand;
            }
        }

        private readonly ICommand openVmdCommand;
        public ICommand OpenVmdCommand
        {
            get
            {
                return openVmdCommand;
            }
        }

        private readonly ICommand saveBvhCommand;
        public ICommand SaveBvhCommand
        {
            get
            {
                return saveBvhCommand;
            }
        }

        private readonly ICommand toggleRunningCommand;
        public ICommand ToggleRunningCommand
        {
            get
            {
                return toggleRunningCommand;
            }
        }

        private readonly ICommand cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                return cancelCommand;
            }
        }

        private readonly ICommand convertToSlCommand;
        public ICommand ConvertToSlCommand
        {
            get
            {
                return convertToSlCommand;
            }
        }
    }
}
