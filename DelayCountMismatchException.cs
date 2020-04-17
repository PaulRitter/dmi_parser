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

        public DelayCountMismatchException(string message, int delayCountExpected, int delayCountActual)
            : base(message) {
            this.delayCountExpected = delayCountExpected;
            this.delayCountActual = delayCountActual;
        }

        public DelayCountMismatchException(string message, Exception inner, int delayCountExpected, int delayCountActual)
            : base(message, inner) {
            this.delayCountExpected = delayCountExpected;
            this.delayCountActual = delayCountActual;
        }
    }
}
