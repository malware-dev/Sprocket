using System;
using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Blocks;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>Reads a logic system out of the script: its slots, triggers and sequences, and each step in them.</summary>
    public static class Systems
    {
        static readonly string[] Times =
        {
            Format.Time,
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
        /// Strict where it matters. A system is its identity, so one without a readable identity, or a second
        /// with the same one, is trouble and not read; so is one with no name, which the editor never makes. A
        /// second system with the same name loads, with a warning, because the name decides nothing but a
        /// person still has to tell the two apart. Whether it is switched on is not here at all - that is
        /// state, and <see cref="StateText" /> holds it.
        /// </remarks>
        public static void ReadSystem(Statement statement, Automation program, Trouble trouble)
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
                trouble.Warn(statement, $"another system is also called {Pen.Spelling(space, called)}.");

            var system = program.Restore(identity, space, called);
            system.Annotation = statement.Annotation;
            system.IsBlueprint = blueprint;

            // The metadata clauses, in any order and each at most once.
            var at = first + 3;
            var changed = false;
            var last = false;
            var from = false;
            while (at < statement.Count)
            {
                if (statement.Says(at, Keyword.Changed) && !changed)
                {
                    system.Changed = Time(statement, at + 1, "'changed'", trouble);
                    changed = true;
                    at += 2;
                }
                else if (statement.Says(at, Keyword.From) && !from && blueprint)
                {
                    at = From(statement, at, system, trouble);
                    from = true;
                }
                else if (statement.Says(at, Keyword.Last) && !last)
                {
                    LastSequence(statement, at, system, trouble);
                    last = true;
                    at += 3;
                }
                else
                {
                    Quotes.Ends(statement, at, trouble);
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
                    trouble.Say(inner, $"unexpected {Quotes.Headed(inner)}; only a system blueprint has slots.");
                else if (inner.Key == Keyword.Whenever || inner.Key == Keyword.When || inner.Key == Keyword.Every)
                    ReadTrigger(inner, system, program, trouble);
                else if (inner.Key == Keyword.Sequence)
                    ReadSequence(inner, system, program, trouble);
                else
                    trouble.Say(inner, $"unexpected {Quotes.Headed(inner)}{(blueprint ? "; expected required, optional, whenever, when, every or sequence." : "; expected whenever, when, every or sequence.")}");
            }

            Resolve(system, statement, trouble);
        }

        /// <summary>
        /// <c>last sequence 4</c>: the highest number the system has handed out, kept only as a failsafe.
        /// </summary>
        /// <remarks>
        /// Whichever is higher wins, this or the sequences in the block, so a sequence somebody added by hand
        /// with a higher number is never undercut by a stale one.
        /// </remarks>
        static void LastSequence(Statement statement, int at, AutomationSystem system, Trouble trouble)
        {
            var number = statement.ValueAt(at + 2);
            if (!statement.Says(at + 1, Keyword.Sequence) || number.Form != ValueForm.Integer || number.Whole < 1
                || number.Whole > int.MaxValue)
            {
                trouble.Say(statement, "expected last sequence <number>.");
                return;
            }

            system.Used((int)number.Whole);
        }

        /// <summary>
        /// Reads <c>from "&lt;guid&gt;" ["&lt;iso&gt;"]</c> at <paramref name="at" /> into the link of <paramref name="system" />,
        /// answering where the next clause starts.
        /// </summary>
        static int From(Statement statement, int at, AutomationSystem system, Trouble trouble)
        {
            Guid origin;
            if (!Guid.TryParseExact(statement.Text(at + 1), "D", out origin) || origin == Guid.Empty)
            {
                trouble.Say(statement, "expected the identity of the blueprint after 'from', a quoted GUID.");
                return at + 2;
            }

            system.Origin = origin;

            // The version is the quoted text after it; a word there is the next clause.
            if (statement.ValueAt(at + 2).Form != ValueForm.Text)
                return at + 2;

            system.OriginChanged = Time(statement, at + 2, "the blueprint's identity", trouble);
            return at + 3;
        }

        /// <summary>The ISO 8601 time at <paramref name="at" />, or null, with trouble naming it as the time after <paramref name="after" />.</summary>
        static DateTime? Time(Statement statement, int at, string after, Trouble trouble)
        {
            DateTime time;
            if (DateTime.TryParseExact(statement.Text(at), Times, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out time))
                return time;

            trouble.Say(statement, $"expected an ISO 8601 time after {after}, like \"2026-09-14T19:02:11Z\".");
            return null;
        }

        static void ReadSlot(Statement statement, AutomationSystem system, Trouble trouble)
        {
            var optional = statement.Key == Keyword.Optional;
            if (!statement.Says(0, Keyword.Slot))
            {
                trouble.Say(statement, optional ? "expected 'slot' after 'optional'." : "expected 'slot' after 'required'.");
                return;
            }

            var name = statement.ValueAt(1);
            if (!Quotes.Named(name))
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
            part.Optional = optional;

            if (!statement.Says(2, Keyword.Expects))
            {
                Quotes.Ends(statement, 2, trouble);
                return;
            }

            var one = statement.ValueAt(3);
            if (one.Says(Keyword.Signal))
                part.IsSignal = true;
            else if (one.Form == ValueForm.Qualified)
                Holding(statement, part, trouble);
            else if (Quotes.Named(one))
                part.Expects = one.Word;
            else
                trouble.Say(statement, "expected signal, one block type or a type the host provides after 'expects'.");

            Quotes.Ends(statement, 4, trouble);
        }

        /// <summary>The type the host provides that the slot <paramref name="part" /> expects, written qualified at 3, or trouble.</summary>
        static void Holding(Statement statement, Part part, Trouble trouble)
        {
            var parts = statement.Parts(3);
            var type = parts.Count == 2 ? HostTypes.Named($"{parts[0].Word}.{parts[1].Word}") : null;
            if (type == null)
            {
                trouble.Say(statement, $"no type '{Quotes.Spelling(parts)}' the host provides.");
                return;
            }

            part.Holds = type.Id;
        }

        /// <summary>The quantifier at <paramref name="at" />, or false and trouble naming <paramref name="after" /> where there is none.</summary>
        static bool Quantified(Statement statement, int at, string after, Trouble trouble, out Quantifier quantifier)
        {
            quantifier = Quantifier.Any;
            if (statement.Says(at, Keyword.All))
                quantifier = Quantifier.All;
            else if (statement.Says(at, Keyword.Sum))
                quantifier = Quantifier.Sum;
            else if (statement.Says(at, Keyword.Average))
                quantifier = Quantifier.Average;
            else if (!statement.Says(at, Keyword.Any))
            {
                trouble.Say(statement, $"expected any, all, sum or average after '{after}'.");
                return false;
            }

            return true;
        }

        /// <summary>The comparison <paramref name="value" /> is, or false where it is none.</summary>
        static bool Related(Value value, out Relation relation)
        {
            relation = Relation.Is;
            if (value.Form != ValueForm.Keyword)
                return false;

            switch (value.Keyword)
            {
                case Keyword.EqualTo: return true;
                case Keyword.NotEqualTo: relation = Relation.IsNot; return true;
                case Keyword.Above: relation = Relation.Above; return true;
                case Keyword.AtLeast: relation = Relation.AtLeast; return true;
                case Keyword.Below: relation = Relation.Below; return true;
                case Keyword.AtMost: relation = Relation.AtMost; return true;
            }

            return false;
        }

        /// <summary>
        /// The member at <paramref name="index" />, bare or qualified by <c>ext</c>, its arguments and its blocks; or false and
        /// trouble where it is not one, <paramref name="what" /> saying what was expected.
        /// </summary>
        static bool Member(Statement statement, int index, string what, Trouble trouble, out string provider, out string id,
            IList<Literal> arguments, IList<string> targets)
        {
            provider = id = "";
            if (!statement.IsMember(index))
            {
                trouble.Say(statement, $"expected {what}.");
                return false;
            }

            var member = statement.Parts(index);
            var bare = member.Count == 1 && member[0].Word.Length > 0;
            var qualified = member.Count == 2 && member[0].Word.Length > 0 && member[1].Word.Length > 0;
            if (!bare && !qualified)
            {
                trouble.Say(statement, $"expected {what}.");
                return false;
            }

            if (qualified && member[0].Word != BlockKinds.Own)
            {
                trouble.Say(statement, $"no namespace '{member[0].Word}'; expected a member, bare or qualified by ext.");
                return false;
            }

            provider = qualified ? member[0].Word : "";
            id = member[member.Count - 1].Word;

            var given = statement.Arguments(index);
            for (var i = 0; i < given.Count; i++)
            {
                if (!Argument(statement, given[i], trouble))
                    return false;

                arguments.Add(Given(given[i]));
            }

            return Names(statement.On(index), targets, statement, trouble);
        }

        /// <summary>Whether <paramref name="value" /> is one a member can be given or compared with, and trouble where it is not.</summary>
        static bool Argument(Statement statement, Value value, Trouble trouble)
        {
            var form = value.Form;
            if (form != ValueForm.List && form != ValueForm.Vector && form != ValueForm.Reference && form != ValueForm.Qualified)
                return true;

            trouble.Say(statement, $"unexpected {Quotes.Spelled(statement, value)}; expected a value.");
            return false;
        }

        /// <summary>
        /// The comparison and the value from <paramref name="at" />, into <paramref name="relation" /> and
        /// <paramref name="value" />; or false and trouble where they are not that, or a number is wanted and not given.
        /// </summary>
        /// <remarks>An ordering compares numbers, and so does a sum, so either is given one.</remarks>
        static bool Compared(Statement statement, int at, Quantifier quantifier, Trouble trouble, out Relation relation,
            out Literal value)
        {
            value = default(Literal);
            if (!Related(statement.ValueAt(at), out relation) || statement.Count <= at + 1)
            {
                trouble.Say(statement, "expected a comparison and a value after the names, like == on.");
                return false;
            }

            var written = statement.ValueAt(at + 1);
            if (!Argument(statement, written, trouble))
                return false;

            value = Given(written);
            var ordered = relation != Relation.Is && relation != Relation.IsNot;
            if ((ordered || quantifier == Quantifier.Sum) && value.Kind != ValueKind.Integer && value.Kind != ValueKind.Real)
            {
                trouble.Say(statement, $"expected a number after '{Keywords.Name(Scribe.Said(relation))}'.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// <c>sequence 3 as 'Let me out' { … }</c>
        /// </summary>
        /// <remarks>
        /// The number is what the sequence is, within its system, so one without a number or with a number its
        /// system already has is trouble and not read. The name is optional.
        /// </remarks>
        static void ReadSequence(Statement statement, AutomationSystem system, Automation program, Trouble trouble)
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

            var at = 1;
            if (statement.Says(1, Keyword.As))
            {
                sequence.Name = Quotes.Named(statement.ValueAt(2)) ? statement.Word(2) : "";
                if (sequence.Name.Length == 0)
                    trouble.Say(statement, "expected a name after 'as'.");

                at = 3;
            }

            Quotes.Ends(statement, at, trouble);
            ReadSteps(statement.Body, sequence.Steps, system, program, trouble);
        }

        /// <summary>The statements of a block, as steps added to <paramref name="into" />: an <c>if</c> as its line, its branches and its end.</summary>
        static void ReadSteps(IList<Statement> body, IList<Step> into, AutomationSystem system, Automation program, Trouble trouble)
        {
            for (var i = 0; i < body.Count; i++)
            {
                var statement = body[i];
                if (statement.Key == Keyword.If)
                {
                    i = ReadIf(body, i, into, system, program, trouble);
                    continue;
                }

                if (statement.Key == Keyword.Else)
                {
                    trouble.Say(statement, "unexpected 'else'; an else is the statement after an if's closing brace.");
                    continue;
                }

                var step = ReadStep(statement, trouble);
                if (step == null)
                    continue;

                step.IsOff = statement.IsOff;
                Uses(statement, step as Act, system, program, trouble);
                into.Add(step);
            }
        }

        /// <summary>
        /// <c>if … then { … }</c> and the <c>else if</c> and <c>else</c> after it, as the lines they are, answering where the
        /// last statement of it stands in <paramref name="body" />.
        /// </summary>
        /// <remarks>
        /// One line for the <c>if</c>, one for each <c>else</c>, the steps of every block between them, and an end, so the
        /// sequence stays one list of lines and every branch ends somewhere.
        /// </remarks>
        static int ReadIf(IList<Statement> body, int at, IList<Step> into, AutomationSystem system, Automation program,
            Trouble trouble)
        {
            var statement = body[at];
            var opens = new If(Tested(statement, 0, system, program, trouble) ?? new Comparison());
            opens.IsOff = statement.IsOff;
            opens.Annotation = statement.Annotation;
            into.Add(opens);
            Blocked(statement, into, system, program, trouble);

            var last = false;
            while (at + 1 < body.Count && body[at + 1].Key == Keyword.Else)
            {
                var branch = body[++at];
                if (branch.IsOff)
                    trouble.Say(branch, "unexpected '//'; an else is switched off with its if.");
                else if (last)
                    trouble.Say(branch, "unexpected 'else'; an else comes last, and once.");

                Comparison test = null;
                if (branch.Says(0, Keyword.If))
                    test = Tested(branch, 1, system, program, trouble) ?? new Comparison();
                else
                {
                    Quotes.Ends(branch, 0, trouble);
                    last = true;
                }

                var other = new Else(test);
                other.Annotation = branch.Annotation;
                into.Add(other);
                Blocked(branch, into, system, program, trouble);
            }

            into.Add(new EndIf());
            return at;
        }

        /// <summary>The steps of the block of an <c>if</c> or an <c>else</c>, or trouble where it has none.</summary>
        static void Blocked(Statement statement, IList<Step> into, AutomationSystem system, Automation program, Trouble trouble)
        {
            if (!statement.HasBlock)
            {
                trouble.Say(statement, "expected '{' to open the block.");
                return;
            }

            ReadSteps(statement.Body, into, system, program, trouble);
        }

        /// <summary>
        /// <c>all ext.filled on [ vent ] == on then</c>, from <paramref name="at" />: what an <c>if</c> or an <c>else if</c>
        /// tests, or null and trouble where it is not that.
        /// </summary>
        static Comparison Tested(Statement statement, int at, AutomationSystem system, Automation program, Trouble trouble)
        {
            var test = new Comparison();
            Quantifier quantifier;
            if (!Quantified(statement, at, "if", trouble, out quantifier))
                return null;

            string provider, id;
            if (!Member(statement, at + 1, "a value on a list of names, like ext.closed on [ inner ]", trouble, out provider,
                    out id, test.Arguments, test.Targets))
                return null;

            Relation relation;
            Literal value;
            if (!Compared(statement, at + 2, quantifier, trouble, out relation, out value))
                return null;

            if (!statement.Says(at + 4, Keyword.Then))
            {
                trouble.Say(statement, "expected 'then' after the value.");
                return null;
            }

            if (!Quotes.Ends(statement, at + 5, trouble))
                return null;

            test.Quantifier = quantifier;
            test.Provider = provider;
            test.MemberId = id;
            test.Relation = relation;
            test.Value = value;
            for (var i = 0; i < test.Targets.Count; i++)
                Uses(statement, test.Targets[i], system, program, false, trouble);

            return test;
        }

        static void ReadTrigger(Statement statement, AutomationSystem system, Automation program, Trouble trouble)
        {
            var trigger = Kind(statement, trouble);
            if (trigger == null)
                return;

            trigger.Does = Does(statement, Doing(statement), trouble);
            if (trigger.Does == null)
                return;

            Uses(statement, trigger.Acts, system, program, trouble);

            var when = trigger as WhenTrigger;
            for (var i = 0; when != null && i < when.Sources.Count; i++)
                Uses(statement, when.Sources[i], system, program, false, trouble);

            var needed = trigger as SignalTrigger;
            if (needed != null && needed.IsWritten)
            {
                for (var i = 0; i < needed.Sources.Count; i++)
                    Uses(statement, needed.Sources[i], system, program, false, trouble);

                for (var i = 0; needed.At != null && !system.IsBlueprint && i < needed.Sources.Count; i++)
                    trouble.Takes(statement, needed.Sources[i], needed.At);
            }
            else if (needed != null)
            {
                Uses(statement, needed.Part, system, program, true, trouble);
                Takes(statement, needed, system, program, trouble);
            }

            trigger.Annotation = statement.Annotation;
            system.AddTrigger(trigger);
        }

        /// <summary>
        /// Trouble where the trigger <paramref name="needed" /> names already starts something: a slot of a system blueprint
        /// another <c>whenever</c> names, or the toolbar slot a system's signal variable holds.
        /// </summary>
        /// <remarks>A toolbar slot holds one action, so a second <c>whenever</c> on it could never be wired.</remarks>
        static void Takes(Statement statement, SignalTrigger needed, AutomationSystem system, Automation program, Trouble trouble)
        {
            if (system.IsBlueprint)
            {
                if (Started(system, needed.Part))
                    trouble.Say(statement, $"slot '{needed.Part}' already starts something; a toolbar slot holds one action.");

                return;
            }

            var declared = program.FindSignalRef(needed.Part);
            for (var i = 0; declared != null && i < declared.Blocks.Count; i++)
                trouble.Takes(statement, declared.Blocks[i], declared.At);
        }

        /// <summary>Whether a <c>whenever</c> of <paramref name="system" /> names <paramref name="slot" />.</summary>
        public static bool Started(AutomationSystem system, string slot)
        {
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var needed = triggers[i] as SignalTrigger;
                if (needed != null && needed.Part == slot)
                    return true;
            }

            return false;
        }

        /// <summary>Where the step a trigger does starts: after <c>then</c>, or after how often.</summary>
        static int Doing(Statement statement)
        {
            if (statement.Key == Keyword.When)
                return statement.Find(Keyword.Then) + 1;

            return statement.Key == Keyword.Every ? 1 : 2;
        }

        /// <summary>The step a trigger does, which is one a sequence could hold and that starts something: run, stop, call or set.</summary>
        static Step Does(Statement statement, int at, Trouble trouble)
        {
            var step = statement.Rest(at);
            if (step.Key != Keyword.Run && step.Key != Keyword.Stop && step.Key != Keyword.Call && step.Key != Keyword.Set)
            {
                trouble.Say(statement, "expected run, stop, call or set.");
                return null;
            }

            var does = ReadStep(step, trouble);
            if (does != null)
                does.Annotation = "";

            return does;
        }

        static Trigger Kind(Statement statement, Trouble trouble)
        {
            if (statement.Key == Keyword.When)
                return When(statement, trouble);

            if (statement.Key == Keyword.Every)
                return Every(statement, trouble);

            if (!statement.Says(1, Keyword.Then) || (!Quotes.Named(statement.ValueAt(0)) && !statement.IsMember(0)))
            {
                trouble.Say(statement, "expected whenever <signal> then <step>.");
                return null;
            }

            // whenever button then run 1: the signal button, a variable of a system's or a signal slot of a blueprint's.
            if (Quotes.Named(statement.ValueAt(0)))
                return SignalTrigger.Named(statement.Word(0));

            return Written(statement, trouble);
        }

        /// <summary>
        /// <c>whenever ext.'finishes-closing' on [ inner ] then</c> or <c>whenever ext.toolbar(1, 1, 4) on [ panel1 ] then</c>: a signal
        /// written in place, a block's own change or a slot on the blocks' toolbars; or null and trouble where it is neither.
        /// </summary>
        static Trigger Written(Statement statement, Trouble trouble)
        {
            var member = statement.Parts(0);
            if (member.Count != 2 || member[0].Word.Length == 0 || member[1].Word.Length == 0)
            {
                trouble.Say(statement, "expected a signal qualified by its namespace, like ext.'finishes-closing' on [ inner ].");
                return null;
            }

            if (member[0].Word != BlockKinds.Own)
            {
                trouble.Say(statement, $"no namespace '{member[0].Word}'; expected a signal qualified by ext.");
                return null;
            }

            var written = SignalTrigger.Written(member[0].Word, member[1].Word);
            var given = statement.Arguments(0);
            if (member[1].Word == Format.Toolbar)
            {
                if (given.Count != 3 || Statement.Counted(given[0]) == 0 || Statement.Counted(given[1]) == 0
                    || Statement.Counted(given[2]) == 0)
                {
                    trouble.Say(statement, "expected ext.toolbar(<toolbar>, <page>, <slot>), each a whole number from 1.");
                    return null;
                }

                written.At = new ToolbarSlot(Statement.Counted(given[0]), Statement.Counted(given[1]), Statement.Counted(given[2]));
            }
            else if (given.Count > 0)
            {
                trouble.Say(statement, "unexpected arguments; a block's own signal takes none.");
                return null;
            }

            return Names(statement.On(0), written.Sources, statement, trouble) ? written : null;
        }

        static Trigger When(Statement statement, Trouble trouble)
        {
            Quantifier quantifier;
            if (!Quantified(statement, 0, "when", trouble, out quantifier))
                return null;

            var then = statement.Find(Keyword.Then);
            if (then < 0)
            {
                trouble.Say(statement, "expected 'then'.");
                return null;
            }

            var when = new WhenTrigger("");
            string provider, id;
            if (!Member(statement, 1, "a value on a list of names, like ext.closed on [ inner ]", trouble, out provider, out id,
                    when.Arguments, when.Sources))
                return null;

            Relation relation;
            Literal value;
            if (!Compared(statement, 2, quantifier, trouble, out relation, out value))
                return null;

            when.EventId = id;
            when.Provider = provider;
            when.Quantifier = quantifier;
            when.Relation = relation;
            when.Value = value;

            var at = 4;
            if (statement.Says(at, Keyword.Every))
            {
                Duration interval;
                if (!Lasting(statement.ValueAt(at + 1), 1, out interval))
                {
                    trouble.Say(statement, "expected a duration after 'every', like 30t.");
                    return null;
                }

                when.Interval = interval;
                at += 2;
            }

            if (at != then)
            {
                trouble.Say(statement, "expected 'then'.");
                return null;
            }

            return when;
        }

        static Trigger Every(Statement statement, Trouble trouble)
        {
            Duration interval;
            if (!Lasting(statement.ValueAt(0), 1, out interval))
            {
                trouble.Say(statement, "expected a duration after 'every', like 600t.");
                return null;
            }

            return new EveryTrigger(interval);
        }

        /// <summary>
        /// A duration as it was written, or false where it was no length of time that can be kept.
        /// </summary>
        /// <remarks>
        /// The text reads into a long and ticks are counted in an int, so the bound is the language's
        /// as much as the reader's: <c>every 4294967296t</c> is not an interval, it is a number that
        /// wraps onto one, and <c>every -600t</c> is not a rate at all. Seconds are held to the same
        /// bound by the ticks they come to.
        /// </remarks>
        static bool Lasting(Value value, long least, out Duration length)
        {
            length = default(Duration);
            if (value.Form != ValueForm.Duration || value.Whole < least || value.Whole > int.MaxValue)
                return false;

            length = value.Word == Parser.Seconds ? Duration.OfSeconds(value.Real) : Duration.OfTicks((int)value.Whole);
            return true;
        }

        /// <summary>A step, or nothing where the statement was not one, which is trouble.</summary>
        static Step ReadStep(Statement statement, Trouble trouble)
        {
            if (statement.Key == Keyword.Await)
                return Quotes.Ends(statement, 0, trouble) ? Noted(new Await(), statement) : null;

            if (statement.Key == Keyword.Run || statement.Key == Keyword.Stop)
                return Quotes.Ends(statement, 1, trouble) ? Called(statement, trouble) : null;

            if (statement.Key == Keyword.Wait)
            {
                Duration length;
                if (Lasting(statement.ValueAt(0), 0, out length))
                    return Quotes.Ends(statement, 1, trouble) ? Noted(new Wait(length), statement) : null;

                trouble.Say(statement, "expected a duration after 'wait', like 60t.");
                return null;
            }

            if (statement.Key == Keyword.Call)
                return Acted(statement, trouble);

            if (statement.Key == Keyword.Set)
                return ReadSet(statement, trouble);

            trouble.Say(statement, $"unexpected {Quotes.Headed(statement)}; expected call, set, run, stop, await or wait.");
            return null;
        }

        /// <summary>
        /// <c>call ext.close on [ inner ]</c> or <c>call SetColor(255, 0, 0) on [ light ]</c>: an action, bare or qualified by
        /// its namespace, on some blocks, given its arguments.
        /// </summary>
        static Step Acted(Statement statement, Trouble trouble)
        {
            var act = new Act("");
            string provider, id;
            if (!Member(statement, 0, "an action on a list of names after 'call', like ext.close on [ inner ]", trouble,
                    out provider, out id, act.Arguments, act.Targets))
                return null;

            if (!Quotes.Ends(statement, 1, trouble))
                return null;

            act.ActionId = id;
            act.Form = provider.Length == 0 ? Nature.Action : Nature.Native;
            if (provider.Length > 0)
                act.Provider = provider;

            return Noted(act, statement);
        }

        /// <summary><c>set Radius on [ light ] to 4.5</c>: one of the block's own values.</summary>
        /// <remarks>
        /// The value is taken by position rather than by what it looks like, so a flag written <c>on</c> is a value and
        /// not mistaken for one of the language's own words.
        /// </remarks>
        static Step ReadSet(Statement statement, Trouble trouble)
        {
            var act = new Act("");
            string provider, id;
            if (!Member(statement, 0, "a value on a list of names after 'set', like Radius on [ light ]", trouble,
                    out provider, out id, act.Arguments, act.Targets))
                return null;

            if (provider.Length > 0 && provider != BlockKinds.Own)
            {
                trouble.Say(statement, $"no value '{Pen.Spelling(provider, id)}' to set; expected one of the block's own, written bare, or one qualified by {BlockKinds.Own}.");
                return null;
            }

            if (act.Arguments.Count > 0)
            {
                trouble.Say(statement, "unexpected arguments; a value takes none.");
                return null;
            }

            if (!statement.Says(1, Keyword.To) || statement.Count < 3)
            {
                trouble.Say(statement, "expected set <value> on [ <names> ] to <value>.");
                return null;
            }

            if (!Quotes.Ends(statement, 3, trouble) || !Argument(statement, statement.ValueAt(2), trouble))
                return null;

            act.ActionId = id;
            act.Form = Nature.Value;
            act.Provider = provider;
            act.Arguments.Add(Given(statement.ValueAt(2)));
            return Noted(act, statement);
        }

        /// <summary>One value, as whatever kind it was written as.</summary>
        /// <remarks>A bare word that is not a flag is a name: one of a list of names a member gives.</remarks>
        static Literal Given(Value value)
        {
            if (value.Form == ValueForm.Text)
                return Literal.Of(value.Word);

            if (value.Form == ValueForm.Integer || value.Form == ValueForm.Duration)
                return Literal.Of(value.Whole);

            if (value.Form == ValueForm.Real)
                return Literal.Of(value.Real);

            if (value.Form == ValueForm.Color)
            {
                return Literal.Colour((byte)(value.Whole >> 24), (byte)(value.Whole >> 16),
                    (byte)(value.Whole >> 8), (byte)value.Whole);
            }

            if (value.Says(Keyword.On))
                return Literal.Of(true);

            if (value.Says(Keyword.Off))
                return Literal.Of(false);

            return Literal.Name(value.Word);
        }

        static Step Noted(Step step, Statement statement)
        {
            step.Annotation = statement.Annotation;
            return step;
        }

        /// <summary><c>run 2</c>, <c>stop 2</c> or <c>run 'Let me out'</c>: another sequence of the same system.</summary>
        static Step Called(Statement statement, Trouble trouble)
        {
            var stops = statement.Key == Keyword.Stop;
            var named = statement.ValueAt(0);
            if (named.Form == ValueForm.Integer && named.Whole >= 1 && named.Whole <= int.MaxValue)
            {
                var numbered = new Run((int)named.Whole);
                numbered.Stops = stops;
                trouble.Wrote(statement, numbered);
                return Noted(numbered, statement);
            }

            if (Quotes.Named(named))
            {
                var run = new Run(0);
                run.Named = named.Word;
                run.Stops = stops;
                trouble.Wrote(statement, run);
                return Noted(run, statement);
            }

            trouble.Say(statement, $"expected a sequence's number or name after '{Keywords.Name(statement.Key)}'.");
            return null;
        }

        /// <summary>
        /// Matches what the system's <c>run</c> steps and triggers name, now the whole system is read.
        /// </summary>
        /// <remarks>
        /// Late, because a sequence may be named or numbered before it is written. Anything naming a
        /// sequence the system has not got is trouble: it can never run, nothing else would say so, and
        /// a number that is not there would take the runner down on the tick that reached it.
        /// </remarks>
        static void Resolve(AutomationSystem system, Statement statement, Trouble trouble)
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
        static void Resolve(AutomationSystem system, Run run, Statement statement, Trouble trouble)
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

        /// <summary>The names in <paramref name="items" />, into <paramref name="into" />; or false, and trouble, where one is not a name.</summary>
        static bool Names(ValueRange items, IList<string> into, Statement statement, Trouble trouble)
        {
            into.Clear();
            for (var i = 0; i < items.Count; i++)
            {
                if (!Quotes.Named(items[i]))
                {
                    trouble.Say(statement, "expected names in the list.");
                    return false;
                }

                into.Add(items[i].Word);
            }

            return true;
        }

        /// <summary>Checks every name a step acts on, as <see cref="Uses(Statement, string, AutomationSystem, Automation, bool, Trouble)" /> does.</summary>
        static void Uses(Statement statement, Act act, AutomationSystem system, Automation program, Trouble trouble)
        {
            for (var i = 0; act != null && i < act.Targets.Count; i++)
                Uses(statement, act.Targets[i], system, program, false, trouble);
        }

        /// <summary>
        /// Checks a name <paramref name="system" /> uses: a block, or a trigger where <paramref name="trigger" /> says so.
        /// </summary>
        /// <remarks>
        /// A system names the program's variables, which are written above it, so they are looked for at once. A system
        /// blueprint names its own slots, which may be written below, so those are judged once the whole script is read.
        /// </remarks>
        static void Uses(Statement statement, string name, AutomationSystem system, Automation program, bool trigger,
            Trouble trouble)
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
