using System;

namespace Lumel.Layout
{
    /// <summary>An element held to a size of its own, whatever it would ask for.</summary>
    public sealed class FixedSize : ISized
    {
        readonly float _height;
        readonly float _width;

        FixedSize(float width, float height)
        {
            _width = width;
            _height = height;
        }

        float ISized.Width => _width;

        float ISized.Height => _height;

        event Action IPlacement.Changed
        {
            add { }
            remove { }
        }

        /// <summary>Held to <paramref name="width" /> across, and as tall as it asks.</summary>
        public static FixedSize Wide(float width)
        {
            return new FixedSize(width, float.NaN);
        }

        /// <summary>Held to <paramref name="height" /> down, and as wide as it asks.</summary>
        public static FixedSize High(float height)
        {
            return new FixedSize(float.NaN, height);
        }

        /// <summary>Held to <paramref name="width" /> across and <paramref name="height" /> down.</summary>
        public static FixedSize Size(float width, float height)
        {
            return new FixedSize(width, height);
        }
    }
}
