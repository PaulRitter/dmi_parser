namespace DMI_Parser.Raw
{
    public struct RawHotspot
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Index;

        public RawHotspot(int x, int y, int index)
        {
            this.X = x;
            this.Y = y;
            this.Index = index;
        }

        public bool isInBounds(int width, int height)
        {
            if (X < 0 || X > width) return false;
            if (Y < 0 || Y > height) return false;
            return true;
        }

        public override string ToString()
        {
            return $"{Dmi.DmiTab}hotspot = {X},{Y},{Index}";
        }
    }
}