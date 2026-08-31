using System.Collections.Generic;
using System.Text;
using Reactor.Sequences;
using Reactor.World;

namespace Reactor.Editing
{
    /// <summary>
    /// The English left in core, and all of it on its way out.
    /// </summary>
    /// <remarks>
    /// This used to word the sentence too, on the reasoning that a sentence is meaning rather than
    /// presentation. The split was drawn one notch too far into words: word order alone disproves it,
    /// since German puts the verb last and core owning the meaning does not move it. The sentence now
    /// says what it refers to and a target says it - see <see cref="Ui.SentenceSlot" />.
    /// <para>
    /// What is left serves the one-line forms a list row and a heading are still strings of, and the
    /// label on a wait preset. Those go the same way; see the order of work in <c>docs/Script.md</c>.
    /// </para>
    /// </remarks>
    static class SequenceText
    {
        /// <summary>Shown in place of an entity that is no longer on the construct.</summary>
        const string MissingEntityText = "something that's gone";

        /// <summary>Shown in a slot the user has not filled in yet.</summary>
        const string EmptySlotText = "…";

        /// <summary>
        /// Names a selection of entities, keeping real names visible for as long as
        /// they fit.
        /// </summary>
        /// <remarks>
        /// Collapsing straight to "4 entities" would turn the thing the user picked
        /// back into an abstraction. Names are what keep a sequence about doors instead
        /// of about identifiers.
        /// </remarks>
        static string DescribeEntities(IWorld world, IList<long> entityIds)
        {
            return Join(world, entityIds, " and ");
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
        static string DescribeEntitiesAny(IWorld world, IList<long> entityIds)
        {
            return Join(world, entityIds, " or ");
        }

        static string Join(IWorld world, IList<long> entityIds, string joiner)
        {
            if (entityIds.Count == 0)
                return EmptySlotText;

            var names = new List<string>();
            for (var i = 0; i < entityIds.Count; i++)
            {
                var entity = world.FindEntity(entityIds[i]);
                names.Add(entity == null ? MissingEntityText : entity.Name);
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
        static string DescribeSources(IWorld world, WhenTrigger when)
        {
            return when.RequiresAll
                ? DescribeEntities(world, when.Sources)
                : DescribeEntitiesAny(world, when.Sources);
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
        static string DescribeGate(IWorld world, StepGate gate)
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

        static string DescribeCondition(IWorld world, Condition condition)
        {
            if (condition == null)
                return EmptySlotText;

            var entities = DescribeEntities(world, condition.Entities);
            var text = EmptySlotText;

            for (var i = 0; i < condition.Entities.Count; i++)
            {
                var entity = world.FindEntity(condition.Entities[i]);
                if (entity == null)
                    continue;

                var state = entity.Kind.FindState(condition.StateId);
                if (state == null)
                    continue;

                text = state.Text;
                break;
            }

            return entities + " " + text;
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

            return text + " " + DescribeEntities(world, step.Targets);
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
