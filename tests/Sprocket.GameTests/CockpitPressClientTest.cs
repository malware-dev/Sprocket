using System.Collections.Generic;
using System.IO;
using Sandbox.Game.Entities;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Issue #102 in a client joined to the dedicated server, on the Sprocket Cockpit Press construct the server pastes and gives the
    /// player (ServerTestPlugin.Joined): seated in the cockpit, the player uses its first toolbar slot, as pressing its key does,
    /// and the sequence it starts on the server switches Press Light off. A ship's toolbar runs its slot on the client alone, so
    /// the light goes off only where the press reached the server.
    /// </summary>
    public sealed class CockpitPressClientTest
    {
        const string Cockpit = "Press Cockpit";
        const string Light = "Press Light";

        readonly GameTestReport _report;
        readonly string _signals;

        /// <summary>The test, answering into <paramref name="report" />, and asking the server to seat the player through files in <paramref name="signals" />.</summary>
        public CockpitPressClientTest(GameTestReport report, string signals)
        {
            _report = report;
            _signals = signals;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            // The server pastes the construct as the client joins; a minute is many times what that takes to arrive here.
            MyShipController cockpit = null;
            IMyFunctionalBlock light = null;
            for (var waited = 0; waited < 60 && (cockpit == null || light == null); waited++)
            {
                cockpit = EditorTour.Named(Cockpit) as MyShipController;
                light = EditorTour.Named(Light) as IMyFunctionalBlock;
                if (cockpit == null || light == null)
                    yield return PluginUpdates.PerSecond;
            }

            if (cockpit == null || light == null)
            {
                _report.Fail("cockpit press", $"{(cockpit == null ? Cockpit : Light)} did not reach this client");
                yield break;
            }

            // The server runs the logic system once the construct is in; five seconds is many times what that takes.
            yield return 5 * PluginUpdates.PerSecond;
            if (!light.Enabled)
            {
                _report.Fail("cockpit press", $"{Light} was off before the press");
                yield break;
            }

            // Seated by the server, as the game seats a player who uses the cockpit; twenty seconds is many times what that takes.
            File.WriteAllText(Path.Combine(_signals, "seat"), Cockpit);
            for (var waited = 0; waited < 20 && MySession.Static.ControlledEntity != cockpit; waited++)
                yield return PluginUpdates.PerSecond;

            var seated = MySession.Static.ControlledEntity == cockpit;
            _report.Check("cockpit seated", seated, seated ? $"the player sits in {Cockpit}" : $"the player is not in {Cockpit} twenty seconds after asking to be seated");
            if (!seated)
                yield break;

            // The toolbar the game shows a seated player, whose slot 1 the player's key uses.
            yield return PluginUpdates.PerSecond;
            var toolbar = MyToolbarComponent.CurrentToolbar;
            var item = toolbar?.GetItemAtSlot(0);
            _report.Note(item == null
                ? "the toolbar shown to the seated player holds nothing in slot 1"
                : $"the toolbar shown to the seated player holds {item.GetType().Name} \"{item.DisplayName}\" in slot 1, enabled {item.Enabled}");
            if (item == null)
            {
                _report.Fail("cockpit press", "the toolbar shown to the seated player holds nothing in slot 1");
                yield break;
            }

            toolbar.ActivateItemAtSlot(0);

            // The press goes to the server, which runs the sequence; the light's switch comes back to this client.
            var thirds = 0;
            for (; thirds < 30 && light.Enabled; thirds++)
                yield return PluginUpdates.PerSecond / 3;

            _report.Check("cockpit press", !light.Enabled, !light.Enabled
                ? $"{Light} went off {thirds / 3.0:0.#} s after the seated player used slot 1"
                : $"{Light} stayed on 10 s after the seated player used slot 1");

            // Let out again, so what follows starts standing.
            File.WriteAllText(Path.Combine(_signals, "unseat"), Cockpit);
            for (var waited = 0; waited < 10 && MySession.Static.ControlledEntity == cockpit; waited++)
                yield return PluginUpdates.PerSecond;
        }
    }
}
