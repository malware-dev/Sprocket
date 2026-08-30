using System;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace VRageMath
{
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
}
