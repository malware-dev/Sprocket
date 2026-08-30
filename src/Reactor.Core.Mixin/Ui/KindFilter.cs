namespace Reactor.Ui
{
    /// <summary>One kind filter offered beside a search box.</summary>
    public sealed class KindFilter
    {
        public KindFilter(string label, int count, bool isCurrent, int command)
        {
            Label = label;
            Count = count;
            IsCurrent = isCurrent;
            Command = command;
        }

        public string Label { get; set; }

        /// <summary>
        /// How many there are of this kind.
        /// </summary>
        /// <remarks>
        /// A fact rather than a decoration. Which kinds are worth reaching for depends on how
        /// much is behind each one - fourteen doors is a useful narrowing, one antenna is not -
        /// and a target that has room to say so should be able to.
        /// </remarks>
        public int Count { get; set; }

        public bool IsCurrent { get; set; }

        public int Command { get; set; }
    }
}
