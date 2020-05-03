using System;
namespace DMI_Parser
{
    public class StateArgumentValueInvalidException<T> : Exception
    {
        public readonly string argumentName;
        public readonly T value;

        public readonly DMIState sourceState;

        public StateArgumentValueInvalidException(string message, string argumentName, T value)
            : base(message) {
            this.argumentName = argumentName;
            this.value = value;
        }

        public StateArgumentValueInvalidException(string message, Exception inner,  string argumentName, T value)
            : base(message, inner) {
            this.argumentName = argumentName;
            this.value = value;
        }

        public override string ToString(){
            return base.ToString() + $"\nArgument: {argumentName} -> {value}";
        }
    }
}