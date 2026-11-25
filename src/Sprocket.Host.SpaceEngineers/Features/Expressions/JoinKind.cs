namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    /// <summary>What a box of the expression builder does with the values in it.</summary>
    public enum JoinKind
    {
        /// <summary>Adds the two values, or joins them where either is a text.</summary>
        Add,

        /// <summary>Takes the second value from the first.</summary>
        Subtract,

        /// <summary>Multiplies the two values.</summary>
        Multiply,

        /// <summary>Divides the first value by the second.</summary>
        Divide,

        /// <summary>The value with its sign turned around.</summary>
        Negate,

        /// <summary>The lower of the values.</summary>
        Lowest,

        /// <summary>The higher of the values.</summary>
        Highest,

        /// <summary>The value rounded to the nearest integer.</summary>
        Rounded,

        /// <summary>The value without its minus sign.</summary>
        WithoutSign,

        /// <summary>On where the two values are the same.</summary>
        Is,

        /// <summary>On where the two values differ.</summary>
        IsNot,

        /// <summary>On where the first value is higher than the second.</summary>
        Above,

        /// <summary>On where the first value is the second or higher.</summary>
        AtLeast,

        /// <summary>On where the first value is lower than the second.</summary>
        Below,

        /// <summary>On where the first value is the second or lower.</summary>
        AtMost,

        /// <summary>The other flag: on becomes off, off becomes on.</summary>
        Not,

        /// <summary>On where both flags are on.</summary>
        And,

        /// <summary>On where either flag is on.</summary>
        Or
    }
}
