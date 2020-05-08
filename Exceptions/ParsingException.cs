using System;

namespace DMI_Parser
{
    public abstract class ParsingException : Exception
    {
        public ParsingException(string message) : base(message) { }

        public ParsingException(string message, Exception inner) : base(message, inner) { }
    }
}