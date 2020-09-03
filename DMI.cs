﻿using System;
using System.Collections;
using System.Collections.Generic;
using MetadataExtractor;
using System.IO;
using System.Drawing;
using DMI_Parser.Parsing;
using DMI_Parser.Raw;
using ImageProcessor;
using ImageProcessor.Imaging.Formats;
using Point = System.Drawing.Point;

namespace DMI_Parser
{
    public class Dmi
    {
        public const string DmiTab = "\t";

        public readonly float Version;
        public int Width { get; private set; }
        public int Height  { get; private set; }
        public List<DMIState> States = new List<DMIState>();

        public event EventHandler WidthChanged;
        public event EventHandler HeightChanged;

        public event EventHandler SizeChanged;

        public event EventHandler StateListChanged;

        public Dmi(float version, int width, int height)
        {
            this.Version = version;
            this.Width = width;
            this.Height = height;

            WidthChanged += (o, e) => SizeChanged?.Invoke(this, EventArgs.Empty);
            HeightChanged += (o, e) => SizeChanged?.Invoke(this, EventArgs.Empty);
        }

        public void setWidth(int width)
        {
            Width = width;
            WidthChanged?.Invoke(this, EventArgs.Empty);
        }
        
        public void setHeight(int height)
        {
            Height = height;
            HeightChanged?.Invoke(this, EventArgs.Empty);
        }

        public void addStates(List<DMIState> dmiStates)
        {
            for (int i = 0; i < dmiStates.Count; i++)
            {
                addState(dmiStates[i]);
            }
        }
        
        public void addState(DMIState dmiState)
        {
            States.Add(dmiState);
            WidthChanged += dmiState.resizeImages;
            HeightChanged += dmiState.resizeImages;
            StateListChanged?.Invoke(this, EventArgs.Empty);
        }

        public void removeState(DMIState dmiState)
        {
            States.Remove(dmiState);
            WidthChanged -= dmiState.resizeImages;
            HeightChanged -= dmiState.resizeImages;
            StateListChanged?.Invoke(this, EventArgs.Empty);
        }

        public virtual void createNewState(string name)
        {
            RawDmiState raw = RawDmiState.Default;
            raw.Id = name;

            Bitmap[,] images = new Bitmap[1, 1];
            images[0,0] = (Bitmap) CreateEmptyImage();
            
            DMIState dmiState = new DMIState(this, images, raw);
            addState(dmiState);
        }
        
        public void SaveAsDmi(Stream imageStream)
        {
            PngChunk zTXtchunk = PngChunk.zTXtChunk(ToString());

            Bitmap image = GetFullBitmap();
            
            MemoryStream imageByteStream = new MemoryStream();
            ImageFactory imageFactory = new ImageFactory()
                .Load(image)
                .Format(new PngFormat())
                .BackgroundColor(Color.Transparent)
                .Save(imageByteStream);
            imageByteStream.Position = 0;
            
            PngChunkStream pngStream = new PngChunkStream(imageByteStream);
            PngChunkStream outStream = new PngChunkStream(imageStream);
            imageStream.Write(new byte[]{137, 80, 78, 71, 13, 10, 26, 10});

            PngChunk c;
            bool metadataInserted = false;
            do
            {
                c = pngStream.readChunk();
                if (c.Type != "IEND" && c.Type != "IDAT" && c.Type != "IHDR") continue;

                if (c.Type == "IDAT")
                {
                    if (!metadataInserted)
                    {
                        outStream.writeChunk(zTXtchunk);
                        metadataInserted = true;
                    }
                    
                    //split idat to chunks of 8192
                    if (c.Data.Length > 8192)
                    {
                        Console.WriteLine(c.Data.Length);
                        for (int i = 0; i < c.Data.Length;)
                        {
                            var len = c.Data.Length - i <= 8192 ? c.Data.Length - i : 8192;
                            byte[] data = new byte[len];
                            for (int j = 0; j < data.Length; j++)
                            {
                                data[j] = c.Data[i++];
                            }
                            Console.WriteLine(data.Length);
                            PngChunk pngChunk = new PngChunk("IDAT", data);
                        }
                        continue;
                    }
                }
                outStream.writeChunk(c);
            } while (c.Type != "IEND" && c.Type != "    ");

            if (!metadataInserted) throw new ParsingException("Failed to insert Metadata");
        }

