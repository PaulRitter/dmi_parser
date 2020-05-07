using System;
using System.Drawing;
using System.Collections.Generic;

namespace DMI_Parser
{
    public class DMIState
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int Position;
        public string Id;
        public DirCount Dirs;
        public int Frames;
        private float[] _delays;
        private int _loop; // 0 => infinite
        private bool _rewind;
        private bool _movement;
        private List<Hotspot> _hotspots;
        private string _rawParserData;
        public Bitmap[,] Images; //index is dir + dir*frame
        //indexing this way lets us just walk through the array when saving -> images already in the correct order

        private Point _startOffset;
        private Point _endOffset;

        public DMIState(int width, int height, int position, string id, int dirs, int frames, float[] delays, int loop, bool rewind, bool movement, List<Hotspot> hotspots, string rawParserData, Bitmap full_image, Point img_offset){
            this.Width = width;
            this.Height = height;
            this.Position = position;
            this._rawParserData = rawParserData;
            this._loop = loop;
            this._rewind = rewind;
            this._movement = movement;
            this._hotspots = hotspots; //TODO validate
            this._startOffset = img_offset;
            setID(id);
            setDirs(dirs);

            setFrames(frames);
            if(delays != null){
                if(delays.Length > this._delays.Length){
                    float[] old_delays = delays;
                    delays = new float[this._delays.Length];
                    for (int i = 0; i < delays.Length; i++)
                    {
                        delays[i] = old_delays[i];
                    }
                }
                setDelays(delays);
            }

            cutImages(full_image, img_offset);
        }

        public Point getEndOffset(){
            return _endOffset;
        }


        public Bitmap getImage(int dir, int frame){
            return Images[dir,frame];
        }

        public void cutImages(Bitmap full_image, Point offset){
            int frame = 0;
            int dir = 0;
            Images = new Bitmap[(int)Dirs,Frames];
            int x = offset.X;
            for (int y = offset.Y; y < full_image.Height; y+=Height)
            {
                for (; x < full_image.Width; x+=Width)
                {
                    Images[dir,frame] = cutSingleImage(full_image, new Point(x,y));
                    dir++;
                    if(dir == (int)Dirs){
                        dir = 0;
                        frame++;
                    }
                    if(frame == Frames){
                        //we are done with our segment
                        int endwidth = x + Width;
                        int endheight = y;
                        if(endwidth >= full_image.Width){
                            endwidth = 0;
                            endheight += Height;
                        }
                        _endOffset = new Point(endwidth, endheight);
                        return;
                    }
                }
                x = 0;
            }
        }

        public Bitmap cutSingleImage(Bitmap full_image, Point offset){
            Rectangle source_rect = new Rectangle(offset.X, offset.Y, Width, Height);
            Rectangle dest_rect = new Rectangle(0, 0, Width, Height);

            Bitmap res;
            try{
                res = new Bitmap(Width, Height);
            }catch(Exception e){
                Console.WriteLine($"{Width},{Height}");
                throw e;
            }
            
            using(var g = Graphics.FromImage(res)){
                g.DrawImage(full_image, dest_rect, source_rect, GraphicsUnit.Pixel);
                return res;
            }
        }

        public void setID(string id){
            this.Id = id;
        }

        public void setDirs(int dirs){
            this.Dirs = (DirCount)dirs;
        }

        public void setFrames(int frames){
            if(frames < 1){
                throw new FrameCountInvalidException("Frame count invalid, only Integers > 1 are allowed", this, frames);
            }

            this.Frames = frames;
            if(frames > 1){
                _delays = new float[frames];
            }else{ //we wont have delays with only one frame
                _delays = null;
            }
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
            _delays[index] = delay; //will throw IndexOutOfRangeException if index invalid, indended
        }

        public override string ToString(){
            string res = "["+Id+"]\nDirs: "+Dirs+"\nFrames: "+Frames+"\n"; 
            
            res += "Loop: ";
            if(_loop == 0){
                res += "indefinitely\n";
            }else{
                res += _loop+"\n";
            }
            
            res += "Delays: ";
            if(_delays != null){
                res += string.Join(" - ", _delays)+"\n";
            }else{
                res += "none\n";
            }

            res += "Rewind: "+_rewind.ToString()+"\n";
            
            res += "Movement: "+_movement.ToString()+"\n";

            res += "Hotspots:\n";
            foreach (var item in _hotspots)
            {
                res += " - "+item+"\n";
            }

            res += $"Images: {Images.Length}\n";

            res += "Raw Data:\n"+_rawParserData;
            return res;
        }
    }
}