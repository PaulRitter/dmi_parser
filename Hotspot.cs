namespace DMI_Parser
{
    public class Hotspot
    {
        public readonly int x;
        public readonly int y;
        public readonly int pictureIndex;

        public Hotspot(int x, int y, int pictureIndex)
        {
            this.x = x; // 18 -> 19
            this.y = y; // height - y
            this.pictureIndex = pictureIndex;
        }

        public int actualX(){
            return x+1;
        }

        //can't be bothered to write this properly. this is actually retarded
        public int actualY(int height){
            return height - y;
        }

        public override string ToString() => $"({x},{y},{pictureIndex})";
        
    }
}