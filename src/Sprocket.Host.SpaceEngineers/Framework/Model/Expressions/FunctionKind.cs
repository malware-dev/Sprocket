namespace Sprocket.Host.SpaceEngineers.Framework.Model.Expressions
{
    /// <summary>The functions an expression has of its own.</summary>
    public enum FunctionKind
    {
        /// <summary>The smallest of two or more numbers.</summary>
        Min,

        /// <summary>The largest of two or more numbers.</summary>
        Max,

        /// <summary>A number to the nearest integer.</summary>
        Round,

        /// <summary>A number without its sign.</summary>
        Abs
    }
}
