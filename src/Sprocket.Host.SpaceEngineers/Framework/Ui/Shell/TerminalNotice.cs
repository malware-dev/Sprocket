using Lumel.Input;
using Lumel.Screens;
using Lumel.Text;
using Lumel.Windows;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
{
    /// <summary>A message box shown over the terminal, saying one thing with an OK to close it, in the editor's look.</summary>
    public partial class TerminalNotice : MessageBox
    {
        /// <summary>The box saying <paramref name="message" />, its slots filled from <paramref name="arguments" />, with <paramref name="ok" /> on its one button, which closes it.</summary>
        public TerminalNotice(string message, Arguments arguments, string ok)
        {
            CreateView();

            Message = Template.Parse(message);
            MessageArguments = arguments;
            YesText = ok;
            YesCommand = new RelayCommand(Close);
        }

        /// <summary>Opens a notice in <paramref name="workspace" /> saying <paramref name="message" />, its slots filled from <paramref name="arguments" />, with <paramref name="ok" /> on its one button.</summary>
        /// <remarks>The game dims what is behind it, as it does behind the editor.</remarks>
        public static void Show(Workspace workspace, string message, Arguments arguments, string ok)
        {
            workspace.Open(new TerminalNotice(message, arguments, ok), true);
        }

        /// <summary>A workspace apart from the game with a notice open in it saying <paramref name="message" />, its slots filled from <paramref name="arguments" />, whose one button, <paramref name="ok" />, closes it.</summary>
        public static SprocketWorkspace Hosted(string message, Arguments arguments, string ok)
        {
            var workspace = new SprocketWorkspace();
            Show(workspace, message, arguments, ok);
            return workspace;
        }
    }
}
