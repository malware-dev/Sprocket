using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Features.Look;
using VRage.Input;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>A value that is one of a few words: a line of them, pressing the one wanted, where the line fits, and a list of them where it does not.</summary>
    public class WordsField : Container, IField
    {
        readonly List<string> _words = new List<string>();
        readonly List<Button> _buttons = new List<Button>();
        readonly HStack _line;
        readonly ListField _list;
        readonly ICommand _submit;

        string _text = "";
        bool _folded;

        /// <summary>One button for each of <paramref name="words" />, and the list of them, none given yet; a double press carries out <paramref name="submit" />.</summary>
        public WordsField(IList<string> words, ICommand submit)
        {
            _submit = submit;
            Focusable = true;
            Align = Align.Fill;

            _line = new HStack { Spacing = 4f };
            for (var i = 0; i < words.Count; i++)
            {
                var word = words[i];
                _words.Add(word);

                var button = new Button { Style = Styles.Word, Text = word };
                button.Command = new Relay(() => Pressed(button, word));
                _buttons.Add(button);
                _line.Children.Add(button);
            }

            _list = new ListField(words) { Focusable = false };
            _list.Edited += Listed;

            var held = Hold();
            held.Add(_line);
            held.Add(_list);
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
                _list.Text = _text;
                for (var i = 0; i < _buttons.Count; i++)
                    _buttons[i].Style = _words[i] == _text ? Styles.WordChosen : Styles.Word;
            }
        }

        /// <inheritdoc />
        public bool IsWrong
        {
            set
            {
                _line.Base.Edge = value ? Palette.Problem : Palette.None;
                _list.IsWrong = value;
            }
        }

        /// <inheritdoc />
        public Element Keyboard => this;

        /// <summary>Gives <paramref name="about" />, in markdown-lite, as the tip of what is seen of the field: its row of words, and the list they fold into.</summary>
        public void Explain(string about)
        {
            _line.Tip = Tip.For(_line.Tip, about);
            _list.Tip = _line.Tip;
        }

        /// <summary>Only the form that shows, so the keyboard and the pointer reach nothing hidden.</summary>
        public override int VisualChildCount => 1;

        /// <summary>The form placed against the right of the room.</summary>
        protected override Align Aligns => Align.Right;

        Element Shown => _folded ? _list : (Element)_line;

        /// <inheritdoc />
        public override Element GetVisualChild(int index)
        {
            return Shown;
        }

        /// <inheritdoc />
        protected override bool OnKey(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            var by = KeyboardChoice.By(key);
            if (by == 0)
                return false;

            var word = KeyboardChoice.Step(_words, _text, by);
            if (word != null)
                Give(word);

            return true;
        }

        /// <inheritdoc />
        protected override void OnText(char c)
        {
            var word = KeyboardChoice.Letter(_words, _text, c);
            if (word != null)
                Give(word);
        }

        /// <summary>As wide as the list and as tall as the taller form, whichever ends up showing.</summary>
        /// <remarks>Measured only when something above it asks the layout again, never while it is drawn.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _line.Measure(g, new Vector2(Unbounded, available.Y));
            _list.Measure(g, available);

            return new Vector2(Math.Min(_line.DesiredSize.X, _list.DesiredSize.X),
                Math.Max(_line.DesiredSize.Y, _list.DesiredSize.Y));
        }

        /// <summary>The line where it fits the room it was given, and the list where it does not, against the right and level in the middle.</summary>
        protected override void ArrangeContent(RectangleF bounds)
        {
            _folded = _line.DesiredSize.X > bounds.Width;

            var shown = Shown;
            var wide = Math.Min(shown.DesiredSize.X, bounds.Width);
            var tall = Math.Min(shown.DesiredSize.Y, bounds.Height);
            shown.Arrange(new RectangleF(bounds.Right - wide, bounds.Y + (bounds.Height - tall) * 0.5f, wide, tall));
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            Shown.Draw(g);
        }

        /// <inheritdoc />
        protected override Element UnderContent(Vector2 point)
        {
            return Shown.Under(point);
        }

        void Listed()
        {
            Text = _list.Text;
            Raise();
        }

        void Pressed(Button button, string word)
        {
            Give(word);
            if (button.Presses > 1)
                _submit.Execute();
        }

        void Give(string word)
        {
            Text = word;
            Raise();
        }

        void Raise()
        {
            var edited = Edited;
            if (edited != null)
                edited();
        }
    }
}
