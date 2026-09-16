namespace Reactor.Host.SpaceEngineers.Core.World
{
    /// <summary>
    /// What the host's things are called, in the words of the people playing.
    /// </summary>
    /// <remarks>
    /// Core calls them entities and always will - it is the honest word for "one addressable
    /// thing" and it belongs to no particular game. But nobody playing Space Engineers has
    /// ever said "entity": they say block, and a screen that says entity to them is a screen
    /// written by somebody who does not play. Another game will say part, or machine, or unit.
    ///
    /// So the word is the host's to give and core's to use. That is the whole of it: three
    /// short strings, supplied once, and core never writes the noun into a sentence itself.
    ///
    /// All three are given rather than worked out. Capitalising a word and pluralising it are
    /// both language-specific, and a core that guessed would be quietly wrong in every
    /// language that does not work like English.
    /// </remarks>
    public sealed class EntityNoun
    {
        public EntityNoun(string one, string many, string heading)
        {
            One = one;
            Many = many;
            Heading = heading;
        }

        /// <summary>One of them, lowercase, mid-sentence: "block".</summary>
        public string One { get; private set; }

        /// <summary>Several, lowercase, mid-sentence: "blocks".</summary>
        public string Many { get; private set; }

        /// <summary>Several, as a heading over a list: "Blocks".</summary>
        public string Heading { get; private set; }
    }
}
