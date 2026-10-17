namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// The next branch of an <see cref="If" />: one that tests again, an <c>else if</c>, or one that runs where nothing held,
    /// an <c>else</c>.
    /// </summary>
    public sealed class Else : Branch
    {
        /// <summary>An <c>else if</c> testing <paramref name="test" />, or an <c>else</c> where it is null.</summary>
        public Else(Comparison test) : base(test)
        {
        }
    }
}
