using System;

namespace DMI_Parser
{
    [System.Serializable]
    public class UnknownKeywordException : ParsingException
    {
        public readonly string keyword;
        public readonly string value;

        public readonly string laststate;

        public UnknownKeywordException(string message, string laststate, string keyword, string value) : base(message) {
            this.laststate = laststate;
            this.keyword = keyword;
            this.value = value;
        }
        public UnknownKeywordException(string message, Exception inner, string laststate, string keyword, string value) : base(message, inner) {
            this.laststate = laststate;
            this.keyword = keyword;
            this.value = value;
        }

        public override string ToString(){
            return base.ToString() + "\nLast state: "+laststate+"\nKeyword: "+keyword+"\nValue: "+value;
        }
    }
}