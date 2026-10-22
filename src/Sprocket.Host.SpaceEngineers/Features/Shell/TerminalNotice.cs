using Lumel.Drawing;
using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Screens;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using VRageMath;

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
            var stage = Staged(message, arguments, ok);
            var screen = new GameScreenSpace();
            new GameScreen("SprocketNotice", new Vector2(EditorView.ScreenWidth, EditorView.ScreenHeight), stage, screen,
                Materials.Fill, new MarkImages(new ScreenSpaceImages(screen))).Open();
        }

        /// <summary>The stage saying <paramref name="message" />, its slots filled from <paramref name="arguments" />, whose one button, <paramref name="ok" />, closes it.</summary>
        public static Stage Staged(string message, Arguments arguments, string ok)
        {
            var notice = new TerminalNotice(message, arguments, ok);
            var stage = new Stage(notice);
            notice._box.YesCommand = new Relay(stage.Close);
            stage.Accept = new Relay(() => notice._box.TakeAccept());
            return stage;
        }
    }
}
