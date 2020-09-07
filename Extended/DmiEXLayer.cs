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
        #region properties

        private bool _visible = true;
        public bool Visible
        {
            get => _visible;
            set
            {
                if (value == _visible) return;
                
                _visible = value;
                VisibilityChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler VisibilityChanged;

        private int _index;
        public int Index
        {
            get => _index;
            set
            {
                if (value == _index) return;
                
                _index = value;
                IndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler IndexChanged;

        private Bitmap _bitmap;

        #endregion

        public event EventHandler ImageChanged;
        public event EventHandler Changed;

        
        public DmiEXLayer(Bitmap bitmap, int index)
        {
            _bitmap = bitmap;
            _index = index;
            IndexChanged += OnSomethingChanged;
            VisibilityChanged += OnSomethingChanged;
            ImageChanged += OnSomethingChanged;
        }

        private void OnSomethingChanged(object sender, EventArgs e) => Changed?.Invoke(this, EventArgs.Empty);

        private void OnImageChanged() => ImageChanged?.Invoke(this, EventArgs.Empty);

        public void SetPixel(Point p, Color c)
        {
            if(c == GetPixel(p)) return;
            
            _bitmap.SetPixel(p.X, p.Y, c);
            OnImageChanged();
        }

        public Color GetPixel(Point p) => _bitmap.GetPixel(p.X, p.Y);

        public void Resize(int width, int height)
        {
            MemoryStream imageStream = new MemoryStream();
            ImageFactory imgF = new ImageFactory()
                .Load(_bitmap)
                .Resize(new ResizeLayer(new Size(width, height), ResizeMode.Crop, AnchorPosition.TopLeft))
                .Format(new PngFormat())
                .Save(imageStream);
            
            _bitmap = new Bitmap(imageStream);
            imageStream.Close();
            OnImageChanged();
        }

        public DmiEXState ToDmiExState(Dmi parent, string id)
        {
            return new DmiEXState(parent, id, _bitmap);
        }

        public void OverrideBitmap(Bitmap bm)
        {
            if(bm.Width != _bitmap.Width || bm.Height != _bitmap.Height) throw new ArgumentException("Bitmap does not match Layer size");

            _bitmap = bm;
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

        public object Clone() => new DmiEXLayer((Bitmap) _bitmap.Clone(), _index) {_visible = _visible};
    }
}