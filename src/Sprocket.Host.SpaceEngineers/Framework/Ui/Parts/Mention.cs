namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Parts
{
    /// <summary>One piece of how a part of a sentence is said: a block, named and shown by its icon, something named under the mark of its kind, or the words around them.</summary>
    /// <remarks>Words carry their own spaces, so the pieces are laid side by side as they come.</remarks>
    public sealed class Mention
    {
        /// <summary>Says <paramref name="text" />, as a block drawn by <paramref name="icon" /> where there is one, and as words for null.</summary>
        public Mention(string text, string icon)
        {
            Text = text;
            Icon = icon;
        }

        Mention(string text, bool member, string mark)
        {
            Text = text;
            IsMember = member;
            Mark = mark;
        }

        /// <summary>What is said: a block's name, the words of something marked, or words.</summary>
        public string Text { get; }

        /// <summary>The block's icon, as its kind names it, or null for anything else.</summary>
        public string Icon { get; }

        /// <summary>Whether this is said under the mark of its kind: a member of a block, a moment, a test, or a sequence.</summary>
        public bool IsMember { get; }

        /// <summary>The mark of what kind of thing it is, one of the symbols, or null for anything said without one.</summary>
        public string Mark { get; }

        /// <summary>Something named, said as <paramref name="text" />, under the mark of its kind, <paramref name="mark" />.</summary>
        public static Mention Member(string text, string mark)
        {
            return new Mention(text, true, mark);
        }
    }
}
