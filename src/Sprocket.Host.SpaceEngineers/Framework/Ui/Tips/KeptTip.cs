using Lumel;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Tips
{
    /// <summary>A tip that stays while the player has turned tips off, since it says what the screen shows nowhere else: what a button of only a mark does, or why something cannot be used.</summary>
    public class KeptTip : CodexTip
    {
        /// <summary>A kept tip read from <paramref name="markdown" />, in markdown-lite.</summary>
        public KeptTip(string markdown) : base(markdown)
        {
        }

        /// <summary>A kept tip read from <paramref name="template" />, markdown-lite with <c>{slots}</c>, whose slots <paramref name="arguments" /> say.</summary>
        public KeptTip(string template, Arguments arguments) : base(template, arguments)
        {
        }

        Tip _plain;

        /// <summary>This tip with each link given as its words alone, shown while tips are off, so nothing in it opens a tip of its own.</summary>
        /// <remarks>Made the first time it is asked for and kept, since the workspace asks on every frame the pointer rests.</remarks>
        public Tip Plain
        {
            get
            {
                if (_plain == null)
                    _plain = Arguments == null ? new Tip(Unlinked(Text)) : new Tip(Unlinked(Text), Arguments);

                return _plain;
            }
        }

        /// <summary>The markdown-lite <paramref name="markdown" /> with each link, <c>[words](target)</c>, made its words; an image, <c>![words](locator)</c>, stays.</summary>
        static string Unlinked(string markdown)
        {
            var said = new System.Text.StringBuilder(markdown.Length);
            var i = 0;
            while (i < markdown.Length)
            {
                var c = markdown[i];
                if (c == '\\' && i + 1 < markdown.Length)
                {
                    said.Append(c).Append(markdown[i + 1]);
                    i += 2;
                    continue;
                }

                var close = c == '[' && (i == 0 || markdown[i - 1] != '!') ? Closing(markdown, i + 1) : -1;
                var end = close >= 0 && close + 1 < markdown.Length && markdown[close + 1] == '(' ? markdown.IndexOf(')', close + 2) : -1;
                if (end < 0)
                {
                    said.Append(c);
                    i++;
                    continue;
                }

                said.Append(markdown, i + 1, close - i - 1);
                i = end + 1;
            }

            return said.ToString();
        }

        /// <summary>Where the <c>]</c> closing words that start at <paramref name="from" /> in <paramref name="markdown" /> is, past any escaped one, or -1 where none does.</summary>
        static int Closing(string markdown, int from)
        {
            var inner = 0;
            for (var i = from; i < markdown.Length; i++)
            {
                if (markdown[i] == '\\')
                    i++;
                else if (markdown[i] == '[')
                    inner++;
                else if (markdown[i] == ']' && inner == 0)
                    return i;
                else if (markdown[i] == ']')
                    inner--;
            }

            return -1;
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
