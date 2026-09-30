namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>One piece of how a part of a sentence is said: a block, named and shown by its icon, a member with its marks, or the words around them.</summary>
    /// <remarks>Words carry their own spaces, so the pieces are laid side by side as they come.</remarks>
    public sealed class Mention
    {
        /// <summary>Says <paramref name="text" />, as a block drawn by <paramref name="icon" /> where there is one, and as words for null.</summary>
        public Mention(string text, string icon)
        {
            Text = text;
            Icon = icon;
        }

        Mention(string text, bool waits)
        {
            Text = text;
            IsMember = true;
            Waits = waits;
        }

        /// <summary>What is said: a block's name, a member's words, or words.</summary>
        public string Text { get; }

        /// <summary>The block's icon, as its kind names it, or null for anything else.</summary>
        public string Icon { get; }

        /// <summary>Whether this is a member of a block.</summary>
        public bool IsMember { get; }

        /// <summary>Whether the member is something that has to happen, which a step waits for.</summary>
        public bool Waits { get; }

        /// <summary>A member of a block, said as <paramref name="text" />, marked where it <paramref name="waits" />.</summary>
        public static Mention Member(string text, bool waits)
        {
            return new Mention(text, waits);
        }
    }
}
