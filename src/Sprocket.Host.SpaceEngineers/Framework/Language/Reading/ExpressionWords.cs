namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>How the operators, functions and combining words of an expression are spelled in a script, and in its terms as read.</summary>
    /// <remarks>Persisted: every program holding an expression spells these words this way.</remarks>
    public static class ExpressionWords
    {
        /// <summary>The operator that adds.</summary>
        public const string Add = "+";

        /// <summary>The operator that subtracts.</summary>
        public const string Subtract = "-";

        /// <summary>The operator that multiplies.</summary>
        public const string Multiply = "*";

        /// <summary>The operator that divides.</summary>
        public const string Divide = "/";

        /// <summary>The term negating the one before it, written as a <c>-</c> ahead of its operand.</summary>
        public const string Negate = "neg";

        /// <summary>The function taking the smallest of its arguments, as a term; written <c>min(</c>.</summary>
        public const string MinFunction = "min()";

        /// <summary>The function taking the largest of its arguments, as a term; written <c>max(</c>.</summary>
        public const string MaxFunction = "max()";

        /// <summary>The function rounding its argument, as a term; written <c>round(</c>.</summary>
        public const string RoundFunction = "round()";

        /// <summary>The function taking the size of its argument, as a term; written <c>abs(</c>.</summary>
        public const string AbsFunction = "abs()";

        /// <summary>The word that combines readings by adding them.</summary>
        public const string Sum = "sum";

        /// <summary>The word that combines readings by averaging them.</summary>
        public const string Average = "average";

        /// <summary>The word that combines readings by taking the smallest.</summary>
        public const string Min = "min";

        /// <summary>The word that combines readings by taking the largest.</summary>
        public const string Max = "max";

        /// <summary>The term spelling the function written <paramref name="name" />, or null where it is no function.</summary>
        public static string FunctionTerm(string name)
        {
            switch (name)
            {
                case "min": return MinFunction;
                case "max": return MaxFunction;
                case "round": return RoundFunction;
                case "abs": return AbsFunction;
            }

            return null;
        }
    }
}
