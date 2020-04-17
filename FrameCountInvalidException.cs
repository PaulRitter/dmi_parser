using System;
using System.Collections.Generic;
using System.Text;

namespace DMI_Parser
{
    [Serializable]
    public class FrameCountInvalidException : Exception
    {
        public readonly int frameCount;

        public readonly DMIState sourceState;

        public FrameCountInvalidException(string message, DMIState sourceState, int frameCount)
            : base(message) {
            this.frameCount = frameCount;
            this.sourceState = sourceState;
        }

        public FrameCountInvalidException(string message, Exception inner, DMIState sourceState, int frameCount)
            : base(message, inner) {
            this.frameCount = frameCount;
            this.sourceState = sourceState;
        }

        public override string ToString(){
            return base.ToString() + "\nNewFrames: "+ frameCount + "\n" + sourceState;
        }
    }
}

