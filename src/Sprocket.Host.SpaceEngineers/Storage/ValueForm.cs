
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
    enum ValueForm
    {
        /// <summary>A name: a bare word that is nobody's keyword, or anything in single quotes.</summary>
        Identifier,

        /// <summary>A bare word that is one of the format's own.</summary>
        Keyword,

        /// <summary>Text, in double quotes. Never a name.</summary>
        Text,

        Integer,
        Real,
        Duration,
        Color,

        /// <summary>
        /// Three numbers in brackets, checked and then not kept.
        /// </summary>
        /// <remarks>
        /// Nothing in the model can hold one yet, so there is nowhere for its numbers to go. The syntax is
        /// still read and still checked, so a save that carries one - hand-written, or from a later build -
        /// reads cleanly instead of failing.
        /// </remarks>
        Vector,

        List,

        /// <summary>
        /// A name in parts, <c>MalTek.Airlock</c>, whose parts are a run of <see cref="Reading.Parts" />.
        /// </summary>
        Qualified
    }
}
