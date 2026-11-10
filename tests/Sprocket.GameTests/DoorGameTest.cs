using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRageMath;
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing rows "reverse on a moving door" and "A stuck door's status", run in the real game on a plain door and a battery
    /// built by the test: what the door's own reverse does while it opens and while it closes, and the status the game reports
    /// for a door told to open with its power cut.
    /// </summary>
    public sealed class DoorGameTest
    {
        readonly GameTestReport _report;
        int _thirds;

        /// <summary>The test, answering into <paramref name="report" />.</summary>
        public DoorGameTest(GameTestReport report)
        {
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var grid = new BuiltGrid(new Vector3D(0, 0, 320), BuiltGrid.Battery(Vector3I.Zero), BuiltGrid.Block<MyObjectBuilder_Door>("", new Vector3I(1, 0, 0)));
            var door = grid.Find<IMyDoor>();
            var battery = grid.Find<IMyBatteryBlock>();
            if (door == null || battery == null)
            {
                _report.Fail("door", "the door or its battery was not built");
                yield break;
            }

            door.CloseDoor();
            var watch = Until(() => door.Status == DoorStatus.Closed);
            while (watch.MoveNext())
                yield return watch.Current;

            // A door built by the test reads opening without moving on its first order, measured: 0 open after 3 s. One whole
            // opening and closing first, so every order after it moves the door.
            door.OpenDoor();
            yield return 3 * PluginUpdates.PerSecond;
            door.CloseDoor();
            watch = Until(() => door.Status == DoorStatus.Closed);
            while (watch.MoveNext())
                yield return watch.Current;

            // Reverse while it opens: the game's own toggle, which Sprocket offers as reverse.
            door.OpenDoor();
            yield return PluginUpdates.PerSecond / 2;
            var before = door.Status;
            var ratio = door.OpenRatio;
            door.GetActionWithName("Open").Apply(door);
            yield return PluginUpdates.PerSecond / 6;
            _report.Check("reverse on an opening door", before == DoorStatus.Opening && door.Status == DoorStatus.Closing,
                $"the door was {before} at {ratio:0.##} open, and after reverse it is {door.Status}");

            watch = Until(() => door.Status == DoorStatus.Closed);
            while (watch.MoveNext())
                yield return watch.Current;

            door.OpenDoor();
            watch = Until(() => door.Status == DoorStatus.Open);
            while (watch.MoveNext())
                yield return watch.Current;

            // Reverse while it closes.
            door.CloseDoor();
            yield return PluginUpdates.PerSecond / 2;
            before = door.Status;
            ratio = door.OpenRatio;
            door.GetActionWithName("Open").Apply(door);
            yield return PluginUpdates.PerSecond / 6;
            _report.Check("reverse on a closing door", before == DoorStatus.Closing && door.Status == DoorStatus.Opening,
                $"the door was {before} at {ratio:0.##} open, and after reverse it is {door.Status}");

            watch = Until(() => door.Status == DoorStatus.Open);
            while (watch.MoveNext())
                yield return watch.Current;

            door.CloseDoor();
            watch = Until(() => door.Status == DoorStatus.Closed);
            while (watch.MoveNext())
                yield return watch.Current;

            // Told to open with no power on the grid: the game leaves it closed, and keeps no order for when the power is back.
            battery.Enabled = false;
            yield return PluginUpdates.PerSecond;
            door.OpenDoor();
            yield return 3 * PluginUpdates.PerSecond;
            var unpowered = door.Status;
            var unpoweredRatio = door.OpenRatio;
            battery.Enabled = true;
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("a door told to open with no power", unpowered == DoorStatus.Closed && unpoweredRatio == 0 && door.Status == DoorStatus.Closed,
                $"3 s after the order it is {unpowered} at {unpoweredRatio:0.##} open, and 3 s after the power is back it is {door.Status} at {door.OpenRatio:0.##} open");

            // Power cut while it opens: it stops where it is and still reads opening.
            door.OpenDoor();
            yield return PluginUpdates.PerSecond / 2;
            battery.Enabled = false;
            ratio = door.OpenRatio;
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("an opening door with its power cut", door.Status == DoorStatus.Opening && door.OpenRatio == ratio,
                $"cut at {ratio:0.##} open, it is {door.Status} at {door.OpenRatio:0.##} open 3 s later");
            battery.Enabled = true;
            watch = Until(() => door.Status == DoorStatus.Open);
            while (watch.MoveNext())
                yield return watch.Current;

            // Power cut while it closes.
            door.CloseDoor();
            yield return PluginUpdates.PerSecond / 2;
            battery.Enabled = false;
            ratio = door.OpenRatio;
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("a closing door with its power cut", door.Status == DoorStatus.Closing && door.OpenRatio == ratio,
                $"cut at {ratio:0.##} open, it is {door.Status} at {door.OpenRatio:0.##} open 3 s later");
            battery.Enabled = true;
        }

        /// <summary>Waits in thirds of a second until <paramref name="done" /> holds, for at most ten seconds.</summary>
        IEnumerator<int> Until(System.Func<bool> done)
        {
            _thirds = 0;
            while (!done() && _thirds < 30)
            {
                _thirds++;
                yield return PluginUpdates.PerSecond / 3;
            }
        }
    }
}
