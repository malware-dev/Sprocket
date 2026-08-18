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
            return Join(world, deviceIds, " and ");
        }

        /// <summary>
        /// Names a selection where any one of them counts, joined with "or".
        /// </summary>
        /// <remarks>
        /// Trigger sources fire on whichever of them happens first, so "and" would
        /// state the opposite of what the sequence does. It also fixes the grammar:
        /// "when the port door or the starboard door finishes opening" agrees, where
        /// "and" would need "finish".
        /// </remarks>
        public static string DescribeDevicesAny(IWorld world, IList<long> deviceIds)
        {
            return Join(world, deviceIds, " or ");
        }

        static string Join(IWorld world, IList<long> deviceIds, string joiner)
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
                return names[0] + joiner + names[1];

            var builder = new StringBuilder();
            builder.Append(names[0]).Append(", ").Append(names[1]);
            builder.Append(joiner).Append(names.Count - 2);
            builder.Append(" more");
            return builder.ToString();
        }

        /// <summary>
        /// Names a trigger's sources with the joiner that says what it actually does.
        /// </summary>
        public static string DescribeSources(IWorld world, WhenTrigger when)
        {
            return when.RequiresAll
                ? DescribeDevices(world, when.Sources)
                : DescribeDevicesAny(world, when.Sources);
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
                return "When " + DescribeSources(world, when) + " " + text;
            }

            var every = trigger as EveryTrigger;
            if (every != null)
                return "Every " + DescribeTicks(every.IntervalTicks);

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
            // A step with a duration and nothing to do is simply a wait, and reads
            // better as one than as "after 5 seconds - nothing".
            if (!step.HasAction)
            {
                var pause = step.Gate as AfterGate;
                if (pause != null)
                    return "wait " + DescribeDuration(pause.Ticks);
            }

            var body = DescribeAction(world, step);
            var gate = DescribeGate(world, step.Gate);
            return gate.Length == 0 ? body : gate + " - " + body;
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
                return "after " + DescribeTicks(after.Ticks);

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

        /// <summary>
        /// A duration in the units a person would say it in.
        /// </summary>
        /// <remarks>
        /// Seconds here rather than ticks, because nobody thinks "wait three hundred
        /// ticks". The tick count is still shown alongside when the duration is being
        /// chosen, which is where the two vocabularies need to meet.
        /// </remarks>
        public static string DescribeDuration(int ticks)
        {
            if (ticks < 60)
                return ticks == 1 ? "1 tick" : ticks + " ticks";

            var seconds = ticks / 60;
            if (seconds < 60)
                return seconds == 1 ? "1 second" : seconds + " seconds";

            var minutes = seconds / 60;
            return minutes == 1 ? "a minute" : minutes + " minutes";
        }

        static string DescribeTicks(int ticks)
        {
            return ticks == 1 ? "tick" : ticks + " ticks";
        }
    }
}
