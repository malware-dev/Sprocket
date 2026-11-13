using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Plugins;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>
    /// The dedicated server plugin that runs a game test once the world is ready, then stops the server. It reads the scenarios'
    /// blueprints from the folder SPROCKET_GAMETEST_BLUEPRINTS and writes its report to SPROCKET_GAMETEST_REPORT; without both it does nothing.
    /// With SPROCKET_GAMETEST_PHASE set to reload it runs the tests of a world an earlier run saved; set to joined, it stands in for
    /// the other players while a client joined to it runs its tests (Run-JoinedTests.ps1); otherwise it runs every test and saves
    /// the world for that run.
    /// </summary>
    public sealed class ServerTestPlugin : IPlugin
    {
        GameTestReport _report;
        IEnumerator<int> _steps;
        int _waiting;

        /// <inheritdoc />
        public void Init(object gameInstance)
        {
            var blueprints = Environment.GetEnvironmentVariable("SPROCKET_GAMETEST_BLUEPRINTS");
            var report = Environment.GetEnvironmentVariable("SPROCKET_GAMETEST_REPORT");
            if (string.IsNullOrEmpty(blueprints) || string.IsNullOrEmpty(report))
                return;

            _report = new GameTestReport(report);
            var phase = Environment.GetEnvironmentVariable("SPROCKET_GAMETEST_PHASE");
            _steps = phase == "reload" ? Reloaded() : phase == "joined" ? Joined(blueprints) : Steps(blueprints);
        }

        /// <inheritdoc />
        /// <remarks>Called by the game once a frame. It does nothing until the world is ready, and nothing after the test ends.</remarks>
        public void Update()
        {
            if (_steps == null || MySession.Static == null || !MySession.Static.Ready)
                return;

            if (_waiting > 0)
            {
                _waiting--;
                return;
            }

            bool more;
            try
            {
                more = _steps.MoveNext();
            }
            catch (Exception exception)
            {
                _report.Fail("run", exception.ToString());
                more = false;
            }

            if (more)
            {
                _waiting = _steps.Current;
                return;
            }

            _steps = null;
            _report.Finish();

            // Ended at once rather than shut down, so the world is as the tests saved it, never as the server would on its way out.
            Environment.Exit(_report.IsClean ? 0 : 1);
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        IEnumerator<int> Steps(string blueprints)
        {
            // Two seconds of a running world before anything is pasted into it.
            yield return 2 * PluginUpdates.PerSecond;

            var tests = new[] { new StockWatchGameTest(blueprints, _report).Run(), new MoveItemsGameTest(blueprints, _report).Run(), new MoveAmountsGameTest(blueprints, _report).Run(), new InventoryReadingsGameTest(blueprints, _report).Run(), new RefineryMovesGameTest(blueprints, _report).Run(), new ProjectorGameTest(_report).Run(), new PistonStatusGameTest(blueprints, _report).Run(), new DoorGameTest(_report).Run(), new RailgunGameTest(_report).Run(), new ScriptLimitGameTest(blueprints, _report).Run(), new ButtonWiringGameTest(blueprints, _report).Run(), new RotorBrakingGameTest(_report).Run(), new ForcedDoorGameTest(blueprints, _report).Run(), new LaserAntennaGameTest(_report).Run(), new TurretControllerGameTest(_report).Run(), new WheelHeightGameTest(_report).Run(), new OldAirlockGameTest(blueprints, _report).Run(), new HeadOffsetGameTest(_report).Run(), new SensorGameTest(_report).Run() };
            foreach (var test in tests)
            {
                while (test.MoveNext())
                    yield return test.Current;
            }

            // The world as the tests left it, for the run that loads it (OldAirlockReloadGameTest). The save is written in the
            // background, so it is given time to finish before the server ends.
            MyAPIGateway.Session.Save();
            yield return 20 * PluginUpdates.PerSecond;
        }

        /// <summary>
        /// Waits for a client to join, then pastes Sprocket Kept Count, gives it to a new NPC the client is not shared with and its
        /// programmable block to the client, so the client reaches the block's terminal and Sprocket refuses it the construct's
        /// automation (TerminalNoticeClientTest); ends once the client has left.
        /// </summary>
        IEnumerator<int> Joined(string blueprints)
        {
            // The client starts once this server is up and takes a few minutes to load: ten minutes is many times that.
            var players = new List<IMyPlayer>();
            for (var waited = 0; waited < 600 && players.Count == 0; waited++)
            {
                players.Clear();
                MyAPIGateway.Players.GetPlayers(players);
                yield return PluginUpdates.PerSecond;
            }

            _report.Check("joined client", players.Count > 0, players.Count > 0 ? $"{players[0].DisplayName} joined" : "no client joined within ten minutes");
            if (players.Count == 0)
                yield break;

            // Ten seconds for the client to spawn, then the construct thirty metres from it: in sight, and clear of it.
            yield return 10 * PluginUpdates.PerSecond;
            var near = players[0].GetPosition() + new Vector3D(0, 0, -30);
            var pasted = new PastedBlueprint(Path.Combine(blueprints, "Sprocket Kept Count", "bp.sbc"), near);
            var automation = pasted.Find("Kept Automation");
            _report.Check("joined paste", automation != null, automation != null ? "Sprocket Kept Count stands for the client" : "Kept Automation is missing after the paste");
            if (automation == null)
                yield break;

            yield return 5 * PluginUpdates.PerSecond;
            var stranger = MySession.Static.Players.CreateNewNpcIdentity("Sprocket Test Stranger");
            automation.CubeGrid.ChangeGridOwnership(stranger.IdentityId, MyOwnershipShareModeEnum.None);
            yield return 3 * PluginUpdates.PerSecond;

            // The player's own programmable block on the stranger's construct, as one placed on a grid that is not theirs: its
            // terminal opens for them, and the construct's automation is the stranger's. The player is brought to stand before it,
            // as a script moves a player, since a client does not move its own character on a server.
            // The game hands over only a block nobody owns, so it is first given up, then given.
            ((MyCubeBlock)automation).ChangeBlockOwnerRequest(0, MyOwnershipShareModeEnum.None);
            yield return 2 * PluginUpdates.PerSecond;
            ((MyCubeBlock)automation).ChangeBlockOwnerRequest(players[0].IdentityId, MyOwnershipShareModeEnum.None);
            yield return 3 * PluginUpdates.PerSecond;
            MyVisualScriptLogicProvider.SetPlayersPosition(players[0].IdentityId, automation.GetPosition() + automation.WorldMatrix.Forward * 2);
            _report.Pass("joined owner", $"Sprocket Kept Count belongs to {stranger.IdentityId}, shared with nobody, but Kept Automation to {players[0].DisplayName}");

            // Sprocket Status Pages, the player's own, beside it: the construct JoinedEditsClientTest edits and the server switches.
            var pages = new PastedBlueprint(Path.Combine(blueprints, "Sprocket Status Pages", "bp.sbc"), near + new Vector3D(40, 0, 0));
            var paged = pages.Find("Pages Automation");
            _report.Check("joined pages paste", paged != null, paged != null ? "Sprocket Status Pages stands for the client" : "Pages Automation is missing after the paste");
            if (paged != null)
            {
                yield return 3 * PluginUpdates.PerSecond;
                paged.CubeGrid.ChangeGridOwnership(players[0].IdentityId, MyOwnershipShareModeEnum.None);
            }

            // The client tests run until it leaves: fifteen minutes at most. Meanwhile a logic system is switched where the client
            // asks, as a second client would switch it (#36).
            var signals = Environment.GetEnvironmentVariable("SPROCKET_GAMETEST_SIGNALS");
            var hands = string.IsNullOrEmpty(signals) ? null : new ChangingHandsServer(_report, signals, blueprints);
            for (var waited = 0; waited < 900 && players.Count > 0; waited++)
            {
                yield return PluginUpdates.PerSecond;

                // #2 once the client asks for it, played through to its end.
                if (hands != null && hands.Asked)
                {
                    var played = hands.Run(players[0], near + new Vector3D(-40, 0, 0));
                    hands = null;
                    while (played.MoveNext())
                        yield return played.Current;
                }

                if (paged != null && !string.IsNullOrEmpty(signals))
                {
                    foreach (var on in new[] { false, true })
                    {
                        var switched = Switched(paged, signals, on);
                        while (switched.MoveNext())
                            yield return switched.Current;
                    }
                }

                players.Clear();
                MyAPIGateway.Players.GetPlayers(players);
            }

            _report.Check("joined client left", players.Count == 0, players.Count == 0 ? "the client left" : "the client stayed past fifteen minutes");
        }

        /// <summary>
        /// Switches the logic system the client names in the file switch-on or switch-off in <paramref name="signals" />, of the
        /// construct <paramref name="block" /> keeps, <paramref name="on" /> through the server's own editor of it, and answers with
        /// the file switched-on or switched-off; where the client has asked and not been answered.
        /// </summary>
        IEnumerator<int> Switched(IMyTerminalBlock block, string signals, bool on)
        {
            var word = on ? "on" : "off";
            var asked = Path.Combine(signals, "switch-" + word);
            var done = Path.Combine(signals, "switched-" + word);
            if (!File.Exists(asked) || File.Exists(done))
                yield break;

            var before = Switches(block);

            var name = File.ReadAllText(asked).Trim();
            var editor = InGameEditor.Of(block);
            var found = editor != null && editor.SwitchSystem(name, on);

            _report.Check("joined server switched " + word, found, found ? $"{name} switched {word} on the server" : $"no logic system {name} on {block.CustomName} here");
            yield return 3 * PluginUpdates.PerSecond;
            var now = editor?.IsOn(name);
            _report.Note($"three seconds after, the switches the server's {block.CustomName} carries {(Switches(block) == before ? "are as they were" : "changed")}; the unit's own logic system is {(now == null ? "missing" : now.Value ? "on" : "off")}");
            File.WriteAllText(done, word);
        }

        /// <summary>Everything but the program that <paramref name="block" /> carries in its storage, the switches among it.</summary>
        static string Switches(IMyTerminalBlock block)
        {
            return string.Join(" | ", block.Storage?.Values.Where(value => !value.StartsWith("sprocket ")) ?? Enumerable.Empty<string>());
        }

        IEnumerator<int> Reloaded()
        {
            // Five seconds of a running world, for Sprocket to take up the constructs it loaded.
            yield return 5 * PluginUpdates.PerSecond;

            var test = new OldAirlockReloadGameTest(_report).Run();
            while (test.MoveNext())
                yield return test.Current;
        }
    }
}
