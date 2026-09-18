using System.Collections.Generic;
using System.Text;
using Reactor.Host.SpaceEngineers.Runtime;
using Reactor.Host.SpaceEngineers.Sequences;
using Reactor.Host.SpaceEngineers.World;

namespace Reactor.Host.SpaceEngineers.Words
{
    /// <summary>
    /// The words a sentence reads as.
    /// </summary>
    /// <remarks>
    /// A slot says what it refers to and nothing about what it says. What it reads as is decided
    /// here, out of phrases the blocks themselves supply: a door says "finishes opening", and this
    /// decides that the sentence is "When the inner door finishes opening", in that order, with that
    /// capital. Order and capitals are the parts no block could know and nothing can derive.
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


        /// <summary>The word a trigger's line opens with.</summary>
        public static string Lead(SentenceTrigger trigger)
        {
            if (trigger.Kind == TriggerKind.Every)
                return "Every";

            // A required trigger with slots names its block, so it reads like any other moment: how
            // it reaches Reactor is not what the sentence is about. Only one naming nothing has to
            // say that out loud, because it has nothing else to show.
            return trigger.Kind == TriggerKind.Required && trigger.Slots.Count == 0
                ? "When something on this ship says so"
                : "When";
        }


        /// <summary>What a slot of a trigger reads as.</summary>
        /// <remarks>
        /// A duration in a trigger is a rate, and rates are said in ticks. Seconds would be a promise
        /// the simulation does not keep - it sags under load, and a repeat that claimed seconds would
        /// drift exactly when the machine matters most.
        /// </remarks>
        public static string InTrigger(GridWorld world, SentenceSlot slot)
        {
            var duration = slot as DurationSlot;
            if (duration != null)
                return TimeText.Rate(duration.Ticks);

            return Of(world, slot);
        }


        /// <summary>
        /// What comes before a step's slots: the word "wait", a gate, or nothing at all.
        /// </summary>
        /// <remarks>
        /// Ordinarily nothing. A step reads as "close the inner door" and the list being a list is
        /// what says when it happens.
        /// </remarks>
        public static string Lead(GridWorld world, SentenceStep step)
        {
            if (step.Kind == StepKind.Run)
                return "run";

            return step.Kind == StepKind.Wait ? "wait" : "";
        }


        /// <summary>What a slot of a step reads as.</summary>
        /// <remarks>
        /// A duration in a step is a length of time somebody chose, and is said in the units they
        /// chose it in - nobody thinks "wait three hundred ticks".
        /// </remarks>
        public static string InStep(GridWorld world, SentenceSlot slot)
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
        public static string Entities(GridWorld world, IList<long> entityIds, bool requiresAll)
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


        /// <summary>
        /// The values an action was given, in the order it wanted them.
        /// </summary>
        /// <remarks>
        /// Spelled the way they are written down, so the line on the screen and the line in the
        /// program say the same thing - and so somebody who types a value into this slot sees back
        /// exactly what they typed.
        /// </remarks>
        static string Given(ValuesSlot slot)
        {
            if (slot.IsEmpty)
                return Empty;

            var said = slot.Values[0].Said;
            for (var i = 1; i < slot.Values.Count; i++)
                said += " " + slot.Values[i].Said;

            return said;
        }

        static string Of(GridWorld world, SentenceSlot slot)
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

            var duration = slot as DurationSlot;
            if (duration != null)
                return TimeText.Duration(duration.Ticks);

            var given = slot as ValuesSlot;
            if (given != null)
                return Given(given);

            var sequence = slot as SequenceSlot;
            if (sequence != null)
            {
                if (sequence.IsEmpty)
                    return Empty;

                return sequence.Name.Length > 0 ? sequence.Name : "sequence " + sequence.Number;
            }

            // A slot kind core invented and nobody here has words for. Saying nothing would leave a
            // gap that reads exactly like a slot waiting to be filled, which is a thing that happens.
            return "(" + slot.GetType().Name + ")";
        }

    }
}
