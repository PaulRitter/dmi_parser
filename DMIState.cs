using System;

namespace DMI_Parser
{
    public class DMIState
    {
        private string id;
        private int dirs;
        private int frames;
        private float[] delays;

        public DMIState(string id, int dirs, int frames, float[] delays){
            setID(id);
            setDirs(dirs);
            setFrames(frames);
            setDelays(delays);
        }

        public void setID(string id){
            this.id = id;
        }

        public void setDirs(int dirs){
            this.dirs = dirs;
        }

        public void setFrames(int frames){
            if(frames < 1){
                //TODO throw FrameCountInvalidException
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
                //TODO throw FrameCountMismatchException
            }

            if(this.delays.Length != delays.Length){
                //TODO throw DelayCountInvalidException
            }
            this.delays = delays;
        }

        public void setDelay(int index, float delay){
            delays[index] = delay; //will throw IndexOutOfRangeException, indended
        }
    }
}