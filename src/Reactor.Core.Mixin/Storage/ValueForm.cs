namespace Reactor.Storage
{
    /// <summary>
    /// What a value in a script is written as.
    /// </summary>
    /// <remarks>
    /// Not <see cref="World.ValueKind" />, and deliberately: this is the shape the text had, which is
    /// the parser's business, while a kind is what the model holds. They meet in one place - a
    /// declaration's keys - and nowhere else, so keeping them apart is what lets the language have a
    /// vector before the model has anywhere to put one.
    /// </remarks>
    enum ValueForm
    {
        /// <summary>A bare word. May be a keyword, so nothing here is sure it is data.</summary>
        Identifier,

        /// <summary>A quoted string, which is text or an identifier that needed quoting.</summary>
        Text,

        Integer,
        Real,
        Color,
        Vector,
        List
    }
}
