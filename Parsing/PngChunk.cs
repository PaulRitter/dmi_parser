using System;
using System.IO;
using System.Linq;
using System.Text;
using DMI_Parser.Utils;

namespace DMI_Parser.Parsing
{
    public struct PngChunk
    {
        public string Type;
        public byte[] Data;

        public PngChunk(string type, byte[] data)
        {
            Type = type;
            Data = data;
        }

        public byte[] getCRC(){
            byte[] relevant_data = new byte[4+Data.Length];
            Encoding.ASCII.GetBytes(Type).CopyTo(relevant_data, 0);
            Data.CopyTo(relevant_data, 4);
            return ByteUtils.CRC32(relevant_data);
        }

        public byte[] toBytes()
        {
            MemoryStream stream = new MemoryStream();
            
            stream.Write(BitConverter.GetBytes((uint) Data.Length).Reverse().ToArray());
            stream.Write(Encoding.ASCII.GetBytes(Type));
            stream.Write(Data);
            stream.Write(getCRC());

            return stream.ToArray();
        }

        public static PngChunk zTXtChunk(string text)
        {
            byte[] add_data = {68, 101, 115, 99, 114, 105, 112, 116, 105, 111, 110, 0, 0, 0, 0}; //keyword "Description", Null-seperator (0) and compression method (0)

            byte[] text_data = ByteUtils.Compress(text);
            
            byte[] data = new byte[add_data.Length + text_data.Length];
            add_data.CopyTo(data,0);
            text_data.CopyTo(data, add_data.Length);
            
            return new PngChunk("zTXt", data);
        }
    }
    
}