using System;
using DMI_Parser.Raw;

namespace DMI_Parser
{
    public class Hotspot
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Dir;
        public readonly int Frame;
        
        public static Hotspot FromRawHotspot(RawHotspot rawHotspot, DirCount dirs)
        {
            int dirCount = (int) dirs;
            return new Hotspot(rawHotspot.X, rawHotspot.Y, rawHotspot.Index % dirCount, rawHotspot.Index / dirCount);
        }
        
        public Hotspot(int x, int y, int dir, int frame)
        {
            this.X = x;
            this.Y = y;
            this.Dir = dir;
            this.Frame = frame;
        }

        public RawHotspot ToRawHotspot(int dirs)
        {
            return new RawHotspot(X, Y, dirs*Frame + Dir + 1);
        }

        public string ToSaveableString(int height, int dirs)
        {
            RawHotspot rawHotspot = ToRawHotspot(dirs);
            return rawHotspot.ToString();
        }
        
        public override string ToString() => $"Hotspot:(Pos({X},{Y}),Img:({Dir},{Frame}))";
        
    }
}