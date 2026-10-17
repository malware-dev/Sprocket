using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using VRage.Input;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>A value that is on or off, given with a switch saying them.</summary>
    /// <remarks>
    /// A switch because that is what on and off look like everywhere in this game; any other choice is a line of its words,
    /// which has room for longer ones. Inside a row of its own, so it is as wide as its words rather than as wide as the form.
    /// A double press finishes the form, as Done does.
    /// </remarks>
    public class SwitchField : HStack, IField
    {
        readonly Switch _switch;
        readonly ICommand _submit;
        readonly string _on;
        readonly string _off;
        readonly List<string> _words = new List<string>();

        string _text = "";

        /// <summary>A switch between <paramref name="on" /> and <paramref name="off" />, given neither yet; a double press carries out <paramref name="submit" />.</summary>
        public SwitchField(string on, string off, ICommand submit)
        {
            _submit = submit;
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
            if (_switch.Presses > 1)
            {
                _submit.Execute();
                return;
            }

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
