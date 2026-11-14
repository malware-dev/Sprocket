
namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>What a piece of script text is, before anything knows what it means.</summary>
    public enum TokenKind
    {
        /// <summary>Nothing left.</summary>
        End,

        /// <summary><c>//</c>, which switches off the step after it on its line.</summary>
        Off,

        /// <summary>The end of a statement.</summary>
        Newline,

        /// <summary>
        /// A name: a bare word that is not one of the format's own, or anything in single quotes.
        /// </summary>
        Identifier,

        /// <summary>A bare word that is one of the format's own, carrying no text.</summary>
        Keyword,

        /// <summary>
        /// Text, in double quotes.
        /// </summary>
        Text,

        /// <summary>A whole number.</summary>
        Integer,

        /// <summary>A number with a point or an exponent.</summary>
        Real,

        /// <summary>A whole number of ticks and its unit, written as one word: <c>600t</c>.</summary>
        Duration,

        /// <summary>Red, green, blue and alpha, packed in that order.</summary>
        Color,

        /// <summary>One line of text somebody typed, without its <c>#</c>.</summary>
        Annotation,

        /// <summary><c>#@</c> and its space, which open an attribute line: a name, a colon, and one value.</summary>
        Attribute,

        /// <summary><c>:</c>, between an attribute's name and its value.</summary>
        Colon,

        /// <summary><c>{</c></summary>
        BlockOpen,

        /// <summary><c>}</c></summary>
        BlockClose,

        /// <summary><c>[</c></summary>
        ListOpen,

        /// <summary><c>]</c></summary>
        ListClose,

        /// <summary><c>(</c>, which opens a vector or a member's arguments.</summary>
        VectorOpen,

        /// <summary><c>)</c></summary>
        VectorClose,

        /// <summary><c>,</c>, between the items of a list or a member's arguments.</summary>
        Comma,

        /// <summary>
        /// Joins the parts of a name: <c>MalTek.Airlock</c>.
        /// </summary>
        Dot,

        /// <summary>The <c>=</c> that says what a variable holds or what answers a slot.</summary>
        Equals,

        /// <summary>One of the comparisons, <c>==</c>, <c>!=</c>, <c>&lt;</c>, <c>&gt;</c>, <c>&lt;=</c> or <c>&gt;=</c>, carried as its keyword.</summary>
        Comparison,

        /// <summary><c>|</c>, between the options of a type.</summary>
        Bar,

        /// <summary>
        /// Text that is not a token at all, carrying what is wrong with it.
        /// </summary>
        Bad
    }
}
