using Lumel.Input;
using Lumel.Layout;
using Lumel.Screens;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>A message box shown over the terminal, saying one thing with an OK to close it, in the editor's look.</summary>
    public partial class TerminalNotice : VStack
    {
        /// <summary>The box saying <paramref name="message" />, its slots filled from <paramref name="arguments" />, with <paramref name="ok" /> on its one button.</summary>
        public TerminalNotice(string message, Arguments arguments, string ok)
        {
            CreateView();

            _box.Message = Template.Read(message);
            _box.MessageArguments = arguments;
            _box.YesText = ok;
        }

        /// <summary>Opens a screen over the game saying <paramref name="message" />, its slots filled from <paramref name="arguments" />, with <paramref name="ok" /> on its one button.</summary>
        /// <remarks>The game dims what is behind it, as it does behind the editor.</remarks>
        public static void Show(string message, Arguments arguments, string ok)
        {
            Hosted(message, arguments, ok).Open("SprocketNotice");
        }

        /// <summary>The workspace saying <paramref name="message" />, its slots filled from <paramref name="arguments" />, whose one button, <paramref name="ok" />, closes it.</summary>
        public static ScreenSpaceWorkspace Hosted(string message, Arguments arguments, string ok)
        {
            var notice = new TerminalNotice(message, arguments, ok);
            var workspace = new SprocketWorkspace(notice);
            notice._box.YesCommand = new Relay(workspace.Close);
            workspace.Accept = new Relay(() => notice._box.TakeAccept());
            return workspace;
        }
    }
}
