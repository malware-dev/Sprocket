using System;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace VRageMath
{
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
}
