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
    /// Core never touches a file - the banned list says so, because Space Engineers does not whitelist
    /// IO and a mod stores its data in the save. So this turns a program into a string and back, and
    /// whoever is hosting decides where the string lives: a file beside the prototype, a section of the
    /// world save, a datapad.
    /// <para>
    /// <b>The script is the program.</b> There is nothing else to project it from - the model lives in
    /// memory in a mod the game may unload at any moment - so this is not a view of the truth, it is
    /// the truth written down. Three things follow and all three are load-bearing: it stays readable so
    /// a save that breaks can be looked at, a construct nobody recognises is dropped whole rather than
    /// taking its neighbours with it, and a program that did not read cleanly is marked
    /// <see cref="Automation.Intact" /> false so that the next save cannot write the damage over what
    /// is still on disk.
    /// </para>
    /// <para>
    /// It is a language and not prose, and that is the line to hold: keywords here are syntax, the way
    /// <c>if</c> and <c>while</c> are, and nobody localises those. The moment it drifts towards English
    /// it puts word order and vocabulary back into core and freezes them in a file format, where they
    /// become a compatibility promise. Every word a person reads is the host's; every word in here is
    /// the format's.
    /// </para>
    /// </remarks>
    public static class ProgramText
    {
        const int Version = 1;

        const int Draft = 8;

        /// <summary>What Sprocket's own actions and moments are qualified by: <c>sprocket.close</c>.</summary>
        /// <remarks>Persisted in every program that uses one, so it cannot change without changing what those programs say.</remarks>
        const string Own = "sprocket";

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
            pen.Start(Keyword.Sprocket).Whole(Version).Says(Keyword.Alpha).Whole(Draft);
            pen.End();

            // Before anything that names one, so a reader has them in hand by the time it meets a step.
            var refs = program.Refs;
            if (refs.Count > 0)
                pen.Blank();

            for (var i = 0; i < refs.Count; i++)
                WriteRef(pen, values, refs[i]);

            var systems = program.Systems;
            for (var i = 0; i < systems.Count; i++)
                WriteSystem(pen, systems[i]);

            // After every system, because an instance names the system it is one of and a reader
            // should have met it by then.
            for (var i = 0; i < systems.Count; i++)
            {
                var instances = systems[i].Instances;
                for (var n = 0; n < instances.Count; n++)
                    WriteInstance(pen, systems[i], instances[n]);
            }

            return pen.ToString();
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
            // would be read as something it does not mean.
            string refused = null;
            if (constructs.Count == 0 || constructs[0].Key != Keyword.Sprocket)
                refused = "Line 1: expected sprocket <format>.";
            else if (constructs[0].Whole(0) != Version)
                refused = "Line 1: written for sprocket " + constructs[0].Whole(0) + "; this reads sprocket " + Version + ".";
            else if (DraftOf(constructs[0]) != Draft)
                refused = "Line 1: written for " + (DraftOf(constructs[0]) < 0 ? "the finished format" : "alpha " + DraftOf(constructs[0]))
                    + "; this reads alpha " + Draft + ".";

            if (refused != null)
            {
                program.Unreadable(saved);
                program.Refused(refused);
                warning = "";
                return refused;
            }

            Ends(constructs[0], constructs[0].Says(1, Keyword.Alpha) ? 3 : 1, trouble);

            for (var i = 0; i < constructs.Count; i++)
            {
                var construct = constructs[i];
                if (construct.IsOff)
                    trouble.Say(construct, "'//' only switches off a step in a sequence.");
                else if (construct.Key == Keyword.Block)
                    ReadRef(construct, program, values, trouble);
                else if (construct.Key == Keyword.System)
                    ReadSystem(construct, program, trouble);
                else if (construct.Key == Keyword.Instance)
                    ReadInstance(construct, program, trouble);
                else if (i > 0)
                    trouble.Say(construct, "unexpected " + Headed(construct) + "; expected block, system or instance.");
            }

            trouble.Settle(program);

            if (trouble.What.Length > 0)
            {
                program.Unreadable(saved);
                program.Damaged(trouble.What);
            }

            warning = trouble.Warning;
            return trouble.What;
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

        static void WriteRef(Pen pen, Values values, EntityRef declared)
        {
            pen.Start(Keyword.Block).Word(BlockKinds.Sort(declared.KindId)).Word(declared.Name);
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
        /// whole number because that is what it looks like. Which is the point - core does not know what
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

        static void ReadRef(Statement statement, Automation program, Values values, Trouble trouble)
        {
            var name = statement.ValueAt(1);
            if (statement.Word(0).Length == 0 || !Named(name) || statement.ValueAt(2).Form != ValueForm.List)
            {
                trouble.Say(statement, "expected block '<type>/<subtype>' <name> [ <keys> ].");
                return;
            }

            if (!Ends(statement, 3, trouble))
                return;

            if (program.FindRef(name.Word) != null)
            {
                trouble.Say(statement, "block '" + name.Word + "' is already declared.");
                return;
            }

            var declared = new EntityRef(statement.Word(1), BlockKinds.Definition(statement.Word(0)));
            var keys = statement.Items(2);
            for (var i = 0; i < keys.Count; i++)
                ReadKey(values, keys[i], declared.Keys, statement, trouble);

            program.Restore(declared);
        }

        /// <summary>
        /// A key, or trouble where this build has no kind that could hold it.
        /// </summary>
        /// <remarks>
        /// Said rather than dropped, because a key is how a block is found again: dropping one quietly
        /// leaves the next save writing a block that matches less than the one it replaces.
        /// </remarks>
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

        static void WriteSystem(Pen pen, AutomationSystem system)
        {
            pen.Blank();
            pen.Note(system.Annotation);
            pen.Start(Keyword.System).Text(system.Identity.ToString("D"))
                .Says(Keyword.As).Qualified(system.Namespace, system.Name);

            if (system.Changed.HasValue)
            {
                pen.Says(Keyword.Changed)
                    .Text(system.Changed.Value.ToString(Times[0], CultureInfo.InvariantCulture));
            }

            if (system.LastSequenceNumber > system.HighestSequenceNumber())
                pen.Says(Keyword.Last).Says(Keyword.Sequence).Whole(system.LastSequenceNumber);

            pen.Body();

            // The parts before the ones that fill them in, so a reader meets the vocabulary before
            // the answers.
            var used = new List<string>();
            system.Acted(used);

            var parts = system.Parts;
            for (var i = 0; i < parts.Count; i++)
            {
                if (!parts[i].IsTrigger && parts[i].Kept(used.Contains(parts[i].Id)))
                    WritePart(pen, parts[i]);
            }

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
                WriteTrigger(pen, system, triggers[i]);

            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
                WriteSequence(pen, sequences[i]);

            pen.Shut();
        }

        /// <summary>
        /// <c>system "3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43" as MalTek.Airlock changed "…" last sequence 4 { … }</c>
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
            Guid identity;
            if (!Guid.TryParseExact(statement.Text(0), "D", out identity) || identity == Guid.Empty)
            {
                trouble.Say(statement, "expected the system's identity, a quoted GUID.");
                return;
            }

            var name = statement.Parts(2);
            if (!statement.Says(1, Keyword.As) || name.Count == 0 || name.Count > 2
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

            // The two metadata clauses, in either order and each at most once.
            var at = 3;
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
                    trouble.Say(inner, "'//' only switches off a step in a sequence.");
                    continue;
                }

                if ((inner.Key == Keyword.Required || inner.Key == Keyword.Optional) && inner.Says(0, Keyword.Trigger))
                    ReadTrigger(inner, system, trouble);
                else if (inner.Key == Keyword.Required || inner.Key == Keyword.Optional)
                    ReadPart(inner, system, trouble);
                else if (inner.Key == Keyword.When || inner.Key == Keyword.Every)
                    ReadTrigger(inner, system, trouble);
                else if (inner.Key == Keyword.Sequence)
                    ReadSequence(inner, system, program, trouble);
                else
                    trouble.Say(inner, "unexpected " + Headed(inner) + "; expected required, optional, when, every or sequence.");
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
        /// <c>required block inner expects Door</c>: something the construct has to provide, or may
        /// leave out where it is <c>optional</c>.
        /// </summary>
        /// <remarks>
        /// <c>required</c> or <c>optional</c> heads it, so every line that starts with either word is
        /// something the ship answers rather than something Sprocket does. What a part expects is written
        /// only when the author said it; nothing means anything may fill it.
        /// </remarks>
        static void WritePart(Pen pen, Part part)
        {
            if (part.Annotation.Length > 0)
                pen.Blank();

            pen.Note(part.Annotation);
            pen.Start(part.Optional ? Keyword.Optional : Keyword.Required).Says(Keyword.Block).Word(part.Id);

            if (part.Expects.Count == 1)
                pen.Says(Keyword.Expects).Word(part.Expects[0]);
            else if (part.Expects.Count > 1)
                Names(pen.Says(Keyword.Expects), part.Expects);

            pen.End();
        }

        static void ReadPart(Statement statement, AutomationSystem system, Trouble trouble)
        {
            var optional = statement.Key == Keyword.Optional;
            if (!statement.Says(0, Keyword.Block))
            {
                trouble.Say(statement, optional
                    ? "expected 'block' or 'trigger' after 'optional'."
                    : "expected 'block' or 'trigger' after 'required'.");
                return;
            }

            var name = statement.ValueAt(1);
            if (!Named(name))
            {
                trouble.Say(statement, "expected the part's name.");
                return;
            }

            if (system.FindPart(name.Word) != null)
            {
                trouble.Say(statement, "part '" + name.Word + "' is already declared.");
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
            if (one.Form == ValueForm.List)
                Names(statement, 3, part.Expects, trouble, "expected a block type or a list of them after 'expects'.");
            else if (one.IsWord && one.Word.Length > 0)
                part.Expects.Add(one.Word);
            else
                trouble.Say(statement, "expected a block type or a list of them after 'expects'.");

            Ends(statement, 4, trouble);
        }

        /// <summary>
        /// <c>instance 1 of "3f2a…" as 'Port airlock' { inner = [ door1 ] }</c>
        /// </summary>
        /// <remarks>
        /// One of a system, on this construct. It sits outside the system rather than inside it, because
        /// the system is what is shared and which of somebody's blocks answer its parts is not.
        /// </remarks>
        static void WriteInstance(Pen pen, AutomationSystem system, SystemInstance instance)
        {
            pen.Blank();
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
                pen.Head(parts[i]).Says(Keyword.Equals);

                var positions = instance.Positions(parts[i]);
                if (positions == null)
                    Names(pen, instance.FilledWith(parts[i]));
                else
                    Slots(pen, instance.FilledWith(parts[i]), positions);

                pen.End();
            }

            pen.Shut();
        }

        /// <summary>
        /// Which parts this one fills, in a settled order, leaving out a part the system does not keep.
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
                if (part == null || part.Kept(used.Contains(slot)))
                    parts.Add(slot);
            }

            parts.Sort(StringComparer.Ordinal);
            return parts;
        }

        /// <summary>
        /// One of a system, or nothing where it does not say which system it is one of.
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
                trouble.Say(statement, "expected instance <number> of <system>.");
                return;
            }

            var system = Named(program, statement, 2);
            if (system == null)
            {
                trouble.Say(statement, "no system called " + Spelled(statement.ValueAt(2)) + ".");
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

            var body = statement.Body;
            for (var i = 0; i < body.Count; i++)
            {
                var inner = body[i];
                if (inner.IsOff)
                {
                    trouble.Say(inner, "'//' only switches off a step in a sequence.");
                    continue;
                }

                if (inner.Key != Keyword.None || !inner.Says(0, Keyword.Equals) || inner.ValueAt(1).Form != ValueForm.List)
                {
                    trouble.Say(inner, "expected <part> = [ <answers> ].");
                    continue;
                }

                if (!Ends(inner, 2, trouble))
                    continue;

                Part part = system.FindPart(inner.Head);
                if (part == null)
                {
                    trouble.Say(inner, "system '" + system.Name + "' has no part '" + inner.Head + "'.");
                    continue;
                }

                if (part.IsTrigger)
                {
                    Answered(inner, instance, trouble);
                    continue;
                }

                var filled = new List<string>();
                if (!Names(inner, 1, filled, trouble, "expected block names in the list."))
                    continue;

                for (var n = 0; n < filled.Count; n++)
                    trouble.Owes(inner, filled[n], null);

                instance.Fill(inner.Head, filled);
            }
        }

        /// <summary><c>call = [ panel1:toolbar(1, 1, 4) ]</c>: the toolbar slots that answer a trigger in one of a system.</summary>
        static void Answered(Statement statement, SystemInstance instance, Trouble trouble)
        {
            var items = statement.Items(1);
            var blocks = new List<string>();
            var at = new List<ToolbarSlot>();
            for (var i = 0; i < items.Count; i++)
            {
                string block;
                int toolbar, page, slot;
                if (!statement.Slot(items[i], out block, out toolbar, out page, out slot) || block.Length == 0
                    || toolbar == 0 || page == 0 || slot == 0)
                {
                    trouble.Say(statement, "expected toolbar slots in the list, like [ panel1:toolbar(1, 1, 4) ].");
                    return;
                }

                blocks.Add(block);
                at.Add(new ToolbarSlot(toolbar, page, slot));
                trouble.Owes(statement, block, null);
            }

            instance.Fill(statement.Head, blocks, at);
        }

        /// <summary>A trigger part's toolbar slots, each on its block, as a list.</summary>
        static void Slots(Pen pen, IList<string> blocks, IList<ToolbarSlot> at)
        {
            pen.OpenList();
            for (var i = 0; blocks != null && i < blocks.Count && i < at.Count; i++)
                pen.Slot(blocks[i], at[i]);

            pen.CloseList();
        }

        /// <summary>The system a statement names by identity or by name, or null.</summary>
        static AutomationSystem Named(Automation program, Statement statement, int at)
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
                WriteStep(pen, steps[i]);

            pen.Shut();
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

            var body = statement.Body;
            for (var i = 0; i < body.Count; i++)
            {
                var step = ReadStep(body[i], trouble);
                if (step == null)
                    continue;

                step.IsOff = body[i].IsOff;
                Owes(body[i], step as Act, system, trouble);
                sequence.Steps.Add(step);
            }
        }

        /// <summary>
        /// <c>required trigger call to run 1</c>, <c>when any sprocket.closed [ inner ] then run 2</c>, or
        /// <c>every 600t stop 3</c>.
        /// </summary>
        /// <remarks>
        /// A trigger says the step it does, written as a sequence writes it, because it belongs to the system rather than
        /// to one sequence.
        /// </remarks>
        static void WriteTrigger(Pen pen, AutomationSystem system, Trigger trigger)
        {
            // The ones a construct answers are parts, in a list of their own broken up only where one carries words; the
            // rest each start a paragraph.
            var needed = trigger as RequiredTrigger;
            if (needed != null)
            {
                var part = system.FindPart(needed.Part);
                var note = part == null ? "" : part.Annotation;
                if (note.Length > 0)
                    pen.Blank();

                pen.Note(note);
                pen.Start(part != null && part.Optional ? Keyword.Optional : Keyword.Required).Says(Keyword.Trigger)
                    .Word(needed.Part).Says(Keyword.To);
                Does(pen, trigger.Does);
                pen.End();
                return;
            }

            pen.Blank();
            pen.Note(trigger.Annotation);

            var when = trigger as WhenTrigger;
            if (when != null)
            {
                pen.Start(Keyword.When).Says(when.RequiresAll ? Keyword.All : Keyword.Any).Qualified(Own, when.EventId);
                Names(pen, when.Sources);
                if (when.IntervalTicks > 0)
                    pen.Says(Keyword.Every).Ticks(when.IntervalTicks);

                pen.Says(Keyword.Then);
                Does(pen, trigger.Does);
                pen.End();
                return;
            }

            var every = trigger as EveryTrigger;
            if (every != null)
            {
                pen.Start(Keyword.Every).Ticks(every.IntervalTicks);
                Does(pen, trigger.Does);
                pen.End();
            }
        }

        /// <summary>The step a trigger does, on the line the trigger is written on.</summary>
        static void Does(Pen pen, Step step)
        {
            pen.Says(Head(step));
            Rest(pen, step);
        }

        static void ReadTrigger(Statement statement, AutomationSystem system, Trouble trouble)
        {
            var trigger = Kind(statement, system, trouble);
            if (trigger == null)
                return;

            trigger.Does = Does(statement, Doing(statement), trouble);
            if (trigger.Does == null)
                return;

            Owes(statement, trigger.Acts, system, trouble);

            var when = trigger as WhenTrigger;
            for (var i = 0; when != null && i < when.Sources.Count; i++)
                trouble.Owes(statement, when.Sources[i], system);

            // A trigger part's words are the part's, where the configuration panel shows them.
            var needed = trigger as RequiredTrigger;
            if (needed == null)
            {
                trigger.Annotation = statement.Annotation;
            }
            else
            {
                var part = system.PartFor(needed.Part);
                part.IsTrigger = true;
                part.Optional = statement.Key == Keyword.Optional;
                part.Annotation = statement.Annotation;
            }

            system.AddTrigger(trigger);
        }

        /// <summary>Where the step a trigger does starts: after <c>to</c>, after <c>then</c>, or after how often.</summary>
        static int Doing(Statement statement)
        {
            if (statement.Key == Keyword.When)
                return statement.Find(Keyword.Then) + 1;

            return statement.Key == Keyword.Every ? 1 : 3;
        }

        /// <summary>The step a trigger does, which is one a sequence could hold and that starts something: run, stop, act or set.</summary>
        static Step Does(Statement statement, int at, Trouble trouble)
        {
            var step = statement.Rest(at);
            if (step.Key != Keyword.Run && step.Key != Keyword.Stop && step.Key != Keyword.Act && step.Key != Keyword.Set)
            {
                trouble.Say(statement, "expected run, stop, act or set.");
                return null;
            }

            var does = ReadStep(step, trouble);
            if (does != null)
                does.Annotation = "";

            return does;
        }

        static Trigger Kind(Statement statement, AutomationSystem system, Trouble trouble)
        {
            if (statement.Key == Keyword.When)
                return When(statement, trouble);

            if (statement.Key == Keyword.Every)
                return Every(statement, trouble);

            // required trigger call to run 1: the trigger part call, which each instance answers with toolbar slots.
            var name = Named(statement.ValueAt(1)) ? statement.Word(1) : "";
            if (name.Length == 0 || !statement.Says(2, Keyword.To))
            {
                trouble.Say(statement, "expected trigger <name> to <step>.");
                return null;
            }

            if (system.FindPart(name) != null)
            {
                trouble.Say(statement, "part '" + name + "' is already declared.");
                return null;
            }

            return RequiredTrigger.Named(name);
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

            var moment = statement.Parts(1);
            if (moment.Count != 2 || moment[0].Word != Own || moment[1].Word.Length == 0)
            {
                trouble.Say(statement, "expected a moment qualified by what adds it, like sprocket.closed.");
                return null;
            }

            var when = new WhenTrigger(moment[1].Word);
            when.RequiresAll = statement.Says(0, Keyword.All);
            if (statement.ValueAt(2).Form != ValueForm.List)
            {
                trouble.Say(statement, "expected a list of part names.");
                return null;
            }

            if (!Names(statement, 2, when.Sources, trouble, "expected a list of part names."))
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
            // A value is a set; an action is an act, by the game's own id or qualified by what adds it.
            var act = step as Act;
            if (act != null)
                return act.Form == Form.Value ? Keyword.Set : Keyword.Act;

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
                    Names(pen, act.Targets);

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
                        pen.Qualified(Own, act.ActionId);

                    pen.Says(Keyword.On);
                    Names(pen, act.Targets);

                    if (act.Values.Count > 0)
                    {
                        pen.Says(Keyword.With);

                        for (var i = 0; i < act.Values.Count; i++)
                            Written(pen, act.Values[i]);
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

                // Said rather than dropped, because a wait without its unit is somebody writing a
                // length of time and Sprocket is the only thing that can tell them it read as nothing.
                trouble.Say(statement, "expected a duration after 'wait', like 60t.");
                return null;
            }

            if (statement.Key == Keyword.Act)
                return Acted(statement, trouble);

            if (statement.Key == Keyword.Set)
                return Given(statement, trouble);

            trouble.Say(statement, "unexpected " + Headed(statement) + "; expected act, set, run, stop, await or wait.");
            return null;
        }

        /// <summary>
        /// <c>act sprocket.close on [ inner ]</c> or <c>act SetColor on [ light ] with 255 0 0</c>: an action, by the game's own
        /// id or qualified by what adds it, on some parts, given what it asks for.
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
            else if (member.Count == 2 && member[0].Word == Own && member[1].Word.Length > 0)
            {
                act = new Act(member[1].Word);
                act.Form = Form.Native;
            }
            else
            {
                trouble.Say(statement, "expected an action after 'act', like Reverse or sprocket.close.");
                return null;
            }

            if (!statement.Says(1, Keyword.On) || statement.ValueAt(2).Form != ValueForm.List)
            {
                trouble.Say(statement, "expected 'on' and a list of parts.");
                return null;
            }

            if (!Names(statement, 2, act.Targets, trouble, "expected part names in the list."))
                return null;

            if (statement.Count > 3 && (!statement.Says(3, Keyword.With) || statement.Count < 5))
            {
                trouble.Say(statement, "expected 'with' and values after the parts.");
                return null;
            }

            for (var at = 4; at < statement.Count; at++)
                act.Values.Add(Given(statement.ValueAt(at)));

            return Noted(act, statement);
        }

        /// <summary><c>set Radius on [ light ] to 4.5</c>: one of the block's own values.</summary>
        /// <remarks>
        /// The value is taken by position rather than by what it looks like, so a flag written <c>on</c> is a value and
        /// not mistaken for one of the language's own words.
        /// </remarks>
        static Step Given(Statement statement, Trouble trouble)
        {
            if (statement.Word(0).Length == 0 || !statement.Says(1, Keyword.On) || statement.ValueAt(2).Form != ValueForm.List
                || !statement.Says(3, Keyword.To) || statement.Count < 5)
            {
                trouble.Say(statement, "expected set <value> on [ <parts> ] to <value>.");
                return null;
            }

            if (!Ends(statement, 5, trouble))
                return null;

            var act = new Act(statement.Word(0));
            act.Form = Form.Value;
            if (!Names(statement, 2, act.Targets, trouble, "expected part names in the list."))
                return null;

            act.Values.Add(Given(statement.ValueAt(4)));
            return Noted(act, statement);
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
                return Noted(numbered, statement);
            }

            if (named.IsWord && named.Word.Length > 0)
            {
                var run = new Run(0);
                run.Named = named.Word;
                run.Stops = stops;
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
        /// <para>
        /// Reported against the system's own line rather than the step's, because a step does not keep
        /// where it was written and the system is what the name was looked for in.
        /// </para>
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

        static void Names(Pen pen, IList<string> names)
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

            trouble.Say(statement, "unexpected " + Spelled(statement.ValueAt(count)) + ".");
            return false;
        }

        /// <summary>The word a statement starts with, as it is written, for trouble to name it.</summary>
        static string Headed(Statement statement)
        {
            return "'" + (statement.Key == Keyword.None ? statement.Head : Keywords.Name(statement.Key)) + "'";
        }

        /// <summary>A value as it is written, for trouble to name it.</summary>
        static string Spelled(Value value)
        {
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

        /// <summary>Owes every part an act names, to be judged once the whole script is read.</summary>
        static void Owes(Statement statement, Act act, AutomationSystem system, Trouble trouble)
        {
            for (var i = 0; act != null && i < act.Targets.Count; i++)
                trouble.Owes(statement, act.Targets[i], system);
        }

    }
}
