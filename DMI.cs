using System;
using System.Collections;
using System.Collections.Generic;
using MetadataExtractor;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using DMI_Parser.Parsing;
using DMI_Parser.Raw;

namespace DMI_Parser
{
    public class Dmi
    {
        public const string DMI_TAB = "        ";

        //todo
        public string Name;
        public readonly float Version;
        public readonly int Width;
        public readonly int Height;
        public List<DMIState> States = new List<DMIState>();

        public Dmi(float version, int width, int height)
        {
            this.Version = version;
            this.Width = width;
            this.Height = height;
        }
        
        //todo instance-method for saving
        public bool Save()
        {
            string filepath = $"{Name}.dmi";

            string metadata = ToString();
            Bitmap image = GetFullBitmap();
            
            image.Save(filepath, ImageFormat.Png);
            
            Stream pngStream = new System.IO.FileStream(filepath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            PngBitmapDecoder pngDecoder = new PngBitmapDecoder(pngStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            BitmapFrame pngFrame = pngDecoder.Frames[0];
            InPlaceBitmapMetadataWriter pngInplace = pngFrame.CreateInPlaceBitmapMetadataWriter();
            if (pngInplace.TrySave())
            { pngInplace.SetQuery("/Text/Description", "Have a nice day."); }
            pngStream.Close();

            return true;
        }

        //compiles a Bitmap of the entire DMI
        public Bitmap GetFullBitmap()
        {
            int imgCount = getTotalImageCount();

            int imgCountWidth = (int)Math.Ceiling(Math.Sqrt(imgCount));
            int imgCountHeight = (int) Math.Ceiling((double)imgCount / (double)imgCountWidth);

            int bitmapWidth = imgCountWidth * Width;
            int bitmapHeight = imgCountHeight * Height;
            
            Bitmap res = new Bitmap(bitmapWidth, bitmapHeight);
            Point offset = Point.Empty;

            Console.WriteLine($"{bitmapWidth},{bitmapHeight}");
            foreach (var state in States)
            {
                for (int dir = 0; dir < (int)state.Dirs; dir++)
                {
                    for (int frame = 0; frame < state.Frames; frame++)
                    {
                        Bitmap newImage = state.getImage(dir, frame);

                        Console.WriteLine($"{offset}");
                        
                        for (int x = 0; x < newImage.Width; x++)
                        {
                            for (int y = 0; y < newImage.Height; y++)
                            {
                                //Console.WriteLine($"{x},{y}");
                                res.SetPixel(offset.X + x, offset.Y + y, newImage.GetPixel(x,y));
                            }
                        }
                        
                        offset = new Point(offset.X+Width, offset.Y+0);
                        if (offset.X >= bitmapWidth)
                        {
                            offset = new Point(0, offset.Y+Height);
                        }
                    }
                }
            }

            return res;
        }

        public int getTotalImageCount()
        {
            int res = 0;
            foreach (var state in States)
            {
                res += state.Images.Length;
            }

            return res;
        }
        
        public override string ToString()
        {
            string res = "#BEGIN DMI";
            res += $"version = {Version}";
            res += $"\n{DMI_TAB}width = {Width}";
            res += $"\n{DMI_TAB}height = {Height}";
            foreach (var state in States)
            {
                res += $"\n{state}";
            }

            res += "#END DMI";
            return res;
        }

        public static Dmi FromFile(String filepath)
        {
            FileStream stream = File.Open(filepath, FileMode.Open);
            Dmi result = FromFile(stream);
            stream.Close();
            return result;
        }
        
        public static Dmi FromFile(FileStream stream)
        {
            //get metadata
            IEnumerator metadata = getDMIMetadata(stream).GetEnumerator();

            //file bitmap
            Bitmap image = new Bitmap(stream);
            StateCutter imgCutter = null;

            //dmi info
            Dmi newDmi = null;
            float? version = null;
            int? width = null;
            int? height = null;

            //for building states
            Point offset = new Point(0, 0);
            List<DMIState> states = new List<DMIState>();
            bool readingState = false;
            RawDmiState partialState = new RawDmiState();
            
            //parse data
            while (metadata.MoveNext())
            {
                string[] current = ((string) metadata.Current).Trim().Split('='); //make this regex
                switch (current[0].Trim())
                {
                    case "version":
                        if (version != null)
                        {
                            throw new StateArgumentDuplicateException("Argument duplicated", "version");
                        }

                        version = float.Parse(current[1].Replace('.', ','));
                        break;
                    case "width":
                        if (width != null)
                        {
                            throw new StateArgumentDuplicateException("Argument duplicated", "width");
                        }

                        width = int.Parse(current[1]);
                        break;
                    case "height":
                        if (height != null)
                        {
                            throw new StateArgumentDuplicateException("Argument duplicated", "height");
                        }

                        height = int.Parse(current[1]);
                        break;
                    case "state":
                        if (readingState)
                        {
                            if (!partialState.isValid())
                            {
                                throw new InvalidStateException("Invalid State at end of state-parsing", partialState);
                            }

                            //some files dont have width and height specified and just assume ist 32x32... fuck you
                            width ??= 32;
                            height ??= 32;

                            version ??= 1.0f; //todo find current version and use it here
                            newDmi ??= new Dmi(version.Value, width.Value, height.Value);
                            
                            imgCutter ??= new StateCutter(image, height.Value, width.Value);

                            Bitmap[,] images = imgCutter.CutImages(partialState.Dirs.Value, partialState.Frames.Value);
                            DMIState newState = new DMIState(newDmi, images, partialState);
                            newDmi.States.Add(newState);
                            partialState = new RawDmiState();
                        }

                        partialState.Id = current[1].Trim().Trim('"');
                        readingState = true;
                        break;
                    case "dirs":
                        if (partialState.Dirs != null)
                        {
                            throw new StateArgumentDuplicateException("Argument duplicated", "dirs");
                        }

                        int newDir = int.Parse(current[1]);
                        switch (newDir)
                        {
                            case 1:
                                partialState.Dirs = DirCount.SINGLE;
                                break;
                            case 4:
                                partialState.Dirs = DirCount.CARDINAL;
                                break;
                            case 8:
                                partialState.Dirs = DirCount.ALL;
                                break;
                            default:
                                throw new StateArgumentValueInvalidException<int>("Dir count invalid", "dirs",
                                    newDir);
                        }
                        break;
                    case "frames":
                        if (partialState.Frames != null)
                        {
                            throw new StateArgumentDuplicateException("Argument duplicated", "frames");
                        }

                        partialState.Frames = int.Parse(current[1]);
                        break;
                    case "delay":
                        if (partialState._delays != null)
                        {
                            throw new StateArgumentDuplicateException("Argument duplicated", "delay");
                        }

                        string[] raw_delays = current[1].Split(',');
                        partialState._delays = new float[raw_delays.Length];
                        int i = 0;
                        foreach (string delay in raw_delays)
                        {
                            partialState._delays[i] = float.Parse(delay.Replace('.', ','));
                            i++;
                        }
                        break;
                    case "loop":
                        if (partialState.Loop != 0)
                        {
                            throw new StateArgumentDuplicateException("Argument duplicated", "loop");
                        }

                        partialState.Loop = int.Parse(current[1]);
                        break;
                    case "rewind":
                        if (partialState.Rewind)
                        {
                            throw new StateArgumentDuplicateException("Argument duplicated", "rewind");
                        }

                        if (current[1].Trim() == "1")
                        {
                            partialState.Rewind = true;
                        }
                        break;
                    case "movement":
                        if (partialState.Movement)
                        {
                            throw new StateArgumentDuplicateException("Argument duplicated", "movement");
                        }

                        if (current[1].Trim() == "1")
                        {
                            partialState.Movement = true;
                        }
                        break;
                    case "hotspot":
                        //can have multiple
                        string[] values = current[1].Split(',');
                        partialState.Hotspots.Add(new RawHotspot(int.Parse(values[0]), int.Parse(values[1]), int.Parse(values[2])));
                        break;
                    default:
                        throw new UnknownKeywordException("Unknown Keyword received", partialState.Id, current[0], current[1]);
                }
            }

            //evil copy pasta
            if (readingState)
            {
                if (!partialState.isValid())
                {
                    throw new InvalidStateException("Invalid State at end of state-parsing", partialState);
                }

                //some files dont have width and height specified and just assume ist 32x32... fuck you
                width ??= 32;
                height ??= 32;

                version ??= 1.0f; //todo find current version and use it here
                newDmi ??= new Dmi(version.Value, width.Value, height.Value);
                            
                imgCutter ??= new StateCutter(image, height.Value, width.Value);

                Bitmap[,] images = imgCutter.CutImages(partialState.Dirs.Value, partialState.Frames.Value);
                DMIState newState = new DMIState(newDmi, images, partialState);
                newDmi.States.Add(newState);
            }

            return newDmi;
        }

        private static String[] getDMIMetadata(FileStream stream)
        {
            IReadOnlyList<MetadataExtractor.Directory> directories;
            try
            {
                directories = ImageMetadataReader.ReadMetadata(stream);
            }
            catch (ImageProcessingException e)
            {
                throw new InvalidFileException("File could not be read as a .dmi", e);
            }

            foreach (var directory in directories)
            {
                foreach (var tag in directory.Tags)
                {
                    Console.WriteLine(tag.Name);
                    if (tag.Name != "Textual Data")
                        continue;
                    string[] raw_metadata = tag.Description.Split(
                        new[] {"\r\n", "\r", "\n"},
                        StringSplitOptions.None
                    );
                    int start = -1;
                    int end = -1;
                    for (int i = 0; i < raw_metadata.Length; i++)
                    {
                        if (raw_metadata[i].Replace(" ", string.Empty).EndsWith("#BEGINDMI"))
                        {
                            start = i + 1;
                        }
                        else if (raw_metadata[i].Replace(" ", string.Empty).EndsWith("#ENDDMI"))
                        {
                            end = i;
                        }
                    }

                    if (end != -1 && start != -1)
                    {
                        string[] dmi_metadata = new string[end - start];
                        Array.Copy(raw_metadata, start, dmi_metadata, 0, end - start);
                        return dmi_metadata;
                    }
                }

                //TODO better error reporting here
                if (directory.HasError)
                {
                    foreach (var error in directory.Errors)
                        Console.WriteLine($"ERROR: {error}");
                }
            }

            return null;
        }
    }
}