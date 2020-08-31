using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using ImageProcessor;
using ImageProcessor.Imaging.Formats;

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
        
        //todo potential memoryleak
        public static BitmapImage Bitmap2BitmapImage(Bitmap bitmap) => ImageFactory2BitmapImage(Bitmap2ImageFactory(bitmap));

        public static Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            using(MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                Bitmap bitmap = new Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }
    }
}