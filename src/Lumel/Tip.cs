using Lumel.Text;

namespace Lumel
{
    /// <summary>What an element says while the pointer rests on it: a document, and the words for its slots where it has them.</summary>
    /// <remarks>
    ///     Read once, when it is made, and drawn by the screen's one tooltip, which lays a changed argument
    ///     out again without reading anything again. Made once and kept, like a style: a tip made afresh is
    ///     a tip read afresh.
    /// </remarks>
    public class Tip
    {
        /// <summary>A tip read from <paramref name="markdown" />, in markdown-lite.</summary>
        public Tip(string markdown) : this(markdown, Markdown.Parse(markdown ?? ""), null)
        {
        }

        /// <summary>A tip read from <paramref name="template" />, markdown-lite with <c>{slots}</c>, whose slots <paramref name="arguments" /> say.</summary>
        public Tip(string template, Arguments arguments) : this(template, Template.Read(template ?? ""), arguments)
        {
        }

        /// <summary>A tip built in code.</summary>
        public Tip(Document document) : this(null, document, null)
        {
        }

        Tip(string text, Document document, Arguments arguments)
        {
            Text = text;
            Document = document;
            Arguments = arguments;
        }

        /// <summary>The markdown-lite this was read from, or null for a tip built in code.</summary>
        public string Text { get; }

        /// <summary>What the tip says.</summary>
        public Document Document { get; }

        /// <summary>The words for the slots in <see cref="Document" />, or null where it has none.</summary>
        public Arguments Arguments { get; }

        /// <summary>The tip saying <paramref name="markdown" />: <paramref name="current" /> where it already says just that, a new one otherwise, and null for nothing to say.</summary>
        /// <remarks>For a view that says its tip on every refresh, so the words are read only when they change.</remarks>
        public static Tip For(Tip current, string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return null;

            return current != null && current.Arguments == null && current.Text == markdown ? current : new Tip(markdown);
        }

        /// <summary>The same, for a template whose slots <paramref name="arguments" /> say.</summary>
        public static Tip For(Tip current, string template, Arguments arguments)
        {
            if (string.IsNullOrEmpty(template))
                return null;

            return current != null && current.Arguments == arguments && current.Text == template
                ? current
                : new Tip(template, arguments);
        }
    }
}
