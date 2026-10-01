
namespace Lumel.Layout
{
    /// <summary>How big a grid's column or row is: a fixed size, as big as its largest cell, or a share of what is left.</summary>
    public struct Length
    {
        /// <summary>As big as the largest cell in it.</summary>
        public static readonly Length Auto = new Length(0f, 0f, true);

        readonly bool _auto;
        readonly float _size;
        readonly float _weight;

        Length(float size, float weight, bool auto)
        {
            _size = size;
            _weight = weight;
            _auto = auto;
        }

        /// <summary>The size, for a fixed length.</summary>
        public float Size => _size;

        /// <summary>The weight a share is taken by, for a flex length.</summary>
        public float Weight => _weight;

        /// <summary>Whether this is as big as its largest cell.</summary>
        public bool IsAuto => _auto;

        /// <summary>Whether this takes a share of what the fixed and auto lengths leave.</summary>
        public bool IsFlex => !_auto && _weight > 0f;

        /// <summary>Whether this is a size given outright.</summary>
        public bool IsFixed => !_auto && !(_weight > 0f);

        /// <summary>A share of <paramref name="weight" /> against the other flex lengths.</summary>
        public static Length Flex(float weight)
        {
            return new Length(0f, weight, false);
        }

        /// <summary>A fixed size of <paramref name="size" />.</summary>
        public static implicit operator Length(float size)
        {
            return new Length(size, 0f, false);
        }
    }
}
