using System;
using System.Collections.Generic;
using Sandbox.Game.World;
using VRage.Plugins;

namespace Sprocket.GameTests
{
    /// <summary>
    /// The dedicated server plugin that runs a game test once the world is ready, then stops the server. It reads the scenarios'
    /// blueprints from the folder SPROCKET_GAMETEST_BLUEPRINTS and writes its report to SPROCKET_GAMETEST_REPORT; without both it does nothing.
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
            _steps = Steps(blueprints);
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

            // Ended at once rather than shut down, so the server never saves the world with the test's grids in it.
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

            var tests = new[] { new StockWatchGameTest(blueprints, _report).Run(), new MoveItemsGameTest(blueprints, _report).Run(), new InventoryReadingsGameTest(blueprints, _report).Run(), new RefineryMovesGameTest(blueprints, _report).Run(), new ProjectorGameTest(_report).Run(), new PistonStatusGameTest(blueprints, _report).Run(), new DoorGameTest(_report).Run(), new RailgunGameTest(_report).Run(), new ScriptLimitGameTest(blueprints, _report).Run() };
            foreach (var test in tests)
            {
                while (test.MoveNext())
                    yield return test.Current;
            }
        }
    }
}
