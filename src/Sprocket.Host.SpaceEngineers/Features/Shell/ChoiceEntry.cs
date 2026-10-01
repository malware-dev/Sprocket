using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>One way on offered by the chrome: an answer to a question, or an action at the foot of the screen.</summary>
    public sealed class ChoiceEntry
    {
        /// <summary>An entry reading <paramref name="text" /> that <paramref name="command" /> carries out, lit where <paramref name="isCurrent" />.</summary>
        public ChoiceEntry(string text, bool isCurrent, ICommand command)
        {
            Text = text;
            IsCurrent = isCurrent;
            Command = command;
        }

        /// <summary>What it says.</summary>
        public string Text { get; private set; }

        /// <summary>Whether it is where somebody already is, as the listing's action is on the listing.</summary>
        public bool IsCurrent { get; private set; }

        /// <summary>What choosing it does, or null where the editor refuses it.</summary>
        public ICommand Command { get; private set; }
    }
}
