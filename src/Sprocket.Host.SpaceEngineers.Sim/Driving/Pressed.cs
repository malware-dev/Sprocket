using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Sim.Driving
{
    /// <summary>Button triggers written straight into a program, the way the trigger picker makes them, and read back.</summary>
    public static class Pressed
    {
        /// <summary>
        /// The trigger that is button <paramref name="button" /> of the block <paramref name="declared" />, not yet added to
        /// <paramref name="system" />: a signal variable of <paramref name="program" /> for a system, or a signal slot
        /// answered in its first instance for a system blueprint.
        /// </summary>
        public static SignalTrigger On(Automation program, AutomationSystem system, string declared, int button = 1)
        {
            var at = new ToolbarSlot(1, 1, button);
            if (!system.IsBlueprint)
                return SignalTrigger.Named(program.DeclareSignal(declared, at, declared + " " + button).Name);

            var part = system.TakeTrigger(system.First, new[] { declared }, new[] { at }, "Button Panel", "");
            return SignalTrigger.Named(part);
        }

        /// <summary>The toolbar slots answering the trigger <paramref name="part" /> of <paramref name="system" /> in its first use, or null where none does.</summary>
        public static IList<ToolbarSlot>? Where(Automation program, AutomationSystem system, string part)
        {
            var blocks = new List<string>();
            var at = new List<ToolbarSlot>();
            program.Answers(system, system.First, part, blocks, at);
            return at.Count == 0 ? null : at;
        }

        /// <summary>The blocks answering the trigger <paramref name="part" /> of <paramref name="system" /> in its first use, or null where none does.</summary>
        public static IList<string>? By(Automation program, AutomationSystem system, string part)
        {
            var blocks = new List<string>();
            var at = new List<ToolbarSlot>();
            program.Answers(system, system.First, part, blocks, at);
            return blocks.Count == 0 ? null : blocks;
        }
    }
}
