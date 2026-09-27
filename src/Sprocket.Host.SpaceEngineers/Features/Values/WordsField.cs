using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using VRage.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Values
{
    /// <summary>A value that is one of three words, given by pressing the one wanted.</summary>
    /// <remarks>Three words fit on one line, and a line of words shows every answer at once, which a list hides until it is opened.</remarks>
    public class WordsField : HStack, IField
    {
        readonly List<string> _words = new List<string>();
        readonly List<Button> _buttons = new List<Button>();

        string _text = "";

        /// <summary>One button for each of <paramref name="words" />, none of them given yet.</summary>
        public WordsField(IList<string> words)
        {
            Spacing = 4f;
            Focusable = true;

            for (var i = 0; i < words.Count; i++)
            {
                var word = words[i];
                _words.Add(word);

                var button = new Button { Style = Styles.Word, Text = word, Command = new Relay(delegate { Give(word); }) };
                _buttons.Add(button);
                Children.Add(button);
            }
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
                for (var i = 0; i < _buttons.Count; i++)
                    _buttons[i].Style = _words[i] == _text ? Styles.WordChosen : Styles.Word;
            }
        }

        /// <inheritdoc />
        public bool IsWrong
        {
            set { Edge = value ? Palette.Problem : Palette.None; }
        }

        /// <inheritdoc />
        public Element Keyboard => this;

        /// <inheritdoc />
        /// <remarks>An arrow steps to the next word, stopping at either end.</remarks>
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
        /// <remarks>A letter goes to the next word starting with it, round to the start.</remarks>
        public override void OnText(char c)
        {
            var word = Choosing.Letter(_words, _text, c);
            if (word != null)
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
