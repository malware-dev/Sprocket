namespace Reactor.Sequences
{
    /// <summary>
    /// One numbered line of a sequence.
    /// </summary>
    /// <remarks>
    /// Flat and numbered. Nesting is what defeats novices, so there isn't any - every
    /// line sits at the same level and reads top to bottom.
    ///
    /// A line either starts something (<see cref="Act"/>), waits for what has been started
    /// (<see cref="Await"/>), or spends time (<see cref="Wait"/>).
    /// </remarks>
    public abstract class Step
    {
        protected Step()
        {
            Annotation = "";
        }

        /// <summary>
        /// What somebody wrote about this, in their own words, or empty.
        /// </summary>
        /// <remarks>
        /// Data somebody typed, not a comment: it is part of the model, round-tripped like a name, and
        /// shown by whatever is drawing. What a target labels it on screen - "description", "notes",
        /// anything, in any language - is the host's word and has no bearing on this one.
        /// </remarks>
        public string Annotation { get; set; }
    }
}
