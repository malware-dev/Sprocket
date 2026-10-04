using Lumel.Input;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    /// <summary>One of the ways a row offers to add to the sequence: its words, what it says under the pointer, and what it does.</summary>
    public struct Adder
    {
        /// <summary>An adder saying <paramref name="text" />, explained by <paramref name="about" />, doing <paramref name="adds" />.</summary>
        public Adder(string text, string about, ICommand adds)
        {
            Text = text;
            Shown = StatusRow.Plus + text;
            About = about;
            Adds = adds;
        }

        /// <summary>What it says, without the plus it is shown with.</summary>
        public string Text { get; private set; }

        /// <summary>What it says with the plus, made once so a row shown every tick makes no text of its own.</summary>
        public string Shown { get; private set; }

        /// <summary>What it says under the pointer.</summary>
        public string About { get; private set; }

        /// <summary>What pressing it does, or null while nothing may be added.</summary>
        public ICommand Adds { get; private set; }
    }
}
