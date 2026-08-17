using System.Collections.Generic;
using System.Text;
using Reactor.Sequences;
using Reactor.World;

namespace Reactor.Editing
{
    /// <summary>
    /// Turns sequences into the sentences the user reads.
    /// </summary>
    /// <remarks>
    /// This lives in core rather than in a host because it is meaning, not
    /// presentation. A target decides how a slot looks and how it is picked; it does
    /// not get to decide what the sequence says.
    /// </remarks>
    public static class SequenceText
    {
        /// <summary>Shown in place of a device that is no longer on the construct.</summary>
        public const string MissingDeviceText = "something that's gone";

        /// <summary>Shown in a slot the user has not filled in yet.</summary>
        public const string EmptySlotText = "…";

        /// <summary>
        /// Names a selection of devices, keeping real names visible for as long as
        /// they fit.
        /// </summary>
        /// <remarks>
        /// Collapsing straight to "4 devices" would turn the thing the user picked
        /// back into an abstraction. Names are what keep a sequence about doors instead
        /// of about identifiers.
        /// </remarks>
        public static string DescribeDevices(IWorld world, IList<long> deviceIds)
        {
            if (deviceIds.Count == 0)
                return EmptySlotText;

            var names = new List<string>();
            for (var i = 0; i < deviceIds.Count; i++)
            {
                var device = world.FindDevice(deviceIds[i]);
                names.Add(device == null ? MissingDeviceText : device.Name);
            }

            if (names.Count == 1)
                return names[0];
            if (names.Count == 2)
                return names[0] + " and " + names[1];

            var builder = new StringBuilder();
            builder.Append(names[0]).Append(", ").Append(names[1]);
            builder.Append(" and ").Append(names.Count - 2);
            builder.Append(" more");
            return builder.ToString();
        }

        /// <summary>The whole first line: "When the hangar door finishes opening".</summary>
        public static string DescribeTrigger(IWorld world, Trigger trigger)
        {
            var when = trigger as WhenTrigger;
            if (when != null)
            {
                var events = Vocabulary.EventsFor(world, when.Sources);
                var text = EmptySlotText;
                for (var i = 0; i < events.Count; i++)
                {
                    if (events[i].Id == when.EventId)
                    {
                        text = events[i].Text;
                        break;
                    }
                }
                return "When " + DescribeDevices(world, when.Sources) + " " + text;
            }

            var every = trigger as EveryTrigger;
            if (every != null)
                return "Every " + DescribeBeats(every.IntervalBeats);

            return EmptySlotText;
        }

        /// <summary>
        /// One numbered line of the sequence.
        /// </summary>
        /// <remarks>
        /// Ordinarily just "close the inner door". Nothing says when it happens,
        /// because the list being a list already does - a step waits for its own
        /// action to finish before the next one starts. A gate only appears when the
        /// step is waiting on something this sequence did not cause.
        /// </remarks>
        public static string DescribeStep(IWorld world, Step step)
        {
            var body = DescribeAction(world, step);
            var gate = DescribeGate(world, step.Gate);
            return gate.Length == 0 ? body : gate + " — " + body;
        }

        /// <summary>The "once …" or "only if …" prefix, or empty for the usual case.</summary>
        public static string DescribeGate(IWorld world, StepGate gate)
        {
            var once = gate as OnceGate;
            if (once != null)
                return "once " + DescribeCondition(world, once.Condition);

            var only = gate as OnlyIfGate;
            if (only != null)
                return "only if " + DescribeCondition(world, only.Condition);

            var after = gate as AfterGate;
            if (after != null)
                return "after " + DescribeBeats(after.Beats);

            return "";
        }

        public static string DescribeCondition(IWorld world, Condition condition)
        {
            if (condition == null)
                return EmptySlotText;

            var devices = DescribeDevices(world, condition.Devices);
            var text = EmptySlotText;

            for (var i = 0; i < condition.Devices.Count; i++)
            {
                var device = world.FindDevice(condition.Devices[i]);
                if (device == null)
                    continue;

                var state = device.Kind.FindState(condition.StateId);
                if (state == null)
                    continue;

                text = state.Text;
                break;
            }

            return devices + " " + text;
        }

        static string DescribeAction(IWorld world, Step step)
        {
            if (step.Targets.Count == 0)
                return EmptySlotText;

            var actions = Vocabulary.ActionsFor(world, step.Targets);
            var text = EmptySlotText;
            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i].Id == step.ActionId)
                {
                    text = actions[i].Text;
                    break;
                }
            }

            return text + " " + DescribeDevices(world, step.Targets);
        }

        static string DescribeBeats(int ticks)
        {
            return ticks == 1 ? "beat" : ticks + " beats";
        }
    }
}
