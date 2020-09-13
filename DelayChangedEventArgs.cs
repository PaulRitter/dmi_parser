using System;

namespace DMI_Parser
{
    public class DelayChangedEventArgs : EventArgs
    {
        public readonly int ChangedIndex;

        public DelayChangedEventArgs(int changedIndex)
        {
            ChangedIndex = changedIndex;
        }
    }
}