﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Media.Media3D;
using System.Collections;
using System.Collections.ObjectModel;
using MMF.CG.Model.MMD;
using MMF.CG.Motion;
using MMF.CG.Skinning;
using MMF.CG;

namespace VMDToBVH.Models
{
    public class BVH : IEnumerable
    {
        private readonly List<CompositeElement> m_joint_list = new List<CompositeElement>();
        private readonly FrameElementList m_frame_list = new FrameElementList();
        private CompositeElement m_root;
        private FramesElement m_frames;
        private FrameTimeElement m_frame_time;

        public BVH()
        {
        }

        public BVH(MMDModel model, IMotionProvider motion, Func<int, bool> progress = null)
        {
            var movedBones = new HashSet<string>();
            foreach (var frame in motion.Motion.BoneFrames.boneFrameList)
            {
                if (frame.BonePosition != SlimDX.Vector3.Zero)
                    movedBones.Add(frame.BoneName);
            }

            var rootBone = model.Skinning.Bone.First((b) => b.Parent == null);
            m_root = new RootElement(this, rootBone, movedBones);
            m_frame_time = new FrameTimeElement(1.0 / 30.0);
            m_frames = new FramesElement(motion.FinalFrame + 1);

            for (int i = 0; i <= motion.FinalFrame; i++)
            {
                motion.CurrentFrame = i;
                model.Update();
                m_frame_list.Add(new FrameElement(this, model, motion));
                if (progress != null)
                {
                    if (!progress(i))
                        return;
                }
                System.Windows.Forms.Application.DoEvents();
            }
        }

