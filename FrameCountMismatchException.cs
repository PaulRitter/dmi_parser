using System;
using System.Collections.Generic;
using System.Text;

namespace DMI_Parser
{
    [Serializable]
    public class FrameCountMismatchException : Exception
    {
        public readonly DMIState sourceState;

        public FrameCountMismatchException(string message, DMIState sourceState)
            : base(message) {
                this.sourceState = sourceState;
            }

        public FrameCountMismatchException(string message, Exception inner, DMIState sourceState)
            : base(message, inner) {
                this.sourceState = sourceState;
            }

        public override string ToString(){
            return base.ToString() + "\n" + sourceState;
        }
    }
}
