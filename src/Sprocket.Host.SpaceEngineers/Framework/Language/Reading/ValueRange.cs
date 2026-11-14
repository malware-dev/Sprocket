
namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>
    /// The items of one list, as a run of the reading's items rather than a list of their own.
    /// </summary>
    /// <remarks>A struct, so asking a statement for a list's items allocates nothing.</remarks>
    public struct ValueRange
    {
        readonly ValueChunks _all;
        readonly int _from;

        /// <summary>The <paramref name="count" /> values of <paramref name="all" /> from <paramref name="from" />.</summary>
        public ValueRange(ValueChunks all, int from, int count) : this()
        {
            _all = all;
            _from = from;
            Count = count;
        }

        /// <summary>How many values it holds.</summary>
        public int Count { get; private set; }

        /// <summary>The value at <paramref name="index" />, counted from the first of the range.</summary>
        public Value this[int index] => _all[_from + index];
    }
}
