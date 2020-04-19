using System;

namespace DMI_Parser
{
    public class InvalidFileException : Exception
    {
        public InvalidFileException(string message)
            : base(message) { }

        public InvalidFileException(string message, Exception inner)
            : base(message, inner) {  }
        
    }
}