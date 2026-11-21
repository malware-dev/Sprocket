namespace Sprocket.Host.SpaceEngineers.Framework.Model.Expressions
{
    /// <summary>Why an expression has no value.</summary>
    public enum ExpressionFailure
    {
        /// <summary>It has one.</summary>
        None,

        /// <summary>It divides by zero.</summary>
        DividedByZero,

        /// <summary>A reading, or the smallest, largest or average of readings, has no block to take a value from.</summary>
        NoValue,

        /// <summary>A reading names more than one block, and nothing says how to combine their values.</summary>
        ManyBlocks,

        /// <summary>An operand is of a kind its operator does not take.</summary>
        Mistyped,

        /// <summary>A setting it names has no value in the installation.</summary>
        Unanswered,

        /// <summary>It comes to a number too large for an integer, a real or a length of time.</summary>
        Overflowed
    }
}
