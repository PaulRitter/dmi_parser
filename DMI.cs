using System;
using System.Collections;
using System.Collections.Generic;
using MetadataExtractor;
using System.IO;
using System.Drawing;

namespace DMI_Parser
{
    public class DMI
    {
        public readonly float version;
        public readonly int width;
        public readonly int height;
        public List<DMIState> states;
        public Bitmap full_image;

        private DMI(float version, int width, int height, List<DMIState> states, Bitmap full_image){
            this.version = version;
            this.width = width;
            this.height = height;
            this.states = states;
            this.full_image = full_image;
        }

        public static DMI fromFile(String filepath){
            FileStream stream = File.Open(filepath, FileMode.Open);
            DMI result = fromFile(stream);
            stream.Close();
            return result;
        }

        public static DMI fromFile(FileStream stream){
            //get metadata
            IEnumerator metadata = getDMIMetadata(stream).GetEnumerator();

            //file bitmap
            Bitmap full_image = new Bitmap(stream);

            //dmi info
            float version = -1;
            int width = -1;
            int height = -1;

            //for building states
            int position = 0;
            Point offset = new Point(0,0);
            List<DMIState> states = new List<DMIState>();
            bool readingState = false;
            string stateID = null;
            int stateDirs = -1;
            int stateFrames = -1;
            float[] stateDelays = null;
            int stateLoop = 0;
            bool stateRewind = false;
            bool stateMovement = false;
            List<Hotspot> stateHotspots = new List<Hotspot>();
            List<string> raw = new List<string>();

            //parse data
            while (metadata.MoveNext())
            {
                string[] current = ((string)metadata.Current).Trim().Split('='); //make this regex
                switch (current[0].Trim())
                {
                    case "version":
                        if(version != -1){
                            throw new StateArgumentDuplicateException("Argument duplicated", "version");
                        }
                        version = float.Parse(current[1].Replace('.',','));
                        break;
                    case "width":
                        if(width != -1){
                            throw new StateArgumentDuplicateException("Argument duplicated", "width");
                        }
                        width = int.Parse(current[1]);
                        break;
                    case "height":
                        if(height != -1){
                            throw new StateArgumentDuplicateException("Argument duplicated", "height");
                        }
                        height = int.Parse(current[1]);
                        break;
                    case "state":
                        if(readingState){
                            if(stateDirs == -1 ||stateFrames == -1 || stateID == null){
                                throw new InvalidStateException("Invalid State at end of state-parsing", stateID, stateDirs, stateFrames, stateDelays);
                            }

                            //some files dont have width and height specified and just assume ist 32x32... fuck you
                            if(width == -1){
                                width = 32;
                            }
                            if(height == -1){
                                height = 32;
                            }
                            DMIState newState = new DMIState(width, height, position++, stateID, stateDirs, stateFrames, stateDelays, stateLoop, stateRewind, stateMovement, stateHotspots, string.Join("\n",raw), full_image, offset);
                            states.Add(newState);
                            offset = newState.getEndOffset();
                            stateID = null;
                            stateDirs = -1;
                            stateFrames = -1;
                            stateDelays = null;
                            stateLoop = 0;
                            stateRewind = false;
                            stateMovement = false;
                            stateHotspots = new List<Hotspot>();
                            raw = new List<string>();
                        }
                        stateID = current[1].Trim().Trim('"');
                        readingState = true;
                        raw.Add((string)metadata.Current);
                        break;
                    case "dirs":
                        if(stateDirs != -1){
                            throw new StateArgumentDuplicateException("Argument duplicated", "dirs");
                        }
                        stateDirs = int.Parse(current[1]);
                        raw.Add((string)metadata.Current);
                        break;
                    case "frames":
                        if(stateFrames != -1){
                            throw new StateArgumentDuplicateException("Argument duplicated", "frames");
                        }
                        stateFrames = int.Parse(current[1]);
                        raw.Add((string)metadata.Current);
                        break;
                    case "delay":
                        if(stateDelays != null){
                            throw new StateArgumentDuplicateException("Argument duplicated", "delay");
                        }
                        string[] raw_delays = current[1].Split(',');
                        stateDelays = new float[raw_delays.Length];
                        int i = 0;
                        foreach (string delay in raw_delays)
                        {
                            stateDelays[i] = float.Parse(delay.Replace('.',','));
                            i++;
                        }
                        raw.Add((string)metadata.Current);
                        break;
                    case "loop":
                        if(stateLoop != 0){
                            throw new StateArgumentDuplicateException("Argument duplicated", "loop");
                        }
                        stateLoop = int.Parse(current[1]);
                        raw.Add((string)metadata.Current);
                        break;
                    case "rewind":
                        if(stateRewind){
                            throw new StateArgumentDuplicateException("Argument duplicated", "rewind");
                        }
                        if(current[1].Trim() == "1"){
                            stateRewind = true;
                        }
                        raw.Add((string)metadata.Current);
                        break;
                    case "movement":
                        if(stateMovement){
                            throw new StateArgumentDuplicateException("Argument duplicated", "movement");
                        }
                        if(current[1].Trim() == "1"){
                            stateMovement = true;
                        }
                        raw.Add((string)metadata.Current);
                        break;
                    case "hotspot":
                        //can have multiple
                        string[] values = current[1].Split(',');
                        stateHotspots.Add(new Hotspot(int.Parse(values[0]), int.Parse(values[1]), int.Parse(values[2])));
                        raw.Add((string)metadata.Current);
                        break;
                    default:
                        throw new UnknownKeywordException("Unknown Keyword received", stateID, current[0], current[1]);
                }
            }

            //evil copy pasta
            if(readingState){
                if(stateDirs == -1 ||stateFrames == -1 || stateID == null){
                    throw new InvalidStateException("Invalid State at end of state-parsing", stateID, stateDirs, stateFrames, stateDelays);
                }

                //some files dont have width and height specified and just assume ist 32x32... fuck you
                if(width == -1){
                    width = 32;
                }
                if(height == -1){
                    height = 32;
                }
                DMIState newState = new DMIState(width, height, position++, stateID, stateDirs, stateFrames, stateDelays, stateLoop, stateRewind, stateMovement, stateHotspots, string.Join("\n",raw), full_image, offset);
                states.Add(newState);
                offset = newState.getEndOffset();
                stateID = null;
                stateDirs = -1;
                stateFrames = -1;
                stateDelays = null;
                stateLoop = 0;
                stateRewind = false;
                stateMovement = false;
                stateHotspots = new List<Hotspot>();
                raw = new List<string>();
            }
            
            return new DMI(version, width, height, states, full_image);
        }

        private static String[] getDMIMetadata(FileStream stream){
            IReadOnlyList<MetadataExtractor.Directory> directories;
            try{
                directories = ImageMetadataReader.ReadMetadata(stream);
            }catch(ImageProcessingException e){
                throw new InvalidFileException("File could not be read as a .dmi", e);
            }

            foreach (var directory in directories)
            {
                foreach (var tag in directory.Tags){
                    if(tag.Name != "Textual Data")
                        continue;
                    string[] raw_metadata = tag.Description.Split(
                        new[] { "\r\n", "\r", "\n" },
                        StringSplitOptions.None
                    );
                    int start = -1;
                    int end = -1;
                    for (int i = 0; i < raw_metadata.Length; i++)
                    {
                        if(raw_metadata[i].Replace(" ",string.Empty).EndsWith("#BEGINDMI")){
                            start = i+1;
                        }else if(raw_metadata[i].Replace(" ",string.Empty).EndsWith("#ENDDMI")){
                            end = i;
                        }
                    }
                    
                    if(end != -1 && start != -1){
                        string[] dmi_metadata = new string[end-start];
                        Array.Copy(raw_metadata, start, dmi_metadata, 0, end-start);                        
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
