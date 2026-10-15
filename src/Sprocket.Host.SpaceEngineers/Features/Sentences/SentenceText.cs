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

        /// <summary>The word a trigger watching for something is offered with, as the language says it.</summary>
        public const string WhenWord = "when";

        /// <summary>The word a trigger on a clock is offered with, as the language says it.</summary>
        public const string EveryWord = "every";

        /// <summary>The word a wait for a length of time is said with, on its step and where it is offered.</summary>
        public const string WaitWord = "delay";

        /// <summary>The word running a sequence is said with, on its step, on a trigger and where either is offered.</summary>
        public const string RunWord = "run";

        /// <summary>The word stopping a sequence is said with, on its step, on a trigger and where either is offered.</summary>
        public const string StopWord = "stop";

        /// <summary>The word a check is said with, on its step and where it is offered.</summary>
        public const string IfWord = "if";

        /// <summary>The words a further check of an if is said with, on its row and where it is offered.</summary>
        public const string OtherwiseIfWords = "otherwise if";

        /// <summary>The word the branch of an if taken when no check holds is said with, on its row and where it is offered.</summary>
        public const string OtherwiseWord = "otherwise";

        /// <summary>What a wait for everything started above it reads as, on a row of its own and where it is offered.</summary>
        public const string AwaitWords = "wait for pending tasks";

        const string OnWord = "on";
        const string ToWord = "to";
        const string IsWord = "is";

        /// <summary>How many blocks a sentence names before it counts the rest.</summary>
        const int Said = 2;

        /// <summary>What a test adding a value up over its blocks says first.</summary>
        const string SumWords = "the sum of";

        /// <summary>What a test averaging a value over its blocks says first.</summary>
        const string AverageWords = "the average of";

        /// <summary>Every way a test can read its blocks, in the order they are offered.</summary>
        public static readonly IReadOnlyList<Quantifier> Readings = new[] { Quantifier.Any, Quantifier.All, Quantifier.Sum, Quantifier.Average };

        /// <summary>How many characters of a text a sentence says before cutting it short.</summary>
        const int Quoted = 48;

        /// <summary>What a text cut short ends with.</summary>
        const string Cut = "…";


        /// <summary>A slot nobody has filled in yet.</summary>
        public const string Empty = "…";


        /// <summary>Something the sentence names that is no longer on the construct.</summary>
        public const string Missing = "a lost block";

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
        /// As the program writes it: a comparison is followed by "then", "when any ext.closed on [ inner ] == on then
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
        /// A duration in a trigger is a rate, said in the unit the program writes it in.
        /// </remarks>
        public static string InTrigger(GridWorld world, SentenceSlot slot)
        {
            var duration = slot as DurationSlot;
            if (duration != null)
                return TimeText.Rate(duration.Length);

            return Of(world, slot);
        }

        /// <summary>The same, as pieces added to <paramref name="into" />: each block it names with its icon, and the words around them.</summary>
        public static void InTrigger(GridWorld world, SentenceSlot slot, List<Mention> into)
        {
            if (!Blocks(world, slot, into) && !Marked(world, slot, into))
                into.Add(new Mention(InTrigger(world, slot), null));
        }


        /// <summary>
        /// What comes before a step's slots: "delay", "run", "stop", "set", "if", "otherwise if", "otherwise", the words of a wait for
        /// what is running, or nothing at all.
        /// </summary>
        /// <remarks>
        /// Ordinarily nothing. A step reads as "close the inner door" and the list being a list is
        /// what says when it happens.
        /// </remarks>
        public static string Lead(StepKind kind)
        {
            if (kind == StepKind.Run)
                return RunWord;

            if (kind == StepKind.Stop)
                return StopWord;

            if (kind == StepKind.Set)
                return SetWord;

            if (kind == StepKind.If)
                return IfWord;

            if (kind == StepKind.OtherwiseIf)
                return OtherwiseIfWords;

            if (kind == StepKind.Otherwise)
                return OtherwiseWord;

            if (kind == StepKind.Await)
                return AwaitWords;

            return kind == StepKind.Wait ? WaitWord : "";
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

        /// <summary>
        /// The same, for a test where <paramref name="tests" /> says so, which reads its value as the program does: a value on
        /// some blocks, then how it is compared, which says its own verb.
        /// </summary>
        public static string Joining(IList<SentenceSlot> slots, int slot, bool tests)
        {
            // A test's value can stand after the part saying how its blocks are read; a step's always comes first.
            var at = tests ? Valued(slots) : 0;
            var value = at >= 0 && at < slots.Count ? slots[at] as ActionSlot : null;
            if (value == null || value.Action == null || !(tests || value.Action.Sets))
                return "";

            if (slot == at + 1)
                return OnWord;

            return slot == at + 2 && !tests ? ToWord : "";
        }

        /// <summary>How a test reads its blocks, as its sentence says it and the list choosing it offers it: any, all, the sum of, the average of.</summary>
        public static string Quantified(Quantifier quantifier)
        {
            switch (quantifier)
            {
                case Quantifier.All: return "all";
                case Quantifier.Sum: return SumWords;
                case Quantifier.Average: return AverageWords;
            }

            return "any";
        }

        /// <summary>Where among <paramref name="slots" /> the value stands, or -1 where none does.</summary>
        static int Valued(IList<SentenceSlot> slots)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i] is ActionSlot)
                    return i;
            }

            return -1;
        }


        /// <summary>
        /// <paramref name="said" /> as several subjects take it, its first word made plural: "finish closing", "are open".
        /// </summary>
        /// <remarks>
        /// A block's words are written for one block, and a list joined with "and" puts several before them. Only the
        /// verb's own ending changes, so the rest of what the block says stays as it was written.
        /// </remarks>
        public static string Plural(string said)
        {
            if (string.IsNullOrEmpty(said))
                return said;

            var space = said.IndexOf(' ');
            var first = space < 0 ? said : said.Substring(0, space);
            var rest = space < 0 ? "" : said.Substring(space);
            return Many(first) + rest;
        }

        /// <summary>The plural of a verb written for one: are, have, finish, become.</summary>
        static string Many(string verb)
        {
            switch (verb)
            {
                case "is": return "are";
                case "has": return "have";
                case "was": return "were";
                case "does": return "do";
                case "goes": return "go";
            }

            if (verb.EndsWith("ies", StringComparison.Ordinal) && verb.Length > 3)
                return verb.Substring(0, verb.Length - 3) + "y";

            if (verb.EndsWith("ches", StringComparison.Ordinal) || verb.EndsWith("shes", StringComparison.Ordinal)
                || verb.EndsWith("sses", StringComparison.Ordinal) || verb.EndsWith("xes", StringComparison.Ordinal)
                || verb.EndsWith("zes", StringComparison.Ordinal))
                return verb.Substring(0, verb.Length - 2);

            return verb.EndsWith("s", StringComparison.Ordinal) && !verb.EndsWith("ss", StringComparison.Ordinal)
                ? verb.Substring(0, verb.Length - 1)
                : verb;
        }

        /// <summary>A value as a sentence names it: in lower case where Sprocket worded it, and as the game declared it where it did.</summary>
        /// <remarks>The game's own names are the terminal's, as-is, as the terminal view lists them.</remarks>
        public static string Value(EntityAction value)
        {
            return value.Unaltered && !value.Renamed ? value.Named : Lowered(value.Named);
        }

        /// <summary><paramref name="words" /> in lower case, an acronym among them kept as it is: "AI behavior on/off".</summary>
        public static string Lowered(string words)
        {
            if (string.IsNullOrEmpty(words))
                return words;

            var said = new StringBuilder(words.Length);
            var start = 0;
            while (start < words.Length)
            {
                var end = words.IndexOf(' ', start);
                if (end < 0)
                    end = words.Length;

                var word = words.Substring(start, end - start);
                said.Append(Acronym(word) ? word : word.ToLowerInvariant());
                if (end < words.Length)
                    said.Append(' ');

                start = end + 1;
            }

            return said.ToString();
        }

        /// <summary>Whether <paramref name="word" /> is two letters or more, all of them capitals.</summary>
        static bool Acronym(string word)
        {
            var letters = 0;
            for (var i = 0; i < word.Length; i++)
            {
                if (!char.IsLetter(word[i]))
                    continue;

                if (!char.IsUpper(word[i]))
                    return false;

                letters++;
            }

            return letters >= 2;
        }

        /// <summary>A set as one line: "set charge mode on Standby Battery to recharge".</summary>
        /// <remarks>
        /// The words its step reads in, for where it is said as a line rather than drawn as slots.
        /// </remarks>
        public static string Setting(string value, string blocks, string given)
        {
            return SetWord + " " + value + " " + OnWord + " " + blocks + " " + ToWord + " " + given;
        }


        /// <summary>A test being picked, as far as it has got: "If Inner Door is open", "If power on Airlock Light is …".</summary>
        /// <param name="blocks">The blocks as <see cref="Entities(GridWorld, IList{long}, bool)" /> says them.</param>
        /// <param name="chosen">The state or the value chosen, in the words its list shows it in, or empty.</param>
        /// <param name="state">Whether what is chosen is a state of the blocks' status, which reads as the blocks themselves.</param>
        /// <param name="reads">How the blocks are read, as <see cref="Quantified" /> says it, or empty where there is one block: "If the sum of radius on the lights is …".</param>
        /// <param name="opens">What the line opens with: "If" for a check, "When" for what starts something.</param>
        public static string Testing(string blocks, string chosen, bool state, string reads, string opens = "If")
        {
            var lead = reads.Length == 0 ? opens + " " : opens + " " + reads + " ";
            if (chosen.Length == 0)
                return lead + blocks + " " + Empty;

            return state
                ? lead + blocks + " " + IsWord + " " + chosen
                : lead + chosen + " " + OnWord + " " + blocks + " " + IsWord + " " + Empty;
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
        /// A duration is said in the unit it was written in.
        /// </remarks>
        public static string InStep(GridWorld world, SentenceSlot slot)
        {
            var duration = slot as DurationSlot;
            if (duration != null)
                return TimeText.Duration(duration.Length);

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
        /// <remarks>
        /// A line break in text reads as a slash, since a sentence is one line, and text longer than a sentence has room for is
        /// cut short: a screen's text can run to a hundred thousand characters.
        /// </remarks>
        static string Around(IList<Field> fields, int at, string filled)
        {
            if (filled != null && filled.Length > Quoted)
                filled = filled.Substring(0, Quoted).TrimEnd() + Cut;

            if (filled != null && filled.IndexOf('\n') >= 0)
                filled = filled.Replace("\r", "").Replace("\n", " / ");

            if (at >= fields.Count)
                return filled;

            var field = fields[at];
            var said = field.Scaled(filled) + (field.Unit ?? "");
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
                return happening.Event == null ? Empty : happening.Plural ? Plural(happening.Event.Text) : happening.Event.Text;

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

                return action.Action.Sets ? Value(action.Action) : action.Action.Text;
            }

            var duration = slot as DurationSlot;
            if (duration != null)
                return TimeText.Duration(duration.Length);

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
