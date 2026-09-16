namespace Reactor.Storage
{
    /// <summary>
    /// The items of one list, as a run of the reading's items rather than a list of their own.
    /// </summary>
    /// <remarks>
    /// A struct, so asking a statement for a list's items makes nothing. An empty one is what a position
    /// that holds no list answers, and it never reads its backing list.
    /// </remarks>
    struct ValueRange
    {
        readonly ValueChunks _all;
        readonly int _from;

        public ValueRange(ValueChunks all, int from, int count) : this()
        {
            _all = all;
            _from = from;
            Count = count;
        }

        public int Count { get; private set; }

        public Value this[int index] => _all[_from + index];
    }
}
