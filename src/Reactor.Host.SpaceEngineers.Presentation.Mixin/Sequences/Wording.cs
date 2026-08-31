using System.Collections.Generic;
using System.Text;
using Reactor.Ui;
using Reactor.World;

namespace Reactor.Host.SpaceEngineers.Presentation.Sequences
{
    /// <summary>
    /// The English a sentence reads as.
    /// </summary>
    /// <remarks>
    /// Core says what a sentence refers to and nothing about what it says. Every word a person reads
    /// on this screen is decided here, which is what makes another target able to say it differently -
    /// or in another language - without core knowing.
    /// <para>
    /// English only, and deliberately hardcoded. This host speaks one language; the day it speaks two,
    /// this is the file that changes and nothing above it moves.
    /// </para>
    /// </remarks>
    public static class Wording
    {
        /// <summary>A slot nobody has filled in yet.</summary>
        const string Empty = "…";

        /// <summary>Something the sentence names that is no longer on the construct.</summary>
        const string Missing = "something that's gone";

        /// <summary>The word a trigger opens with.</summary>
        public static string Lead(SentenceTrigger trigger)
        {
            return trigger.Kind == TriggerKind.Every ? "Every" : "When";
        }

        /// <summary>
        /// What comes before a step's slots: the word "wait", a gate, or nothing at all.
        /// </summary>
        /// <remarks>
        /// Ordinarily nothing. A step reads as "close the inner door" and the list being a list is
        /// what says when it happens.
        /// </remarks>
        public static string Lead(IWorld world, SentenceStep step)
        {
            if (step.Kind == StepKind.Wait)
                return "wait";

            return step.Gate == null ? "" : Gate(world, step.Gate);
        }

        /// <summary>What a slot of a trigger reads as.</summary>
        /// <remarks>
        /// A duration in a trigger is a rate, and rates are said in ticks. Seconds would be a promise
        /// the simulation does not keep - it sags under load, and a repeat that claimed seconds would
        /// drift exactly when the machine matters most.
        /// </remarks>
        public static string InTrigger(IWorld world, SentenceSlot slot)
        {
            var duration = slot as DurationSlot;
            if (duration != null)
                return Rate(duration.Ticks);

            return Of(world, slot);
        }

        /// <summary>What a slot of a step reads as.</summary>
        /// <remarks>
        /// A duration in a step is a length of time somebody chose, and is said in the units they
        /// chose it in - nobody thinks "wait three hundred ticks".
        /// </remarks>
        public static string InStep(IWorld world, SentenceSlot slot)
        {
            var duration = slot as DurationSlot;
            if (duration != null)
                return Duration(duration.Ticks);

            return Of(world, slot);
        }

        /// <summary>
        /// A length of time in the units a person would say it in.
        /// </summary>
        /// <remarks>
        /// The tick count is still shown alongside when the duration is being chosen, which is where
        /// the two vocabularies need to meet.
        /// </remarks>
        public static string Duration(int ticks)
        {
            if (ticks < 60)
                return ticks == 1 ? "1 tick" : ticks + " ticks";

            var seconds = ticks / 60;
            if (seconds < 60)
                return seconds == 1 ? "1 second" : seconds + " seconds";

            var minutes = seconds / 60;
            return minutes == 1 ? "a minute" : minutes + " minutes";
        }

        /// <summary>
        /// Names a selection, keeping real names visible for as long as they fit.
        /// </summary>
        /// <remarks>
        /// Collapsing straight to "4 blocks" would turn the thing somebody picked back into an
        /// abstraction. Names are what keep a sequence about doors instead of about identifiers.
        /// </remarks>
        public static string Entities(IWorld world, IList<long> entityIds, bool requiresAll)
        {
            if (entityIds.Count == 0)
                return Empty;

            var joiner = requiresAll ? " and " : " or ";

            var names = new List<string>();
            for (var i = 0; i < entityIds.Count; i++)
            {
                var entity = world.FindEntity(entityIds[i]);
                names.Add(entity == null ? Missing : entity.Name);
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

        static string Of(IWorld world, SentenceSlot slot)
        {
            var entities = slot as EntitiesSlot;
            if (entities != null)
                return Entities(world, entities.Entities, entities.RequiresAll);

            var happening = slot as EventSlot;
            if (happening != null)
                return happening.Event == null ? Empty : happening.Event.Text;

            var action = slot as ActionSlot;
            if (action != null)
                return action.Action == null ? Empty : action.Action.Text;

            var state = slot as StateSlot;
            if (state != null)
                return state.State == null ? Empty : state.State.Text;

            var duration = slot as DurationSlot;
            if (duration != null)
                return Duration(duration.Ticks);

            // A slot kind core invented and nobody here has words for. Saying nothing would leave a
            // gap that reads exactly like a slot waiting to be filled, which is a thing that happens.
            return "(" + slot.GetType().Name + ")";
        }

        static string Gate(IWorld world, SentenceGate gate)
        {
            var words = new StringBuilder();

            if (gate.Kind == GateKind.Once)
                words.Append("once");
            else if (gate.Kind == GateKind.OnlyIf)
                words.Append("only if");
            else
                words.Append("after");

            for (var i = 0; i < gate.Slots.Count; i++)
            {
                var duration = gate.Slots[i] as DurationSlot;
                words.Append(' ').Append(duration != null ? Rate(duration.Ticks) : Of(world, gate.Slots[i]));
            }

            return words.ToString();
        }

        static string Rate(int ticks)
        {
            return ticks == 1 ? "tick" : ticks + " ticks";
        }
    }
}
