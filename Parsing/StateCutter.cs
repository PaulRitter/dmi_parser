using System;
using System.Drawing;

namespace DMI_Parser.Parsing
{
    public class StateCutter
    {
        private readonly Bitmap _fullImage;
        private readonly int _height;
        private readonly int _width;
        private Point _lastOffset = Point.Empty;

        public StateCutter(Bitmap fullImage, int height, int width)
        {
            _height = height;
            _width = width;
            _fullImage = fullImage;
        }

        public Bitmap[,] CutImages(DirCount dirCount, int frames)
        {
            int frame = 0;
            int dir = 0;
            int dirs = (int) dirCount;
            Bitmap[,] images = new Bitmap[(int) dirs, frames];
            int x = _lastOffset.X;
            for (int y = _lastOffset.Y; y < _fullImage.Height; y += _height)
            {
                for (; x < _fullImage.Width; x += _width)
                {
                    images[dir, frame] = CutSingleImage(new Point(x, y));
                    dir++;
                    if (dir == dirs)
                    {
                        dir = 0;
                        frame++;
                    }

                    if (frame == frames)
                    {
                        //we are done with our segment
                        int endwidth = x + _width;
                        int endheight = y;
                        if (endwidth >= _fullImage.Width)
                        {
                            endwidth = 0;
                            endheight += _height;
                        }

                        _lastOffset = new Point(endwidth, endheight);
                        return images;
                    }
                }
                x = 0;
            }

            throw new ParsingException("Out of bounds on image while cutting"); //todo add details
        }
        private Bitmap CutSingleImage(Point offset){
            Rectangle sourceRect = new Rectangle(offset.X, offset.Y, _width, _height);
            Rectangle destRect = new Rectangle(0, 0, _width, _height);

            Bitmap res;
            try{
                res = new Bitmap(_width, _height);
            }catch(Exception){
                Console.WriteLine($"{_width},{_height}");
                throw;
            }
            
            using(var g = Graphics.FromImage(res)){
                g.DrawImage(_fullImage, destRect, sourceRect, GraphicsUnit.Pixel);
                return res;
            }
        }
    }

    

}