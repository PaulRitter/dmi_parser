using System;

namespace DMI_Parser
{
    public class StateArgumentDuplicateException : Exception
    {
        public readonly string argumentID;

        public StateArgumentDuplicateException(string message, string argumentID)
            : base(message) {
                this.argumentID = argumentID;
            }

        public StateArgumentDuplicateException(string message, Exception inner, string argumentID)
            : base(message, inner) {
                this.argumentID = argumentID;
            }
        
        public override string ToString(){
            return base.ToString() + "\nArgumentID: "+argumentID;
        }

    }
}