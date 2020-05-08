using System;

namespace DMI_Parser
{
    public class StateArgumentDuplicateException : ParsingException
    {
        public readonly string ArgumentId;

        public StateArgumentDuplicateException(string message, string argumentId)
            : base(message) {
                this.ArgumentId = argumentId;
            }

        public StateArgumentDuplicateException(string message, Exception inner, string argumentId)
            : base(message, inner) {
                this.ArgumentId = argumentId;
            }
        
        public override string ToString(){
            return base.ToString() + "\nArgumentID: "+ArgumentId;
        }

    }
}