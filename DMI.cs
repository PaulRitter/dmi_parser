using System;
using MetadataExtractor;

namespace DMI_Parser
{
    public class DMI
    {
        float version;
        int width;
        int height;
        DMIState[] states;

        public static DMI fromFile(String filepath){
            String[] metadata = getMetadata(filepath);
            foreach (var item in metadata)
            {
                Console.WriteLine(item);    
            }

 
            return null;
        }

        private static String[] getMetadata(String filepath){
            var directories = ImageMetadataReader.ReadMetadata(filepath);

            String[] dmi_metadata = null;
            foreach (var directory in directories)
            {
                foreach (var tag in directory.Tags){
                    if(tag.Name != "Textual Data")
                        continue;
                    dmi_metadata = tag.Description.Split(
                        new[] { "\r\n", "\r", "\n" },
                        StringSplitOptions.None
                    );
                    if(dmi_metadata[0] == "Description: # BEGIN DMI")
                        return dmi_metadata;
                    Console.WriteLine("["+tag.Name+"]: "+tag.Description);
                }

                if (directory.HasError)
                {
                    foreach (var error in directory.Errors)
                        Console.WriteLine($"ERROR: {error}");
                }
            }

            return dmi_metadata;
        }
    }
}
