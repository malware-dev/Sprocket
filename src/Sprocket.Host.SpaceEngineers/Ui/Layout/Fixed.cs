using System;

namespace Sprocket.Host.SpaceEngineers.Ui.Layout
{
    /// <summary>An element held to a size of its own, whatever it would ask for.</summary>
    public sealed class Fixed : ISized
    {
        readonly float _height;
        readonly float _width;

        Fixed(float width, float height)
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
        public static Fixed Wide(float width)
        {
            return new Fixed(width, float.NaN);
        }

        /// <summary>Held to <paramref name="height" /> down, and as wide as it asks.</summary>
        public static Fixed High(float height)
        {
            return new Fixed(float.NaN, height);
        }

        /// <summary>Held to <paramref name="width" /> across and <paramref name="height" /> down.</summary>
        public static Fixed Size(float width, float height)
        {
            return new Fixed(width, height);
        }
    }
}
