using System;
using System.Drawing;
using System.Collections.Generic;
using DMI_Parser.Raw;

namespace DMI_Parser
{
    public class DMIState
    {
        public readonly Dmi Parent;
        public int Width => Parent.Width;
        public int Height => Parent.Height;

        public string Id { get; private set; }
        public DirCount Dirs { get; private set; }
        public int Frames { get; private set; }
        private float[] _delays;
        public int Loop { get; private set; } // 0 => infinite
        public bool Rewind { get; private set; }
        public bool Movement { get; private set; }
        private List<Hotspot> _hotspots;
        public Bitmap[,] Images; //index is dir + dir*frame
        
        //Events
        public event EventHandler stateChanged;
        public event EventHandler idChanged;
        public event EventHandler dirCountChanged;
        public event EventHandler frameCountChanged;
        public event EventHandler loopCountChanged;
        public event EventHandler rewindChanged;

        public DMIState(Dmi parent, Bitmap[,] images, RawDmiState rawDmiState)
        {
            //can set all these without validation
            Parent = parent;
            Loop = rawDmiState.Loop;
            Rewind = rawDmiState.Rewind;
            Movement = rawDmiState.Movement;
            Id = rawDmiState.Id;

            //validating and adding hotspots
            _hotspots = new List<Hotspot>();
            List<int> alreadyRegisteredIndexes = new List<int>();
            foreach (var hspot in rawDmiState.Hotspots)
            {
                if (hspot.isInBounds(parent.Width, parent.Height) && !alreadyRegisteredIndexes.Contains(hspot.Index))
                {
                    _hotspots.Add(Hotspot.fromRawHotspot(hspot, rawDmiState.Dirs.Value, rawDmiState.Frames.Value));
                    alreadyRegisteredIndexes.Add(hspot.Index);
                }
                else
                {
                    //todo [logging] warning
                }
            }
            
            // todo validate dir and framecount with delays and picturearray
            this.Frames = rawDmiState.Frames.Value;
            this.Dirs = rawDmiState.Dirs.Value;
            this.Images = images;
            /*setDirs((DirCount)dirs);

            setFrames(frames);
            if (delays != null)
            {
                if (delays.Length > this._delays.Length)
                {
                    float[] old_delays = delays;
                    delays = new float[this._delays.Length];
                    for (int i = 0; i < delays.Length; i++)
                    {
                        delays[i] = old_delays[i];
                    }
                }

                setDelays(delays);
            }

            cutImages(full_image, img_offset);*/

            //subscribing our generic event to all specific ones
            idChanged += OnSomethingChanged;
            dirCountChanged += OnSomethingChanged;
            frameCountChanged += OnSomethingChanged;
            loopCountChanged += OnSomethingChanged;
            rewindChanged += OnSomethingChanged;
        }

        private void OnSomethingChanged(object s, EventArgs e)
        {
            stateChanged?.Invoke(this, null);
        }

        public Bitmap getImage(int dir, int frame){
            return Images[dir,frame];
        }

        public float getDelay(int frame)
        {
            if(frame < 0 || frame > _delays.Length-1) throw new ArgumentException($"Delay for Frame {frame} does not exist");
            
            return _delays[frame];
        }

        public void setID(string id)
        {
            if (id == this.Id) return;

            this.Id = id;
            idChanged?.Invoke(this, null);
        }

        public void setDirs(DirCount dirs)
        {
            if (dirs == this.Dirs) return;
            
            this.Dirs = dirs; //todo actually implement
            dirCountChanged?.Invoke(this, null);
        }

        public void setFrames(int frames)
        {
            if (frames == this.Frames) return;
            
            if(frames < 1){
                throw new FrameCountInvalidException("Frame count invalid, only Integers > 1 are allowed", this, frames);
            }

            this.Frames = frames; //todo actually implement, maybe have images on first init already be pre-cut?
            if(frames > 1){
                _delays = new float[frames];
            }else{ //we wont have delays with only one frame
                _delays = null;
            }
            frameCountChanged?.Invoke(this, null);
        }

        public void setDelays(float[] delays){
            if((this._delays == null) && Frames == 1){
                throw new FrameCountMismatchException("Only one Frame cannot allow delays", this);
            }

            if(this._delays.Length != delays.Length){
                throw new DelayCountMismatchException("Delaycount doesn't match", this, this._delays.Length, delays.Length);
            }
            this._delays = delays;
        }

        public void setDelay(int index, float delay){
            _delays[index] = delay; //will throw IndexOutOfRangeException if index invalid, intended
        }

        public void setLoop(int loop)
        {
            if (loop == Loop)  return;
            
            if (loop < 0)
                throw new ArgumentException("Loopcount cannot be < 0");

            Loop = loop;
            loopCountChanged?.Invoke(this, null);
        }

        public void setRewind(bool rewind)
        {
            if(rewind == Rewind) return;

            Rewind = rewind;
            rewindChanged?.Invoke(this, null);
        }

        public override string ToString()
        {
            string res = $"state = \"{Id}\"";
            res += $"{Dmi.DMI_TAB}dirs = {Dirs}";
            res += $"{Dmi.DMI_TAB}frames = {Frames}";

            if (_delays != null)
            {
                string[] delayStrings = new string[_delays.Length];
                for (var i = 0; i < _delays.Length; i++)
                {
                    delayStrings[i] = _delays[i].ToString().Replace(',', '.');
                }
                res += $"{Dmi.DMI_TAB}delay = {String.Join(",", delayStrings)}";
            }
            
            if (Loop != 0)
                res += $"{Dmi.DMI_TAB}loop = {Loop}";

            if (Rewind)
                res += $"{Dmi.DMI_TAB}rewind = 1";

            if (Movement)
                res += $"{Dmi.DMI_TAB}movement = 1";

            foreach (var hspot in _hotspots)
            {
                res += hspot.ToSaveableString(Height, (int)Dirs);
            }
            
            return res;
        }
    }
}