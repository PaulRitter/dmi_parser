﻿using System;
using System.Collections;
using System.Collections.Generic;
using MetadataExtractor;

namespace DMI_Parser
{
    public class DMI
    {
        float version;
        int width;
        int height;
        List<DMIState> states;

        private DMI(float version, int width, int height, List<DMIState> states){
            this.version = version;
            this.width = width;
            this.height = height;
            this.states = states;
        }

        public static DMI fromFile(String filepath){
            //get metadata
            IEnumerator metadata = getDMIMetadata(filepath).GetEnumerator();

            //dmi info
            float version = -1;
            int width = -1;
            int height = -1;


            //for building states
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
                if(current.Length != 2){

                }
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
                            states.Add(new DMIState(stateID, stateDirs, stateFrames, stateDelays, stateLoop, stateRewind, stateMovement, stateHotspots, string.Join("\n",raw)));
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
            
            return new DMI(version, width, height, states);
        }

        private static String[] getDMIMetadata(String filepath){
            IReadOnlyList<Directory> directories;
            try{
                directories = ImageMetadataReader.ReadMetadata(filepath);
            }catch(ImageProcessingException e){
                throw new InvalidFileException("File could not be read as a .dmi", e, filepath);
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
