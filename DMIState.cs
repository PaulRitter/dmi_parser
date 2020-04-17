using System;

namespace DMI_Parser
{
    public class DMIState
    {
        private string id;
        private int dirs;
        private int frames;
        private float[] delays;
        private int loop; // 0 => infinite
        private bool rewind;

        public DMIState(string id, int dirs, int frames, float[] delays, int loop, bool rewind){
            setID(id);
            setDirs(dirs);
            setFrames(frames);
            if(delays != null)
                setDelays(delays);
            this.loop = loop;
            this.rewind = rewind;
        }

        public void setID(string id){
            this.id = id;
        }

        public void setDirs(int dirs){
            this.dirs = dirs;
        }

        public void setFrames(int frames){
            if(frames < 1){
                throw new FrameCountInvalidException("Frame count invalid, only Integers > 1 are allowed", frames);
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
                throw new FrameCountMismatchException("Only one Frame cannot allow delays");
            }

            if(this.delays.Length != delays.Length){
                throw new DelayCountMismatchException("Delaycount doesn't match", this.delays.Length, delays.Length);
            }
            this.delays = delays;
        }

        public void setDelay(int index, float delay){
            delays[index] = delay; //will throw IndexOutOfRangeException if index invalid, indended
        }

        public override string ToString(){
            string res = "["+id+"]\nDirs: "+dirs+"\nFrames: "+frames+"\nLoop: "; 
            if(loop == 0){
                res += "indefinitely\n";
            }else{
                res += loop+"\n";
            }
            if(delays != null){
                res += "Delays: "+string.Join(" - ", delays)+"\n";
            }
            if(rewind){
                res += "Rewind: true\n";
            }
            return res;
        }
    }
}