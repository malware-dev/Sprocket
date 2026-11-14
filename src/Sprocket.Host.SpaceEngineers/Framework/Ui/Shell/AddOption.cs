using System.Collections.Generic;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
{
    /// <summary>One of the ways a place offers to add something.</summary>
    public struct AddOption
    {
        /// <summary>An adder saying <paramref name="text" />, explained by <paramref name="about" />, doing <paramref name="adds" />.</summary>
        public AddOption(string text, string about, ICommand adds)
        {
            Text = text;
            Shown = StatusRow.Plus + text;
            About = about;
            Adds = adds;
            Choices = null;
        }

        /// <summary>An adder saying <paramref name="text" />, explained by <paramref name="about" />, offering <paramref name="choices" /> in a menu.</summary>
        public AddOption(string text, string about, IList<AddOption> choices)
        {
            Text = text;
            Shown = StatusRow.Plus + text;
            About = about;
            Adds = null;
            Choices = choices;
        }

        /// <summary>What it says, without the plus it is shown with.</summary>
        public string Text { get; private set; }

        /// <summary>What it says with the plus.</summary>
        /// <remarks>Made once, so a row shown every tick allocates no text.</remarks>
        public string Shown { get; private set; }

        /// <summary>What it says under the pointer.</summary>
        public string About { get; private set; }

        /// <summary>What pressing it does, or null while nothing may be added, and for one that offers <see cref="Choices" />.</summary>
        public ICommand Adds { get; private set; }

        /// <summary>What it offers in a menu, or null for one that adds a single thing.</summary>
        public IList<AddOption> Choices { get; private set; }
    }
}
