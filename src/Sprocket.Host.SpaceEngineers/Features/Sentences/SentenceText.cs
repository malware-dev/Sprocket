using System;
using System.Collections.Generic;
using System.Text;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Fields;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
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
        const string IsWord = "is";

        /// <summary>How many blocks a sentence names before it counts the rest.</summary>
        const int Said = 2;


        /// <summary>A slot nobody has filled in yet.</summary>
        public const string Empty = "…";


        /// <summary>Something the sentence names that is no longer on the construct.</summary>
        public const string Missing = "something that's gone";

        static readonly NameOrder ByName = new NameOrder();


        /// <summary>The word a trigger's line opens with, for a trigger of <paramref name="kind" /> started by <paramref name="parts" /> parts.</summary>
        public static string Lead(TriggerKind kind, int parts)
        {
            if (kind == TriggerKind.Every)
                return "Every";

            // A required trigger with slots names its block, so it reads like any other moment: how
            // it reaches Sprocket is not what the sentence is about. Only one naming nothing has to
            // say that out loud, because it has nothing else to show.
            return kind == TriggerKind.Signal && parts == 0
                ? "When something on this construct starts it"
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

        /// <summary>The same, as pieces added to <paramref name="into" />: each block it names with its icon, and the words around them.</summary>
        public static void InTrigger(GridWorld world, SentenceSlot slot, List<Mention> into)
        {
            if (!Blocks(world, slot, into) && !Marked(world, slot, into))
                into.Add(new Mention(InTrigger(world, slot), null));
        }


        /// <summary>
        /// What comes before a step's slots: "wait", "run", "stop", "set", "if", "otherwise if", "otherwise", or nothing at all.
        /// </summary>
        /// <remarks>
        /// Ordinarily nothing. A step reads as "close the inner door" and the list being a list is
        /// what says when it happens.
        /// </remarks>
        public static string Lead(StepKind kind)
        {
            if (kind == StepKind.Run)
                return "run";

            if (kind == StepKind.Stop)
                return "stop";

            if (kind == StepKind.Set)
                return SetWord;

            if (kind == StepKind.If)
                return "if";

            if (kind == StepKind.OtherwiseIf)
                return "otherwise if";

            if (kind == StepKind.Otherwise)
                return "otherwise";

            return kind == StepKind.Wait ? "wait" : "";
        }


        /// <summary>The word that joins one of a line's slots to what comes before it, or nothing.</summary>
        /// <remarks>
        /// A set reads as the program writes it: set a value on some blocks to something. Every other
        /// line reads with its slots side by side. Worked out from the slots, so a step and a trigger
        /// that does the same thing read the same.
        /// </remarks>
        public static string Joining(IList<SentenceSlot> slots, int slot)
        {
            return Joining(slots, slot, false);
        }

        /// <summary>The same, for a test where <paramref name="tests" /> says so, which reads its value as the program does: a value on some blocks is something.</summary>
        public static string Joining(IList<SentenceSlot> slots, int slot, bool tests)
        {
            var first = slots.Count > 0 ? slots[0] as ActionSlot : null;
            if (first == null || first.Action == null || !first.Action.Sets)
                return "";

            if (slot == 1)
                return OnWord;

            if (slot != 2)
                return "";

            return tests ? IsWord : ToWord;
        }


        /// <summary>A set as one line: "set Charge Mode on Standby Battery to Recharge".</summary>
        /// <remarks>
        /// The words its step reads in, for where it is said as a line rather than drawn as slots.
        /// </remarks>
        public static string Setting(string value, string blocks, string given)
        {
            return SetWord + " " + value + " " + OnWord + " " + blocks + " " + ToWord + " " + given;
        }


        /// <summary>A test being picked, as far as it has got: "If Inner Door is closed", "If On/Off on Airlock Light is …".</summary>
        /// <param name="blocks">The blocks as <see cref="Entities(GridWorld, IList{long}, bool)" /> says them.</param>
        /// <param name="chosen">The state or the value chosen, in the words its list shows it in, or empty.</param>
        /// <param name="value">Whether what is chosen is a value, which reads "the value on the blocks is".</param>
        public static string Testing(string blocks, string chosen, bool value)
        {
            if (chosen.Length == 0)
                chosen = Empty;

            return value
                ? "If " + chosen + " " + OnWord + " " + blocks + " " + IsWord + " " + Empty
                : "If " + blocks + " " + chosen;
        }

        /// <summary>
        /// A trigger or a step being picked, as far as it has got: "When Airlock Button …".
        /// </summary>
        /// <remarks>
        /// In the order and the words of the line it will become, with a part not chosen yet said as
        /// <see cref="Empty" />, so the title of the picker reads as the row it is making.
        /// </remarks>
        /// <param name="forEvents">Whether it is what starts something rather than what is done.</param>
        /// <param name="blocks">The blocks as <see cref="Entities(GridWorld, IList{long}, bool)" /> says them.</param>
        /// <param name="chosen">What is chosen, in the words its list shows it in, or empty.</param>
        /// <param name="sets">Whether what is chosen sets a value, which reads "set it on the blocks".</param>
        public static string Picking(bool forEvents, string blocks, string chosen, bool sets)
        {
            if (chosen.Length == 0)
                chosen = Empty;

            if (forEvents)
                return "When " + blocks + " " + chosen;

            chosen = char.ToUpperInvariant(chosen[0]) + chosen.Substring(1);
            return sets ? chosen + " " + OnWord + " " + blocks : chosen + " " + blocks;
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

        /// <summary>The same, as pieces added to <paramref name="into" />: each block it names with its icon, and the words around them.</summary>
        public static void InStep(GridWorld world, SentenceSlot slot, List<Mention> into)
        {
            if (!Blocks(world, slot, into) && !Marked(world, slot, into))
                into.Add(new Mention(InStep(world, slot), null));
        }

        /// <summary>
        /// Names a selection as pieces added to <paramref name="into" />, each block with its icon, joined the way
        /// <see cref="Entities(GridWorld, IList{long}, bool)" /> joins their names.
        /// </summary>
        public static void Entities(GridWorld world, IList<long> entityIds, bool requiresAll, List<Mention> into)
        {
            if (entityIds.Count == 0)
            {
                into.Add(new Mention(Empty, null));
                return;
            }

            var joiner = requiresAll ? " and " : " or ";
            var named = entityIds.Count > 2 ? 2 : entityIds.Count;
            var entities = Found(world, entityIds);
            var order = InOrder(entities);

            for (var i = 0; i < named; i++)
            {
                if (i > 0)
                    into.Add(new Mention(entityIds.Count == 2 ? joiner : ", ", null));

                var entity = entities[order[i]];
                into.Add(entity == null ? new Mention(Missing, null) : new Mention(entity.Name, entity.Kind.Icon));
            }

            if (entityIds.Count > 2)
                into.Add(new Mention(joiner + (entityIds.Count - 2) + " more", null));
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

            return Joined(Found(world, entityIds), requiresAll);
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

            var entities = new List<GridEntity>(refs.Count);
            for (var i = 0; i < refs.Count; i++)
                entities.Add(bindings.Entity(refs[i]));

            return Joined(entities, requiresAll);
        }

        /// <summary>The blocks <paramref name="entityIds" /> as the world finds them, null for one it does not.</summary>
        static List<GridEntity> Found(GridWorld world, IList<long> entityIds)
        {
            var entities = new List<GridEntity>(entityIds.Count);
            for (var i = 0; i < entityIds.Count; i++)
                entities.Add(world.FindEntity(entityIds[i]));

            return entities;
        }

        /// <summary>The order <paramref name="entities" /> are named in, as places in it, by <see cref="NameOrder" />.</summary>
        static int[] InOrder(List<GridEntity> entities)
        {
            var order = new int[entities.Count];
            for (var i = 0; i < order.Length; i++)
                order[i] = i;

            if (order.Length > 1)
                Array.Sort(order, new PlaceOrder(entities));

            return order;
        }

        /// <summary>The first <see cref="Said" /> of <paramref name="entities" /> by name, in the order they are named, and how many more there are.</summary>
        static string Joined(List<GridEntity> entities, bool requiresAll)
        {
            var order = InOrder(entities);
            var count = entities.Count;
            var names = new List<string>(Said);
            for (var i = 0; i < count && i < Said; i++)
            {
                var entity = entities[order[i]];
                names.Add(entity == null ? Missing : entity.Name);
            }

            var joiner = requiresAll ? " and " : " or ";

            if (count == 1)
                return names[0];
            if (count == 2)
                return names[0] + joiner + names[1];

            var builder = new StringBuilder();
            builder.Append(names[0]).Append(", ").Append(names[1]);
            builder.Append(joiner).Append(count - 2);
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
        public static string Given(ArgumentsSlot slot)
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

        static bool Blocks(GridWorld world, SentenceSlot slot, List<Mention> into)
        {
            var entities = slot as EntitiesSlot;
            if (entities == null)
                return false;

            Entities(world, entities.Entities, entities.RequiresAll, into);
            return true;
        }

        /// <summary>
        /// Adds what <paramref name="slot" /> names to <paramref name="into" /> under the mark of its kind, where it names
        /// something that has one: something a block does, a moment, a test, or a sequence.
        /// </summary>
        static bool Marked(GridWorld world, SentenceSlot slot, List<Mention> into)
        {
            var mark = MarkOf(slot);
            if (mark == null)
                return false;

            into.Add(Mention.Member(Of(world, slot), mark));
            return true;
        }

        /// <summary>The mark of what <paramref name="slot" /> names, or null where it names nothing that has one.</summary>
        static string MarkOf(SentenceSlot slot)
        {
            var action = slot as ActionSlot;
            if (action != null)
                return action.Action == null ? null : MarkText.Of(action.Action);

            var happening = slot as EventSlot;
            if (happening != null)
                return happening.Event == null ? null : MarkText.Of(happening.Event);

            var state = slot as StateSlot;
            if (state != null)
                return state.Moment == null ? null : Symbols.Value;

            var sequence = slot as SequenceSlot;
            if (sequence != null)
                return sequence.IsEmpty ? null : Symbols.Sequence;

            return null;
        }

        static string Of(GridWorld world, SentenceSlot slot)
        {
            var entities = slot as EntitiesSlot;
            if (entities != null)
                return Entities(world, entities.Entities, entities.RequiresAll);

            var happening = slot as EventSlot;
            if (happening != null)
                return happening.Event == null ? Empty : happening.Event.Text;

            var state = slot as StateSlot;
            if (state != null)
                return state.Moment == null ? Empty : state.Holds ? state.Moment.State : state.Moment.Unstate;

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

            var given = slot as ArgumentsSlot;
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

        /// <summary>Places in a list of blocks, by the blocks in them; two blocks that are both gone keep the order they were picked in.</summary>
        sealed class PlaceOrder : IComparer<int>
        {
            readonly List<GridEntity> _entities;

            public PlaceOrder(List<GridEntity> entities)
            {
                _entities = entities;
            }

            public int Compare(int x, int y)
            {
                var byBlock = ByName.Compare(_entities[x], _entities[y]);
                return byBlock != 0 ? byBlock : x.CompareTo(y);
            }
        }
    }
}
