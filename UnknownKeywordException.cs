using System;

namespace DMI_Parser
{
    [System.Serializable]
    public class UnknownKeywordException : Exception
    {
        public readonly string keyword;
        public readonly string value;

        public UnknownKeywordException(string message, string keyword, string value) : base(message) {
            this.keyword = keyword;
            this.value = value;
        }
        public UnknownKeywordException(string message, Exception inner, string keyword, string value) : base(message, inner) {
            this.keyword = keyword;
            this.value = value;
        }

    }
}