namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// A step that runs the steps after it, up to the <see cref="Else" /> or <see cref="EndIf" /> that ends its branch, where
    /// its test holds; where it does not, the sequence goes on at that one.
    /// </summary>
    /// <remarks>
    /// One line among the sequence's lines, as every step is: its branch is the lines after it, and they are counted as
    /// any others. The script writes it as a block.
    /// </remarks>
    public sealed class If : Branch
    {
        /// <summary>An <c>if</c> testing <paramref name="test" />.</summary>
        public If(Comparison test) : base(test)
        {
        }
    }
}
