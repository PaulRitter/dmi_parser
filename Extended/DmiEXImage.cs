using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using DMI_Parser.Utils;
using ImageProcessor;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Formats;

namespace DMI_Parser.Extended
{
    public class DmiEXImage : ICloneable
    {
        private List<DmiEXLayer> _layers = new List<DmiEXLayer>();
        public int Width { get; private set; }
        public int Height { get; private set; }
        public event EventHandler ImageChanged;
        public event EventHandler LayerListChanged;
        
        public DmiEXImage(int width, int height) : this(new Bitmap(width,height)) {}

        public DmiEXImage(Bitmap bm)
        {
            Width = bm.Width;
            Height = bm.Height;
            AddLayer(new DmiEXLayer(bm, 0));
            LayerListChanged += OnImageChanged;
        }
        
        private void OnImageChanged(object sender = null, EventArgs e = null) => ImageChanged?.Invoke(this, EventArgs.Empty); 

        public DmiEXLayer AddLayer(DmiEXLayer l)
        {
            if(_layers.Contains(l)) throw new ArgumentException("Layer already part of image");
            
            ClearIndex(l.Index);
            _layers.Add(l);
            SortLayers();
            l.IndexChanged += SortLayers;

            l.Changed += OnImageChanged; //any change on the layer means a change on the image
            LayerListChanged?.Invoke(this, EventArgs.Empty);
            return l;
        }
        
        public DmiEXLayer AddLayer(int index) => AddLayer(new DmiEXLayer(new Bitmap(Width, Height), index));

        public void RemoveLayer(DmiEXLayer l)
        {
            if(_layers.Count == 1) throw new WarningException("You can't remove the only Layer of the image");

            l.IndexChanged -= SortLayers;
            l.Changed -= OnImageChanged;
            _layers.Remove(l);
            LayerListChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveLayer(int index) => RemoveLayer(GetLayerByIndex(index));
        
        public void SetLayerIndex(DmiEXLayer layer, int index)
        {
            ClearIndex(index);
            layer.Index = index;
        }

        private void ClearIndex(int index)
        {
            DmiEXLayer[] layers = _layers.ToArray();
            for (int i = 0; i < layers.Length; i++)
            {
                DmiEXLayer layer = layers[i];
                if (layer.Index != index) continue;
                layer.Index++;
                index++;
            }
        }

        private void SortLayers(object sender = null, EventArgs e = null)
            => _layers.Sort((l1,l2)=>l1.Index.CompareTo(l2.Index));

        public DmiEXLayer[] GetLayers() => _layers.ToArray();

        public DmiEXLayer GetLayerByIndex(int index)
        {
            DmiEXLayer layer = _layers.Find((l) => l.Index == index);
            if(layer == null) throw new ArgumentException("No Layer with that Index exists");
            return layer;
        }

        public Bitmap GetBitmap()
        {
            MemoryStream memoryStream = new MemoryStream();
            
            ImageFactory imgF = new ImageFactory()
                .Load(new Bitmap(Width, Height));

            SortLayers(); //better safe than sorry, probably not needed, todo [logging] check if this changes anything at anytime, then provide warning
            for (int i = 0; i < _layers.Count; i++)
            {
                DmiEXLayer dmiExLayer = _layers[i];
                if (!dmiExLayer.Visible) continue;

                ImageLayer l = new ImageLayer { Image = dmiExLayer.GetBitmap() };
                imgF.Overlay(l);
            }

            imgF.Resolution(Width, Height)
                .Format(new PngFormat())
                .BackgroundColor(Color.Transparent)
                .Save(memoryStream);

            return new Bitmap(memoryStream);
        }
        
        public void Resize(int width, int height)
        {
            Width = width;
            Height = height;
            foreach (var layer in _layers)
            {
                layer.Resize(width,height);
            }
            OnImageChanged();
        }
        
        public object Clone()
        {
            DmiEXImage image = new DmiEXImage(Width, Height) { _layers = new List<DmiEXLayer>() };
            foreach (var layer in _layers)
            {
                image.AddLayer((DmiEXLayer)layer.Clone());
            }

            return image;
        }
    }
}