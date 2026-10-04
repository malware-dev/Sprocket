namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// The next branch of an <see cref="If" />: one that tests again, an <c>else if</c>, or one that runs where nothing held,
    /// an <c>else</c>.
    /// </summary>
    /// <remarks>Reached from the branch before it, it ends that branch, and the sequence goes on after the whole <c>if</c>.</remarks>
    public sealed class Else : Branch
    {
        /// <summary>An <c>else if</c> testing <paramref name="test" />, or an <c>else</c> where it is null.</summary>
        public Else(Comparison test) : base(test)
        {
        }
    }
}
