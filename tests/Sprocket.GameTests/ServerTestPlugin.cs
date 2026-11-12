using System;
using System.Collections.Generic;
using System.IO;
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

            // The client tests run until it leaves: fifteen minutes at most.
            for (var waited = 0; waited < 900 && players.Count > 0; waited++)
            {
                yield return PluginUpdates.PerSecond;
                players.Clear();
                MyAPIGateway.Players.GetPlayers(players);
            }

            _report.Check("joined client left", players.Count == 0, players.Count == 0 ? "the client left" : "the client stayed past fifteen minutes");
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
