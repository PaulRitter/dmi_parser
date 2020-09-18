using System;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageProcessor;
using ImageProcessor.Imaging.Formats;
using Color = System.Drawing.Color;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace DMI_Parser.Utils
{
    public static class BitmapUtils
    {
        public static ImageFactory Bitmap2ImageFactory(Bitmap bitmap)
        {
            ImageFactory imgF = new ImageFactory()
                .Load(bitmap)
                .Resolution(bitmap.Width, bitmap.Height)
                .Format(new PngFormat())
                .BackgroundColor(Color.Transparent);

            return imgF;
        }

        public static BitmapImage ImageFactory2BitmapImage(ImageFactory imageFactory)
        {
            MemoryStream imgStream = new MemoryStream(); 
            imageFactory.Save(imgStream);

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = imgStream;
            bitmapImage.EndInit();

            return bitmapImage;
        }
        
        public static BitmapImage Bitmap2BitmapImage(Bitmap bitmap) => ImageFactory2BitmapImage(Bitmap2ImageFactory(bitmap));

        public static Bitmap Resized(this Bitmap bitmap, int new_width, int new_height)
        {
            Bitmap new_bm = new Bitmap(new_width, new_height);
            using (Graphics g = Graphics.FromImage(new_bm))
            {
                g.DrawImage(bitmap, new Point(0,0));
            }

            return new_bm;
        }
    }
}