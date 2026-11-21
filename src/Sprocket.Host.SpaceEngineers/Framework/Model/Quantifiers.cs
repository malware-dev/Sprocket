namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>What is true of the ways a test reads its blocks.</summary>
    public static class Quantifiers
    {
        /// <summary>Whether <paramref name="quantifier" /> reads a number on every block and tests one total of them: a sum, an average, the lowest or the highest.</summary>
        public static bool Totals(Quantifier quantifier)
        {
            return quantifier == Quantifier.Sum || quantifier == Quantifier.Average || quantifier == Quantifier.Min || quantifier == Quantifier.Max;
        }

        /// <summary>Whether <paramref name="quantifier" /> tests a number whatever it is compared by: a sum, the lowest or the highest.</summary>
        public static bool Numeric(Quantifier quantifier)
        {
            return quantifier == Quantifier.Sum || quantifier == Quantifier.Min || quantifier == Quantifier.Max;
        }
    }
}
