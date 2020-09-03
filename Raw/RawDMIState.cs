using System.Collections.Generic;

#nullable enable
namespace DMI_Parser.Raw
{
    public class RawDmiState
    {
        public string Id;
        public DirCount? Dirs;
        public int? Frames;
        public float[] _delays;
        public int Loop = 0; // 0 => infinite
        public bool Rewind = false;
        public bool Movement = false;
        public List<RawHotspot> Hotspots = new List<RawHotspot>();

        public bool isValid()
        {
            if (Id == null) return false;

            if (Dirs == null) return false;

            if (Frames == null) return false;

            return true;
        }

        public override string ToString()
        {
            //TODO tostring
            return "TODO RawDmiState.ToString()";
        }
        
        public static RawDmiState Default => new RawDmiState
        {
            Dirs = DirCount.SINGLE,
            Frames = 1,
        };
    }
}