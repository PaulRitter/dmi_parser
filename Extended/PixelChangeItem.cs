using System.Drawing;

namespace DMI_Parser.Extended
{
    public struct PixelChangeItem
    {
        public readonly Point Point;
        public readonly Color Color;

        public PixelChangeItem(Point point, Color color)
        {
            Point = point;
            Color = color;
        }
    }
}