namespace Sprocket.Host.SpaceEngineers.Framework.Model.Expressions
{
    /// <summary>How a reading combines the values of the blocks it names.</summary>
    public enum Combination
    {
        /// <summary>It does not: it reads one block.</summary>
        None,

        /// <summary>The values added.</summary>
        Sum,

        /// <summary>The values' average.</summary>
        Average,

        /// <summary>The smallest value.</summary>
        Min,

        /// <summary>The largest value.</summary>
        Max,

        /// <summary>Whether a comparison holds on any of the values, a flag.</summary>
        Any,

        /// <summary>Whether a comparison holds on all of the values, a flag.</summary>
        All
    }
}
