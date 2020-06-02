using System;
using DMI_Parser.Raw;

namespace DMI_Parser
{
    public class InvalidStateException : ParsingException
    {
        public readonly RawDmiState rawState;

        public InvalidStateException(string message, RawDmiState rawState)
            : base(message)
        {
            this.rawState = rawState;
        }

        public InvalidStateException(string message, Exception inner, RawDmiState rawState)
            : base(message, inner)
        {
            this.rawState = rawState;
        }

        public override string ToString()
        {
            return base.ToString() + "\r\n"+rawState.ToString();
        }
    }
}