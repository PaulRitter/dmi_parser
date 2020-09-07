using System;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using DMI_Parser.Utils;
using ImageProcessor;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Formats;

namespace DMI_Parser.Extended
{
    public class DmiEXLayer : IComparable, ICloneable
    {
        public Bitmap Bitmap { get; private set; }

        private int _index;
        private bool _visible = true;

        public event EventHandler IndexChanged;
        public event EventHandler VisibilityChanged;
        public event EventHandler ImageChanged;
        public event EventHandler Changed;

        public bool Visible
        {
            get => _visible;
            set
            {
                _visible = value;
                VisibilityChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                IndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public DmiEXLayer(Bitmap bitmap, int index)
        {
            Bitmap = bitmap;
            _index = index;
            IndexChanged += InvokeSomethingChanged;
            VisibilityChanged += InvokeSomethingChanged;
            ImageChanged += InvokeSomethingChanged;
        }

        private void InvokeSomethingChanged(object sender, EventArgs e) => Changed?.Invoke(this, EventArgs.Empty);

        private void OnImageChanged()
        {
            ImageChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetPixel(Point p, Color c)
        {
            if(c == GetPixel(p)) return;
            Bitmap.SetPixel(p.X, p.Y, c);
            _bufferedImage = null;
            OnImageChanged();
        }

        public Color GetPixel(Point p) => Bitmap.GetPixel(p.X, p.Y);

        private BitmapImage _bufferedImage;
        public BitmapImage GetImage()
        {
            return _bufferedImage ??= BitmapUtils.Bitmap2BitmapImage(Bitmap);
        }

        public void Resize(int width, int height)
        {
            MemoryStream imageStream = new MemoryStream();
            ImageFactory imgF = new ImageFactory()
                .Load(Bitmap)
                .Resize(new ResizeLayer(new Size(width, height), ResizeMode.Crop, AnchorPosition.TopLeft))
                .Format(new PngFormat())
                .Save(imageStream);
            
            Bitmap newBitmap = new Bitmap(imageStream);
            imageStream.Close();
            Bitmap = newBitmap;
            OnImageChanged();
        }

        public DmiEXState ToDmiExState(Dmi parent, string id)
        {
            return new DmiEXState(parent, id, Bitmap);
        }

        public void OverrideBitmap(Bitmap bm)
        {
            if(bm.Width != Bitmap.Width || bm.Height != Bitmap.Height) throw new ArgumentException("Bitmap does not match Layer size");

            Bitmap = bm;
            OnImageChanged();
        }

#nullable enable
        public int CompareTo(object? obj)
        {
            if (obj == null) return 1;

            if (obj is DmiEXLayer otherLayer)
            {
                if (otherLayer.Index < this.Index)
                {
                    return -1;
                }
                
                if (otherLayer.Index > this.Index)
                {
                    return 1;
                }

                return 0;
            }
            else
                throw new ArgumentException("Object is not a Layer");
        }

        public object Clone() => new DmiEXLayer((Bitmap) Bitmap.Clone(), _index) {_visible = _visible};
    }
}