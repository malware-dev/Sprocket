using Lumel;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>A tip that stays while the player has turned tips off, since it says what the screen shows nowhere else: what a button of only a mark does, or why something cannot be used.</summary>
    public class KeptTip : Tip
    {
        /// <summary>A kept tip read from <paramref name="markdown" />, in markdown-lite.</summary>
        public KeptTip(string markdown) : base(markdown)
        {
        }

        /// <summary>A kept tip read from <paramref name="template" />, markdown-lite with <c>{slots}</c>, whose slots <paramref name="arguments" /> say.</summary>
        public KeptTip(string template, Arguments arguments) : base(template, arguments)
        {
        }

        /// <summary>The kept tip saying <paramref name="markdown" />: <paramref name="current" /> where it is a kept tip saying just that, a new one otherwise, and null for nothing to say.</summary>
        /// <remarks>For a view that sets its tip on every refresh: the words are parsed only when they change.</remarks>
        public new static Tip For(Tip current, string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return null;

            return current is KeptTip && current.Arguments == null && current.Text == markdown ? current : new KeptTip(markdown);
        }

        /// <summary>The kept tip saying <paramref name="template" />, its slots filled from <paramref name="arguments" />: <paramref name="current" /> where it is a kept tip saying just that, a new one otherwise, and null for nothing to say.</summary>
        public new static Tip For(Tip current, string template, Arguments arguments)
        {
            if (string.IsNullOrEmpty(template))
                return null;

            return current is KeptTip && current.Arguments == arguments && current.Text == template
                ? current
                : new KeptTip(template, arguments);
        }
    }
}
