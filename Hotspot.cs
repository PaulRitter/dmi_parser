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
            this.X = x; // 18 -> 19
            this.Y = y; // height - y
            this.Dir = dir;
            this.Frame = frame;
        }

        public RawHotspot ToRawHotspot(int height, int dirs)
        {
            //why
            // actualY = height - savedY
            int savedY = height - Y;
            // actualX = savedX + 1
            int savedX = X - 1;

            return new RawHotspot(savedX, savedY, dirs*Frame + Dir);
        }

        public string ToSaveableString(int height, int dirs)
        {
            RawHotspot rawHotspot = ToRawHotspot(height, dirs);
            return rawHotspot.ToString();
        }
        
        public override string ToString() => $"Hotspot:(Pos({X},{Y}),Img:({Dir},{Frame}))";
        
    }
}