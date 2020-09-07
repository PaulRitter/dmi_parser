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
            RawDmiState raw = dmiState.toRaw();
            
            DmiEXImage[,] images = new DmiEXImage[(int)raw.Dirs.Value,raw.Frames.Value];
            for (int dir = 0; dir < (int)raw.Dirs.Value; dir++)
            {
                for (int frame = 0; frame < raw.Frames.Value; frame++)
                {
                    images[dir, frame] = new DmiEXImage(dmiState.Images[dir, frame]);
                }
            }
            
            return new DmiEXState(parent, images, raw);
        }

        public new DmiEXImage[,] Images { get; private set; }


        public DmiEXState(Dmi parent, DmiEXImage[,] images, RawDmiState rawDmiState) : base(parent, null, rawDmiState)
        {
            Images = images;
        }

        public DmiEXState(Dmi parent, string id, Bitmap bitmap) : this(parent, new DmiEXImage[,]{{new DmiEXImage(bitmap)}}, RawDmiState.Default(id))
        {
        }

        public override BitmapImage GetImage(int dir, int frame)
        {
            return Images[dir, frame].GetImage();
        }

        public override Bitmap GetBitmap(int dir, int frame)
        {
            return Images[dir, frame].GetBitmap();
        }

        protected override void clearImageArray(int dirs, int frames)
        {
            Images = new DmiEXImage[dirs,frames]; //does not have event call since its used internally only to redo the image array
        }

        public void OverrideImageArray(DmiEXImage[,] array)
        {
            Dirs = (DirCount)array.GetLength(0);
            Frames = array.GetLength(1);

            Images = array;
            OnImageArrayChanged();
        }

        protected override ICloneable[,] GetImages() => Images;

        public override int getImageCount() => Images.Length;

        protected override void addImage(int dir, int frame, object img)
        {
            Images[dir, frame] = (DmiEXImage) img;
        }

        protected override void resizeImage(int dir, int frame)
        {
            Images[dir,frame].Resize(Width, Height);
        }
    }
}