using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using DMI_Parser.Raw;
using DMI_Parser.Utils;
using ImageProcessor;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Formats;

namespace DMI_Parser
{
    public class DMIState
    {
        public readonly Dmi Parent;
        public int Width => Parent.Width;
        public int Height => Parent.Height;

        #region properties
        private string _id;
        public string Id
        {
            get => _id;
            set
            {
                if (value == _id) return;
                
                _id = value;
                IdChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler IdChanged;

        private DirCount _dirs;
        public DirCount Dirs
        {
            get => _dirs;
            set
            {
                if (value == _dirs) return;
            
                _dirs = value;
                ResizeImageArray(Dirs, Frames);
                DirCountChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler DirCountChanged;

        private int _frames;
        public int Frames
        {
            get => _frames;
            set
            {
                if (value == _frames) return;
            
                if(value < 1){
                    throw new FrameCountInvalidException("Frame count invalid, only Integers > 1 are allowed", this, value);
                }

                _frames = value;
                if(_frames > 1)
                {
                    double[] newDelays;
                    if (Delays == null)
                    {
                        newDelays = new double[_frames]; //todo whats the default delay value?
                    }
                    else
                    {
                        var oldDelays = Delays;
                        newDelays = new double[_frames];
                        for (var i = 0; i < newDelays.Length && i < oldDelays.Length; i++)
                        {
                            newDelays[i] = oldDelays[i];
                        }
                    }
                    _delays = newDelays;
                }else{ //we wont have delays with only one frame
                    _delays = null;
                }
                
                ResizeImageArray(Dirs, Frames);
                FrameCountChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler FrameCountChanged;
        
        private double[] _delays;
        public double[] Delays
        {
            get => _delays; //todo does this mean you could edit it from the outside? investigate
            private set
            {
                if (value == _delays) return;
                
                if((value != null) && Frames == 1){
                    throw new FrameCountMismatchException("Only one Frame cannot allow delays", this);
                }

                if((value == null && value != _delays) || value?.Length != _delays?.Length){
                    throw new DelayCountMismatchException("Delaycount doesn't match", this, value?.Length ?? 0, _delays?.Length ?? 0);
                }
                
                _delays = value;
                DelayListChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler DelayListChanged;
        public void SetDelay(int index, double delay)
        {
            if (_delays[index].Equals(delay)) return;
            
            _delays[index] = delay;
            DelayChanged?.Invoke(this, new DelayChangedEventArgs(index));
        }

        public event EventHandler<DelayChangedEventArgs> DelayChanged;

        private int _loop; // 0 => infinite
        public int Loop
        {
            get => _loop;
            set
            {
                if (value == _loop) return;

                if (value < 0) throw new ArgumentException("Loopcount cannot be < 0");
                
                _loop = value;
                LoopCountChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler LoopCountChanged;

        private bool _rewind;
        public bool Rewind
        {
            get => _rewind;
            set
            {
                if (value == _rewind) return;

                _rewind = value;
                RewindChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler RewindChanged;

        private bool _movement;
        public bool Movement
        {
            get => _movement;
            set
            {
                if (value == _movement) return;

                _movement = value;
                MovementChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler MovementChanged;
        
        private List<Hotspot> _hotspots; //todo hotspots
        public EventHandler HotspotListChanged;

        public Hotspot GetHotspot(int frameIndex, int dirIndex) =>
            _hotspots.Find(hotspot => hotspot.Frame == frameIndex && hotspot.Dir == dirIndex);

        public Hotspot AddHotspot(int x, int y, int frameIndex, int dirIndex)
        {
            Hotspot hotspotToChange = _hotspots.Find(hotspot => hotspot.Frame == frameIndex && hotspot.Dir == dirIndex);
            if (hotspotToChange != null)
            {
                _hotspots.Remove(hotspotToChange);
            }

            Hotspot hotspot = new Hotspot(x, y, dirIndex, frameIndex); 
            _hotspots.Add(hotspot);
            HotspotListChanged?.Invoke(this, EventArgs.Empty);
            return hotspot;
        }

        public void RemoveHotspot(int frameIndex, int dirIndex)
        {
            Hotspot hotspot = _hotspots.Find(h => h.Frame == frameIndex && h.Dir == dirIndex);
            if(hotspot == null) throw new ArgumentException("No Hotspot found with that index");

            RemoveHotspot(hotspot);
        }

        public void RemoveHotspot(Hotspot hotspot)
        {
            _hotspots.Remove(hotspot);
            HotspotListChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        private Bitmap[,] _images; //index is dir, frame
        
        public event EventHandler StateChanged;
        public event EventHandler ImageArrayChanged;

        /*todo
        public DMIState(Dmi parent)
        {
            
        }
        */
        
        public DMIState(Dmi parent, Bitmap[,] images, RawDmiState rawDmiState)
        {
            //can set all these without validation
            Parent = parent;
            _loop = rawDmiState.Loop;
            _rewind = rawDmiState.Rewind;
            _movement = rawDmiState.Movement;
            _id = rawDmiState.Id;
            _images = images;

            //validating and adding hotspots
            _hotspots = new List<Hotspot>();
            List<int> alreadyRegisteredIndexes = new List<int>();
            foreach (var hspot in rawDmiState.Hotspots)
            {
                if (hspot.isInBounds(parent.Width, parent.Height) && !alreadyRegisteredIndexes.Contains(hspot.Index))
                {
                    _hotspots.Add(Hotspot.FromRawHotspot(hspot, rawDmiState.Dirs.Value));
                    alreadyRegisteredIndexes.Add(hspot.Index);
                }
                else
                {
                    //todo [logging] warning
                }
            }
            
            // validate dir and framecount with delays and picturearray
            // if frames, dirs & delays mismatch image, adjust them to the array
            if (_images != null && (!rawDmiState.Dirs.HasValue || _images.GetLength(0) != (int)rawDmiState.Dirs.Value))
            {
                //todo [logging] warning
                _dirs = (DirCount)_images.GetLength(0);
            }
            else
            {
                _dirs = rawDmiState.Dirs.Value;
            }
        
            if (_images != null && (!rawDmiState.Frames.HasValue || _images.GetLength(1) != (int)rawDmiState.Frames.Value))
            {
                //todo [logging] warning
                _frames = _images.GetLength(1);
            }
            else
            {
                _frames = rawDmiState.Frames.Value;
            }

            if (_images != null && (rawDmiState._delays == null && Frames != 1 ) || (rawDmiState._delays != null && rawDmiState._delays.Length != Frames))
            {
                //todo [logging] warning
            
                double[] new_delays = new double[Frames];
                for (int i = 0; i < new_delays.Length && i < rawDmiState._delays?.Length; i++)
                {
                    new_delays[i] = rawDmiState._delays[i];
                }

                _delays = new_delays;
            }
            else
            {
                _delays = rawDmiState._delays;
            }
            

            //subscribing our generic event to all specific ones
            IdChanged += OnAnyChange;
            DirCountChanged += OnAnyChange;
            FrameCountChanged += OnAnyChange;
            DelayListChanged += OnAnyChange;
            LoopCountChanged += OnAnyChange;
            RewindChanged += OnAnyChange;
            MovementChanged += OnAnyChange;
            ImageArrayChanged += OnAnyChange;
        }

        protected void OnAnyChange(object sender, EventArgs e) => StateChanged?.Invoke(sender, e);

        public virtual Bitmap GetBitmap(int dir, int frame)
        {
            return (Bitmap)_images[dir, frame].Clone();
        }
        
        //used by dir setter and frame setter to resize the image array
        protected virtual void ResizeImageArray(DirCount dirs, int frames)
        {
            ICloneable[,] oldImages = GetImages();
            clearImageArray((int)dirs, frames);
            for (int dir = 0; dir < (int)dirs; dir++)
            {
                ICloneable latestImage = null;
                for (int frame = 0; frame < frames; frame++)
                {
                    if (dir < oldImages.GetLength(0) && frame < oldImages.GetLength(1))
                    {
                        SetImage(dir, frame, oldImages[dir, frame]);
                        latestImage = oldImages[dir, frame];
                    }
                    else
                    {
                        SetImage(dir, frame, latestImage == null ? Parent.CreateEmptyImage() : latestImage.Clone());
                    }
                }
            }
            OnImageArrayChanged();
        }
        
        protected void OnImageArrayChanged()
        {
            ImageArrayChanged?.Invoke(this,EventArgs.Empty);
        }

        public void resizeImages(object sender, EventArgs e) => resizeImages();

        //resizes all images using the parents width/height
        public void resizeImages()
        {
            ICloneable[,] images = GetImages();
            for (int dir = 0; dir < images.GetLength(0); dir++)
            {
                for (int frame = 0; frame < images.GetLength(1); frame++)
                {
                    ResizeImage(dir, frame);
                }
            }
        }

        protected virtual void ResizeImage(int dir, int frame)
        {
            _images[dir, frame] = _images[dir, frame].Resized(Parent.Width, Parent.Height);
        }

        protected virtual void clearImageArray(int dirs, int frames) => _images = new Bitmap[dirs,frames];

        protected virtual ICloneable[,] GetImages() => _images;
        protected virtual void SetImage(int dir, int frame, object img)
        {
            _images[dir, frame] = (Bitmap) img;
        }

        public virtual int getImageCount() => _images.Length;

        public RawDmiState ToRaw()
        {
            RawDmiState raw = new RawDmiState();
            
            raw._delays = _delays;
            raw.Dirs = Dirs;
            raw.Frames = Frames;
            raw.Hotspots = new List<RawHotspot>();
            foreach (var hotspot in _hotspots)
            {
                raw.Hotspots.Add(hotspot.ToRawHotspot((int)Dirs));
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
            res += $"\n{Dmi.DmiTab}dirs = {(int)Dirs}";
            res += $"\n{Dmi.DmiTab}frames = {Frames}";

            if (_delays != null)
            {
                string[] delayStrings = new string[_delays.Length];
                for (var i = 0; i < _delays.Length; i++)
                {
                    delayStrings[i] = _delays[i].ToString().Replace(',', '.');
                }
                res += $"\n{Dmi.DmiTab}delay = {String.Join(",", delayStrings)}";
            }
            
            if (Loop != 0)
                res += $"\n{Dmi.DmiTab}loop = {Loop}";

            if (Rewind)
                res += $"\n{Dmi.DmiTab}rewind = 1";

            if (Movement)
                res += $"\n{Dmi.DmiTab}movement = 1";

            foreach (var hspot in _hotspots)
            {
                res += "\n"+hspot.ToSaveableString(Height, (int)Dirs);
            }
            
            return res;
        }
    }
}