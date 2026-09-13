namespace Reactor.Sequences
{
    /// <summary>
    /// What a name may be, in one place.
    /// </summary>
    /// <remarks>
    /// Every screen that takes a name runs it through here rather than trimming it locally, because
    /// a rule kept in four places is a rule with four versions. A target may also refuse a name
    /// earlier and more kindly - telling somebody they have run out of room while they type beats
    /// silently shortening what they wrote - but the model refuses it here regardless, since a target
    /// is the part that can be written wrong.
    /// </remarks>
    public static class Naming
    {
        /// <summary>
        /// How long a name may be.
        /// </summary>
        /// <remarks>
        /// Chosen for what a list row can show rather than for anything it is stored in. A name that
        /// does not fit the row it identifies has already failed at the only job it has, and the
        /// bytes were never the cost.
        /// <para>
        /// One number for systems and sequences both. Two tuned limits would be two things to
        /// remember and two ways for a screen to disagree with the model.
        /// </para>
        /// </remarks>
        public const int MaxLength = 32;

        /// <summary>
        /// A name as it should be stored: trimmed, and cut to <see cref="MaxLength" />.
        /// </summary>
        /// <remarks>
        /// Empty comes back empty, which is how a caller that requires a name knows to refuse. The
        /// cut is a plain truncation rather than anything cleverer - a name arriving over length has
        /// been typed past a limit a target should have shown, so the model's job is to be safe and
        /// the target's is to be kind about it.
        /// </remarks>
        public static string Clean(string name)
        {
            var wanted = (name ?? "").Trim();
            return wanted.Length <= MaxLength ? wanted : wanted.Substring(0, MaxLength).TrimEnd();
        }
    }
}
