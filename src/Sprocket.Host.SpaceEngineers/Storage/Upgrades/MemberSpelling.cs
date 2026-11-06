using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Storage.Upgrades
{
    /// <summary>Alpha 22: Sprocket's own members are spelled with underscores, so <c>rotate-to</c> is <c>rotate_to</c>, three took clearer names, and an assembler's mode says <c>disassembling</c>.</summary>
    /// <remarks>Slot, sequence and state names stay as they were written, and so do the game's own member ids.</remarks>
    public sealed class MemberSpelling : FormatChange
    {
        /// <inheritdoc />
        public override int Arrived => 22;

        /// <inheritdoc />
        public override string Program(Automation program, GridWorld world)
        {
            var count = 0;
            foreach (var system in program.Systems)
                count += Spells(system);

            return count == 0 ? "" : $"alpha 21: {count} use{(count == 1 ? "" : "s")} of a member spelled with underscores.";
        }

        static int Spells(AutomationSystem system)
        {
            var count = 0;
            foreach (var sequence in system.Sequences)
            {
                foreach (var step in sequence.Steps)
                    count += Spells(step);
            }

            foreach (var trigger in system.Triggers)
            {
                var watched = trigger as WhenTrigger;
                if (watched != null && watched.Provider == BlockKinds.Own && watched.EventId == "mode")
                    watched.Value = Worded(watched.Value);

                var moment = trigger as MomentTrigger;
                if (moment != null && Own(moment) && moment.EventId.Contains("-"))
                {
                    moment.EventId = Spelled(moment.EventId);
                    count++;
                }

                count += Spells(trigger.Does);
            }

            return count;
        }

        static string Spelled(string id)
        {
            switch (id)
            {
                case "label-of-button": return "button_label";
                case "send-a-saved-message": return "transmit_message";
                case "able-to-take-apart": return "able_to_disassemble";
                case "controlled-by-a-person": return "manually_controlled";
                default: return id.Replace('-', '_');
            }
        }

        static bool Own(MomentTrigger trigger)
        {
            var when = trigger as WhenTrigger;
            if (when != null)
                return when.Provider == BlockKinds.Own;

            var signal = trigger as SignalTrigger;
            return signal != null && signal.IsWatched;
        }

        static Literal Worded(Literal value)
        {
            return value.Kind == ValueKind.Name && value.Text == "taking-apart" ? Literal.Name("disassembling") : value;
        }

        static int Spells(Step step)
        {
            var set = step as Act;
            if (set != null && set.Provider == BlockKinds.Own && set.ActionId == "mode")
            {
                for (var i = 0; i < set.Arguments.Count; i++)
                    set.Arguments[i] = Worded(set.Arguments[i]);
            }

            var compared = (step as Branch)?.Test as Comparison;
            if (compared != null && compared.Provider == BlockKinds.Own && compared.MemberId == "mode")
                compared.Value = Worded(compared.Value);

            var act = step as Act;
            if (act != null && act.Provider == BlockKinds.Own && act.ActionId != null && act.ActionId.Contains("-"))
            {
                act.ActionId = Spelled(act.ActionId);
                return 1;
            }

            var test = (step as Branch)?.Test as Comparison;
            if (test == null || test.Provider != BlockKinds.Own || !test.MemberId.Contains("-"))
                return 0;

            test.MemberId = Spelled(test.MemberId);
            return 1;
        }
    }
}