        public void Load(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                Element current = null;
                Stack<Element> stack = new Stack<Element>();

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine().Trim();
                    string[] fields = line.Split(' ', '\t');
                    switch (fields[0])
                    {
                        case "":
                            continue;
                        case "{":
                            stack.Push(current);
                            continue;
                        case "}":
                            stack.Pop();
                            continue;
                        case "HIERARCHY":
                            continue;
                        case "ROOT":
                            current = new RootElement(this, fields[1]);
                            break;
                        case "JOINT":
                            current = new JointElement(this, fields[1]);
                            break;
                        case "End":
                            current = new EndSiteElement(this, fields[1]);
                            break;
                        case "OFFSET":
                            current = new OffsetElement(fields[1], fields[2], fields[3]);
                            break;
                        case "CHANNELS":
                            current = new ChannelsElement(fields);
                            break;
                        case "MOTION":
                            continue;
                        case "Frames:":
                            current = new FramesElement(fields[1]);
                            break;
                        case "Frame":
                            current = new FrameTimeElement(fields[2]);
                            break;
                        default:
                            current = new FrameElement(this, fields);
                            break;
                    }

                    if (stack.Count == 0)
                    {
                        this.Add(current);
                    }
                    else
                    {
                        CompositeElement parent = stack.Peek() as CompositeElement;
                        parent.Add(current);
                    }
                }
            }
        }

        public void Save(string fileName, int durationSec)
        {
            int durationFrame = durationSec * 30;

            if (m_frames.Value < durationFrame) //指定秒数のフレーム(30FPS)未満なら一気に保存
            {
                using (FileStream stream = new FileStream(fileName, FileMode.Create))
                {
                    Save(stream);
                }
                return;
            }
            SaveSplit(fileName, durationFrame);
        }

        public void Save(Stream stream)
        {
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("HIERARCHY");
                m_root.Write(writer, 0);
                writer.WriteLine("MOTION");
                writer.WriteLine("Frames: {0}", m_frames.Value);
                writer.WriteLine("Frame Time: {0:0.000000}", m_frame_time.Value);
                foreach (FrameElement frame in m_frame_list)
                {
                    frame.Write(writer);
                }
            }
        }

        private void SaveSplit(string fileName, int durationFrame)
        {
            int index = 1;　//複数ファイルのインデックス
            int startFrame = 1;

            while (startFrame < m_frames.Value)
            {
                SavePartial(string.Format("{0}\\{1}{2}{3}", 
                                          Path.GetDirectoryName(fileName), 
                                          Path.GetFileNameWithoutExtension(fileName),
                                          index,
                                          Path.GetExtension(fileName)),
                            startFrame,
                            durationFrame);

                startFrame += durationFrame - 1;
                index += 1;
            }
        }

        private void SavePartial(string fileName, int startFrame, int durationFrame)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Create))
            {
                SavePartial(stream, startFrame, durationFrame);
            }
        }

        private void SavePartial(Stream stream, int startFrame, int durationFrame)
        {
            long endFrame = startFrame + durationFrame - 1;
            if (endFrame >= m_frames.Value) endFrame = m_frames.Value-1;

            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("HIERARCHY");
                m_root.Write(writer, 0);
                writer.WriteLine("MOTION");
                writer.WriteLine("Frames: {0}", endFrame - startFrame+1);
                writer.WriteLine("Frame Time: {0}", m_frame_time.Value);
                m_frame_list[0].Write(writer);
                for(int i = startFrame+1; i<= endFrame; i++)
                {
                    m_frame_list[i].Write(writer);
                }
            }
        }

        private void Add(Element child)
        {
            RootElement root = child as RootElement;
            if (root != null)
            {
                m_root = root;
                return;
            }

            JointElement joint = child as JointElement;
            if (joint != null)
            {
                m_root = joint;
                m_joint_list.Add(joint);
                return;
            }

            FramesElement frames = child as FramesElement;
            if (frames != null)
            {
                m_frames = frames;
                return;
            }

            FrameTimeElement frame_time = child as FrameTimeElement;
            if (frame_time != null)
            {
                m_frame_time = frame_time;
                return;
            }

            FrameElement frame = child as FrameElement;
            if (frame != null)
            {
                m_frame_list.Add(frame);
                return;
            }

            throw new NotSupportedException();
        }

        public List<CompositeElement> JointList
        {
            get
            {
                return m_joint_list;
            }
        }

        public List<FrameElement> FrameList
        {
            get
            {
                return m_frame_list;
            }
        }

        public CompositeElement Root
        {
            get
            {
                return m_root;
            }
        }

        public FrameTimeElement FrameTime
        {
            get
            {
                return m_frame_time;
            }
        }

        public FramesElement Frames
        {
            get
            {
                return m_frames;
            }
        }

        #region IEnumerable メンバ

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return m_root;
            yield return m_frame_list;
        }

        #endregion
    }

    public abstract class Element
    {
        protected Matrix3D Convert(SlimDX.Matrix m)
        {
            var r = new Matrix3D(m.M11, m.M12, m.M13, m.M14,
                                m.M21, m.M22, m.M23, m.M24,
                                m.M31, m.M32, m.M33, m.M34,
                                m.M41, m.M42, m.M43, m.M44);
            return r;
        }

        protected Vector3D Convert(SlimDX.Vector3 v)
        {
            var r = new Vector3D(v.X, v.Y, v.Z);
            return r;
        }
    }

    public abstract class CompositeElement : Element
    {
        private readonly BVH m_bvh;
        private string m_name;
        private readonly ObservableCollection<CompositeElement> m_joint_list;
        private OffsetElement m_offset;
        private ChannelsElement m_channels;
        private readonly JointFrameList m_joint_frame_list;

        public CompositeElement(BVH bvh, string name)
        {
            m_bvh = bvh;
            m_name = name;
            m_joint_list = new ObservableCollection<CompositeElement>();
            m_joint_frame_list = new JointFrameList(bvh, this);
        }

        public void Add(Element child)
        {
            OffsetElement offset = child as OffsetElement;
            if (offset != null)
            {
                m_offset = offset;
                return;
            }

            ChannelsElement channels = child as ChannelsElement;
            if (channels != null)
            {
                m_channels = channels;
                m_bvh.JointList.Add(this);
                return;
            }

            CompositeElement joint = child as CompositeElement;
            if (joint != null)
            {
                if (joint.Name == "\\u4e0a\\u534a\\u8eab2")
                {
                    joint.m_name = "upper_body2";
                }
                m_joint_list.Add(joint);
                return;
            }

            throw new NotSupportedException();
        }

        public void Write(TextWriter writer, int indent)
        {
            writer.WriteLine("{0}{1} {2}", new string(' ', indent * 2), ElementName, m_name);
            writer.WriteLine("{0}{{", new string(' ', indent * 2));
            m_offset.Write(writer, indent + 1);
            if (m_channels != null)
                m_channels.Write(writer, indent + 1);
            foreach (CompositeElement joint in m_joint_list)
            {
                joint.Write(writer, indent + 1);
            }
            writer.WriteLine("{0}}}", new string(' ', indent * 2));
        }

        public ChannelsElement Channels
        {
            get
            {
                return m_channels;
            }
        }

        public string Name
        {
            get
            {
                return m_name;
            }
        }

        public abstract string ElementName { get; }

        public ObservableCollection<CompositeElement> JointList
        {
            get
            {
                return m_joint_list;
            }
        }

        public OffsetElement Offset
        {
            get
            {
                return m_offset;
            }
        }

        public List<object> Children
        {
            get
            {
                List<object> list = new List<object>();
                foreach (CompositeElement joint in JointList)
                    list.Add(joint);
                list.Add(m_joint_frame_list);
                return list;
            }
        }

        public ContainerUIElement3D Visual { get; set; }
    }

    public class JointFrameList
    {
        private readonly BVH m_bvh;
        private readonly CompositeElement m_joint;

        public JointFrameList(BVH bvh, CompositeElement joint)
        {
            m_bvh = bvh;
            m_joint = joint;
        }

        public List<JointFrame> Frames
        {
            get
            {
                List<JointFrame> list = new List<JointFrame>();
                foreach (FrameElement frame in m_bvh.FrameList)
                {
                    list.Add(frame.GetJointFrame(m_joint.Name));
                }
                return list;
            }
        }
    }

    public class JointElement : CompositeElement
    {
        public JointElement(BVH bvh, string name)
            : base(bvh, name)
        {
        }

        public JointElement(BVH bvh, Bone bone, HashSet<string> movedBones)
            : base(bvh, bone.BoneName)
        {
            Add(new OffsetElement(bone));
            Add(new ChannelsElement(movedBones.Contains(bone.BoneName)));

            foreach(var child in bone.Children)
            {
                Add(new JointElement(bvh, child, movedBones));
            }
        }

        public override string ElementName
        {
            get { return "JOINT"; }
        }
    }

    public class RootElement : CompositeElement
    {
        public RootElement(BVH bvh, string name)
            : base(bvh, name)
        {
        }

        public RootElement(BVH bvh, Bone bone, HashSet<string> movedBones)
            : base(bvh, bone.BoneName)
        {
            Add(new OffsetElement(bone));
            Add(new ChannelsElement(movedBones.Contains(bone.BoneName)));

            foreach(var child in bone.Children)
            {
                Add(new JointElement(bvh, child, movedBones));
            }
        }

        public override string ElementName
        {
            get { return "ROOT"; }
        }
    }

    public class EndSiteElement : CompositeElement
    {
        public EndSiteElement(BVH bvh, string name)
            : base(bvh, name)
        {
        }

        public override string ElementName
        {
            get { return "End"; }
        }
    }

    public class OffsetElement : Element
    {
        private readonly Vector3D m_value;

        public OffsetElement(string x, string y, string z)
        {
            m_value = new Vector3D(double.Parse(x), double.Parse(y), double.Parse(z));
        }

        public OffsetElement(Bone bone)
        {
            var pos = Convert(bone.Position);

            var parent = bone.Parent;
            if (parent != null)
            {
                var parentPos = Convert(parent.Position);
                m_value = pos - parentPos;
            }
            else
            {
                m_value = pos;
            }

            m_value.Z = -m_value.Z;
        }

        public void Write(TextWriter writer, int indent)
        {
            writer.WriteLine("{0}OFFSET {1:0.000000} {2:0.000000} {3:0.000000}", new string(' ', indent * 2), m_value.X, m_value.Y, m_value.Z);
        }

        public override string ToString()
        {
            return m_value.ToString();
        }

        public Vector3D Value
        {
            get
            {
                return m_value;
            }
        }
    }

    public class ChannelsElement : Element
    {
        private readonly int m_count;
        private readonly string[] m_channel_list;

        public ChannelsElement(string[] args)
        {
            m_count = int.Parse(args[1]);
            m_channel_list = args.Skip(2).ToArray();
        }

        public ChannelsElement(bool isMoved)
        {
            if (isMoved)
            {
                m_count = 6;
                m_channel_list = new[] { 
                    "Xposition", "Yposition", "Zposition",
                    "Zrotation", "Xrotation", "Yrotation" 
                };
            }
            else
            {
                m_count = 3;
                m_channel_list = new[] { 
                    "Zrotation", "Xrotation", "Yrotation" 
                };
            }
        }

        public string[] ChannelList
        {
            get
            {
                return m_channel_list;
            }
        }

        public void Write(TextWriter writer, int indent)
        {
            writer.WriteLine("{0}CHANNELS {1} {2}",
                             new string(' ', indent * 2),
                             m_count,
                             string.Join(" ", m_channel_list));
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", m_count, string.Join(" ", m_channel_list));
        }
    }

    public class FramesElement : Element
    {
        public FramesElement(string value)
        {
            this.Value = long.Parse(value);
        }

        public FramesElement(long frames)
        {
            this.Value = frames;
        }

        public long Value { get; set; }
    }

    public class FrameTimeElement : Element
    {
        public FrameTimeElement(string value)
        {
            this.Value = double.Parse(value);
        }

        public FrameTimeElement(double value)
        {
            this.Value = value;
        }

        public double Value { get; set; }
    }

    public class FrameElement : Element
    {
        private readonly BVH m_bvh;
        private readonly Dictionary<string, JointFrame> m_map;

        public FrameElement(BVH bvh)
        {
            m_bvh = bvh;
            m_map = new Dictionary<string, JointFrame>();
            foreach (CompositeElement joint in m_bvh.JointList)
            {
                JointFrame a = new JointFrame();
                foreach (string channel in joint.Channels.ChannelList)
                {
                    a.AddChannel(channel, 0);
                }
                m_map[joint.Name] = a;
            }
        }

        public FrameElement(BVH bvh, string[] args)
            :this(bvh)
        {
            Queue<string> queue = new Queue<string>(args);

            foreach (CompositeElement joint in m_bvh.JointList)
            {
                JointFrame a = m_map[joint.Name];
                foreach (string channel in joint.Channels.ChannelList)
                {
                    a.SetValue(channel, double.Parse(queue.Dequeue()));
                }
            }
            if (queue.Count != 0)
                throw new InvalidDataException();
        }

        public FrameElement(BVH bvh, MMDModel model, IMotionProvider motion)
            : this(bvh)
        {
            foreach (Bone bone in model.Skinning.Bone)
            {
                if (!m_map.ContainsKey(bone.BoneName))
                    continue;
                JointFrame a = m_map[bone.BoneName];

                var m = Convert(bone.GlobalPose);

                if (bone.Parent != null)
                {
                    var p = Convert(bone.Parent.GlobalPose);
                    p.Invert();
                    m *= p;
                }
                m.TranslatePrepend(Convert(bone.Position));
                m.Translate(Convert(-bone.Position));

                a.Matrix = m;

                a.SetValue("Xrotation", -a.GetValue("Xrotation"));
                a.SetValue("Yrotation", -a.GetValue("Yrotation"));
                a.SetValue("Zposition", -a.GetValue("Zposition"));
            }
        }

        public void Write(TextWriter writer)
        {
            foreach (CompositeElement joint in m_bvh.JointList)
            {
                JointFrame a = m_map[joint.Name];
                foreach (string channel in joint.Channels.ChannelList)
                {
                    writer.Write("{0:0.000000} ", a.GetValue(channel));
                }
            }
            writer.WriteLine();
        }

        public JointFrame GetJointFrame(string joint_name)
        {
            if (!m_map.ContainsKey(joint_name))
                return null;
            return m_map[joint_name];
        }

        public override string ToString()
        {
            using (StringWriter writer = new StringWriter())
            {
                Write(writer);
                return writer.ToString();
            }
        }
    }

    public class JointFrame
    {
        private readonly List<string> m_channels = new List<string>();
        private readonly List<double> m_values = new List<double>();
        private readonly Dictionary<string, double> m_map = new Dictionary<string,double>();

        public void AddChannel(string name, double value)
        {
            m_channels.Add(name);
            m_values.Add(value);
            m_map[name] = value;            
        }

        public double GetValue(string name)
        {
            double d;
            m_map.TryGetValue(name, out d);
            return d;
        }

        public void SetValue(string name, double value)
        {
            m_map[name] = value;
            for (int i = 0; i < m_channels.Count; i++)
            {
                if (m_channels[i] == name)
                {
                    m_values[i] = value;
                    break;
                }
            }
        }

        public void AddValue(string name, double value)
        {
            SetValue(name, GetValue(name) + value);
        }

        public override string ToString()
        {
            return string.Join(" ", m_values.Select((v) => string.Format("{0,7:##0.000}", v)).ToArray());
        }

        public Matrix3D Matrix
        {
            get
            {
                Matrix3D matrix = new Matrix3D();

                for (int i = 0; i < m_channels.Count; i++)
                {
                    string channel = m_channels[i];
                    double v = m_values[i];

                    switch (channel)
                    {
                        case "Xposition":
                            matrix.TranslatePrepend(new Vector3D(v, 0, 0));
                            break;
                        case "Yposition":
                            matrix.TranslatePrepend(new Vector3D(0, v, 0));
                            break;
                        case "Zposition":
                            matrix.TranslatePrepend(new Vector3D(0, 0, v));
                            break;
                        case "Xrotation":
                            matrix.RotatePrepend(new Quaternion(new Vector3D(1, 0, 0), v));
                            break;
                        case "Yrotation":
                            matrix.RotatePrepend(new Quaternion(new Vector3D(0, 1, 0), v));
                            break;
                        case "Zrotation":
                            matrix.RotatePrepend(new Quaternion(new Vector3D(0, 0, 1), v));                            
                            break;
                    }
                }

                return matrix;
            }
            set
            {
                // 回転順がzyxの場合のみ対応
                for (int i = 0; i < m_channels.Count; i++)
                {
                    string channel = m_channels[i];
                    double v;

                    switch (channel)
                    {
                        case "Xposition":
                            v = value.OffsetX;
                            break;
                        case "Yposition":
                            v = value.OffsetY;
                            break;
                        case "Zposition":
                            v = value.OffsetZ;
                            break;
                        case "Xrotation":
                            v = Math.Asin(value.M23) * 180 / Math.PI; 
                            break;
                        case "Yrotation":
                            v = Math.Atan2(-value.M13, value.M33) * 180 / Math.PI; 
                            break;
                        case "Zrotation":
                            v = Math.Atan2(-value.M21, value.M22) * 180 / Math.PI; 
                            break;
                        default:
                            continue;
                    }

                    m_values[i] = v;
                    m_map[channel] = v;
                }
            }
        }
    }

    public class FrameElementList : List<FrameElement>
    {
        public List<FrameElement> Frames
        {
            get
            {
                return this;
            }
        }
    }
}
