using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using DMI_Parser.Raw;
using DMI_Parser.Utils;

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
            idChanged += stateChanged;
            dirCountChanged += stateChanged;
            frameCountChanged += stateChanged;
            loopCountChanged += stateChanged;
            rewindChanged += stateChanged;
        }

        public virtual BitmapImage getImage(int dir, int frame){
            return BitmapUtils.Bitmap2BitmapImage(Images[dir,frame]);
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
            
            this.Dirs = dirs;
            resizeImageArray(Dirs, Frames);
            dirCountChanged?.Invoke(this, null);
        }

        public void setFrames(int frames)
        {
            if (frames == this.Frames) return;
            
            if(frames < 1){
                throw new FrameCountInvalidException("Frame count invalid, only Integers > 1 are allowed", this, frames);
            }

            this.Frames = frames;
            resizeImageArray(Dirs, Frames);
            if(frames > 1)
            {
                if (_delays == null)
                {
                    _delays = new float[frames];
                }
                else
                {
                    float[] oldDelays = _delays;
                    _delays = new float[frames];
                    for (int i = 0; i < _delays.Length && i < oldDelays.Length; i++)
                    {
                        _delays[i] = oldDelays[i];
                    }
                }
            }else{ //we wont have delays with only one frame
                _delays = null;
            }
            frameCountChanged?.Invoke(this, null);
        }

        //used by setDirs and setFrames to resize the image array
        private void resizeImageArray(DirCount dirs, int frames)
        {
            ICloneable[,] oldImages = getOldImagesForArrayResize();
            clearImageArray((int)dirs, frames);
            for (int dir = 0; dir < (int)dirs; dir++)
            {
                ICloneable lastestImage = null;
                for (int frame = 0; frame < frames; frame++)
                {
                    if (dir < oldImages.GetLength(0) && frame < oldImages.GetLength(1))
                    {
                        addImage(dir, frame, oldImages[dir, frame]);
                        lastestImage = oldImages[dir, frame];
                    }
                    else
                    {
                        addImage(dir, frame, lastestImage == null ? Parent.CreateEmptyImage() : lastestImage.Clone());
                    }
                }
            }
        }
        
        protected virtual void clearImageArray(int dirs, int frames) => Images = new Bitmap[(int)dirs,frames];

        protected virtual ICloneable[,] getOldImagesForArrayResize() => Images;
        protected virtual void addImage(int dir, int frame, object img)
        {
            Images[dir, frame] = (Bitmap) img;
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

        public RawDmiState toRaw()
        {
            RawDmiState raw = new RawDmiState();
            
            raw._delays = _delays;
            raw.Dirs = Dirs;
            raw.Frames = Frames;
            raw.Hotspots = new List<RawHotspot>();
            foreach (var hotspot in _hotspots)
            {
                raw.Hotspots.Add(hotspot.ToRawHotspot(Height, (int)Dirs));
            }
            raw.Id = Id;
            raw.Loop = Loop;
            raw.Movement = Movement;
            raw.Rewind = Rewind;
            
            return raw;
        }
        
        public override string ToString()
        {
            string res = $"state = \"{Id}\"";
            res += $"{Dmi.DmiTab}dirs = {Dirs}";
            res += $"{Dmi.DmiTab}frames = {Frames}";

            if (_delays != null)
            {
                string[] delayStrings = new string[_delays.Length];
                for (var i = 0; i < _delays.Length; i++)
                {
                    delayStrings[i] = _delays[i].ToString().Replace(',', '.');
                }
                res += $"{Dmi.DmiTab}delay = {String.Join(",", delayStrings)}";
            }
            
            if (Loop != 0)
                res += $"{Dmi.DmiTab}loop = {Loop}";

            if (Rewind)
                res += $"{Dmi.DmiTab}rewind = 1";

            if (Movement)
                res += $"{Dmi.DmiTab}movement = 1";

            foreach (var hspot in _hotspots)
            {
                res += hspot.ToSaveableString(Height, (int)Dirs);
            }
            
            return res;
        }
    }
}