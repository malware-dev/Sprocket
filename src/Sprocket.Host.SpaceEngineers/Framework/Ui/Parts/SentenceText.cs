using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Parts
{
    /// <summary>
    /// The words a sentence reads as.
    /// </summary>
    public static class SentenceText
    {
        /// <summary>The word a set is said with, on its step and wherever else it is said.</summary>
        public const string SetWord = "set";

        /// <summary>What a step setting a state of its blocks on reads with in place of "set".</summary>
        public const string EnableWord = "enable";

        /// <summary>What a step setting a state of its blocks off reads with in place of "set".</summary>
        public const string DisableWord = "disable";

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

        /// <summary>The words the part of a sequence run when it fails opens with.</summary>
        public const string CatchWords = "if it fails";

        /// <summary>The word the part of a sequence run however it ends opens with.</summary>
        public const string FinallyWord = "finally";

        /// <summary>The words offering a step that sets one of the logic system's states.</summary>
        public const string SetStateWords = "set state";

        /// <summary>What a wait for everything started above it reads as, on a row of its own and where it is offered.</summary>
        public const string AwaitWords = "wait for pending tasks";

        /// <summary>The word ending a sequence is said with, on its step and where it is offered.</summary>
        public const string AbortWord = "abort";

        /// <summary>The word an abort's message is said after.</summary>
        public const string SayingWord = "saying";

        /// <summary>The word an abort with no message and no level is said with: "abort silently".</summary>
        public const string SilentlyWord = "silently";

        /// <summary>The words an abort's level is said after: "as a warning".</summary>
        public const string AsAWords = "as a";

        static readonly string[] AbortLevelWords = { "note", "warning", "problem" };

        /// <summary>Each level an abort can show at, by <see cref="AbortLevel" />: note, warning, problem.</summary>
        public static IReadOnlyList<string> AbortLevels => AbortLevelWords;

        /// <summary>The level <paramref name="word" />, one of <see cref="AbortLevels" />, names, or a note where it names none.</summary>
        public static AbortLevel AbortLevelOf(string word)
        {
            var at = Array.IndexOf(AbortLevelWords, word);
            return at < 0 ? AbortLevel.Note : (AbortLevel)at;
        }

        /// <summary>The message of an abort as a field called <paramref name="name" />, said after "saying".</summary>
        public static Field AbortMessageField(string name)
        {
            var field = Field.Called(name);
            field.Lead = SayingWord;
            return field;
        }

        /// <summary>The level of an abort as a field called <paramref name="name" />, one of <see cref="AbortLevels" />, said after "as a".</summary>
        public static Field AbortLevelField(string name)
        {
            var field = Field.Among(name, AbortLevelWords, AbortLevelWords);
            field.Lead = AsAWords;
            return field;
        }

        /// <summary>An abort's message as its sentence says it: in quotes, or nothing where it has none.</summary>
        public static string Quote(string message)
        {
            return string.IsNullOrEmpty(message) ? "" : $"\"{message}\"";
        }

        /// <summary>The word joining a value to the blocks it is read or set on.</summary>
        public const string OnWord = "on";

        const string ToWord = "to";

        const int Said = 2;

        /// <summary>The word a test says before the value it names.</summary>
        public const string TheWord = "the";

        /// <summary>Every way a test can read its blocks, in the order they are offered.</summary>
        public static readonly IReadOnlyList<Quantifier> Readings = new[] { Quantifier.Any, Quantifier.All, Quantifier.Sum, Quantifier.Average };

        const int Quoted = 48;

        const string Cut = "…";


        /// <summary>A slot nobody has filled in yet.</summary>
        public const string Empty = "…";


        /// <summary>Something the sentence names that is no longer on the construct.</summary>
        public const string Missing = "a lost block";

        /// <summary>Blocks in the order a person reads their names.</summary>
        public static readonly NameOrder ByName = new NameOrder();


        /// <summary>The word a trigger's line opens with, for a trigger of <paramref name="kind" /> started by <paramref name="parts" /> parts.</summary>
        public static string Lead(TriggerKind kind, int parts)
        {
            if (kind == TriggerKind.Every)
                return "Every";

            return kind == TriggerKind.Signal && parts == 0
                ? "When something on this construct starts it"
                : "When";
        }


        /// <summary>The words between what starts a trigger and what it does.</summary>
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
        public static string InTrigger(GridWorld world, SentenceSlot slot)
        {
            var duration = slot as DurationSlot;
            if (duration != null)
                return duration.Setting.Length > 0 ? duration.Setting : TimeText.Rate(duration.Length);

            return Of(world, slot);
        }

        /// <summary>What a slot of a trigger reads as, as pieces added to <paramref name="into" />: each block it names with its icon, and the words around them.</summary>
        public static void InTrigger(GridWorld world, SentenceSlot slot, List<Mention> into)
        {
            if (!Blocks(world, slot, into) && !Marked(world, slot, into))
                into.Add(new Mention(InTrigger(world, slot), null));
        }


        /// <summary>
        /// What comes before a step's slots: "delay", "run", "stop", "set", "if", "otherwise if", "otherwise", the words of a wait for
        /// what is running, or nothing at all.
        /// </summary>
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

            if (kind == StepKind.Catch)
                return CatchWords;

            if (kind == StepKind.Finally)
                return FinallyWord;

            if (kind == StepKind.Await)
                return AwaitWords;

            if (kind == StepKind.Abort)
                return AbortWord;

            return kind == StepKind.Wait ? WaitWord : "";
        }


        /// <summary>The word that joins one of a line's slots to what comes before it, or nothing.</summary>
        public static string Joining(IList<SentenceSlot> slots, int slot)
        {
            return Joining(slots, slot, false);
        }

        /// <summary>
        /// The word that joins one of a line's slots to what comes before it, or nothing, read as a test where
        /// <paramref name="tests" /> says so.
        /// </summary>
        public static string Joining(IList<SentenceSlot> slots, int slot, bool tests)
        {
            // A test's parts carry the words joining them, given by the shape that laid them out (TestShape.Lay); an
            // action's blocks carry the word the action declares.
            if (tests || !string.IsNullOrEmpty(slots[slot].Joined))
                return slots[slot].Joined;

            // A step setting one of the system's states says what it is set to after it.
            if (slot == 1 && slots.Count > 1 && slots[0] is SystemStateSlot)
                return ToWord;

            // A step enabling a state names its value after the verb.
            var at = Valued(slots);
            var value = at >= 0 && at < slots.Count ? slots[at] as ActionSlot : null;
            if (value == null || value.Action == null || !value.Action.Sets)
                return "";

            if (slot == at + 1)
                return OnWord;

            return slot == at + 2 ? ToWord : "";
        }

        /// <summary>Whether <paramref name="quantifier" /> reads a total of a number on the blocks, a sum or an average.</summary>
        public static bool Totals(Quantifier quantifier) => quantifier == Quantifier.Sum || quantifier == Quantifier.Average;

        /// <summary>How a test reads its blocks, as the list choosing it offers it: any, all / each, the sum of, the average of.</summary>
        /// <remarks>A line says the reading in its own words, which <see cref="TestShape" /> gives.</remarks>
        public static string Quantified(Quantifier quantifier)
        {
            switch (quantifier)
            {
                case Quantifier.All: return "all / each";
                case Quantifier.Sum: return "the sum of";
                case Quantifier.Average: return "the average of";
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
                return $"{verb.Substring(0, verb.Length - 3)}y";

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
            foreach (var letter in word)
            {
                if (!char.IsLetter(letter))
                    continue;

                if (!char.IsUpper(letter))
                    return false;

                letters++;
            }

            return letters >= 2;
        }

        /// <summary>
        /// The verb a step setting <paramref name="value" /> to <paramref name="arguments" /> reads with, where the value is a
        /// state of its blocks: "enable" for on and "disable" for off; empty for any other value or setting.
        /// </summary>
        public static string Switching(EntityAction value, IList<Literal> arguments)
        {
            if (value == null || !value.IsStatus || value.Holds != ValueKind.Flag || arguments.Count != 1
                || arguments[0].Kind != ValueKind.Flag)
                return "";

            return arguments[0].Flag ? EnableWord : DisableWord;
        }

        /// <summary>A state enabled or disabled as one line: "enable stockpiling on Tank".</summary>
        public static string Switched(string verb, string value, string blocks) => $"{verb} {value} {OnWord} {blocks}";

        /// <summary>A set as one line: "set charge mode on Standby Battery to recharge".</summary>
        public static string Setting(string value, string blocks, string given)
        {
            return $"{SetWord} {value} {OnWord} {blocks} {ToWord} {given}";
        }


        /// <summary>What a slot of a step reads as.</summary>
        public static string InStep(GridWorld world, SentenceSlot slot)
        {
            var duration = slot as DurationSlot;
            if (duration != null)
                return duration.Setting.Length > 0 ? duration.Setting : TimeText.Duration(duration.Length);

            return Of(world, slot);
        }

        /// <summary>What a slot of a step reads as, as pieces added to <paramref name="into" />: each block it names with its icon, and the words around them.</summary>
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
                into.Add(new Mention($"{joiner}{(entityIds.Count - 2)} more", null));
        }

        /// <summary>
        /// Names slots by their names as pieces added to <paramref name="into" />, each under its mark of <paramref name="marks" />
        /// where there are marks, joined the way <see cref="Slots(IList{string})" /> joins them.
        /// </summary>
        public static void Slots(IList<string> names, IList<string> marks, List<Mention> into)
        {
            if (names.Count == 0)
            {
                into.Add(new Mention(Empty, null));
                return;
            }

            var named = names.Count > Said ? Said : names.Count;
            for (var i = 0; i < named; i++)
            {
                if (i > 0)
                    into.Add(new Mention(names.Count == 2 ? " and " : ", ", null));

                into.Add(marks == null ? new Mention(names[i], null) : Mention.Member(names[i], marks[i]));
            }

            if (names.Count > Said)
                into.Add(new Mention($" and {names.Count - Said} more", null));
        }

        /// <summary>Names slots by their names, the first two of them and how many more there are: "slot 1 and slot 2", "slot 1, slot 2 and 3 more".</summary>
        public static string Slots(IList<string> names)
        {
            var pieces = new List<Mention>();
            Slots(names, null, pieces);

            var said = new StringBuilder();
            foreach (var piece in pieces)
                said.Append(piece.Text);

            return said.ToString();
        }

        /// <summary>
        /// Names a selection as one piece a block, each with its icon, in the order <see cref="Entities(GridWorld, IList{long}, bool, List{Mention})" />
        /// names them: all of them where there are no more than <paramref name="most" />, and otherwise all but one of that many;
        /// answers how many it left out, for a last line the caller words.
        /// </summary>
        public static int Listed(GridWorld world, IList<long> entityIds, int most, List<Mention> into)
        {
            if (entityIds.Count == 0)
            {
                into.Add(new Mention(Empty, null));
                return 0;
            }

            var named = entityIds.Count > most ? most - 1 : entityIds.Count;
            var entities = Found(world, entityIds);
            var order = InOrder(entities);
            for (var i = 0; i < named; i++)
            {
                var entity = entities[order[i]];
                into.Add(entity == null ? new Mention(Missing, null) : new Mention(entity.Name, entity.Kind.Icon));
            }

            return entityIds.Count - named;
        }

        /// <summary>
        /// Names a selection, keeping real names visible for as long as they fit.
        /// </summary>
        public static string Entities(GridWorld world, IList<long> entityIds, bool requiresAll)
        {
            if (entityIds.Count == 0)
                return Empty;

            return Joined(Found(world, entityIds), requiresAll);
        }

        /// <summary>
        /// Names a selection given as claims, for a sentence read straight off the model.
        /// </summary>
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
                Array.Sort(order, new BlockPlaceOrder(entities));

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
        public static string Given(ArgumentsSlot slot)
        {
            if (slot.IsEmpty)
                return Empty;

            // One per field rather than one per value: a color's three numbers are one field.
            var said = "";
            var after = "";
            for (var i = 0; i < slot.Filled.Count; i++)
            {
                if (i < slot.Fields.Count && !slot.Fields[i].IsSaid(slot.Filled[i]))
                    continue;

                if (string.IsNullOrEmpty(slot.Filled[i]) || i < slot.Fields.Count && slot.Fields[i].Quiet == slot.Filled[i])
                    continue;

                var word = Around(slot.Fields, i, slot.Filled[i]);
                if (i < slot.Fields.Count && slot.Fields[i].IsPart)
                    after = word;
                else
                    said += (said.Length > 0 ? " " : "") + word;
            }

            return after.Length == 0 ? said : said.Length == 0 ? after : $"{said} {after}";
        }

        /// <summary><paramref name="words" />, cut short with an ellipsis where they run longer than a sentence quotes.</summary>
        public static string Short(string words)
        {
            return words != null && words.Length > Quoted ? words.Substring(0, Quoted).TrimEnd() + Cut : words;
        }

        /// <summary>One value as a sentence says it, with the words its field puts around it.</summary>
        /// <remarks>
        /// A line break reads as a slash, and long text is cut short: a screen's text can run to a hundred thousand
        /// characters.
        /// </remarks>
        static string Around(IList<Field> fields, int at, string filled)
        {
            filled = Short(filled);

            if (filled != null && filled.IndexOf('\n') >= 0)
                filled = filled.Replace("\r", "").Replace("\n", " / ");

            if (at >= fields.Count)
                return filled;

            var field = fields[at];
            var scaled = field.Scaled(filled);
            var said = scaled + ((scaled == "1" ? field.UnitOfOne : null) ?? field.Unit ?? "");
            return string.IsNullOrEmpty(field.Lead) ? said : $"{field.Lead} {said}";
        }

        static bool Blocks(GridWorld world, SentenceSlot slot, List<Mention> into)
        {
            var entities = slot as EntitiesSlot;
            if (entities == null)
                return false;

            if (entities.SaysSlots)
                Slots(entities.Slots, entities.Marks, into);
            else
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

            var kept = slot as SystemStateSlot;
            if (kept != null)
                return kept.IsEmpty ? null : Symbols.State;

            return null;
        }

        static string Of(GridWorld world, SentenceSlot slot)
        {
            var entities = slot as EntitiesSlot;
            if (entities != null)
                return entities.SaysSlots ? Slots(entities.Slots) : Entities(world, entities.Entities, entities.RequiresAll);

            var happening = slot as EventSlot;
            if (happening != null)
                return happening.Event == null ? Empty : happening.Plural ? Plural(happening.Event.Text) : happening.Event.Text;

            var state = slot as StateSlot;
            if (state != null)
                return state.Moment == null ? Empty : state.Holds ? state.Moment.State : state.Moment.Unstate;

            // A value is said by name alone, since "set" stands before the slot.
            var action = slot as ActionSlot;
            if (action != null)
            {
                if (action.Action == null)
                    return Empty;

                return action.Action.Sets ? Value(action.Action) : action.Action.Text;
            }

            var duration = slot as DurationSlot;
            if (duration != null)
                return duration.Setting.Length > 0 ? duration.Setting : TimeText.Duration(duration.Length);

            var given = slot as ArgumentsSlot;
            if (given != null)
                return Given(given);

            var sequence = slot as SequenceSlot;
            if (sequence != null)
            {
                if (sequence.IsEmpty)
                    return Empty;

                return sequence.Name.Length > 0 ? sequence.Name : $"sequence {sequence.Number}";
            }

            var kept = slot as SystemStateSlot;
            if (kept != null)
                return kept.IsEmpty ? Empty : kept.Name;

            // A slot kind with no words here; saying nothing would read as an empty slot.
            return $"({slot.GetType().Name})";
        }

        /// <summary><paramref name="text" /> with its first letter a capital, to start a sentence with.</summary>
        public static string Capitalised(string text)
        {
            if (string.IsNullOrEmpty(text) || char.IsUpper(text[0]))
                return text;

            return char.ToUpperInvariant(text[0]) + text.Substring(1);
        }
    }
}