        //compiles a Bitmap of the entire DMI
        public Bitmap GetFullBitmap()
        {
            int imgCount = GetTotalImageCount();

            int imgCountWidth = (int)Math.Ceiling(Math.Sqrt(imgCount));
            int imgCountHeight = (int) Math.Ceiling((double)imgCount / imgCountWidth);

            int bitmapWidth = imgCountWidth * Width;
            int bitmapHeight = imgCountHeight * Height;
            
            Bitmap res = new Bitmap(bitmapWidth, bitmapHeight);
            
            Point offset = Point.Empty;

            foreach (var state in States)
            {
                for (int dir = 0; dir < (int)state.Dirs; dir++)
                {
                    for (int frame = 0; frame < state.Frames; frame++)
                    {
                        Bitmap newImage = state.getBitmap(dir, frame);

                        for (int x = 0; x < newImage.Width; x++)
                        {
                            for (int y = 0; y < newImage.Height; y++)
                            {
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

        public virtual ICloneable CreateEmptyImage()
        {
            return new Bitmap(Width, Height);
        }

        private int GetTotalImageCount()
        {
            int res = 0;
            foreach (var state in States)
            {
                res += state.getImageCount();
            }

            return res;
        }
        
        public override string ToString()
        {
            string res = "# BEGIN DMI";
            res += $"\nversion = {Version}";
            res += $"\n{DmiTab}width = {Width}";
            res += $"\n{DmiTab}height = {Height}";
            foreach (var state in States)
            {
                res += $"\n{state}";
            }

            res += "\n# END DMI\n";
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
            IEnumerator metadata = GetDmiMetadata(stream)?.GetEnumerator();
            if(metadata == null) throw new ParsingException("No DMI-Metadata found");

            //file bitmap
            Bitmap image = new Bitmap(stream);
            StateCutter imgCutter = null;

            //dmi info
            Dmi newDmi = null;
            float? version = null;
            int? width = null;
            int? height = null;

            //for building states
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

                            version = 4.0f; //because we are saving in 4.0
                            newDmi ??= new Dmi(version.Value, width.Value, height.Value);
                            
                            imgCutter ??= new StateCutter(image, height.Value, width.Value);

                            Bitmap[,] images = imgCutter.CutImages(partialState.Dirs.Value, partialState.Frames.Value);
                            DMIState newState = new DMIState(newDmi, images, partialState);
                            newDmi.addState(newState);
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

                        string[] rawDelays = current[1].Split(',');
                        partialState._delays = new float[rawDelays.Length];
                        for (int i = 0; i < rawDelays.Length; i++)
                        {
                            partialState._delays[i] = float.Parse(rawDelays[i].Replace('.', ','));
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
                newDmi.addState(newState);
            }

            return newDmi;
        }

        private static String[] GetDmiMetadata(FileStream stream)
        {
            IReadOnlyList<MetadataExtractor.Directory> directories;
            try
            {
                directories = ImageMetadataReader.ReadMetadata(stream);
            }
            catch (ImageProcessingException e)
            {
                throw new InvalidFileException("File could not be read as a .png", e);
            }

            foreach (var directory in directories)
            {
                foreach (var tag in directory.Tags)
                {
                    if (tag.Name != "Textual Data")
                        continue;
                    string[] rawMetadata = tag.Description.Split(
                        new[] {"\r\n", "\r", "\n"},
                        StringSplitOptions.None
                    );
                    int start = -1;
                    int end = -1;
                    for (int i = 0; i < rawMetadata.Length; i++)
                    {
                        if (rawMetadata[i].Replace(" ", string.Empty).EndsWith("#BEGINDMI"))
                        {
                            start = i + 1;
                        }
                        else if (rawMetadata[i].Replace(" ", string.Empty).EndsWith("#ENDDMI"))
                        {
                            end = i;
                        }
                    }

                    if (end != -1 && start != -1)
                    {
                        string[] dmi_metadata = new string[end - start];
                        Array.Copy(rawMetadata, start, dmi_metadata, 0, end - start);
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