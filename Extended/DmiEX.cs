using System;
using DMI_Parser.Raw;

namespace DMI_Parser.Extended
{
    public class DmiEX : Dmi, ICloneable
    {
        public DmiEX(float version, int width, int height) : base(version, width, height) {}

        public override ICloneable CreateEmptyImage()
        {
            return new DmiEXImage(Width, Height);
        }

        public static DmiEX FromDmi(string path) => FromDmi(FromFile(path));
        
        public static DmiEX FromDmi(Dmi dmi)
        {
            DmiEX dmiEx = new DmiEX(dmi.Version, dmi.Width, dmi.Height);
            
            foreach (var state in dmi.States)
            {
                dmiEx.AddState(DmiEXState.FromDmiState(dmiEx, state));
            }

            return dmiEx;
        }

        public override DMIState AddNewState(string name)
        {
            RawDmiState raw = RawDmiState.Default(name);

            DmiEXImage[,] images = new DmiEXImage[1, 1];
            images[0,0] = (DmiEXImage) CreateEmptyImage();
            
            DmiEXState dmiState = new DmiEXState(this, images, raw);
            return AddState(dmiState);
        }

        public object Clone()
        {
            DmiEX newDmiex = new DmiEX(Version, Width, Height);
            foreach (var state in States)
            {
                DmiEXState dmiExState = (DmiEXState) state;
                newDmiex.AddState((DmiEXState) dmiExState.Clone());
            }
            return newDmiex;
        }
    }
}