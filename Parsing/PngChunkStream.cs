using System;
using System.IO;
using System.Text;

namespace DMI_Parser.Parsing
{
    public class PngChunkStream
    {
        private Stream _stream;

        public PngChunkStream(Stream stream)
        {
            _stream = stream;
            readBytes(8); //skip header
        }

        public PngChunk readChunk()
        {
            var length = BitConverter.ToUInt32(readLength());
            var name = Encoding.ASCII.GetString(readBytes(4));
            var data = readBytes((int) length);
            readBytes(4); //skip crc
            return new PngChunk(name, data);
        }
        
        private byte[] readLength()
        {
            byte[] ilen = readBytes(4);
            byte[] len = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                len[i] = ilen[3 - i];
            }

            return len;;
        }

        private byte[] readBytes(int count)
        {
            byte[] result = new byte[count];
            for (int i = 0; i < result.Length; i++)
            {
                int b = _stream.ReadByte();
                if (b == -1) break;
                result[i] = (byte)b;
            }

            return result;
        }

        public void writeChunk(PngChunk chunk)
        {
            _stream.Write(chunk.toBytes());
        }
    }
}