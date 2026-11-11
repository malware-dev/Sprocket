using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Entities.Blocks;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing row "A piston's status at its limit", run in the real game on the Sprocket Piston Status blueprint: the status the
    /// game reports with the piston extended fully, with its speed then set to 0, and after Three Button moves it to 3 m, from above
    /// and from below.
    /// </summary>
    public sealed class PistonStatusGameTest
    {
        readonly string _blueprint;
        readonly GameTestReport _report;
        int _reached;
        int _thirds;

        /// <summary>The test on the scenario's blueprint in <paramref name="blueprints" />, answering into <paramref name="report" />.</summary>
        public PistonStatusGameTest(string blueprints, GameTestReport report)
        {
            _blueprint = Path.Combine(blueprints, "Sprocket Piston Status", "bp.sbc");
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var pasted = new PastedBlueprint(_blueprint, new Vector3D(600, 0, 0));
            var piston = pasted.Find("Status Piston") as IMyPistonBase;
            var button = pasted.Find("Three Button") as MyButtonPanel;
            var arrived = pasted.Find("At Three") as IMyFunctionalBlock;
            if (piston == null || button == null || arrived == null)
            {
                _report.Fail("piston blueprint", "the piston, Three Button or At Three is missing after the paste");
                yield break;
            }

            // The blueprint holds the piston's head as a grid of its own, which the paste attaches.
            piston.LimitReached += delegate { _reached++; };
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("a pasted piston with its head", piston.IsAttached, piston.IsAttached ? "the head came with the paste" : "the piston was pasted without its head");
            if (!piston.IsAttached)
                yield break;

            // A piston moves slower than its speed on the server, measured: 7.58 m in 3 s at a speed of 5, and 6 m in 6 s at 2. So
            // each move is watched until it is done, for at most fifteen seconds.
            piston.Velocity = 5;
            piston.Extend();
            var watch = Until(() => piston.Status != Sandbox.ModAPI.Ingame.PistonStatus.Extending);
            while (watch.MoveNext())
                yield return watch.Current;

            _report.Check("a piston extended fully", piston.Status == Sandbox.ModAPI.Ingame.PistonStatus.Extended && Near(piston, 10),
                $"after {Waited}, at {piston.CurrentPosition:0.##} m the game reports {piston.Status}, and raised LimitReached {_reached} times");

            piston.Velocity = 0;
            yield return PluginUpdates.PerSecond;
            _report.Check("a piston at its end with speed 0", piston.Status == Sandbox.ModAPI.Ingame.PistonStatus.Stopped,
                $"at {piston.CurrentPosition:0.##} m with speed 0 the game reports {piston.Status}");

            // From 10 m down to 3 m: Sprocket's move to puts the lower limit at 3 m and the speed inward.
            arrived.Enabled = false;
            _reached = 0;
            button.ActivateButton(0);
            watch = Until(() => arrived.Enabled);
            while (watch.MoveNext())
                yield return watch.Current;

            _report.Check("a move to 3 m from above", arrived.Enabled && Near(piston, 3),
                $"after {Waited}, at {piston.CurrentPosition:0.##} m the game reports {piston.Status}, raised LimitReached {_reached} times, and At Three is {(arrived.Enabled ? "on" : "off")}");

            // From 0 m up to 3 m, which is the row's case.
            piston.Velocity = -5;
            piston.MinLimit = 0;
            piston.Retract();
            watch = Until(() => piston.Status == Sandbox.ModAPI.Ingame.PistonStatus.Retracted);
            while (watch.MoveNext())
                yield return watch.Current;

            arrived.Enabled = false;
            _reached = 0;
            button.ActivateButton(0);
            watch = Until(() => arrived.Enabled);
            while (watch.MoveNext())
                yield return watch.Current;

            _report.Check("a move to 3 m from below", arrived.Enabled && Near(piston, 3) && piston.Status == Sandbox.ModAPI.Ingame.PistonStatus.Extended,
                $"after {Waited}, at {piston.CurrentPosition:0.##} m the game reports {piston.Status}, raised LimitReached {_reached} times, and At Three is {(arrived.Enabled ? "on" : "off")}");
        }

        string Waited => $"{_thirds / 3.0:0.#} s";

        /// <summary>Waits in thirds of a second until <paramref name="done" /> holds, for at most fifteen seconds.</summary>
        IEnumerator<int> Until(System.Func<bool> done)
        {
            _thirds = 0;
            while (!done() && _thirds < 45)
            {
                _thirds++;
                yield return PluginUpdates.PerSecond / 3;
            }
        }

        static bool Near(IMyPistonBase piston, float position)
        {
            return System.Math.Abs(piston.CurrentPosition - position) < 0.01f;
        }
    }
}
