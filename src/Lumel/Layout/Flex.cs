using System;

namespace Lumel.Layout
{
    /// <summary>A child of a stack taking a share of the room the others leave.</summary>
    public sealed class Flex : IStacked
    {
        /// <summary>A share of weight one, which is all the room left when nothing else grows.</summary>
        public static readonly Flex Grow = new Flex(1f, true);

        /// <summary>
        /// The room a stack is arranged in beyond what its other children want, asking for none as it is measured, so a spacer
        /// placed by it pushes what follows to the far end.
        /// </summary>
        public static readonly Flex Spare = new Flex(1f, false);

        readonly float _weight;
        readonly bool _claims;

        Flex(float weight, bool claims)
        {
            _weight = weight;
            _claims = claims;
        }

        float ISized.Width => float.NaN;

        float ISized.Height => float.NaN;

        float IStacked.Grow => _weight;

        bool IStacked.Claims => _claims;

        event Action IPlacement.Changed
        {
            add { }
            remove { }
        }

        /// <summary>A share of <paramref name="weight" /> against the weights of the other children that grow.</summary>
        public static Flex By(float weight)
        {
            return new Flex(weight, true);
        }
    }
}
