using System;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace VRageMath
{
    /// <summary>
    /// Eight bits per channel, straight-alpha, exactly as the game's is. Worth being fussy about: a colour
    /// type that quietly used floats would make every blend in the simulator subtly unlike the one in the
    /// game, and that is precisely the class of difference a simulator exists to not have.
    /// </summary>
    public struct Color
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public Color(int r, int g, int b)
            : this(r, g, b, 255)
        {
        }

        public Color(int r, int g, int b, int a)
        {
            R = Clamp(r);
            G = Clamp(g);
            B = Clamp(b);
            A = Clamp(a);
        }

        public Color(Color color, float alpha)
        {
            R = color.R;
            G = color.G;
            B = color.B;
            A = Clamp((int)Math.Round(alpha * 255f));
        }

        public static Color White { get { return new Color(255, 255, 255); } }

        public static Color Black { get { return new Color(0, 0, 0); } }

        public static Color Transparent { get { return new Color(0, 0, 0, 0); } }

        static byte Clamp(int value)
        {
            if (value < 0)
                return 0;
            if (value > 255)
                return 255;
            return (byte)value;
        }

        public override string ToString()
        {
            return "{R:" + R + " G:" + G + " B:" + B + " A:" + A + "}";
        }
    }
}
