using Microsoft.Win32;
using MMDFileParser.MotionParser;
using MMF;
using MMF.DeviceManager;
using MMF.Model.PMX;
using MMF.Motion;
using SlimDX;
using System;
using System.Collections.Generic;
using System.IO;
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
                Model = PMXModel.OpenLoad(ofd.FileName, renderContext);
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
                MMDMotion motion = (MMDMotion)model.MotionManager.AddMotionFromFile(ofd.FileName, true);
                MotionData motionData;
                using (var fs = new FileStream(ofd.FileName, FileMode.Open))
                    motionData = MotionData.getMotion(fs);
                SetMotion(motion, motionData);
            }
        }

        private bool CalcBvhCommandCanExecute(object arg)
        {
            return motion != null && !isConverting;
        }

        private void CalcBvhCommandExecute(object param)
        {
            IsConverting = true;

            var bvh = new BVH(model, motionData, motion, (frame) => { CurrentFrame = frame; return IsConverting; });
            if (IsConverting)
            {
                Scale = 1.0;
                BVH = bvh;
            }
            IsConverting = false;
            CurrentFrame = 0;
        }

        private bool ConvertToSlCommandCanExecute(object arg)
        {
            return bvh != null;
        }

        private void ConvertToSlCommandExecute(object obj)
        {
            var converter = new BVHConverter(bvh);
            Scale = 1.0 / converter.Scale;
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
                foreach (CompositeElement joint in bvh.JointList)
                {
                    JointFrame jf = bvh.FrameList[0].GetJointFrame(joint.Name);
                    jf.SetValue("Xrotation", isEnableAllJoints ? 0.1 : 0.0);
                }

                bvh.Save(sfd.FileName, splitIntervalSec);
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
                RaisePropertyChanged();
            }
        }

        private PMXModel model;
        public PMXModel Model
        {
            get
            {
                return model;
            }
            set
            {
                model = value;
                worldSpace.AddResource(model);
                SetMotion(null, null);

                RaisePropertyChanged();
            }
        }

        private MMDMotion motion;
        private MotionData motionData;
        public MMDMotion Motion
        {
            get
            {
                return motion;
            }
        }

        public void SetMotion(MMDMotion value, MotionData motionData)
        {
            motion = value;
            this.motionData = motionData;

            if (motion != null)
            {
                motion.FrameTicked += (_, e) =>
                {
                    if (isRunning && CurrentFrame >= FinalFrame)
                    {
                        IsRunning = false;
                        CurrentFrame = 0;
                    }
                    else
                    {
                        RaisePropertyChanged(() => CurrentFrame);
                    }
                };
                model.MotionManager.ApplyMotion(motion, 0);
                motion.Stop();
            }

            RaisePropertyChanged();
            RaisePropertyChanged(() => FinalFrame);
            RaisePropertyChanged(() => CurrentFrame);
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
                    motion.Start(CurrentFrame, ActionAfterMotion.Nothing);
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

        private bool isEnableAllJoints;
        public bool IsEnableAllJoints
        {
            get
            {
                return isEnableAllJoints;
            }
            set
            {
                isEnableAllJoints = value;
                RaisePropertyChanged();
            }
        }

        private int splitIntervalSec = 30;
        public int SplitIntervalSec
        {
            get
            {
                return splitIntervalSec;
            }
            set
            {
                splitIntervalSec = value;
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
