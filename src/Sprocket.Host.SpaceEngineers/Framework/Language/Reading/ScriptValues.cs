
namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>
    /// Every value one read of a script produced.
    /// </summary>
    /// <remarks>
    /// Measured: a list per statement was most of what a large program's load allocated, so a statement names a run of
    /// these instead.
    /// </remarks>
    public sealed class ScriptValues
    {
        readonly ValueChunks _values = new ValueChunks();
        readonly ValueChunks _items = new ValueChunks();
        readonly ValueChunks _parts = new ValueChunks();
        readonly ValueChunks _terms = new ValueChunks();

        /// <summary>Every statement's values, each statement a run of them.</summary>
        public ValueChunks Values => _values;

        /// <summary>Every list's items, each list a run of them.</summary>
        public ValueChunks Items => _items;

        /// <summary>Every qualified name's parts, each name a run of them.</summary>
        public ValueChunks Parts => _parts;

        /// <summary>Every expression's terms, each expression a run of them.</summary>
        public ValueChunks Terms => _terms;

        /// <summary>
        /// Whether the script was written for a format earlier than <see cref="Keywords.ContextualDraft" />, where every keyword
        /// is one wherever it stands and a quoted name is never a flag.
        /// </summary>
        public bool Earlier { get; set; }
    }
}
