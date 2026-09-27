using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Images;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Ui.Windows;
using System.Text;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRage.Input;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Values
{
    /// <summary>A value that is one of more words than fit on a line: the word given, and a list of them all hanging under it when pressed.</summary>
    /// <remarks>
    /// While it holds the keyboard an arrow steps to the next word and a letter to the next word starting with it, round to
    /// the start, so pressing one letter again walks every word it starts. Words stop at either end under the arrows, so
    /// holding one comes to rest on the last.
    /// </remarks>
    public class ListField : HStack, IField
    {
        const float Mark = 20f;

        readonly List<string> _words = new List<string>();
        readonly List<StringBuilder> _spelled = new List<StringBuilder>();
        readonly Label _said;
        readonly Action _close;

        string _text = "";

        /// <summary>A line saying no word yet, opening a list of <paramref name="words" /> in <paramref name="windows" /> when pressed.</summary>
        /// <remarks>The windows are read when the list is opened, so they may be given after the field is made.</remarks>
        public ListField(IList<string> words, Func<WindowLayer> windows)
        {
            Spacing = 8f;
            Style = Styles.ListLine;
            Focusable = true;

            Children.Add(new Label { Style = Styles.ListWord }.As(out _said));
            Children.Add(new Glyph { Symbol = Symbols.Down, Style = Styles.Icon, Placement = Fixed.Size(Mark, Mark) });

            var rows = new VStack();
            for (var i = 0; i < words.Count; i++)
            {
                var word = words[i];
                _words.Add(word);
                _spelled.Add(new StringBuilder(word));

                rows.Children.Add(new HStack
                {
                    Style = Styles.ListRow,
                    Command = new Relay(delegate { Choose(word); }),
                    Children = { new Label { Style = Styles.ListWord, Text = word } }
                });
            }

            var list = new Popup { Style = Styles.List, Anchor = this, Content = rows };

            Command = new Relay(delegate
            {
                var layer = windows();
                if (layer == null)
                    return;

                if (layer.IsOpen(list))
                    layer.Close(list);
                else
                    layer.Open(list);
            });

            _close = delegate
            {
                var layer = windows();
                if (layer != null)
                    layer.Close(list);
            };
        }

        /// <inheritdoc />
        public event Action Edited;

        /// <inheritdoc />
        public string Text
        {
            get { return _text; }
            set
            {
                _text = value ?? "";
                _said.Text = _text;
            }
        }

        /// <inheritdoc />
        public bool IsWrong
        {
            set { Style = value ? Styles.ListLineWrong : Styles.ListLine; }
        }

        /// <inheritdoc />
        public Element Keyboard => this;

        /// <summary>As wide as the longest word there is to choose and the mark after it, so choosing another moves nothing.</summary>
        /// <remarks>Measured only when something above it asks the layout again, never while it is drawn.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var size = base.MeasureContent(g, available);

            var widest = 0f;
            for (var i = 0; i < _spelled.Count; i++)
                widest = Math.Max(widest, g.MeasureText(_said.Font, _spelled[i], _said.FontSize).X);

            size.X = Math.Max(size.X, widest + Spacing + Mark);
            return size;
        }

        /// <inheritdoc />
        public override bool OnKey(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            var by = Choosing.By(key);
            if (by == 0)
                return false;

            var word = Choosing.Step(_words, _text, by);
            if (word != null)
                Give(word);

            return true;
        }

        /// <inheritdoc />
        public override void OnText(char c)
        {
            var word = Choosing.Letter(_words, _text, c);
            if (word != null)
                Give(word);
        }

        void Choose(string word)
        {
            _close();
            Give(word);
        }

        void Give(string word)
        {
            Text = word;

            var edited = Edited;
            if (edited != null)
                edited();
        }
    }
}
