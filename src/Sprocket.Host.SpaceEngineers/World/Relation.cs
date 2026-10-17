
namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// How a reading is measured against something.
    /// </summary>
    /// <remarks>
    /// Called a relation rather than a comparison because these are relational operators, and
    /// because <c>Comparison</c> is already a delegate in <c>System</c> meaning something else
    /// entirely. Arity would have kept the compiler happy; a reader glancing at a file that
    /// imports both would still have had to work it out.
    /// <para>
    /// Which of these mean anything depends on what is being read. Everything can be equal to
    /// something; only a number can be above it. A host declares a property's kind, so the
    /// editor can offer the relations that make sense and no others - the same trick the
    /// rest of the vocabulary uses, where an impossible sentence is unreachable rather than
    /// refused.
    /// </para>
    /// </remarks>
    public enum Relation
    {
        /// <summary>Equal to.</summary>
        Is,
        /// <summary>Not equal to.</summary>
        IsNot,
        /// <summary>Greater than.</summary>
        Above,
        /// <summary>Greater than or equal to.</summary>
        AtLeast,
        /// <summary>Less than.</summary>
        Below,
        /// <summary>Less than or equal to.</summary>
        AtMost
    }
}
