using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Entities.Blocks;
using VRageMath;
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing row "A forced door against a player", run in the real game on the Sprocket Forced Door blueprint: Force Button opens
    /// the door forced, the door is closed from outside twice as it opens, and it is switched off while forced, then each time
    /// closed again to see whether Sprocket still holds it.
    /// </summary>
    public sealed class ForcedDoorGameTest
    {
        readonly string _blueprint;
        readonly GameTestReport _report;
        readonly List<string> _closed = new List<string>();
        int _reopened;
        int _thirds;

        /// <summary>The test on the scenario's blueprint in <paramref name="blueprints" />, answering into <paramref name="report" />.</summary>
        public ForcedDoorGameTest(string blueprints, GameTestReport report)
        {
            _blueprint = Path.Combine(blueprints, "Sprocket Forced Door", "bp.sbc");
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var pasted = new PastedBlueprint(_blueprint, new Vector3D(1050, 0, 0));
            var door = pasted.Find("Forced Door") as IMyDoor;
            var button = pasted.Find("Force Button") as MyButtonPanel;
            if (door == null || button == null)
            {
                _report.Fail("forced door blueprint", "Forced Door or Force Button is missing after the paste");
                yield break;
            }

            // A new door reads opening without moving on its first order, measured on a built door (DoorGameTest): one whole
            // opening and closing first, so every order after it moves the door.
            yield return 3 * PluginUpdates.PerSecond;
            door.OpenDoor();
            yield return 3 * PluginUpdates.PerSecond;
            door.CloseDoor();
            var watch = Until(() => door.Status == DoorStatus.Closed);
            while (watch.MoveNext())
                yield return watch.Current;

            // Closed from outside twice as it opens forced: Sprocket looks every ten ticks and tells it to open again each time. A large
            // door opens in about 0.75 s, measured (0.67 open 0.5 s after the order), so a door turned back may arrive between two looks
            // and read open rather than opening.
            button.ActivateButton(0);
            yield return PluginUpdates.PerSecond / 2;
            var first = Reverse(door);
            while (first.MoveNext())
                yield return first.Current;

            yield return PluginUpdates.PerSecond / 6;
            var second = Reverse(door);
            while (second.MoveNext())
                yield return second.Current;

            watch = Until(() => door.Status == DoorStatus.Open);
            while (watch.MoveNext())
                yield return watch.Current;

            _report.Check("a forced door closed twice as it opens", _reopened == 2 && door.Status == DoorStatus.Open,
                $"{_reopened} of 2 closings were turned back ({string.Join(", ", _closed)}), and {Waited} after the second it is {door.Status} at {door.OpenRatio:0.##} open");

            // Arrived, it is let go as it says so: closed from outside on the update it is seen open, it stays closed.
            door.CloseDoor();
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("a forced door let go once open", door.Status == DoorStatus.Closed,
                $"3 s after being closed from outside it is {door.Status} at {door.OpenRatio:0.##} open");

            // Switched off while forced, it is let go at once: switched back on and closed, it stays closed.
            button.ActivateButton(0);
            yield return PluginUpdates.PerSecond / 2;
            var opening = door.Status;
            door.Enabled = false;
            yield return PluginUpdates.PerSecond;
            door.Enabled = true;
            door.CloseDoor();
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("a forced door let go when switched off", opening == DoorStatus.Opening && door.Status == DoorStatus.Closed,
                $"it was {opening} when switched off, and 3 s after being switched on and closed it is {door.Status} at {door.OpenRatio:0.##} open");
        }

        string Waited => $"{_thirds / 3.0:0.#} s";

        /// <summary>Closes <paramref name="door" /> from outside and counts it in <see cref="_reopened" /> where it reads opening or open again within two seconds, noting in <see cref="_closed" /> how far open it was.</summary>
        IEnumerator<int> Reverse(IMyDoor door)
        {
            _closed.Add($"closed at {door.OpenRatio:0.##} open");
            door.CloseDoor();
            for (var sixth = 0; sixth < 12; sixth++)
            {
                yield return PluginUpdates.PerSecond / 6;
                if (door.Status != DoorStatus.Opening && door.Status != DoorStatus.Open)
                    continue;

                _reopened++;
                yield break;
            }
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
