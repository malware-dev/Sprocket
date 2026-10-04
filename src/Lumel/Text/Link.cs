namespace Lumel.Text
{
    /// <summary>Where a link in a document leads: a target string that only the host reads.</summary>
    /// <remarks>One link's words may become several runs; they share this one instance, which is how they are one link.</remarks>
    public sealed class Link
    {
        /// <summary>A link to <paramref name="target" />.</summary>
        public Link(string target)
        {
            Target = target;
        }

        /// <summary>What the link leads to, as written between its parentheses.</summary>
        public string Target { get; }
    }
}
