using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Language.Format;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>Reads a logic system out of the script: its header, slots, types, states and sequences, and checks the names it uses.</summary>
    public static class SystemReader
    {
        static readonly string[] Times =
        {
            ScriptFormat.Time,
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFF'Z'",
            "yyyy'-'MM'-'dd'T'HH':'mm':'sszzz",
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFzzz",
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss",
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFF",
            "yyyy'-'MM'-'dd'T'HH':'mm'Z'",
            "yyyy'-'MM'-'dd'T'HH':'mm",
            "yyyy'-'MM'-'dd"
        };

        /// <summary>
        /// <c>system "3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43" as MalTek.Airlock changed "…" last sequence 4 { … }</c>, or the
        /// same as a <c>system blueprint</c>.
        /// </summary>
        /// <remarks>
        /// A system with no readable identity, an identity already read, or no name is trouble and not read; a second system
        /// with the same name loads with a warning.
        /// </remarks>
        public static void ReadSystem(Statement statement, Automation program, ScriptTrouble trouble)
        {
            var blueprint = statement.Says(0, Keyword.Blueprint);
            var first = blueprint ? 1 : 0;

            Guid identity;
            if (!Guid.TryParseExact(statement.Text(first), "D", out identity) || identity == Guid.Empty)
            {
                trouble.Say(statement, "expected the system's identity, a quoted GUID.");
                return;
            }

            var name = statement.Parts(first + 2);
            if (!statement.Says(first + 1, Keyword.As) || name.Count == 0 || name.Count > 2
                || name[name.Count - 1].Word.Length == 0)
            {
                trouble.Say(statement, "expected 'as' and the system's name.");
                return;
            }

            if (program.FindSystem(identity) != null)
            {
                trouble.Say(statement, $"system {identity:D} is already declared.");
                return;
            }

            var space = name.Count == 2 ? name[0].Word : "";
            var called = name[name.Count - 1].Word;
            if (program.FindSystem(space, called) != null)
                trouble.Warn(statement, $"another system is also called {ScriptWriter.Spelling(space, called)}.");

            var system = program.Restore(identity, space, called);
            system.Annotation = statement.Annotation;
            system.Attributes = ScriptAttributes.Unused(statement, ScriptAttributes.LastSequence, ScriptAttributes.Categories, ScriptAttributes.ListedOn);
            system.IsBlueprint = blueprint;
            LastSequence(statement, system, trouble);
            ListedOn(statement, system, trouble);

            // The header's clauses, each on a line of its own, in either order and each at most once.
            var at = first + 3;
            var changed = false;
            var from = false;
            while (at < statement.Count)
            {
                var clause = statement.Says(at, Keyword.Changed) && !changed || statement.Says(at, Keyword.From) && !from && blueprint;
                if (!clause)
                {
                    ScriptQuotes.Ends(statement, at, trouble);
                    break;
                }

                if (!statement.Lines.Contains(at))
                {
                    trouble.Say(statement, $"expected '{Keywords.Name(statement.ValueAt(at).Keyword)}' on a line of its own under the system's header.");
                    break;
                }

                if (statement.Says(at, Keyword.Changed))
                {
                    system.Changed = Time(statement, at + 1, "'changed'", trouble);
                    changed = true;
                    at += 2;
                }
                else
                {
                    at = From(statement, at, system, trouble);
                    from = true;
                }

                // A clause takes its line to the end.
                if (at < statement.Count && !statement.Lines.Contains(at))
                {
                    ScriptQuotes.Ends(statement, at, trouble);
                    break;
                }
            }

            var body = statement.Body;
            for (var i = 0; i < body.Count; i++)
            {
                var inner = body[i];
                if (inner.IsOff)
                {
                    trouble.Say(inner, "unexpected '//'; only a step in a sequence can be switched off.");
                    continue;
                }

                if ((inner.Key == Keyword.Required || inner.Key == Keyword.Optional) && blueprint)
                    ReadSlot(inner, system, trouble);
                else if (inner.Key == Keyword.Required || inner.Key == Keyword.Optional)
                    trouble.Say(inner, $"unexpected {ScriptQuotes.Headed(inner)}; only a system blueprint has slots.");
                else if (inner.Key == Keyword.Type)
                    StateReader.ReadType(inner, system, trouble);
                else if (inner.Key == Keyword.Let)
                    StateReader.ReadState(inner, system, program, trouble);
                else if (inner.Key == Keyword.Whenever || inner.Key == Keyword.When || inner.Key == Keyword.Every)
                    TriggerReader.ReadTrigger(inner, system, program, trouble);
                else if (inner.Key == Keyword.Sequence)
                    ReadSequence(inner, system, program, trouble);
                else
                    trouble.Say(inner, $"unexpected {ScriptQuotes.Headed(inner)}{(blueprint ? "; expected required, optional, type, let, whenever, when, every or sequence." : "; expected type, let, whenever, when, every or sequence.")}");
            }

            Earlier(statement, system);
            Resolve(system, statement, trouble);
        }

        /// <summary>
        /// <c>#@ lastSequence: 4</c>: the highest number Sprocket has handed out in the system, so a deleted sequence's number
        /// is never handed out again.
        /// </summary>
        /// <remarks>Whichever is higher wins, this or the highest sequence in the block.</remarks>
        static void ListedOn(Statement statement, AutomationSystem system, ScriptTrouble trouble)
        {
            ScriptAttribute attribute;
            if (!ScriptAttributes.Find(statement, ScriptAttributes.ListedOn, out attribute))
                return;

            if (attribute.Value.Form != ValueForm.Text)
            {
                trouble.Say(statement, $"expected text for the attribute {ScriptAttributes.ListedOn}.");
                return;
            }

            system.ListedOn = attribute.Value.Word;
        }

        static void LastSequence(Statement statement, AutomationSystem system, ScriptTrouble trouble)
        {
            ScriptAttribute attribute;
            if (!ScriptAttributes.Find(statement, ScriptAttributes.LastSequence, out attribute))
                return;

            var number = attribute.Value;
            if (number.Form != ValueForm.Integer || number.Whole < 1 || number.Whole > int.MaxValue)
            {
                trouble.Say(statement, $"expected a whole number above 0 for the attribute {ScriptAttributes.LastSequence}.");
                return;
            }

            system.Used((int)number.Whole);
        }

        /// <summary>
        /// Stands the slots of <paramref name="system" /> as its installation panel shows them where <paramref name="statement" />
        /// carries <c>#@ categories: [ "Doors", "Lights" ]</c>: the slots under no category first, then each category's slots in
        /// the order listed.
        /// </summary>
        /// <remarks>Nothing is said about a list that cannot be read: the attribute is the host's, and only says how slots stood.</remarks>
        static void Earlier(Statement statement, AutomationSystem system)
        {
            ScriptAttribute attribute;
            if (!ScriptAttributes.Find(statement, ScriptAttributes.Categories, out attribute) || attribute.Value.Form != ValueForm.List)
                return;

            var listed = new List<string>();
            var items = statement.Within(attribute.Value);
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].Form == ValueForm.Text && !listed.Contains(items[i].Word))
                    listed.Add(items[i].Word);
            }

            var order = new List<Part>();
            foreach (var part in system.Parts)
            {
                if (!listed.Contains(part.Category))
                    order.Add(part);
            }

            foreach (var category in listed)
            {
                foreach (var part in system.Parts)
                {
                    if (part.Category == category)
                        order.Add(part);
                }
            }

            system.Arrange(order);
        }

        /// <summary>
        /// Reads <c>from blueprint "&lt;guid&gt;" ["&lt;iso&gt;"]</c> at <paramref name="at" /> into the link of
        /// <paramref name="system" />, answering where the next clause starts.
        /// </summary>
        static int From(Statement statement, int at, AutomationSystem system, ScriptTrouble trouble)
        {
            if (!statement.Says(at + 1, Keyword.Blueprint))
            {
                trouble.Say(statement, "expected 'blueprint' after 'from'.");
                return statement.Count;
            }

            Guid origin;
            if (!Guid.TryParseExact(statement.Text(at + 2), "D", out origin) || origin == Guid.Empty)
            {
                trouble.Say(statement, "expected the identity of the blueprint after 'from blueprint', a quoted GUID.");
                return statement.Count;
            }

            system.Origin = origin;

            // The version is the quoted text after it.
            if (statement.ValueAt(at + 3).Form != ValueForm.Text)
                return at + 3;

            system.OriginChanged = Time(statement, at + 3, "the blueprint's identity", trouble);
            return at + 4;
        }

        /// <summary>The ISO 8601 time at <paramref name="at" />, or null, with trouble naming it as the time after <paramref name="after" />.</summary>
        static DateTime? Time(Statement statement, int at, string after, ScriptTrouble trouble)
        {
            DateTime time;
            if (DateTime.TryParseExact(statement.Text(at), Times, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out time))
                return time;

            trouble.Say(statement, $"expected an ISO 8601 time after {after}, like \"2026-09-14T19:02:11Z\".");
            return null;
        }

        /// <summary><c>#@ short: "Call"</c> on a slot: the few words shown with each toolbar slot that answers it, text.</summary>
        static void Short(Statement statement, Part part, ScriptTrouble trouble)
        {
            ScriptAttribute attribute;
            if (!ScriptAttributes.Find(statement, ScriptAttributes.Short, out attribute))
                return;

            if (attribute.Value.Form != ValueForm.Text)
            {
                trouble.Say(statement, $"expected text for the attribute {ScriptAttributes.Short}.");
                return;
            }

            part.Short = attribute.Value.Word;
        }

        /// <summary><c>#@ category: "Doors"</c> on a slot: the category it is shown under with the slots beside it under the same one, text.</summary>
        static void Category(Statement statement, Part part, ScriptTrouble trouble)
        {
            ScriptAttribute attribute;
            if (!ScriptAttributes.Find(statement, ScriptAttributes.Category, out attribute))
                return;

            if (attribute.Value.Form != ValueForm.Text)
            {
                trouble.Say(statement, $"expected text for the attribute {ScriptAttributes.Category}.");
                return;
            }

            part.Category = attribute.Value.Word;
        }

        static void ReadSlot(Statement statement, AutomationSystem system, ScriptTrouble trouble)
        {
            var optional = statement.Key == Keyword.Optional;
            if (!statement.Says(0, Keyword.Slot))
            {
                trouble.Say(statement, optional ? "expected 'slot' after 'optional'." : "expected 'slot' after 'required'.");
                return;
            }

            var name = statement.ValueAt(1);
            if (!ScriptQuotes.Named(name))
            {
                trouble.Say(statement, "expected the slot's name.");
                return;
            }

            if (system.FindPart(name.Word) != null)
            {
                trouble.Say(statement, $"slot '{name.Word}' is already declared.");
                return;
            }

            var part = system.PartFor(name.Word);
            part.Annotation = statement.Annotation;
            part.Attributes = ScriptAttributes.Unused(statement, ScriptAttributes.Short, ScriptAttributes.Category);
            Short(statement, part, trouble);
            Category(statement, part, trouble);
            part.Optional = optional;

            if (!statement.Says(2, Keyword.Expects))
            {
                ScriptQuotes.Ends(statement, 2, trouble);
                return;
            }

            var one = statement.ValueAt(3);
            if (one.Says(Keyword.Signal))
                part.IsSignal = true;
            else if (one.Form == ValueForm.Qualified)
                Holding(statement, part, trouble);
            else if (ScriptQuotes.Named(one))
                part.Expects = one.Word;
            else
                trouble.Say(statement, "expected signal, one block type or a type the host provides after 'expects'.");

            ScriptQuotes.Ends(statement, 4, trouble);
        }

        /// <summary>The type the host provides that the slot <paramref name="part" /> expects, written qualified at 3, or trouble.</summary>
        static void Holding(Statement statement, Part part, ScriptTrouble trouble)
        {
            var parts = statement.Parts(3);
            var type = parts.Count == 2 ? HostTypes.Named($"{parts[0].Word}.{parts[1].Word}") : null;
            if (type == null)
            {
                trouble.Say(statement, $"no type '{ScriptQuotes.Spelling(parts)}' the host provides.");
                return;
            }

            part.Holds = type.Id;
        }

        /// <summary>
        /// <c>sequence 3 as 'Let me out' { … }</c>
        /// </summary>
        /// <remarks>
        /// One without a number, or with a number its system already has, is trouble and not read; the name is optional.
        /// </remarks>
        static void ReadSequence(Statement statement, AutomationSystem system, Automation program, ScriptTrouble trouble)
        {
            var number = statement.ValueAt(0);
            if (number.Form != ValueForm.Integer || number.Whole < 1 || number.Whole > int.MaxValue)
            {
                trouble.Say(statement, "expected the sequence's number.");
                return;
            }

            if (system.FindSequence((int)number.Whole) != null)
            {
                trouble.Say(statement, $"sequence {number.Whole} is already declared.");
                return;
            }

            var sequence = program.Restore(system, (int)number.Whole, null);
            sequence.Annotation = statement.Annotation;
            sequence.Attributes = ScriptAttributes.Unused(statement);

            var at = 1;
            if (statement.Says(1, Keyword.As))
            {
                sequence.Name = ScriptQuotes.Named(statement.ValueAt(2)) ? statement.Word(2) : "";
                if (sequence.Name.Length == 0)
                    trouble.Say(statement, "expected a name after 'as'.");

                at = 3;
            }

            ScriptQuotes.Ends(statement, at, trouble);
            StepReader.ReadSteps(statement.Body, sequence.Steps, system, program, trouble);
        }

        /// <summary>
        /// Matches what the system's <c>run</c> steps and triggers name, now the whole system is read.
        /// </summary>
        /// <remarks>Anything naming a sequence the system has not got is trouble.</remarks>
        static void Resolve(AutomationSystem system, Statement statement, ScriptTrouble trouble)
        {
            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                var steps = sequences[i].Steps;
                for (var s = 0; s < steps.Count; s++)
                    Resolve(system, steps[s] as Run, statement, trouble);
            }

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
                Resolve(system, triggers[i].Does as Run, statement, trouble);
        }

        /// <summary>Turns the name a <c>run</c> or <c>stop</c> used into the number, or says there is no such sequence.</summary>
        static void Resolve(AutomationSystem system, Run run, Statement statement, ScriptTrouble trouble)
        {
            if (run == null)
                return;

            statement = trouble.Where(run, statement);
            if (!string.IsNullOrEmpty(run.Named))
            {
                var named = Called(system, run.Named);
                if (named == null)
                    trouble.Say(statement, $"no sequence called '{run.Named}'.");
                else
                    run.Number = named.Number;

                run.Named = "";
                return;
            }

            if (system.FindSequence(run.Number) == null)
                trouble.Say(statement, $"no sequence {run.Number} to {(run.Stops ? "stop" : "run")}.");
        }

        static Sequence Called(AutomationSystem system, string name)
        {
            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                if (string.Equals(sequences[i].Name, name, StringComparison.Ordinal))
                    return sequences[i];
            }

            return null;
        }

        /// <summary>Checks every name a step acts on, as <see cref="Uses(Statement, string, AutomationSystem, Automation, bool, ScriptTrouble)" /> does.</summary>
        public static void Uses(Statement statement, Act act, AutomationSystem system, Automation program, ScriptTrouble trouble)
        {
            if (act == null)
                return;

            var lists = new List<IList<string>>();
            act.NameLists(lists);
            foreach (var names in lists)
            {
                for (var i = 0; i < names.Count; i++)
                    Uses(statement, names[i], system, program, false, trouble);
            }
        }

        /// <summary>
        /// Checks a name <paramref name="system" /> uses: a block, or a trigger where <paramref name="trigger" /> says so.
        /// </summary>
        /// <remarks>
        /// A variable is looked for at once; a system blueprint's slot is judged once the whole script is read.
        /// </remarks>
        public static void Uses(Statement statement, string name, AutomationSystem system, Automation program, bool trigger,
            ScriptTrouble trouble)
        {
            if (system.IsBlueprint)
            {
                trouble.Owes(statement, name, system, trigger);
                return;
            }

            if (trigger ? program.FindSignalRef(name) == null : program.FindRef(name) == null)
                trouble.Say(statement, $"no variable '{name}' holding {(trigger ? "a trigger" : "a block")} above this line.");
        }
    }
}
