using System;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

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
}
