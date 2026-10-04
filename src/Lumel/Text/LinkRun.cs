namespace Lumel.Text
{
    /// <summary>Words of a link, or a slot among them.</summary>
    public sealed class LinkRun : Run
    {
        /// <summary>The words <paramref name="words" /> of <paramref name="link" />, set apart as <paramref name="kind" /> says.</summary>
        public LinkRun(Link link, string words, RunKind kind)
            : base(words, kind)
        {
            Link = link;
        }

        LinkRun(Link link, RunKind kind, string slot)
            : base(kind, slot)
        {
            Link = link;
        }

        /// <summary>The link the words are part of.</summary>
        public Link Link { get; }

        /// <summary>A slot of <paramref name="link" /> for the argument named <paramref name="name" />, whose words are set apart as <paramref name="kind" /> says.</summary>
        public static LinkRun Slotted(Link link, string name, RunKind kind)
        {
            return new LinkRun(link, kind, name);
        }
    }
}
