using System;
using System.Collections.Generic;
using System.Globalization;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>
    /// Writes a program out as a script, and reads one back.
    /// </summary>
    /// <remarks>
    /// Space Engineers does not whitelist IO, and a mod stores its data in the save. So this turns a
    /// program into a string and back, and whoever holds the string decides where it lives.
    /// <para>
    /// <b>The script is the program.</b> There is nothing else to project it from - the model lives in
    /// memory in a mod the game may unload at any moment - so this is not a view of the truth, it is
    /// the truth written down. Two things follow and both are load-bearing: it stays readable so a save
    /// that breaks can be looked at and mended, and a text with anything wrong in it is read as nothing,
    /// kept whole as it was written and marked <see cref="Automation.Intact" /> false so that the next
    /// save cannot write over it.
    /// </para>
    /// <para>
    /// It is a language and not prose, and that is the line to hold: keywords here are syntax, the way
    /// <c>if</c> and <c>while</c> are, and nobody localises those. The moment it drifts towards English
    /// it puts word order and vocabulary into the model and freezes them in a file format, where they
    /// become a compatibility promise. Every word a person reads is the host's; every word in here is
    /// the format's.
    /// </para>
    /// </remarks>
    public static class ProgramText
    {
        const int Version = 1;

        const int Draft = 10;

        /// <summary>The host function this host answers a trigger with: a slot on a block's toolbar.</summary>
        /// <remarks>Persisted in every program with a trigger, and written by <see cref="Pen.Slot" />.</remarks>
        public const string Toolbar = "toolbar";

        static readonly string[] Times =
        {
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'",
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFF'Z'",
            "yyyy'-'MM'-'dd'T'HH':'mm':'sszzz",
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFzzz",
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss",
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFF",
            "yyyy'-'MM'-'dd'T'HH':'mm'Z'",
            "yyyy'-'MM'-'dd'T'HH':'mm",
            "yyyy'-'MM'-'dd"
        };

        public static string Save(Automation program, Values values)
        {
            var pen = new Pen();
            pen.Note(program.Annotation);
            pen.Start(Keyword.Sprocket).Whole(Version).Says(Keyword.Alpha).Whole(Draft);
            pen.End();

            // Before anything that names one, so a reader has them in hand by the time it meets a step. A variable
            // nothing names is left out.
            var blocks = new HashSet<string>(StringComparer.Ordinal);
            var triggers = new HashSet<string>(StringComparer.Ordinal);
            Used(program, blocks, triggers);

            var refs = program.Refs;
            var first = true;
            for (var i = 0; i < refs.Count; i++)
            {
                if (!blocks.Contains(refs[i].Name))
                    continue;

                if (first)
                    pen.Blank();

                first = false;
                WriteRef(pen, values, refs[i]);
            }

            // After the blocks, since each is on one of them.
            var held = program.TriggerRefs;
            for (var i = 0; i < held.Count; i++)
            {
                if (triggers.Contains(held[i].Name))
                    WriteTriggerRef(pen, held[i]);
            }

            var systems = program.Systems;
            for (var i = 0; i < systems.Count; i++)
                WriteSystem(pen, systems[i], true);

            // After every system, because an instance names the system it is one of and a reader
            // should have met it by then.
            for (var i = 0; i < systems.Count; i++)
            {
                var instances = systems[i].Instances;
                for (var n = 0; n < instances.Count; n++)
                    WriteInstance(pen, program, systems[i], instances[n]);
            }

            return pen.ToString();
        }

        /// <summary>The blueprint <paramref name="system" /> written out alone, as a player's library keeps it.</summary>
        /// <remarks>A blueprint names nothing outside itself, so the system is the whole of it; its instances are the construct's.</remarks>
        public static string Blueprint(AutomationSystem system)
        {
            var pen = new Pen();
            pen.Start(Keyword.Sprocket).Whole(Version).Says(Keyword.Alpha).Whole(Draft);
            pen.End();
            WriteSystem(pen, system, true);
            return pen.ToString();
        }

        /// <summary>The blueprints <paramref name="systems" /> written out together, as a backup keeps them: no blocks, and no installations.</summary>
        public static string Blueprints(IList<AutomationSystem> systems)
        {
            var pen = new Pen();
            pen.Start(Keyword.Sprocket).Whole(Version).Says(Keyword.Alpha).Whole(Draft);
            pen.End();
            for (var i = 0; i < systems.Count; i++)
                WriteSystem(pen, systems[i], true);

            return pen.ToString();
        }

        /// <summary>What <paramref name="system" /> says, written out alone without when it last changed: what tells whether it has.</summary>
        public static string Said(AutomationSystem system)
        {
            var pen = new Pen();
            WriteSystem(pen, system, false);
            return pen.ToString();
        }

        /// <summary>
        /// Reads the blueprint <paramref name="text" /> into <paramref name="program" />, beside what it holds, answering what was
        /// wrong with it, or empty.
        /// </summary>
        /// <remarks>
        /// Read into a program of its own first, because a text with an error in it is trusted in no part: where anything is
        /// wrong, <paramref name="program" /> is left as it was. The text holds one system blueprint and nothing else, as
        /// <see cref="Blueprint" /> writes it.
        /// </remarks>
        public static string AddBlueprint(string text, Automation program, Values values)
        {
            AutomationSystem lone;
            var trouble = Lone(text, values, out lone);
            if (trouble.Length > 0)
                return trouble;

            var identity = lone.Identity;
            if (program.FindSystem(identity) != null)
                return "system " + identity.ToString("D") + " is already declared.";

            var parser = new Parser(text);
            var constructs = parser.Read();
            var read = new Trouble(text, parser.Trouble);
            for (var i = 0; i < constructs.Count; i++)
            {
                if (constructs[i].Key == Keyword.System)
                    ReadSystem(constructs[i], program, read);
            }

            read.Settle();
            return read.What;
        }

        /// <summary>
        /// Reads the blueprint <paramref name="text" /> on its own into <paramref name="read" />, answering what was wrong with it,
        /// or empty.
        /// </summary>
        /// <remarks>The text holds one system blueprint and nothing else, as <see cref="Blueprint" /> writes it; where it does not, <paramref name="read" /> is null.</remarks>
        public static string Lone(string text, Values values, out AutomationSystem read)
        {
            read = null;
            var trial = new Automation();
            var trouble = Load(text, trial, values);
            if (trouble.Length > 0)
                return trouble;

            if (trial.Systems.Count != 1 || !trial.Systems[0].IsBlueprint || trial.Refs.Count > 0 || trial.TriggerRefs.Count > 0
                || trial.Systems[0].Instances.Count > 0)
                return "expected one system blueprint and nothing else.";

            read = trial.Systems[0];
            return "";
        }

        /// <summary>
        /// The variables something in <paramref name="program" /> names: in <paramref name="blocks" /> those holding a
        /// block, in <paramref name="triggers" /> those holding a trigger.
        /// </summary>
        /// <remarks>
        /// A system names variables in its steps and triggers; a blueprint names only its slots, and its instances name the
        /// variables answering them. A trigger variable names the block it is on.
        /// </remarks>
        static void Used(Automation program, HashSet<string> blocks, HashSet<string> triggers)
        {
            var named = new List<string>();
            var systems = program.Systems;
            for (var i = 0; i < systems.Count; i++)
            {
                var system = systems[i];
                if (!system.IsBlueprint)
                {
                    system.Acted(named);
                    for (var n = 0; n < named.Count; n++)
                    {
                        if (program.FindTriggerRef(named[n]) != null)
                            triggers.Add(named[n]);
                        else
                            blocks.Add(named[n]);
                    }

                    continue;
                }

                var instances = system.Instances;
                for (var n = 0; n < instances.Count; n++)
                {
                    foreach (var slot in instances[n].Filled)
                    {
                        var filled = instances[n].FilledWith(slot);
                        var at = instances[n].Positions(slot);
                        for (var f = 0; f < filled.Count; f++)
                        {
                            blocks.Add(filled[f]);
                            var declared = at == null || f >= at.Count ? null : program.FindTriggerRef(filled[f], at[f]);
                            if (declared != null)
                                triggers.Add(declared.Name);
                        }
                    }
                }
            }

            var held = program.TriggerRefs;
            for (var i = 0; i < held.Count; i++)
            {
                if (triggers.Contains(held[i].Name))
                    blocks.Add(held[i].Block);
            }
        }

        /// <summary>The line, counted from 1, that trouble this reader reported names, or 0 where it names none.</summary>
        /// <remarks>Read back out of the words, which start <c>Line 5:</c> wherever a line is known.</remarks>
        public static int LineOf(string trouble)
        {
            const string lead = "Line ";
            if (trouble == null || !trouble.StartsWith(lead, StringComparison.Ordinal))
                return 0;

            var line = 0;
            for (var i = lead.Length; i < trouble.Length && trouble[i] >= '0' && trouble[i] <= '9'; i++)
                line = line * 10 + (trouble[i] - '0');

            return line;
        }

        /// <summary>
        /// Reads a program back into an empty one, and answers what was wrong with it.
        /// </summary>
        /// <remarks>
        /// An empty answer means everything that was written was read. Anything else leaves the program
        /// empty, because a text with an error in it is trusted in no part, and is for the host to log or
        /// show; the program itself carries the same news as <see cref="Automation.Intact" />, because the
        /// decision it forces - do not save over this - is taken somewhere else entirely.
        /// </remarks>
        public static string Load(string saved, Automation program, Values values)
        {
            string warning;
            return Load(saved, program, values, out warning);
        }

        /// <summary>
        /// Reads a program back into an empty one, and answers what was wrong with it and what only looked
        /// wrong.
        /// </summary>
        /// <remarks>
        /// A warning is something that reads and runs but is probably a mistake, such as two systems with
        /// the same name. It does not mark the program damaged.
        /// </remarks>
        public static string Load(string saved, Automation program, Values values, out string warning)
        {
            if (string.IsNullOrEmpty(saved))
            {
                warning = "";
                return "";
            }

            var parser = new Parser(saved);
            var constructs = parser.Read();
            var trouble = new Trouble(saved, parser.Trouble);

            // Turned away whole rather than read as far as it goes. A different version or draft is
            // not a damaged program, it is one this build does not speak, so whatever happened to read
            // would be read as something it does not mean. A header that would not parse says nothing
            // about its version, so that is damage like any other.
            string refused = null;
            var header = constructs.Count > 0 && constructs[0].Key == Keyword.Sprocket ? constructs[0] : null;
            if (header == null && parser.Trouble.Length == 0)
                refused = (constructs.Count == 0 ? "Line 1: " : trouble.Line(constructs[0])) + "expected sprocket <format>.";
            else if (header != null && header.Whole(0) != Version)
                refused = trouble.Line(header) + "written for sprocket " + header.Whole(0) + "; this reads sprocket " + Version + ".";
            else if (header != null && DraftOf(header) != Draft)
                refused = trouble.Line(header) + "written for " + (DraftOf(header) < 0 ? "the finished format" : "alpha " + DraftOf(header))
                    + "; this reads alpha " + Draft + ".";

            if (refused != null)
            {
                program.Unreadable(saved);
                program.Refused(refused);
                warning = "";
                return refused;
            }

            if (header != null)
            {
                Header(header, trouble);
                program.Annotation = header.Annotation;
            }

            for (var i = 0; i < constructs.Count; i++)
            {
                var construct = constructs[i];
                if (construct.IsOff)
                    trouble.Say(construct, "unexpected '//'; only a step in a sequence can be switched off.");
                else if (construct.Key == Keyword.Let)
                    ReadLet(construct, program, values, trouble);
                else if (construct.Key == Keyword.System)
                    ReadSystem(construct, program, trouble);
                else if (construct.Key == Keyword.Instance)
                    ReadInstance(construct, program, trouble);
                else if (i > 0)
                    trouble.Say(construct, "unexpected " + Headed(construct) + "; expected let, system or instance.");
            }

            trouble.Settle();

            if (trouble.What.Length > 0)
            {
                program.Unreadable(saved);
                program.Damaged(trouble.What);
            }

            warning = trouble.Warning;
            return trouble.What;
        }

        /// <summary>Says what is wrong with the shape of <paramref name="header" />: whole numbers, and nothing after them.</summary>
        static void Header(Statement header, Trouble trouble)
        {
            var alpha = header.Says(1, Keyword.Alpha);
            if (header.ValueAt(0).Form != ValueForm.Integer || (alpha && header.ValueAt(2).Form != ValueForm.Integer))
            {
                trouble.Say(header, "expected whole numbers in sprocket <format>.");
                return;
            }

            Ends(header, alpha ? 3 : 1, trouble);
        }

        /// <summary>
        /// Which draft of the format a program says it was written against, or none where it does not say.
        /// </summary>
        /// <remarks>
        /// A program with no <c>alpha</c> clause was written against a finished format, and this build
        /// writes an unfinished one - so the absence is a number this build can never be, rather than a
        /// reason to let it through.
        /// </remarks>
        static int DraftOf(Statement statement)
        {
            var at = statement.Find(Keyword.Alpha);
            return at < 0 ? -1 : statement.Whole(at + 1);
        }

        /// <summary><c>let door1 = block 'Door/LargeBlockDoor' [ 7485…, "Airlock Inner Door" ]</c></summary>
        static void WriteRef(Pen pen, Values values, EntityRef declared)
        {
            pen.Note(declared.Annotation);
            pen.Start(Keyword.Let).Word(declared.Name).Says(Keyword.Equals).Says(Keyword.Block).Word(BlockKinds.Sort(declared.KindId));
            pen.OpenList();

            var keys = declared.Keys;
            for (var i = 0; i < keys.Count; i++)
                WriteKey(pen, values, keys[i]);

            pen.CloseList();
            pen.End();
        }

        /// <summary>
        /// One of a declaration's keys, written as whatever sort of value it is.
        /// </summary>
        /// <remarks>
        /// The syntax carries the kind, so nothing here tags anything: a whole number reads back as a
        /// whole number because that is what it looks like. Which is the point - this reader does not know what
        /// a key means, only what it is, and the host that minted the list is the only thing that reads
        /// it.
        /// </remarks>
        static void WriteKey(Pen pen, Values values, EntityKey key)
        {
            if (key.Cells != null)
            {
                pen.OpenList();
                for (var i = 0; i < key.Cells.Length; i++)
                    pen.Vector(key.Cells[i].X, key.Cells[i].Y, key.Cells[i].Z);

                pen.CloseList();
                return;
            }

            if (key.Kind == ValueKind.Text)
            {
                string text;
                values.TryText(key.Kind, key.Value, out text);
                pen.Text(text);
                return;
            }

            if (key.Kind == ValueKind.Integer)
            {
                long whole;
                values.TryInteger(key.Kind, key.Value, out whole);
                pen.Whole(whole);
                return;
            }

            if (key.Kind == ValueKind.Flag)
            {
                pen.Says(values.Flag(key.Kind, key.Value) ? Keyword.On : Keyword.Off);
                return;
            }

            if (key.Kind == ValueKind.Color)
            {
                byte r, g, b, a;
                values.TryColor(key.Kind, key.Value, out r, out g, out b, out a);
                pen.Colour(r, g, b, a);
                return;
            }

            double real;
            values.TryReal(ValueKind.Real, key.Value, out real);
            pen.Real(real);
        }

        /// <summary>A list of vectors of whole numbers as cells, or null where it is anything else.</summary>
        static Vector3I[] Cells(ValueRange items, Statement statement)
        {
            if (items.Count == 0)
                return null;

            var cells = new Vector3I[items.Count];
            for (var i = 0; i < items.Count; i++)
            {
                var numbers = statement.Within(items[i]);
                if (items[i].Form != ValueForm.Vector || numbers.Count != 3)
                    return null;

                for (var n = 0; n < 3; n++)
                {
                    if (numbers[n].Form != ValueForm.Integer)
                        return null;
                }

                cells[i] = new Vector3I((int)numbers[0].Whole, (int)numbers[1].Whole, (int)numbers[2].Whole);
            }

            return cells;
        }

        /// <summary><c>let button = panel1:toolbar(1, 1, 4)</c>: a trigger, the slot this host answers one with, on a block above.</summary>
        static void WriteTriggerRef(Pen pen, TriggerRef declared)
        {
            pen.Note(declared.Annotation);
            pen.Start(Keyword.Let).Word(declared.Name).Says(Keyword.Equals).Slot(declared.Block, declared.At);
            pen.End();
        }

        /// <summary>
        /// <c>let door1 = block '…' [ … ]</c> or <c>let button = panel1:toolbar(1, 1, 4)</c>: a variable, holding a block or a
        /// trigger on one.
        /// </summary>
        static void ReadLet(Statement statement, Automation program, Values values, Trouble trouble)
        {
            var name = statement.ValueAt(0);
            if (!Named(name) || !statement.Says(1, Keyword.Equals))
            {
                trouble.Say(statement, "expected let <name> = <value>.");
                return;
            }

            if (program.Names(name.Word))
            {
                trouble.Say(statement, "variable '" + name.Word + "' is already defined.");
                return;
            }

            if (statement.ValueAt(2).Form == ValueForm.Function)
            {
                if (!Ends(statement, 3, trouble))
                    return;

                string block;
                ToolbarSlot at;
                if (!Trigger(statement, statement.ValueAt(2), program, trouble, out block, out at))
                    return;

                var trigger = new TriggerRef(name.Word, block, at);
                trigger.Annotation = statement.Annotation;
                program.Restore(trigger);
                return;
            }

            if (!statement.Says(2, Keyword.Block) || !Named(statement.ValueAt(3)) || statement.ValueAt(4).Form != ValueForm.List)
            {
                trouble.Say(statement, "expected a block, like block '<type>/<subtype>' [ <keys> ], or a trigger, like panel1:toolbar(1, 1, 4).");
                return;
            }

            if (!Ends(statement, 5, trouble))
                return;

            var declared = new EntityRef(name.Word, BlockKinds.Definition(statement.Word(3)));
            declared.Annotation = statement.Annotation;
            var keys = statement.Items(4);
            for (var i = 0; i < keys.Count; i++)
                ReadKey(values, keys[i], declared.Keys, statement, trouble);

            program.Restore(declared);
        }

        /// <summary>
        /// The trigger the host function <paramref name="value" /> gives back: this host's <c>toolbar</c>, a slot on the
        /// toolbars of a block a variable above holds, or trouble.
        /// </summary>
        static bool Trigger(Statement statement, Value value, Automation program, Trouble trouble, out string block,
            out ToolbarSlot at)
        {
            at = null;
            string function;
            ValueRange given;
            statement.Function(value, out block, out function, out given);
            if (function != Toolbar)
            {
                trouble.Say(statement, "no host function '" + function + "'; expected toolbar(<toolbar>, <page>, <slot>).");
                return false;
            }

            if (given.Count != 3 || Statement.Counted(given[0]) == 0 || Statement.Counted(given[1]) == 0 || Statement.Counted(given[2]) == 0)
            {
                trouble.Say(statement, "expected toolbar(<toolbar>, <page>, <slot>), each a whole number from 1.");
                return false;
            }

            if (program.FindRef(block) == null)
            {
                trouble.Say(statement, "no variable '" + block + "' holding a block above this line.");
                return false;
            }

            at = new ToolbarSlot(Statement.Counted(given[0]), Statement.Counted(given[1]), Statement.Counted(given[2]));
            return true;
        }

        /// <summary>
        /// A key, or trouble where this build has no kind that could hold it.
        /// </summary>
        static void ReadKey(Values values, Value key, IList<EntityKey> into, Statement statement,
            Trouble trouble)
        {
            switch (key.Form)
            {
                case ValueForm.List:
                    Vector3I[] cells = Cells(statement.Within(key), statement);
                    if (cells == null)
                    {
                        trouble.Say(statement, "expected cells in the list among the keys, like [ (0 2 -1), (3 0 0) ].");
                        return;
                    }

                    into.Add(new EntityKey(cells));
                    return;

                case ValueForm.Integer:
                    into.Add(new EntityKey(ValueKind.Integer, EntityValue.Of(key.Whole)));
                    return;

                case ValueForm.Real:
                    into.Add(new EntityKey(ValueKind.Real, EntityValue.Of(key.Real)));
                    return;

                case ValueForm.Text:
                    into.Add(new EntityKey(ValueKind.Text, values.Text(key.Word)));
                    return;

                case ValueForm.Color:
                    byte r, g, b, a;
                    key.Colour(out r, out g, out b, out a);
                    into.Add(new EntityKey(ValueKind.Color, EntityValue.Of(r, g, b, a)));
                    return;

                case ValueForm.Keyword:
                    if (key.Keyword == Keyword.On || key.Keyword == Keyword.Off)
                    {
                        into.Add(new EntityKey(ValueKind.Flag, EntityValue.Of(key.Keyword == Keyword.On)));
                        return;
                    }

                    break;
            }

            trouble.Say(statement, "unsupported key in the block's list.");
        }

        static void WriteSystem(Pen pen, AutomationSystem system, bool dated)
        {
            pen.Blank();
            pen.Note(system.Annotation);
            pen.Start(Keyword.System);
            if (system.IsBlueprint)
                pen.Says(Keyword.Blueprint);

            pen.Text(system.Identity.ToString("D")).Says(Keyword.As).Qualified(system.Namespace, system.Name);

            if (dated && system.Changed.HasValue)
            {
                pen.Says(Keyword.Changed)
                    .Text(system.Changed.Value.ToString(Times[0], CultureInfo.InvariantCulture));
            }

            if (system.LastSequenceNumber > system.HighestSequenceNumber())
                pen.Says(Keyword.Last).Says(Keyword.Sequence).Whole(system.LastSequenceNumber);

            pen.Body();

            // The slots before what uses them, so a reader meets the vocabulary before the answers. A slot nothing uses is
            // left out, and so are an instance's answers to it.
            var used = new List<string>();
            system.Acted(used);

            var parts = system.Parts;
            for (var i = 0; i < parts.Count; i++)
            {
                if (used.Contains(parts[i].Id))
                    WriteSlot(pen, parts[i]);
            }

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
                WriteTrigger(pen, triggers[i]);

            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
                WriteSequence(pen, sequences[i]);

            pen.Shut();
        }

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
        static void ReadSystem(Statement statement, Automation program, Trouble trouble)
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
                trouble.Say(statement, "system " + identity.ToString("D") + " is already declared.");
                return;
            }

            var space = name.Count == 2 ? name[0].Word : "";
            var called = name[name.Count - 1].Word;
            if (program.FindSystem(space, called) != null)
                trouble.Warn(statement, "another system is also called " + Pen.Spelling(space, called) + ".");

            var system = program.Restore(identity, space, called);
            system.Annotation = statement.Annotation;
            system.IsBlueprint = blueprint;

            // The two metadata clauses, in either order and each at most once.
            var at = first + 3;
            var changed = false;
            var last = false;
            while (at < statement.Count)
            {
                if (statement.Says(at, Keyword.Changed) && !changed)
                {
                    system.Changed = Time(statement, at + 1, trouble);
                    changed = true;
                    at += 2;
                }
                else if (statement.Says(at, Keyword.Last) && !last)
                {
                    LastSequence(statement, at, system, trouble);
                    last = true;
                    at += 3;
                }
                else
                {
                    Ends(statement, at, trouble);
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
                    trouble.Say(inner, "unexpected " + Headed(inner) + "; only a system blueprint has slots.");
                else if (inner.Key == Keyword.On || inner.Key == Keyword.When || inner.Key == Keyword.Every)
                    ReadTrigger(inner, system, program, trouble);
                else if (inner.Key == Keyword.Sequence)
                    ReadSequence(inner, system, program, trouble);
                else
                    trouble.Say(inner, "unexpected " + Headed(inner) + (blueprint
                        ? "; expected required, optional, on, when, every or sequence."
                        : "; expected on, when, every or sequence."));
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

        /// <summary>A <c>changed</c> time, or null and trouble when it is not ISO 8601.</summary>
        static DateTime? Time(Statement statement, int at, Trouble trouble)
        {
            DateTime time;
            if (DateTime.TryParseExact(statement.Text(at), Times, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out time))
                return time;

            trouble.Say(statement, "expected an ISO 8601 time after 'changed', like \"2026-09-14T19:02:11Z\".");
            return null;
        }

        /// <summary>
        /// <c>required slot inner expects Door</c>: something the construct has to provide, or may leave out where it is
        /// <c>optional</c>.
        /// </summary>
        /// <remarks>
        /// <c>required</c> or <c>optional</c> heads it, so every line that starts with either word is something the ship
        /// answers rather than something Sprocket does. What a slot for a block expects is written only when the author said
        /// it; nothing means any block may fill it.
        /// </remarks>
        static void WriteSlot(Pen pen, Part part)
        {
            if (part.Annotation.Length > 0)
                pen.Blank();

            pen.Note(part.Annotation);
            pen.Start(part.Optional ? Keyword.Optional : Keyword.Required).Says(Keyword.Slot).Word(part.Id);

            if (part.IsTrigger)
                pen.Says(Keyword.Expects).Says(Keyword.Trigger);
            else if (part.Expects.Count == 1)
                pen.Says(Keyword.Expects).Word(part.Expects[0]);
            else if (part.Expects.Count > 1)
                WriteNames(pen.Says(Keyword.Expects), part.Expects);

            pen.End();
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
            if (!Named(name))
            {
                trouble.Say(statement, "expected the slot's name.");
                return;
            }

            if (system.FindPart(name.Word) != null)
            {
                trouble.Say(statement, "slot '" + name.Word + "' is already declared.");
                return;
            }

            var part = system.PartFor(name.Word);
            part.Annotation = statement.Annotation;
            part.Optional = optional;

            if (!statement.Says(2, Keyword.Expects))
            {
                Ends(statement, 2, trouble);
                return;
            }

            var one = statement.ValueAt(3);
            if (one.Says(Keyword.Trigger))
                part.IsTrigger = true;
            else if (one.Form == ValueForm.List)
                Names(statement, 3, part.Expects, trouble, "expected a block type or a list of them after 'expects'.");
            else if (Named(one))
                part.Expects.Add(one.Word);
            else
                trouble.Say(statement, "expected trigger, a block type or a list of them after 'expects'.");

            Ends(statement, 4, trouble);
        }

        /// <summary>
        /// <c>instance 1 of "3f2a…" as 'Port airlock' { inner = [ door1 ] }</c>
        /// </summary>
        /// <remarks>
        /// One use of a system blueprint, on this construct. It sits outside the blueprint rather than inside it,
        /// because the blueprint is what is shared and which of somebody's blocks answer its slots is not.
        /// </remarks>
        static void WriteInstance(Pen pen, Automation program, AutomationSystem system, SystemInstance instance)
        {
            pen.Blank();
            pen.Note(instance.Annotation);
            pen.Start(Keyword.Instance).Whole(instance.Number)
                .Says(Keyword.Of).Text(system.Identity.ToString("D"));

            if (instance.HasName)
                pen.Says(Keyword.As).Word(instance.Name);

            var parts = Sorted(system, instance);
            if (parts.Count == 0)
            {
                pen.End();
                return;
            }

            pen.Body();
            for (var i = 0; i < parts.Count; i++)
            {
                pen.Note(instance.NoteOn(parts[i]));
                pen.Head(parts[i]).Says(Keyword.Equals);

                var positions = instance.Positions(parts[i]);
                if (positions == null)
                    WriteNames(pen, instance.FilledWith(parts[i]));
                else
                    Slots(pen, program, instance.FilledWith(parts[i]), positions);

                pen.End();
            }

            pen.Shut();
        }

        /// <summary>
        /// Which parts this one fills, in a settled order, leaving out a part nothing in the system uses.
        /// </summary>
        /// <remarks>
        /// Sorted, so a saved file does not churn between runs for no reason: a dictionary hands its
        /// keys back in whatever order it likes, and a save that differs every time is one that gets
        /// written every time and reads as a change in a diff.
        /// </remarks>
        static List<string> Sorted(AutomationSystem system, SystemInstance instance)
        {
            var used = new List<string>();
            system.Acted(used);

            var parts = new List<string>();
            foreach (var slot in instance.Filled)
            {
                Part part = system.FindPart(slot);
                if (part == null || used.Contains(slot))
                    parts.Add(slot);
            }

            parts.Sort(StringComparer.Ordinal);
            return parts;
        }

        /// <summary>
        /// One use of a system blueprint, or nothing where it does not say which blueprint it is one of.
        /// </summary>
        /// <remarks>
        /// The system may be named by its identity or by its name, because a person writing one by hand
        /// has the name in front of them and Sprocket always writes the identity.
        /// </remarks>
        static void ReadInstance(Statement statement, Automation program, Trouble trouble)
        {
            var number = statement.ValueAt(0);
            if (number.Form != ValueForm.Integer || number.Whole < 1 || number.Whole > int.MaxValue
                || !statement.Says(1, Keyword.Of))
            {
                trouble.Say(statement, "expected instance <number> of <system blueprint>.");
                return;
            }

            var system = SystemNamed(program, statement, 2);
            if (system == null)
            {
                trouble.Say(statement, "no system called " + Spelled(statement, 2) + ".");
                return;
            }

            if (!system.IsBlueprint)
            {
                trouble.Say(statement, "system " + Spelled(statement, 2) + " is not a system blueprint; only a blueprint has instances.");
                return;
            }

            if (system.FindInstance((int)number.Whole) != null)
            {
                trouble.Say(statement, "instance " + number.Whole + " of this system is already declared.");
                return;
            }

            var name = "";
            var at = 3;
            if (statement.Says(3, Keyword.As))
            {
                if (!Named(statement.ValueAt(4)))
                {
                    trouble.Say(statement, "expected a name after 'as'.");
                    return;
                }

                name = statement.Word(4);
                at = 5;
            }

            if (!Ends(statement, at, trouble))
                return;

            var instance = system.Restore((int)number.Whole, name);
            instance.Annotation = statement.Annotation;

            var body = statement.Body;
            for (var i = 0; i < body.Count; i++)
            {
                var inner = body[i];
                if (inner.IsOff)
                {
                    trouble.Say(inner, "unexpected '//'; only a step in a sequence can be switched off.");
                    continue;
                }

                if (inner.Key != Keyword.None || !inner.Says(0, Keyword.Equals) || inner.ValueAt(1).Form != ValueForm.List)
                {
                    trouble.Say(inner, "expected <slot> = [ <answers> ].");
                    continue;
                }

                if (!Ends(inner, 2, trouble))
                    continue;

                Part part = system.FindPart(inner.Head);
                if (part == null)
                {
                    trouble.Say(inner, "system blueprint '" + system.Name + "' has no slot '" + inner.Head + "'.");
                    continue;
                }

                if (instance.FilledWith(inner.Head) != null)
                {
                    trouble.Say(inner, "slot '" + inner.Head + "' is already answered.");
                    continue;
                }

                instance.Note(inner.Head, inner.Annotation);

                if (part.IsTrigger)
                {
                    Answered(inner, system, instance, program, trouble);
                    continue;
                }

                var filled = new List<string>();
                if (!Names(inner, 1, filled, trouble, "expected variables holding blocks in the list."))
                    continue;

                for (var n = 0; n < filled.Count; n++)
                {
                    if (program.FindRef(filled[n]) == null)
                        trouble.Say(inner, "no variable '" + filled[n] + "' holding a block above this line.");
                }

                instance.Fill(inner.Head, filled);
            }
        }

        /// <summary>
        /// <c>button = [ panel1:toolbar(1, 1, 4) ]</c>: the triggers that answer a trigger slot in one use of a blueprint, each
        /// written in place or named by a variable above.
        /// </summary>
        static void Answered(Statement statement, AutomationSystem system, SystemInstance instance, Automation program,
            Trouble trouble)
        {
            var items = statement.Items(1);
            var blocks = new List<string>();
            var at = new List<ToolbarSlot>();
            for (var i = 0; i < items.Count; i++)
            {
                if (Named(items[i]))
                {
                    var declared = program.FindTriggerRef(items[i].Word);
                    if (declared == null)
                    {
                        trouble.Say(statement, "no variable '" + items[i].Word + "' holding a trigger above this line.");
                        return;
                    }

                    blocks.Add(declared.Block);
                    at.Add(declared.At);
                    continue;
                }

                string block;
                ToolbarSlot slot;
                if (items[i].Form != ValueForm.Function)
                {
                    trouble.Say(statement, "expected triggers in the list, like [ panel1:toolbar(1, 1, 4) ].");
                    return;
                }

                if (!Trigger(statement, items[i], program, trouble, out block, out slot))
                    return;

                blocks.Add(block);
                at.Add(slot);
            }

            // Each answer to a slot an on names is a toolbar slot that starts it, and holds nothing else.
            for (var i = 0; Started(system, statement.Head) && i < blocks.Count; i++)
                trouble.Takes(statement, blocks[i], at[i]);

            instance.Fill(statement.Head, blocks, at);
        }

        /// <summary>A trigger slot's answers as a list, each by the variable holding it where there is one, or written in place on its block.</summary>
        static void Slots(Pen pen, Automation program, IList<string> blocks, IList<ToolbarSlot> at)
        {
            pen.OpenList();
            for (var i = 0; blocks != null && i < blocks.Count && i < at.Count; i++)
            {
                var declared = program.FindTriggerRef(blocks[i], at[i]);
                if (declared != null)
                    pen.Word(declared.Name);
                else
                    pen.Slot(blocks[i], at[i]);
            }

            pen.CloseList();
        }

        /// <summary>The system a statement names by identity or by name, or null.</summary>
        static AutomationSystem SystemNamed(Automation program, Statement statement, int at)
        {
            Guid identity;
            if (Guid.TryParseExact(statement.Text(at), "D", out identity))
                return program.FindSystem(identity);

            var name = statement.Parts(at);
            if (name.Count == 0 || name.Count > 2)
                return null;

            return program.FindSystem(name.Count == 2 ? name[0].Word : "", name[name.Count - 1].Word);
        }

        static void WriteSequence(Pen pen, Sequence sequence)
        {
            pen.Blank();
            pen.Note(sequence.Annotation);
            pen.Start(Keyword.Sequence).Whole(sequence.Number);
            if (sequence.HasName)
                pen.Says(Keyword.As).Word(sequence.Name);

            pen.Body();

            var steps = sequence.Steps;
            for (var i = 0; i < steps.Count; i++)
            {
                var opens = steps[i] as If;
                var branch = steps[i] as Else;
                if (opens != null)
                    WriteIf(pen, opens);
                else if (branch != null)
                    WriteElse(pen, branch);
                else if (steps[i] is EndIf)
                    pen.Shut();
                else
                    WriteStep(pen, steps[i]);
            }

            pen.Shut();
        }

        /// <summary><c>if all ext.pressurized on [ vent ] == on then {</c>: the line an if starts, and its block opened.</summary>
        static void WriteIf(Pen pen, If step)
        {
            if (step.Annotation.Length > 0)
                pen.Blank();

            pen.Note(step.Annotation);
            pen.Start(Keyword.If, step.IsOff);
            WriteTest(pen, step.Test);
            pen.Body();
        }

        /// <summary><c>} else if … then {</c> or <c>} else {</c>: the branch before closed, and the next one opened.</summary>
        static void WriteElse(Pen pen, Else step)
        {
            pen.Shut();
            pen.Note(step.Annotation);
            pen.Start(Keyword.Else);
            if (step.Test != null)
            {
                pen.Says(Keyword.If);
                WriteTest(pen, step.Test);
            }

            pen.Body();
        }

        /// <summary><c>all ext.pressurized on [ vent ] == on then</c>.</summary>
        static void WriteTest(Pen pen, Comparison test)
        {
            pen.Says(test.RequiresAll ? Keyword.All : Keyword.Any);
            if (test.Provider.Length == 0)
                pen.Word(test.MemberId);
            else
                pen.Qualified(test.Provider, test.MemberId);

            pen.Says(Keyword.On);
            WriteNames(pen, test.Targets);
            pen.Says(Keyword.EqualTo);
            Written(pen, test.Value);
            pen.Says(Keyword.Then);
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
                trouble.Say(statement, "sequence " + number.Whole + " is already declared.");
                return;
            }

            var sequence = program.Restore(system, (int)number.Whole, null);
            sequence.Annotation = statement.Annotation;

            var at = 1;
            if (statement.Says(1, Keyword.As))
            {
                sequence.Name = Named(statement.ValueAt(2)) ? statement.Word(2) : "";
                if (sequence.Name.Length == 0)
                    trouble.Say(statement, "expected a name after 'as'.");

                at = 3;
            }

            Ends(statement, at, trouble);
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
                    Ends(branch, 0, trouble);
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
        /// <c>all ext.pressurized on [ vent ] == on then</c>, from <paramref name="at" />: what an <c>if</c> or an <c>else if</c>
        /// tests, or null and trouble where it is not that.
        /// </summary>
        static Comparison Tested(Statement statement, int at, AutomationSystem system, Automation program, Trouble trouble)
        {
            var all = statement.Says(at, Keyword.All);
            if (!all && !statement.Says(at, Keyword.Any))
            {
                trouble.Say(statement, "expected any or all after 'if'.");
                return null;
            }

            var member = statement.Parts(at + 1);
            var bare = member.Count == 1 && member[0].Word.Length > 0;
            var qualified = member.Count == 2 && member[0].Word.Length > 0 && member[1].Word.Length > 0;
            if (!bare && !qualified)
            {
                trouble.Say(statement, "expected a value or a moment after '" + (all ? "all" : "any") + "'.");
                return null;
            }

            if (qualified && member[0].Word != BlockKinds.Own)
            {
                trouble.Say(statement, "no namespace '" + member[0].Word + "'; expected a value or a moment, bare or qualified by ext.");
                return null;
            }

            if (!statement.Says(at + 2, Keyword.On) || statement.ValueAt(at + 3).Form != ValueForm.List)
            {
                trouble.Say(statement, "expected 'on' and a list of names.");
                return null;
            }

            var test = new Comparison();
            test.RequiresAll = all;
            test.MemberId = member[member.Count - 1].Word;
            test.Provider = qualified ? member[0].Word : "";
            if (!Names(statement, at + 3, test.Targets, trouble, "expected names in the list."))
                return null;

            if (!statement.Says(at + 4, Keyword.EqualTo) || statement.Count <= at + 5)
            {
                trouble.Say(statement, "expected '==' and a value after the names.");
                return null;
            }

            var form = statement.ValueAt(at + 5).Form;
            if (form == ValueForm.List || form == ValueForm.Vector || form == ValueForm.Function || form == ValueForm.Qualified)
            {
                trouble.Say(statement, "unexpected " + Spelled(statement, at + 5) + "; expected a value.");
                return null;
            }

            test.Value = Given(statement.ValueAt(at + 5));
            if (test.IsMoment && test.Value.Kind != ValueKind.Flag)
            {
                trouble.Say(statement, "expected on or off after '==' for a moment.");
                return null;
            }

            if (!statement.Says(at + 6, Keyword.Then))
            {
                trouble.Say(statement, "expected 'then' after the value.");
                return null;
            }

            if (!Ends(statement, at + 7, trouble))
                return null;

            for (var i = 0; i < test.Targets.Count; i++)
                Uses(statement, test.Targets[i], system, program, false, trouble);

            return test;
        }

        /// <summary>
        /// <c>on button then run 1</c>, <c>when any ext.closed [ inner ] then run 2</c>, or <c>every 600t stop 3</c>.
        /// </summary>
        /// <remarks>
        /// A trigger says the step it does, written as a sequence writes it, because it belongs to the system rather than
        /// to one sequence.
        /// </remarks>
        static void WriteTrigger(Pen pen, Trigger trigger)
        {
            pen.Blank();
            pen.Note(trigger.Annotation);

            var needed = trigger as RequiredTrigger;
            if (needed != null)
            {
                pen.Start(Keyword.On).Word(needed.Part).Says(Keyword.Then);
                WriteDoes(pen, trigger.Does);
                pen.End();
                return;
            }

            var when = trigger as WhenTrigger;
            if (when != null)
            {
                pen.Start(Keyword.When).Says(when.RequiresAll ? Keyword.All : Keyword.Any).Qualified(when.Provider, when.EventId);
                WriteNames(pen, when.Sources);
                if (when.IntervalTicks > 0)
                    pen.Says(Keyword.Every).Ticks(when.IntervalTicks);

                pen.Says(Keyword.Then);
                WriteDoes(pen, trigger.Does);
                pen.End();
                return;
            }

            var every = trigger as EveryTrigger;
            if (every != null)
            {
                pen.Start(Keyword.Every).Ticks(every.IntervalTicks);
                WriteDoes(pen, trigger.Does);
                pen.End();
            }
        }

        /// <summary>The step a trigger does, on the line the trigger is written on.</summary>
        static void WriteDoes(Pen pen, Step step)
        {
            pen.Says(Head(step));
            Rest(pen, step);
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

            var needed = trigger as RequiredTrigger;
            if (needed != null)
            {
                Uses(statement, needed.Part, system, program, true, trouble);
                Takes(statement, needed, system, program, trouble);
            }

            trigger.Annotation = statement.Annotation;
            system.AddTrigger(trigger);
        }

        /// <summary>
        /// Trouble where the trigger <paramref name="needed" /> names already starts something: a slot of a system blueprint
        /// another <c>on</c> names, or the toolbar slot a system's trigger variable holds.
        /// </summary>
        /// <remarks>A toolbar slot holds one action, so a second <c>on</c> on it could never be wired.</remarks>
        static void Takes(Statement statement, RequiredTrigger needed, AutomationSystem system, Automation program, Trouble trouble)
        {
            if (system.IsBlueprint)
            {
                if (Started(system, needed.Part))
                    trouble.Say(statement, "slot '" + needed.Part + "' already starts something; a toolbar slot holds one action.");

                return;
            }

            var declared = program.FindTriggerRef(needed.Part);
            if (declared != null)
                trouble.Takes(statement, declared.Block, declared.At);
        }

        /// <summary>Whether an <c>on</c> of <paramref name="system" /> names <paramref name="slot" />.</summary>
        static bool Started(AutomationSystem system, string slot)
        {
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var needed = triggers[i] as RequiredTrigger;
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

            // on button then run 1: the trigger button, a variable of a system's or a trigger slot of a blueprint's.
            if (!Named(statement.ValueAt(0)) || !statement.Says(1, Keyword.Then))
            {
                trouble.Say(statement, "expected on <trigger> then <step>.");
                return null;
            }

            return RequiredTrigger.Named(statement.Word(0));
        }

        static Trigger When(Statement statement, Trouble trouble)
        {
            if (!statement.Says(0, Keyword.All) && !statement.Says(0, Keyword.Any))
            {
                trouble.Say(statement, "expected 'any' or 'all' after 'when'.");
                return null;
            }

            var then = statement.Find(Keyword.Then);
            if (then < 0)
            {
                trouble.Say(statement, "expected 'then'.");
                return null;
            }

            // Every moment this host has is its own, so a moment is always qualified by ext.
            var moment = statement.Parts(1);
            if (moment.Count != 2 || moment[0].Word.Length == 0 || moment[1].Word.Length == 0)
            {
                trouble.Say(statement, "expected a moment qualified by its namespace, like ext.closed.");
                return null;
            }

            if (moment[0].Word != BlockKinds.Own)
            {
                trouble.Say(statement, "no namespace '" + moment[0].Word + "' with moments; expected ext.");
                return null;
            }

            var when = new WhenTrigger(moment[1].Word);
            when.Provider = moment[0].Word;
            when.RequiresAll = statement.Says(0, Keyword.All);
            if (statement.ValueAt(2).Form != ValueForm.List)
            {
                trouble.Say(statement, "expected a list of names.");
                return null;
            }

            if (!Names(statement, 2, when.Sources, trouble, "expected a list of names."))
                return null;

            var at = 3;
            if (statement.Says(at, Keyword.Every))
            {
                int ticks;
                if (!Ticks(statement.ValueAt(at + 1), 1, out ticks))
                {
                    trouble.Say(statement, "expected a duration after 'every', like 30t.");
                    return null;
                }

                when.IntervalTicks = ticks;
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
            int ticks;
            if (!Ticks(statement.ValueAt(0), 1, out ticks))
            {
                trouble.Say(statement, "expected a duration after 'every', like 600t.");
                return null;
            }

            return new EveryTrigger(ticks);
        }

        /// <summary>
        /// A duration as a count of ticks, or false where it was no length of time that can be kept.
        /// </summary>
        /// <remarks>
        /// The text reads into a long and ticks are counted in an int, so the bound is the language's
        /// as much as the reader's: <c>every 4294967296t</c> is not an interval, it is a number that
        /// wraps onto one, and <c>every -600t</c> is not a rate at all.
        /// </remarks>
        static bool Ticks(Value value, long least, out int ticks)
        {
            ticks = 0;
            if (value.Form != ValueForm.Duration || value.Whole < least || value.Whole > int.MaxValue)
                return false;

            ticks = (int)value.Whole;
            return true;
        }

        /// <summary>A step: an action on some parts, a value set on them, a sequence run or stopped, an await, or a length of time.</summary>
        static void WriteStep(Pen pen, Step step)
        {
            if (step.Annotation.Length > 0)
                pen.Blank();

            pen.Note(step.Annotation);
            pen.Start(Head(step), step.IsOff);
            Rest(pen, step);
            pen.End();
        }

        /// <summary>The word a step starts with.</summary>
        static Keyword Head(Step step)
        {
            // A value is a set; an action is a call, bare or qualified by its namespace.
            var act = step as Act;
            if (act != null)
                return act.Form == Form.Value ? Keyword.Set : Keyword.Call;

            if (step is Wait)
                return Keyword.Wait;

            var run = step as Run;
            if (run != null)
                return run.Stops ? Keyword.Stop : Keyword.Run;

            return Keyword.Await;
        }

        /// <summary>Everything in a step after the word it starts with.</summary>
        static void Rest(Pen pen, Step step)
        {
            var act = step as Act;
            if (act != null)
            {
                if (act.Form == Form.Value)
                {
                    pen.Word(act.ActionId).Says(Keyword.On);
                    WriteNames(pen, act.Targets);

                    if (act.Values.Count > 0)
                    {
                        pen.Says(Keyword.To);
                        Written(pen, act.Values[0]);
                    }
                }
                else
                {
                    if (act.Form == Form.Action)
                        pen.Word(act.ActionId);
                    else
                        pen.Qualified(act.Provider, act.ActionId);

                    pen.Says(Keyword.On);
                    WriteNames(pen, act.Targets);

                    if (act.Values.Count > 0)
                    {
                        pen.Says(Keyword.With);

                        for (var i = 0; i < act.Values.Count; i++)
                        {
                            if (i > 0)
                                pen.Comma();

                            Written(pen, act.Values[i]);
                        }
                    }
                }

                return;
            }

            var wait = step as Wait;
            if (wait != null)
            {
                pen.Ticks(wait.Ticks);
                return;
            }

            var run = step as Run;
            if (run != null)
                pen.Whole(run.Number);
        }

        /// <summary>A step, or nothing where the statement was not one, which is trouble.</summary>
        static Step ReadStep(Statement statement, Trouble trouble)
        {
            if (statement.Key == Keyword.Await)
                return Ends(statement, 0, trouble) ? Noted(new Await(), statement) : null;

            if (statement.Key == Keyword.Run || statement.Key == Keyword.Stop)
                return Ends(statement, 1, trouble) ? Called(statement, trouble) : null;

            if (statement.Key == Keyword.Wait)
            {
                int ticks;
                if (Ticks(statement.ValueAt(0), 0, out ticks))
                    return Ends(statement, 1, trouble) ? Noted(new Wait(ticks), statement) : null;

                trouble.Say(statement, "expected a duration after 'wait', like 60t.");
                return null;
            }

            if (statement.Key == Keyword.Call)
                return Acted(statement, trouble);

            if (statement.Key == Keyword.Set)
                return ReadSet(statement, trouble);

            trouble.Say(statement, "unexpected " + Headed(statement) + "; expected call, set, run, stop, await or wait.");
            return null;
        }

        /// <summary>
        /// <c>call ext.close on [ inner ]</c> or <c>call SetColor on [ light ] with 255 0 0</c>: an action, bare or qualified by
        /// its namespace, on some blocks, given what it asks for.
        /// </summary>
        static Step Acted(Statement statement, Trouble trouble)
        {
            var member = statement.Parts(0);
            Act act;
            if (member.Count == 1 && member[0].Word.Length > 0)
            {
                act = new Act(member[0].Word);
                act.Form = Form.Action;
            }
            else if (member.Count == 2 && member[0].Word.Length > 0 && member[1].Word.Length > 0)
            {
                if (member[0].Word != BlockKinds.Own)
                {
                    trouble.Say(statement, "no namespace '" + member[0].Word + "'; expected an action, bare or qualified by ext.");
                    return null;
                }

                act = new Act(member[1].Word);

                act.Form = Form.Native;
                act.Provider = member[0].Word;
            }
            else
            {
                trouble.Say(statement, "expected an action after 'call', like Reverse or ext.close.");
                return null;
            }

            if (!statement.Says(1, Keyword.On) || statement.ValueAt(2).Form != ValueForm.List)
            {
                trouble.Say(statement, "expected 'on' and a list of names.");
                return null;
            }

            if (!Names(statement, 2, act.Targets, trouble, "expected names in the list."))
                return null;

            if (statement.Count > 3 && (!statement.Says(3, Keyword.With) || statement.Count < 5))
            {
                trouble.Say(statement, "expected 'with' and values after the names.");
                return null;
            }

            for (var at = 4; at < statement.Count; at++)
            {
                if (!ReadValue(statement, at, act, trouble))
                    return null;
            }

            return Noted(act, statement);
        }

        /// <summary><c>set Radius on [ light ] to 4.5</c>: one of the block's own values.</summary>
        /// <remarks>
        /// The value is taken by position rather than by what it looks like, so a flag written <c>on</c> is a value and
        /// not mistaken for one of the language's own words.
        /// </remarks>
        static Step ReadSet(Statement statement, Trouble trouble)
        {
            if (!Named(statement.ValueAt(0)) || !statement.Says(1, Keyword.On) || statement.ValueAt(2).Form != ValueForm.List
                || !statement.Says(3, Keyword.To) || statement.Count < 5)
            {
                trouble.Say(statement, "expected set <value> on [ <names> ] to <value>.");
                return null;
            }

            if (!Ends(statement, 5, trouble))
                return null;

            var act = new Act(statement.Word(0));
            act.Form = Form.Value;
            if (!Names(statement, 2, act.Targets, trouble, "expected names in the list."))
                return null;

            return ReadValue(statement, 4, act, trouble) ? Noted(act, statement) : null;
        }

        /// <summary>Adds the value at <paramref name="index" /> of <paramref name="statement" /> to what <paramref name="act" /> is given, and says whether it is one a step can be given.</summary>
        static bool ReadValue(Statement statement, int index, Act act, Trouble trouble)
        {
            var form = statement.ValueAt(index).Form;
            if (form == ValueForm.List || form == ValueForm.Vector || form == ValueForm.Function || form == ValueForm.Qualified)
            {
                trouble.Say(statement, "unexpected " + Spelled(statement, index) + "; expected a value.");
                return false;
            }

            act.Values.Add(Given(statement.ValueAt(index)));
            return true;
        }

        /// <summary>One value of an action, as whatever kind it was written as.</summary>
        /// <remarks>
        /// A bare word that is not a flag is kept as text. Nothing else could be done with it, and
        /// losing it would turn a step somebody wrote into one that quietly does something else.
        /// </remarks>
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

            return Literal.Of(value.Word);
        }

        /// <summary>The same, written back as the kind it is.</summary>
        static void Written(Pen pen, Literal value)
        {
            if (value.Kind == ValueKind.Text)
            {
                pen.Text(value.Text);
                return;
            }

            if (value.Kind == ValueKind.Flag)
            {
                pen.Says(value.Flag ? Keyword.On : Keyword.Off);
                return;
            }

            if (value.Kind == ValueKind.Integer)
            {
                pen.Whole(value.Whole);
                return;
            }

            if (value.Kind == ValueKind.Real)
            {
                pen.Real(value.Real);
                return;
            }

            pen.Colour(value.Red, value.Green, value.Blue, value.Alpha);
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

            if (Named(named))
            {
                var run = new Run(0);
                run.Named = named.Word;
                run.Stops = stops;
                trouble.Wrote(statement, run);
                return Noted(run, statement);
            }

            trouble.Say(statement, "expected a sequence's number or name after '" + Keywords.Name(statement.Key) + "'.");
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
                    trouble.Say(statement, "no sequence called '" + run.Named + "'.");
                else
                    run.Number = named.Number;

                run.Named = "";
                return;
            }

            if (system.FindSequence(run.Number) == null)
                trouble.Say(statement, "no sequence " + run.Number + " to " + (run.Stops ? "stop" : "run") + ".");
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

        static void WriteNames(Pen pen, IList<string> names)
        {
            pen.OpenList();
            for (var i = 0; i < names.Count; i++)
                pen.Word(names[i]);

            pen.CloseList();
        }

        /// <summary>Whether <paramref name="statement" /> says nothing after its first <paramref name="count" /> values, and trouble naming the first where it does.</summary>
        static bool Ends(Statement statement, int count, Trouble trouble)
        {
            if (statement.Count <= count)
                return true;

            trouble.Say(statement, "unexpected " + Spelled(statement, count) + ".");
            return false;
        }

        /// <summary>The word a statement starts with, as it is written, for trouble to name it.</summary>
        static string Headed(Statement statement)
        {
            return "'" + (statement.Key == Keyword.None ? statement.Head : Keywords.Name(statement.Key)) + "'";
        }

        /// <summary>The value at <paramref name="index" /> of <paramref name="statement" /> as it is written, or what it is, for trouble to name it.</summary>
        static string Spelled(Statement statement, int index)
        {
            var value = statement.ValueAt(index);
            if (value.Form == ValueForm.Qualified)
            {
                var parts = statement.Parts(index);
                var joined = "";
                for (var i = 0; i < parts.Count; i++)
                    joined += (i > 0 ? "." : "") + parts[i].Word;

                return "'" + joined + "'";
            }

            if (value.Form == ValueForm.Real)
                return value.Real.ToString(CultureInfo.InvariantCulture);

            if (value.Form == ValueForm.Color)
                return "a color";

            if (value.Form == ValueForm.List)
                return "'['";

            if (value.Form == ValueForm.Vector)
                return "'('";

            if (value.Form == ValueForm.Function)
                return "a host function";

            if (value.Form == ValueForm.Keyword)
                return "'" + Keywords.Name(value.Keyword) + "'";

            if (value.IsWord)
                return "'" + value.Word + "'";

            if (value.Form == ValueForm.Text)
                return "\"" + value.Word + "\"";

            if (value.Form == ValueForm.Integer)
                return value.Whole.ToString(CultureInfo.InvariantCulture);

            if (value.Form == ValueForm.Duration)
                return value.Whole.ToString(CultureInfo.InvariantCulture) + "t";

            return "what follows";
        }

        /// <summary>Whether <paramref name="value" /> is a name, bare or quoted, rather than one of the language's words or anything else.</summary>
        static bool Named(Value value)
        {
            return value.Form == ValueForm.Identifier && value.Word.Length > 0;
        }

        /// <summary>The names in the list at <paramref name="at" />, into <paramref name="into" />; or false, and trouble saying <paramref name="what" />, where one is not a name.</summary>
        static bool Names(Statement statement, int at, IList<string> into, Trouble trouble, string what)
        {
            into.Clear();
            var items = statement.Items(at);
            for (var i = 0; i < items.Count; i++)
            {
                if (!Named(items[i]))
                {
                    trouble.Say(statement, what);
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

            if (trigger ? program.FindTriggerRef(name) == null : program.FindRef(name) == null)
                trouble.Say(statement, "no variable '" + name + "' holding " + (trigger ? "a trigger" : "a block") + " above this line.");
        }
    }
}
