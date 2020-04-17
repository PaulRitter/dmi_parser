using System;
using System.Collections.Generic;
using System.Text;

namespace DMI_Parser
{
    [Serializable]
    public class FrameCountInvalidException : Exception
    {
        public readonly int frameCount;

        public FrameCountInvalidException(string message, int frameCount)
            : base(message) {
            this.frameCount = frameCount;
        }

        public FrameCountInvalidException(string message, Exception inner, int frameCount)
            : base(message, inner) {
            this.frameCount = frameCount;
        }
    }
}

