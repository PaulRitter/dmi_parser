using System;
using System.Collections.Generic;
using System.Text;

namespace DMI_Parser
{
    [Serializable]
    public class FrameCountMismatchException : Exception
    {
        public FrameCountMismatchException(string message)
            : base(message) { }

        public FrameCountMismatchException(string message, Exception inner)
            : base(message, inner) { }
    }
}
