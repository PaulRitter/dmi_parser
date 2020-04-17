using System;
using System.Collections.Generic;
using System.Text;

namespace DMI_Parser
{
    [Serializable]
    public class DelayCountMismatchException : Exception
    {
        public readonly int delayCountExpected;
        public readonly int delayCountActual;
        public readonly DMIState sourceState;

        public DelayCountMismatchException(string message, DMIState sourceState, int delayCountExpected, int delayCountActual)
            : base(message) {
            this.delayCountExpected = delayCountExpected;
            this.delayCountActual = delayCountActual;
            this.sourceState = sourceState;
        }

        public DelayCountMismatchException(string message, Exception inner, DMIState sourceState, int delayCountExpected, int delayCountActual)
            : base(message, inner) {
            this.delayCountExpected = delayCountExpected;
            this.delayCountActual = delayCountActual;
            this.sourceState = sourceState;
        }

        public override string ToString(){
            return base.ToString() + "\nExpected: "+delayCountExpected+"\nActual: "+delayCountActual+"\n" + sourceState;
        }
    }
}
