using System;

namespace Lumel.Layout
{
    /// <summary>A child of a stack taking a share of the room the others leave.</summary>
    public sealed class Flex : IStacked
    {
        /// <summary>A share of weight one, which is all the room left when nothing else grows.</summary>
        public static readonly Flex Grow = new Flex(1f);

        readonly float _weight;

        Flex(float weight)
        {
            _weight = weight;
        }

        float ISized.Width => float.NaN;

        float ISized.Height => float.NaN;

        float IStacked.Grow => _weight;

        event Action IPlacement.Changed
        {
            add { }
            remove { }
        }

        /// <summary>A share of <paramref name="weight" /> against the weights of the other children that grow.</summary>
        public static Flex By(float weight)
        {
            return new Flex(weight);
        }
    }
}
