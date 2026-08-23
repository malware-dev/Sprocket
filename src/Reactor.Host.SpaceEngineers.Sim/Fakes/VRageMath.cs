// Substitutes for the game's own types, declared in the game's own namespaces so that Phosphor's sources
// compile here unaltered.
//
// These are not reimplementations of VRageMath. They carry exactly the members Phosphor names and nothing
// else, because nothing else is ever reached for: the simulator does not run the game, it runs our code.
// When Phosphor grows a use of something absent, this file fails to compile, which is the point - the
// substitute set is meant to be a written record of the surface we depend on, and a record that can drift
// silently is not one.
//
// The mod build never sees any of this. It compiles the same sources against Keen's assemblies, so that
// build remains the arbiter of whether a signature here is right.

using System;

namespace VRageMath
{
    public struct Vector2
    {
        public float X;
        public float Y;

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public Vector2(float value)
        {
            X = value;
            Y = value;
        }

        public static Vector2 Zero
        {
            get { return new Vector2(0f, 0f); }
        }

        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X + b.X, a.Y + b.Y);
        }

        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X - b.X, a.Y - b.Y);
        }

        public static Vector2 operator *(Vector2 a, float scalar)
        {
            return new Vector2(a.X * scalar, a.Y * scalar);
        }

        public static Vector2 operator *(float scalar, Vector2 a)
        {
            return new Vector2(a.X * scalar, a.Y * scalar);
        }

        public static Vector2 operator /(Vector2 a, float scalar)
        {
            return new Vector2(a.X / scalar, a.Y / scalar);
        }

        public override string ToString()
        {
            return "{X:" + X + " Y:" + Y + "}";
        }
    }

    public struct Rectangle
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int Left { get { return X; } }

        public int Top { get { return Y; } }

        public int Right { get { return X + Width; } }

        public int Bottom { get { return Y + Height; } }

        public override string ToString()
        {
            return "{X:" + X + " Y:" + Y + " W:" + Width + " H:" + Height + "}";
        }
    }

    public struct RectangleF
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public RectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public float Left { get { return X; } }

        public float Top { get { return Y; } }

        public float Right { get { return X + Width; } }

        public float Bottom { get { return Y + Height; } }

        public override string ToString()
        {
            return "{X:" + X + " Y:" + Y + " W:" + Width + " H:" + Height + "}";
        }
    }

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
