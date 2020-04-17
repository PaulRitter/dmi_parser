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
                throw new DelayCountMismatchException("Delaycount doesn't match", this.delays.Length, delays.length);
            }
            this.delays = delays;
        }

        public void setDelay(int index, float delay){
            delays[index] = delay; //will throw IndexOutOfRangeException, indended
        }
    }
}