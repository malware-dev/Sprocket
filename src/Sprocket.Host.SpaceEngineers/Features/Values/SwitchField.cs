using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using VRage.Input;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Values
{
    /// <summary>A value that is one of exactly two words, given with a switch saying them.</summary>
    /// <remarks>
    /// Two words are a switch because a switch is what two words that exclude each other look like everywhere in this game.
    /// Inside a row of its own, so it is as wide as its words rather than as wide as the form.
    /// </remarks>
    public class SwitchField : HStack, IField
    {
        readonly Switch _switch;
        readonly string _on;
        readonly string _off;
        readonly List<string> _words = new List<string>();

        string _text = "";

        /// <summary>A switch between <paramref name="on" /> and <paramref name="off" />, given neither yet.</summary>
        public SwitchField(string on, string off)
        {
            _on = on;
            _off = off;
            _words.Add(on);
            _words.Add(off);
            Focusable = true;

            Children.Add(new Switch { Style = Styles.Switch }.As(out _switch));
            _switch.Says(on, off);
            _switch.Command = new Relay(Flip);
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
                _switch.IsOn = _text == _on;
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

        /// <summary>Gives the other word, or the first where neither has been given.</summary>
        void Flip()
        {
            Give(_text == _on ? _off : _on);
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
