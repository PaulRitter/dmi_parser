using System;

namespace DMI_Parser
{
    public class InvalidFileException : Exception
    {
        public readonly string filePath;

        public InvalidFileException(string message, string filePath)
            : base(message) {
                this.filePath = filePath;
            }

        public InvalidFileException(string message, Exception inner, string filePath)
            : base(message, inner) {
                this.filePath = filePath;
            }
        
        public override string ToString(){
            return base.ToString() + "\nFilepath: "+filePath;
        }
    }
}