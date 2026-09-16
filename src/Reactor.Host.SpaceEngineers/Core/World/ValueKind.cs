namespace Reactor.Host.SpaceEngineers.Core.World
{
    /// <summary>What sort of reading a property gives.</summary>
    /// <remarks>
    /// These are the types the language has, not a convenience for the runtime: a literal written in a
    /// script and a reading taken off a block are the same kind of thing, so they are one system rather
    /// than two with a conversion between them. A conversion is where precision quietly dies.
    /// <para>
    /// The list is not ours to choose freely. <c>ITerminalProperty.TypeName</c> is one of
    /// <c>Boolean</c>, <c>Single</c>, <c>Int64</c>, <c>StringBuilder</c> and <c>Color</c>, and every
    /// block can expose all five, so anything missing here is a reading Reactor cannot carry, and
    /// nothing beyond them is here yet.
    /// </para>
    /// <para>
    /// A vector is deliberately absent. Nothing reads as one - positions do not arrive through the
    /// terminal surface - and there are no unions in a mod, so a field per alternative costs the sum
    /// rather than the largest. Twenty-four bytes on every reading of every property on every tick,
    /// for a case that does not exist, is the wrong trade. A position in a block declaration's match
    /// list is script data the host interprets and never becomes one of these.
    /// </para>
    /// </remarks>
    public enum ValueKind
    {
        Flag,

        /// <summary>
        /// A whole number, held exactly.
        /// </summary>
        /// <remarks>
        /// Separate from <see cref="Real" /> because a <c>double</c> holds integers exactly only to
        /// 2^53, about 9.0x10^15, and an entity id runs to seventeen digits. An id that passes through
        /// a <c>double</c> comes back a different id, silently, and resolves to nothing or to the wrong
        /// block.
        /// </remarks>
        Integer,

        Real,

        Text,

        /// <summary>
        /// Red, green, blue and alpha.
        /// </summary>
        /// <remarks>
        /// A kind of its own rather than just a number, so a comparison knows what it is holding and a
        /// file can write it as #RRGGBBAA rather than as a large integer nobody can read. It is stored
        /// packed inside the number, because there are no unions and four fields would be four fields
        /// every reading carries. Only equality means anything on one - see
        /// <see cref="Relations.Suits" />.
        /// </remarks>
        Color
    }
}
