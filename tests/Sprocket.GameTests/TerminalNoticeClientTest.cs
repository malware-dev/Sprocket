using System.Collections.Generic;
using System.IO;
using VRage.Game.Entity;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.ModAPI;
using VRage.Input;
using VRageMath;
using VRageRender;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing row #53, "The notice over the terminal closes with OK or Enter", in a client joined to the dedicated server: this
    /// player's own programmable block, on a construct the server gave to somebody else, opens in the terminal, its Sprocket button
    /// is refused the construct's automation, and the notice saying why is pictured, then closed with Enter, opened again and closed
    /// with OK.
    /// </summary>
    public sealed class TerminalNoticeClientTest
    {
        // The programmable block of the construct the server pastes and gives away (KeptCount, ServerTestPlugin.Joined).
        const string Automation = "Kept Automation";

        // The notice's button that closes it (MyTexts.resx, Sprocket.Notice.Ok).
        const string Ok = "OK";

        readonly string _shots;
        readonly GameTestReport _report;
        readonly EditorInput _input;

        /// <summary>The test, writing pictures to <paramref name="shots" />, answering into <paramref name="report" />, pressing with <paramref name="input" />.</summary>
        public TerminalNoticeClientTest(string shots, GameTestReport report, EditorInput input)
        {
            _shots = shots;
            _report = report;
            _input = input;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            // The server pastes the construct once this client is in, gives it away and this player the programmable block on it: a
            // minute is many times what that takes.
            IMyTerminalBlock block = null;
            for (var waited = 0; waited < 60 && (block == null || block.OwnerId != MySession.Static.LocalPlayerId); waited++)
            {
                block = EditorTour.Named(Automation);
                yield return PluginUpdates.PerSecond;
            }

            var given = block != null && block.OwnerId == MySession.Static.LocalPlayerId;
            _report.Check("notice construct", given, block == null ? $"no block called {Automation} reached this client"
                : given ? $"{Automation} is this player's, on a construct that is not" : $"{Automation} belongs to {block.OwnerId}");
            if (!given)
                yield break;

            List<IMyTerminalControl> controls;
            MyAPIGateway.TerminalControls.GetControls<IMyProgrammableBlock>(out controls);
            var open = controls.Find(control => control.Id == EditorTour.OpenButton) as IMyTerminalControlButton;
            if (open == null)
            {
                _report.Fail("notice", "the programmable block has no Automation button");
                yield break;
            }

            // Closed with Enter, then opened again and closed with OK.
            for (var round = 0; round < 2; round++)
            {
                MyGuiScreenTerminal.Show(MyTerminalPageEnum.ControlPanel, MySession.Static.LocalCharacter, (MyEntity)block);
                yield return 2 * PluginUpdates.PerSecond;

                open.Action(block);
                yield return 2 * PluginUpdates.PerSecond;
                _report.Check("notice opens", _input.IsOpen, _input.IsOpen ? "the Automation button opened the notice over the terminal" : "the Automation button opened nothing over the terminal");
                if (round == 0)
                {
                    var shot = Picture("notice-over-terminal");
                    while (shot.MoveNext())
                        yield return shot.Current;
                }

                var closer = round == 0 ? "Enter" : Ok;
                var closed = round == 0 ? _input.Press(MyKeys.Enter) : _input.PressWords(Ok);
                yield return PluginUpdates.PerSecond;
                var gone = closed && !_input.IsOpen;
                _report.Check($"notice closed with {closer}", gone, gone ? $"{closer} closed the notice" : closed ? "the notice stayed open" : $"the notice took no {closer}");

                var after = Picture($"notice-closed-with-{closer.ToLowerInvariant()}");
                while (after.MoveNext())
                    yield return after.Current;

                MyGuiScreenTerminal.Hide();
                yield return PluginUpdates.PerSecond;
            }
        }

        /// <summary>Takes a picture called <paramref name="name" /> of the screen as it is.</summary>
        IEnumerator<int> Picture(string name)
        {
            var picture = Path.Combine(_shots, name + ".png");
            if (File.Exists(picture))
                File.Delete(picture);

            MyRenderProxy.TakeScreenshot(Vector2.One, picture, false, false, false);
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check(name, File.Exists(picture), File.Exists(picture) ? picture : $"no picture at {picture}");
        }
    }
}
