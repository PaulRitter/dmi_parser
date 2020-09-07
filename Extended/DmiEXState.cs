using System;
using System.Drawing;
using System.Windows.Media.Imaging;
using DMI_Parser.Raw;

namespace DMI_Parser.Extended
{
    public class DmiEXState : DMIState
    {
        public static DmiEXState FromDmiState(DmiEX parent, DMIState dmiState)
        {
            RawDmiState raw = dmiState.ToRaw();
            
            DmiEXImage[,] images = new DmiEXImage[(int)raw.Dirs.Value,raw.Frames.Value];
            for (int dir = 0; dir < (int)raw.Dirs.Value; dir++)
            {
                for (int frame = 0; frame < raw.Frames.Value; frame++)
                {
                    images[dir, frame] = new DmiEXImage(dmiState.GetBitmap(dir, frame));
                }
            }
            
            return new DmiEXState(parent, images, raw);
        }

        private new DmiEXImage[,] _images;
        
        public DmiEXState(Dmi parent, DmiEXImage[,] images, RawDmiState rawDmiState) : base(parent, null, rawDmiState)
        {
            _images = images;
        }

        public DmiEXState(Dmi parent, string id, Bitmap bitmap) : this(parent, new DmiEXImage[,]{{new DmiEXImage(bitmap)}}, RawDmiState.Default(id))
        {
        }

        public override Bitmap GetBitmap(int dir, int frame)
        {
            return _images[dir, frame].GetBitmap();
        }

        protected override void clearImageArray(int dirs, int frames)
        {
            _images = new DmiEXImage[dirs,frames]; //does not have event call since its used internally only to redo the image array
        }

        public void OverrideImageArray(DmiEXImage[,] array)
        {
            Dirs = (DirCount)array.GetLength(0);
            Frames = array.GetLength(1);

            _images = array;
            OnImageArrayChanged();
        }

        protected override ICloneable[,] GetImages() => _images;

        public override int getImageCount() => _images.Length;

        protected override void SetImage(int dir, int frame, object img)
        {
            _images[dir, frame] = (DmiEXImage) img;
        }

        public DmiEXImage GetImage(int dir, int frame) => _images[dir, frame];

        protected override void ResizeImage(int dir, int frame)
        {
            _images[dir,frame].Resize(Width, Height);
        }
    }
}