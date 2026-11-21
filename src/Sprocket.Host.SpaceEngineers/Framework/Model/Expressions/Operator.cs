namespace Sprocket.Host.SpaceEngineers.Framework.Model.Expressions
{
    /// <summary>What an <see cref="Arithmetic" /> does to its two operands.</summary>
    public enum Operator
    {
        /// <summary>Adds numbers or durations, or joins text.</summary>
        Add,

        /// <summary>Subtracts numbers or durations.</summary>
        Subtract,

        /// <summary>Multiplies numbers, or a duration by a number.</summary>
        Multiply,

        /// <summary>Divides numbers, or a duration by a number.</summary>
        Divide
    }
}
