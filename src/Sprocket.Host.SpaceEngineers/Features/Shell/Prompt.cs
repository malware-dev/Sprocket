using System;
using Lumel;
using Lumel.Input;
using Lumel.Text;
using Lumel.Windows;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>A name asked for in a box hanging under whatever was pressed to ask it, with the way on beside the box.</summary>
    /// <remarks>
    /// A name belongs to the control that carries it, so the box opens where the words are. It is left by pressing away or
    /// by Escape, and the way on is dead until what is typed is a name that can be given. The elements it is drawn with are
    /// declared in <c>Prompt.View.cs</c>, and nothing else is.
    /// </remarks>
    public partial class Prompt : Popup
    {
        readonly Relay _taking;

        WindowLayer _layer;
        Func<string, bool> _accepts;
        Action<string> _takes;

        /// <summary>A prompt asking nothing yet, whose way on says <paramref name="take" />.</summary>
        public Prompt(string take)
        {
            CreateView();
            _take.Text = take;

            _taking = new Relay(Take);
            _take.Command = _taking;
            _box.Submit = _taking;
            _box.Cancel = new Relay(Close);
            _box.Edited += Judge;
        }

        /// <summary>The box the name is typed into.</summary>
        public TextBox Box => _box;

        /// <summary>
        /// Asks <paramref name="question" /> under <paramref name="anchor" />, over the screen <paramref name="layer" /> and
        /// <paramref name="input" /> belong to, starting from <paramref name="text" />; a name <paramref name="accepts" />
        /// takes is handed to <paramref name="takes" />.
        /// </summary>
        /// <remarks>With what is there already taken, so typing replaces it.</remarks>
        public void Ask(string question, string text, Element anchor, Func<string, bool> accepts, Action<string> takes,
            WindowLayer layer, InputRoot input)
        {
            _question.Text = question;
            _box.Text = text ?? "";
            _accepts = accepts;
            _takes = takes;
            Anchor = anchor;
            Judge();

            _layer = layer;
            if (layer == null)
                return;

            layer.Open(this);
            if (input == null)
                return;

            input.Focus(_box);
            _box.SelectAll();
        }

        /// <summary>Gives what is typed, where it is a name that can be given, and puts the prompt away.</summary>
        public void Take()
        {
            var said = _box.Text;
            if (_accepts != null && !_accepts(said))
                return;

            Close();

            var takes = _takes;
            if (takes != null)
                takes(said);
        }

        /// <summary>Puts the prompt away, giving nothing.</summary>
        public void Close()
        {
            if (_layer != null)
                _layer.Close(this);
        }

        void Judge()
        {
            _taking.CanExecute = _accepts == null || _accepts(_box.Text);
        }
    }
}
