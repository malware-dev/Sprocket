using System;
using System.Collections.Generic;
using Sandbox.Game.World;
using VRage.Plugins;

namespace Sprocket.GameTests
{
    /// <summary>
    /// The dedicated server plugin that runs a game test once the world is ready, then stops the server. It reads the blueprint
    /// from SPROCKET_GAMETEST_BLUEPRINT and writes its report to SPROCKET_GAMETEST_REPORT; without both it does nothing.
    /// </summary>
    public sealed class ServerTestPlugin : IPlugin
    {
        GameTestReport _report;
        IEnumerator<int> _steps;
        int _waiting;

        /// <inheritdoc />
        public void Init(object gameInstance)
        {
            var blueprint = Environment.GetEnvironmentVariable("SPROCKET_GAMETEST_BLUEPRINT");
            var report = Environment.GetEnvironmentVariable("SPROCKET_GAMETEST_REPORT");
            if (string.IsNullOrEmpty(blueprint) || string.IsNullOrEmpty(report))
                return;

            _report = new GameTestReport(report);
            _steps = Steps(blueprint);
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

        IEnumerator<int> Steps(string blueprint)
        {
            // Two seconds of a running world before anything is pasted into it, at 30 plugin updates a second.
            yield return 60;

            var test = new StockWatchGameTest(blueprint, _report).Run();
            while (test.MoveNext())
                yield return test.Current;
        }
    }
}
