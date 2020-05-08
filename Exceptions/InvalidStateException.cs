using System;

namespace DMI_Parser
{
    public class InvalidStateException : ParsingException
    {
        public readonly string stateID;
        public readonly int dirs;
        public readonly int frames;
        public readonly float[] delay;

        public InvalidStateException(string message, string stateID, int dirs, int frames, float[] delay)
            : base(message) {
                this.stateID = stateID;
                this.dirs = dirs;
                this.frames = frames;
                this.delay = delay;
            }

        public InvalidStateException(string message, Exception inner, string stateID, int dirs, int frames, float[] delay)
            : base(message, inner) { 
                this.stateID = stateID;
                this.dirs = dirs;
                this.frames = frames;
                this.delay = delay;
            }
    }
}