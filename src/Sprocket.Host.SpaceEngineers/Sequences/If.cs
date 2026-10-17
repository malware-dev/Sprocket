namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// A step that runs the steps after it, up to the <see cref="Else" /> or <see cref="EndIf" /> that ends its branch, where
    /// its test holds; where it does not, the sequence goes on at that one.
    /// </summary>
    public sealed class If : Branch
    {
        /// <summary>An <c>if</c> testing <paramref name="test" />.</summary>
        public If(Comparison test) : base(test)
        {
        }
    }
}
