using System;
using System.Drawing;
using System.Collections.Generic;

namespace DMI_Parser
{
    public class DMIState
    {
        private int width;
        private int height;
        private int position;
        private string id;
        private int dirs;
        private int frames;
        private float[] delays;
        private int loop; // 0 => infinite
        private bool rewind;
        private bool movement;
        private List<Hotspot> hotspots;
        private string rawParserData;
        private Bitmap[] images; //index is dir + dir*frame
        //indexing this way lets us just walk through the array when saving -> images already in the correct order
        private Point start_offset;
        private Point end_offset;

        public DMIState(int width, int height, int position, string id, int dirs, int frames, float[] delays, int loop, bool rewind, bool movement, List<Hotspot> hotspots, string rawParserData, Bitmap full_image, Point img_offset){
            this.width = width;
            this.height = height;
            this.position = position;
            this.rawParserData = rawParserData;
            this.loop = loop;
            this.rewind = rewind;
            this.movement = movement;
            this.hotspots = hotspots; //TODO validate
            this.start_offset = img_offset;
            setID(id);
            setDirs(dirs);

            setFrames(frames);
            if(delays != null){
                if(delays.Length > this.delays.Length){
                    float[] old_delays = delays;
                    delays = new float[this.delays.Length];
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
            return end_offset;
        }


        public Bitmap getImage(int dir, int frame){
            return images[dir + dirs * frame];
        }

        public void cutImages(Bitmap full_image, Point offset){
            int frame = 0;
            int dir = 0;
            images = new Bitmap[dirs*frames];
            for (int y = offset.Y; y < full_image.Height; y+=height)
            {
                for (int x = offset.X; x < full_image.Width; x+=width)
                {
                    int i = dir + frame * dirs;
                    images[i] = cutSingleImage(full_image, new Point(x,y));
                    dir++;
                    if(dir == dirs){
                        dir = 0;
                        frame++;
                    }
                    if(frame == frames){
                        //we are done with our segment
                        end_offset = new Point(x + width, y + height);
                        return;
                    }
                }
            }
        }

        public Bitmap cutSingleImage(Bitmap full_image, Point offset){
            Bitmap res = new Bitmap(width, height);
            using(var graphic = Graphics.FromImage(res)){
                graphic.DrawImage(full_image, offset.X, offset.Y, width, height);
                return res;
            }
        }

    

        public void setID(string id){
            this.id = id;
        }

        public void setDirs(int dirs){
            this.dirs = dirs;
        }

        public void setFrames(int frames){
            if(frames < 1){
                throw new FrameCountInvalidException("Frame count invalid, only Integers > 1 are allowed", this, frames);
            }

            this.frames = frames;
            if(frames > 1){
                delays = new float[frames];
            }else{ //we wont have delays with only one frame
                delays = null;
            }
        }

        public void setDelays(float[] delays){
            if((this.delays == null) && frames == 1){
                throw new FrameCountMismatchException("Only one Frame cannot allow delays", this);
            }

            if(this.delays.Length != delays.Length){
                throw new DelayCountMismatchException("Delaycount doesn't match", this, this.delays.Length, delays.Length);
            }
            this.delays = delays;
        }

        public void setDelay(int index, float delay){
            delays[index] = delay; //will throw IndexOutOfRangeException if index invalid, indended
        }

        public override string ToString(){
            string res = "["+id+"]\nDirs: "+dirs+"\nFrames: "+frames+"\n"; 
            
            res += "Loop: ";
            if(loop == 0){
                res += "indefinitely\n";
            }else{
                res += loop+"\n";
            }
            
            res += "Delays: ";
            if(delays != null){
                res += string.Join(" - ", delays)+"\n";
            }else{
                res += "none\n";
            }

            res += "Rewind: "+rewind.ToString()+"\n";
            
            res += "Movement: "+movement.ToString()+"\n";

            res += "Hotspots:\n";
            foreach (var item in hotspots)
            {
                res += " - "+item+"\n";
            }

            res += "Raw Data:\n"+rawParserData;
            return res;
        }
    }
}