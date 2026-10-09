using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using Lumel.Windows;
using System.Text;
using Lumel.Drawing;
using VRage.Input;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>A value that is one of more words than fit on a line: the word given, and a list of them all hanging under it when pressed.</summary>
    /// <remarks>
    /// While it holds the keyboard an arrow steps to the next word and a letter to the next word starting with it, round to
    /// the start, so pressing one letter again walks every word it starts. Words stop at either end under the arrows, so
    /// holding one comes to rest on the last.
    /// <para>
    /// A list longer than the room on either side of the line scrolls, and opens with the word given in view.
    /// </para>
    /// </remarks>
    public class ListField : HStack, IField
    {
        const float Mark = 12f;

        /// <summary>Lifts the mark so its middle is level with the middle of the capitals of the word before it.</summary>
        /// <remarks>Measured on the alignment page.</remarks>
        static readonly Thickness Level = new Thickness(0f, -0.5f, 0f, 0.5f);

        readonly List<string> _words = new List<string>();
        readonly List<StringBuilder> _spelled = new List<StringBuilder>();
        readonly List<Element> _rows = new List<Element>();
        readonly Label _said;
        readonly Action _close;

        string _text = "";

        /// <summary>A line saying no word yet, opening a list of <paramref name="words" /> over the screen it is on when pressed.</summary>
        public ListField(IList<string> words)
        {
            Spacing = 8f;
            Style = Styles.ListLine;
            Focusable = true;

            // The word takes what the line has spare, so the mark stands at its end whichever word is given.
            Children.Add(new Label { Style = Styles.ListWord, Placement = Flex.Grow }.As(out _said));
            Children.Add(Fold());

            var rows = new VStack();
            for (var i = 0; i < words.Count; i++)
            {
                var word = words[i];
                _words.Add(word);
                _spelled.Add(new StringBuilder(word));

                var row = new HStack
                {
                    Style = Styles.ListRow,
                    Command = new Relay(delegate { Choose(word); }),
                    Children = { new Label { Style = Styles.ListWord, Text = word } }
                };

                _rows.Add(row);
                rows.Children.Add(row);
            }

            var scroll = new ScrollView { Style = Styles.ScrollView, FitsContent = true, Content = rows };
            var list = new Popup { Style = Styles.List, Anchor = this, Flush = true, Content = scroll };

            Command = new Relay(delegate
            {
                if (list.IsOpen)
                {
                    list.Close();
                    return;
                }

                list.Open();

                var given = _words.IndexOf(_text);
                if (given >= 0)
                    scroll.BringIntoView(_rows[given]);
            });

            _close = list.Close;
        }

        /// <summary>The mark a drop-down line ends with, placed as every drop-down places it.</summary>
        public static Glyph Fold()
        {
            return new Glyph
            {
                Symbol = Symbols.Down, Style = Styles.ListMark, Placement = Fixed.Size(Mark, Mark), Padding = Level
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

            // Exactly that, never the room it was offered: the word grows into whatever it is given when it is placed.
            size.X = widest + Spacing + Mark;
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
