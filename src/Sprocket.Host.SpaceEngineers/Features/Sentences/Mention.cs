
namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>One piece of how a part of a sentence is said: a block, named and shown by its icon, or the words around one.</summary>
    /// <remarks>Words carry their own spaces, so the pieces are laid side by side as they come.</remarks>
    public sealed class Mention
    {
        /// <summary>Says <paramref name="text" />, as a block drawn by <paramref name="icon" /> where there is one, and as words for null.</summary>
        public Mention(string text, string icon)
        {
            Text = text;
            Icon = icon;
        }

        /// <summary>What is said: a block's name, or words.</summary>
        public string Text { get; }

        /// <summary>The block's icon, as its kind names it, or null for words.</summary>
        public string Icon { get; }
    }
}
