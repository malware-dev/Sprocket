namespace Reactor.Storage
{
    /// <summary>
    /// Every value one read of a script produced.
    /// </summary>
    /// <remarks>
    /// <b>Measured, and it is why this exists.</b> A list per statement was three heap objects per line -
    /// the statement, its list, and the list's array, grown by copying - and on a large program that was
    /// most of what a load allocated, enough that the collector ran inside the load and the cost per
    /// statement stopped being flat. So a statement owns no values; it names a run of these.
    /// <para>
    /// Separate runs because a statement's values have to sit together, and so do a list's items and a
    /// name's parts, and each is read while its statement's values are still being read. Lists do not hold
    /// lists, which is the other half of keeping every run unbroken.
    /// </para>
    /// </remarks>
    sealed class Reading
    {
        readonly ValueChunks _values = new ValueChunks();
        readonly ValueChunks _items = new ValueChunks();
        readonly ValueChunks _parts = new ValueChunks();

        /// <summary>Every statement's values, each statement a run of them.</summary>
        public ValueChunks Values => _values;

        /// <summary>Every list's items, each list a run of them.</summary>
        public ValueChunks Items => _items;

        /// <summary>Every qualified name's parts, each name a run of them.</summary>
        public ValueChunks Parts => _parts;
    }
}
