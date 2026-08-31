using System.Collections.Generic;
using System.Text;
using Reactor.Editing;
using Reactor.Runtime;
using Reactor.Sequences;
using Reactor.Host.SpaceEngineers.Presentation.Ui;
using Reactor.Ui;
using Reactor.World;

namespace Reactor.Host.SpaceEngineers.Presentation.Words
{
    /// <summary>
    /// The words a sentence reads as.
    /// </summary>
    /// <remarks>
    /// Core says what a slot refers to and nothing about what it says. Every word in a sentence on
    /// this screen is decided here, which is what lets another target say it differently - or in
    /// another language - without core knowing.
    /// <para>
    /// Static, because a sentence is worded from what it is handed and nothing else, and because the
    /// views ask for a slot's words while laying out rather than through anything they were given.
    /// </para>
    /// </remarks>
    public static class SentenceText
    {

        /// <summary>A slot nobody has filled in yet.</summary>
        public const string Empty = "…";


        /// <summary>Something the sentence names that is no longer on the construct.</summary>
        public const string Missing = "something that's gone";


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
                return TimeText.Rate(duration.Ticks);

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
                return TimeText.Duration(duration.Ticks);

            return Of(world, slot);
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

            var names = new List<string>();
            for (var i = 0; i < entityIds.Count; i++)
            {
                var entity = world.FindEntity(entityIds[i]);
                names.Add(entity == null ? Missing : entity.Name);
            }

            return Joined(names, requiresAll);
        }

        /// <summary>
        /// The same, for a sentence read straight off the model, where the parts are claims.
        /// </summary>
        /// <remarks>
        /// A slot already holds blocks, because the editor resolved them when it built the screen. A
        /// row and a heading are worded from the sequence itself, so they resolve here.
        /// </remarks>
        public static string Named(Bindings bindings, IList<string> refs, bool requiresAll)
        {
            if (refs.Count == 0)
                return Empty;

            var names = new List<string>();
            for (var i = 0; i < refs.Count; i++)
            {
                var entity = bindings.Entity(refs[i]);
                names.Add(entity == null ? Missing : entity.Name);
            }

            return Joined(names, requiresAll);
        }

        static string Joined(List<string> names, bool requiresAll)
        {
            var joiner = requiresAll ? " and " : " or ";

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
                return TimeText.Duration(duration.Ticks);

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
                words.Append(' ').Append(duration != null ? TimeText.Rate(duration.Ticks) : Of(world, gate.Slots[i]));
            }

            return words.ToString();
        }
    }
}
