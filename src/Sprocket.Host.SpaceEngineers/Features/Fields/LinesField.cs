using System;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>Text written over several lines: where it starts, in the form, and the whole of it on a page of its own.</summary>
    public class LinesField : HStack, IField
    {
        readonly Label _start;
        readonly Button _open;

        string _text = "";

        /// <summary>Empty text of up to <paramref name="longest" /> characters, or any number for zero, its button saying its words in <paramref name="texts" />.</summary>
        public LinesField(int longest, Texts texts)
        {
            Longest = longest;
            Base.Spacing = Gaps.Parts;

            Children.Add(new Label { Style = Styles.ListWord, TextWrapping = TextWrapping.Trim, Placement = Flex.Grow }.As(out _start));

            Children.Add(new Button { Style = Styles.Button, Text = texts.WritingOpen, Focusable = true }.As(out _open));
            _open.Tip = Tip.For(_open.Tip, texts.Codex.WritingOpenAbout);
            _open.Command = new RelayCommand(delegate
            {
                var opened = Opened;
                if (opened != null)
                    opened();
            });
        }

        /// <inheritdoc />
        public event Action Edited;

        /// <summary>Raised when Edit is pressed, to open the page the whole text is written on.</summary>
        public event Action Opened;

        /// <summary>How many characters the text may run to, or zero for no limit.</summary>
        public int Longest { get; }

        /// <inheritdoc />
        public string Text
        {
            get { return _text; }
            set
            {
                _text = value ?? "";
                _start.Text = Start(_text);
            }
        }

        /// <inheritdoc />
        public bool IsWrong
        {
            set { _start.Style = value ? Styles.Problem : Styles.ListWord; }
        }

        /// <inheritdoc />
        public Element Keyboard => _open;

        /// <summary>Takes <paramref name="text" /> as what was written, as the page it was written on is kept.</summary>
        public void Give(string text)
        {
            Text = text;

            var edited = Edited;
            if (edited != null)
                edited();
        }

        /// <summary>The first line of <paramref name="text" /> that has anything but space on it, without the space around it; empty for none.</summary>
        public static string Start(string text)
        {
            var at = 0;
            while (at < text.Length)
            {
                var end = text.IndexOf('\n', at);
                if (end < 0)
                    end = text.Length;

                var line = text.Substring(at, end - at).Trim();
                if (line.Length > 0)
                    return line;

                at = end + 1;
            }

            return "";
        }
    }
}
