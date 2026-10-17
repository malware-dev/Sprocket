
namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>
    /// What a value in a script is written as.
    /// </summary>
    /// <remarks>
    /// Not <see cref="World.ValueKind" />, and deliberately: this is the shape the text had, which is the
    /// parser's business, while a kind is what the model holds. They meet in one place - a declaration's
    /// keys - and nowhere else, so keeping them apart is what lets the language have a vector before the
    /// model has anywhere to put one.
    /// </remarks>
    public enum ValueForm
    {
        /// <summary>A name: a bare word that is nobody's keyword, or anything in single quotes.</summary>
        Identifier,

        /// <summary>A bare word that is one of the format's own.</summary>
        Keyword,

        /// <summary>Text, in double quotes. Never a name.</summary>
        Text,

        /// <summary>A whole number.</summary>
        Integer,

        /// <summary>A number with a point or an exponent.</summary>
        Real,

        /// <summary>A length of time: <c>600t</c>.</summary>
        Duration,

        /// <summary>A color: <c>#FF8800</c>.</summary>
        Color,

        /// <summary>
        /// Three numbers in parentheses, checked and then not kept.
        /// </summary>
        /// <remarks>
        /// Nothing in the model can hold one yet, so there is nowhere for its numbers to go. The syntax is
        /// still read and still checked, so a save that carries one - hand-written, or from a later build -
        /// reads cleanly instead of failing.
        /// </remarks>
        Vector,

        /// <summary>Items in square brackets, separated by commas, a run of <see cref="ScriptValues.Items" />.</summary>
        List,

        /// <summary>
        /// A name in parts, <c>MalTek.Airlock</c>, whose parts are a run of <see cref="ScriptValues.Parts" />.
        /// </summary>
        Qualified,

        /// <summary>
        /// A member and the blocks it is read on or done to, <c>ext.toolbar(1, 1, 4) on [ panel1 ]</c>: the name's parts, the
        /// arguments as a list and the names as a list, a run of <see cref="ScriptValues.Parts" />.
        /// </summary>
        Reference
    }
}
