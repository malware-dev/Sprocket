using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Format.Upgrades
{
    /// <summary>Alpha 15: Sprocket's own switch actions became the setting of the power value, and <c>switched-on</c> was named <c>power</c>.</summary>
    public sealed class PowerValue : FormatChange
    {
        // Sprocket's own switch actions as alpha 14 programs persist them, and the value that took them over.
        const string SwitchOn = "on";

        const string SwitchOff = "off";

        const string Power = "power";

        // The power value's id as alpha 14 programs written before it was named power persist it.
        const string SwitchedOn = "switched-on";

        /// <inheritdoc />
        public override int Arrived => 15;

        /// <inheritdoc />
        public override string Program(Automation program, GridWorld world)
        {
            var switched = 0;
            var renamed = 0;
            foreach (var system in program.Systems)
            {
                switched += Switches(system);
                renamed += Renames(system);
            }

            return switched + renamed == 0
                ? ""
                : $"alpha 14: {switched} switch step{(switched == 1 ? "" : "s")} set power, {renamed} use{(renamed == 1 ? "" : "s")} of switched-on named power.";
        }

        /// <summary>Names the power value <c>power</c> wherever <paramref name="system" /> names it <c>switched-on</c>: set, checked or watched.</summary>
        static int Renames(AutomationSystem system)
        {
            var count = 0;
            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                var steps = sequences[i].Steps;
                for (var s = 0; s < steps.Count; s++)
                {
                    if (Rename(steps[s]))
                        count++;
                }
            }

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var when = triggers[i] as WhenTrigger;
                if (when != null && when.Provider == BlockKinds.Own && when.EventId == SwitchedOn)
                {
                    when.EventId = Power;
                    count++;
                }

                if (Rename(triggers[i].Does))
                    count++;
            }

            return count;
        }

        /// <summary>Names the power value <c>power</c> where <paramref name="step" /> sets or checks it as <c>switched-on</c>.</summary>
        static bool Rename(Step step)
        {
            var act = step as Act;
            if (act != null && act.Provider == BlockKinds.Own && act.ActionId == SwitchedOn)
            {
                act.ActionId = Power;
                return true;
            }

            var test = (step as Branch)?.Test as Comparison;
            if (test == null || test.Provider != BlockKinds.Own || test.MemberId != SwitchedOn)
                return false;

            test.MemberId = Power;
            return true;
        }

        /// <summary>Turns every <c>call ext.on</c> and <c>call ext.off</c> of <paramref name="system" /> into the setting of its power value.</summary>
        static int Switches(AutomationSystem system)
        {
            var count = 0;
            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                var steps = sequences[i].Steps;
                for (var s = 0; s < steps.Count; s++)
                {
                    if (Switch(steps[s]))
                        count++;
                }
            }

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                if (Switch(triggers[i].Does))
                    count++;
            }

            return count;
        }

        /// <summary>Makes <paramref name="step" /> set the power value where it called Sprocket's own switch action.</summary>
        static bool Switch(Step step)
        {
            var act = step as Act;
            if (act == null || act.Sets || act.Provider != BlockKinds.Own || (act.ActionId != SwitchOn && act.ActionId != SwitchOff))
                return false;

            var on = act.ActionId == SwitchOn;
            act.ActionId = Power;
            act.Form = ReachKind.Value;
            act.Provider = BlockKinds.Own;
            act.Arguments.Clear();
            act.Arguments.Add(Literal.Of(on));
            return true;
        }
    }
}
