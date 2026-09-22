using System.Collections.Generic;
using System.Text;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Words
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
        /// <summary>The word a set is said with, on its step and wherever else it is said.</summary>
        public const string SetWord = "set";

        const string OnWord = "on";
        const string ToWord = "to";


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
            // it reaches Sprocket is not what the sentence is about. Only one naming nothing has to
            // say that out loud, because it has nothing else to show.
            return trigger.Kind == TriggerKind.Required && trigger.Slots.Count == 0
                ? "When something on this ship says so"
                : "When";
        }


        /// <summary>The words between what starts a trigger and what it does.</summary>
        /// <remarks>
        /// As the program writes it: a moment is followed by "then", "when any closed [ inner ] then
        /// run 2", and a clock by the verb alone, "every 600t run 3". A trigger that does one plain thing
        /// has no verb here, because that thing's own words follow; one that sets a value has "set",
        /// because its value is said by name alone, as on its step.
        /// </remarks>
        public static string Then(TriggerKind kind, bool stops, bool sequence, bool sets)
        {
            var clock = kind == TriggerKind.Every;

            if (stops)
                return clock ? "stop" : "then stop";

            if (sequence)
                return clock ? "run" : "then run";

            if (sets)
                return clock ? SetWord : "then set";

            return clock ? "" : "then";
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
        /// What comes before a step's slots: "wait", "run", "set", or nothing at all.
        /// </summary>
        /// <remarks>
        /// Ordinarily nothing. A step reads as "close the inner door" and the list being a list is
        /// what says when it happens.
        /// </remarks>
        public static string Lead(GridWorld world, SentenceStep step)
        {
            if (step.Kind == StepKind.Run)
                return "run";

            if (step.Kind == StepKind.Set)
                return SetWord;

            return step.Kind == StepKind.Wait ? "wait" : "";
        }


        /// <summary>The word that joins one of a line's slots to what comes before it, or nothing.</summary>
        /// <remarks>
        /// A set reads as the program writes it: set a value on some blocks to something. Every other
        /// line reads with its slots side by side. Worked out from the slots, so a step and a trigger
        /// that does the same thing read the same.
        /// </remarks>
        public static string Joining(IList<SentenceSlot> slots, int slot)
        {
            var first = slots.Count > 0 ? slots[0] as ActionSlot : null;
            if (first == null || first.Action == null || !first.Action.Sets)
                return "";

            if (slot == 1)
                return OnWord;

            return slot == 2 ? ToWord : "";
        }


        /// <summary>A set as one line: "set Charge Mode on Standby Battery to Recharge".</summary>
        /// <remarks>
        /// The words its step reads in, for where it is said as a line rather than drawn as slots.
        /// </remarks>
        public static string Setting(string value, string blocks, string given)
        {
            return SetWord + " " + value + " " + OnWord + " " + blocks + " " + ToWord + " " + given;
        }


        /// <summary>What a slot of a step reads as.</summary>
        /// <remarks>
        /// A duration is said in the unit it was written in, which is ticks.
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
        public static string Given(ValuesSlot slot)
        {
            if (slot.IsEmpty)
                return Empty;

            // What each field of it shows rather than each value it holds. The two part company
            // wherever a field stands over several values or shows something other than the value
            // itself: three numbers are one colour, and a number is the word it stands for.
            var said = "";
            for (var i = 0; i < slot.Filled.Count; i++)
            {
                if (said.Length > 0)
                    said += " ";

                said += Around(slot.Fields, i, slot.Filled[i]);
            }

            return said;
        }

        /// <summary>One value as a sentence says it, with the words its field puts around it.</summary>
        static string Around(IList<Field> fields, int at, string filled)
        {
            if (at >= fields.Count)
                return filled;

            var field = fields[at];
            var said = filled + (field.Unit ?? "");
            return string.IsNullOrEmpty(field.Lead) ? said : field.Lead + " " + said;
        }

        static string Of(GridWorld world, SentenceSlot slot)
        {
            var entities = slot as EntitiesSlot;
            if (entities != null)
                return Entities(world, entities.Entities, entities.RequiresAll);

            var happening = slot as EventSlot;
            if (happening != null)
                return happening.Event == null ? Empty : happening.Event.Text;

            // A value is said by its name alone in its slot, because "set" is said before the slot and
            // is not part of what pressing it changes.
            var action = slot as ActionSlot;
            if (action != null)
            {
                if (action.Action == null)
                    return Empty;

                return action.Action.Sets ? action.Action.Named : action.Action.Text;
            }

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
