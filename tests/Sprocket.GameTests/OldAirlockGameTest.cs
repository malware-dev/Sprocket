using System;
using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Entities.Blocks;
using VRageMath;
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing row "An old program comes through the upgrade", run in the real game on the Sprocket Old Airlock blueprint with its
    /// program turned back into alpha 21: Cycle Button closes Old Door, moves Old Piston to 2 m and opens the door, and Opened comes
    /// on as the door finishes opening. The program is then read out of the programmable block's storage.
    /// </summary>
    public sealed class OldAirlockGameTest
    {
        // Sprocket's key for a construct's program in its block's storage (BlockStorage.ProgramKey).
        static readonly Guid ProgramKey = new Guid("9F1E4A2C-7D53-4B18-9C6E-2A0D5F3B81E7");

        readonly string _blueprint;
        readonly GameTestReport _report;
        int _thirds;

        /// <summary>The test on the scenario's blueprint in <paramref name="blueprints" />, answering into <paramref name="report" />.</summary>
        public OldAirlockGameTest(string blueprints, GameTestReport report)
        {
            _blueprint = Path.Combine(blueprints, "Sprocket Old Airlock", "bp.sbc");
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var old = Path.Combine(Path.GetDirectoryName(_blueprint), "bp21.sbc");
            File.WriteAllText(old, Older(File.ReadAllText(_blueprint)));

            var pasted = new PastedBlueprint(old, new Vector3D(1200, 0, 0));
            var door = pasted.Find("Old Door") as IMyDoor;
            var piston = pasted.Find("Old Piston") as IMyPistonBase;
            var button = pasted.Find("Cycle Button") as MyButtonPanel;
            var cycled = pasted.Find("Cycled") as IMyFunctionalBlock;
            var opened = pasted.Find("Opened") as IMyFunctionalBlock;
            var automation = pasted.Find("Old Automation");
            if (door == null || piston == null || button == null || cycled == null || opened == null || automation == null)
            {
                _report.Fail("old airlock blueprint", "a block of the airlock is missing after the paste");
                yield break;
            }

            _report.Note("pasted with its program as " + Header(Program(automation)));

            // A blueprint holds the piston without its head (PistonStatusGameTest), and a new door moves only from its second
            // order (DoorGameTest).
            yield return 3 * PluginUpdates.PerSecond;
            piston.GetActionWithName("Add Top Part").Apply(piston);
            door.OpenDoor();
            yield return 3 * PluginUpdates.PerSecond;
            door.CloseDoor();
            var watch = Until(() => door.Status == DoorStatus.Closed);
            while (watch.MoveNext())
                yield return watch.Current;

            cycled.Enabled = false;
            opened.Enabled = false;
            button.ActivateButton(0);
            watch = Until(() => cycled.Enabled);
            while (watch.MoveNext())
                yield return watch.Current;

            yield return PluginUpdates.PerSecond;
            _report.Check("an alpha 21 airlock runs", cycled.Enabled && opened.Enabled && door.Status == DoorStatus.Open
                    && Math.Abs(piston.CurrentPosition - 2f) < 0.01f,
                $"after {_thirds / 3.0:0.#} s Old Door is {door.Status}, Old Piston at {piston.CurrentPosition:0.##} m, Cycled is {Said(cycled)} and Opened {Said(opened)}");

            var program = Program(automation);
            _report.Check("an alpha 21 program is kept in the current draft", program.StartsWith("sprocket 1 alpha 24")
                    && program.Contains("ext.move_to(") && program.Contains("ext.finishes_opening"),
                $"the block keeps it as {Header(program)}, its piston step as {Line(program, "move")} and its trigger as {Line(program, "opening")}");
        }

        /// <summary><paramref name="blueprint" /> with its program as alpha 21 wrote it: the draft on its first line, and member ids spelled with hyphens.</summary>
        /// <remarks>Alpha 22 changed only the spelling of member ids (MemberSpelling), so this is the program alpha 21 wrote.</remarks>
        static string Older(string blueprint)
        {
            return blueprint.Replace("sprocket 1 alpha 24", "sprocket 1 alpha 21")
                .Replace("ext.move_to(", "ext.'move-to'(")
                .Replace("ext.finishes_opening", "ext.'finishes-opening'");
        }

        static string Program(IMyTerminalBlock block)
        {
            string text;
            return block.Storage != null && block.Storage.TryGetValue(ProgramKey, out text) ? text ?? "" : "";
        }

        static string Header(string program)
        {
            var end = program.IndexOf('\n');
            return end < 0 ? $"\"{program}\"" : $"\"{program.Substring(0, end)}\"";
        }

        /// <summary>The first line of <paramref name="program" /> holding <paramref name="word" />, trimmed, or nothing.</summary>
        static string Line(string program, string word)
        {
            foreach (var line in program.Split('\n'))
            {
                if (line.Contains(word))
                    return $"\"{line.Trim()}\"";
            }

            return "nothing";
        }

        static string Said(IMyFunctionalBlock light) => light.Enabled ? "on" : "off";

        /// <summary>Waits in thirds of a second until <paramref name="done" /> holds, for at most fifteen seconds.</summary>
        IEnumerator<int> Until(Func<bool> done)
        {
            _thirds = 0;
            while (!done() && _thirds < 45)
            {
                _thirds++;
                yield return PluginUpdates.PerSecond / 3;
            }
        }
    }
}
