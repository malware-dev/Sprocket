using System;
using System.Collections.Generic;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Blocks;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>Reads a program back from its script, refusing the whole of a text with anything wrong in it.</summary>
    public static class Reader
    {
        /// <summary>
        /// Reads from the whole program <paramref name="text" /> only the system <paramref name="identity" /> and its instances
        /// into <paramref name="program" />, which does not hold that system, answering what was wrong, or empty.
        /// </summary>
        /// <remarks>
        /// For putting a system in the place of another of its identity: the rest of <paramref name="text" /> is what
        /// <paramref name="program" /> holds already, and is checked by reading the whole of it first.
        /// </remarks>
        public static string ReadOne(string text, Guid identity, Automation program)
        {
            var parser = new Parser(text);
            var constructs = parser.Read();
            var read = new Trouble(text, parser.Trouble);
            var id = identity.ToString("D");

            foreach (var construct in constructs)
            {
                if (construct.Key == Keyword.System && construct.Text(construct.Says(0, Keyword.Blueprint) ? 1 : 0) == id)
                    Systems.ReadSystem(construct, program, read);
            }

            foreach (var construct in constructs)
            {
                if (construct.Key == Keyword.Instance && construct.Text(2) == id)
                    ReadInstance(construct, program, read);
            }

            read.Settle();
            return read.What;
        }

        /// <summary>
        /// Reads the blueprint <paramref name="text" /> into <paramref name="program" />, beside what it holds, answering what was
        /// wrong with it, or empty.
        /// </summary>
        /// <remarks>
        /// Read into a program of its own first, because a text with an error in it is trusted in no part: where anything is
        /// wrong, <paramref name="program" /> is left as it was. The text holds one system blueprint and nothing else, as
        /// <see cref="Scribe.Blueprint(AutomationSystem, Guid)" /> writes it.
        /// </remarks>
        public static string AddBlueprint(string text, Automation program, Values values)
        {
            AutomationSystem lone;
            var trouble = Lone(text, values, out lone);
            if (trouble.Length > 0)
                return trouble;

            var identity = lone.Identity;
            if (program.FindSystem(identity) != null)
                return $"system {identity:D} is already declared.";

            var parser = new Parser(text);
            var constructs = parser.Read();
            var read = new Trouble(text, parser.Trouble);
            foreach (var construct in constructs)
            {
                if (construct.Key == Keyword.System)
                    Systems.ReadSystem(construct, program, read);
            }

            read.Settle();
            return read.What;
        }

        /// <summary>
        /// Reads the blueprint <paramref name="text" /> on its own into <paramref name="read" />, answering what was wrong with it,
        /// or empty.
        /// </summary>
        /// <remarks>The text holds one system blueprint and nothing else, as <see cref="Scribe.Blueprint(AutomationSystem, Guid)" /> writes it; where it does not, <paramref name="read" /> is null.</remarks>
        public static string Lone(string text, Values values, out AutomationSystem read)
        {
            read = null;
            var trial = new Automation();
            var trouble = Load(text, trial, values);
            if (trouble.Length > 0)
                return trouble;

            if (trial.Systems.Count != 1 || !trial.Systems[0].IsBlueprint || trial.Refs.Count > 0 || trial.SignalRefs.Count > 0
                || trial.Systems[0].Instances.Count > 0)
                return "expected one system blueprint and nothing else.";

            read = trial.Systems[0];
            return "";
        }

        /// <summary>The line, counted from 1, that trouble the reader reported names, or 0 where it names none.</summary>
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
        /// the same name. It does not mark the program damaged. A program written for an earlier format finds what it
        /// names on the blocks of <paramref name="world" />, where there is one, as it is upgraded.
        /// </remarks>
        public static string Load(string saved, Automation program, Values values, out string warning, GridWorld world = null)
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
                refused = $"{(constructs.Count == 0 ? "Line 1: " : trouble.Line(constructs[0]))}expected sprocket <format>.";
            else if (header != null && header.Whole(0) != Format.Version)
                refused = $"{trouble.Line(header)}written for sprocket {header.Whole(0)}; this reads sprocket {Format.Version}.";
            else if (header != null && DraftOf(header) != Format.Draft && !Upgrades.From(Format.Version, DraftOf(header)))
                refused = $"{trouble.Line(header)}written for {(DraftOf(header) < 0 ? "the finished format" : $"alpha {DraftOf(header)}")}; this reads alpha {Format.Draft}.";

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
                    Systems.ReadSystem(construct, program, trouble);
                else if (construct.Key == Keyword.Instance)
                    ReadInstance(construct, program, trouble);
                else if (i > 0)
                    trouble.Say(construct, $"unexpected {Quotes.Headed(construct)}; expected let, system or instance.");
            }

            trouble.Settle();

            if (trouble.What.Length > 0)
            {
                program.Unreadable(saved);
                program.Damaged(trouble.What);
            }
            else if (header != null && DraftOf(header) != Format.Draft)
            {
                var upgraded = Upgrades.Apply(DraftOf(header), program, world);
                if (upgraded.Length > 0)
                    Told.Say($"upgraded a program from {upgraded}");
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

            Quotes.Ends(header, alpha ? 3 : 1, trouble);
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

        /// <summary>
        /// <c>let door1 = block '…' [ … ]</c> or <c>let button = ext.toolbar(1, 1, 4) on [ panel1 ]</c>: a variable, holding a
        /// block or a signal.
        /// </summary>
        static void ReadLet(Statement statement, Automation program, Values values, Trouble trouble)
        {
            var name = statement.ValueAt(0);
            if (!Quotes.Named(name) || !statement.Says(1, Keyword.Equals))
            {
                trouble.Say(statement, "expected let <name> = <value>.");
                return;
            }

            if (program.Names(name.Word))
            {
                trouble.Say(statement, $"variable '{name.Word}' is already defined.");
                return;
            }

            if (statement.IsMember(2))
            {
                if (!Quotes.Ends(statement, 3, trouble))
                    return;

                var blocks = new List<string>();
                ToolbarSlot at;
                if (!Signal(statement, statement.ValueAt(2), program, trouble, blocks, out at))
                    return;

                var signal = new SignalRef(name.Word, blocks, at);
                signal.Annotation = statement.Annotation;
                program.Restore(signal);
                return;
            }

            if (!statement.Says(2, Keyword.Block) || !Quotes.Named(statement.ValueAt(3)) || statement.ValueAt(4).Form != ValueForm.List)
            {
                trouble.Say(statement, "expected a block, like block '<type>/<subtype>' [ <keys> ], or a signal, like ext.toolbar(1, 1, 4) on [ panel1 ].");
                return;
            }

            if (!Quotes.Ends(statement, 5, trouble))
                return;

            var declared = new EntityRef(name.Word, BlockKinds.Definition(statement.Word(3)));
            declared.Annotation = statement.Annotation;
            var keys = statement.Items(4);
            for (var i = 0; i < keys.Count; i++)
                ReadKey(values, keys[i], declared.Keys, statement, trouble);

            program.Restore(declared);
        }

        /// <summary>
        /// The signal the member <paramref name="value" /> gives back: this host's <c>ext.toolbar</c>, a slot on the
        /// toolbars of the blocks variables above hold, each added to <paramref name="blocks" />; or trouble.
        /// </summary>
        static bool Signal(Statement statement, Value value, Automation program, Trouble trouble, IList<string> blocks,
            out ToolbarSlot at)
        {
            at = null;
            var member = statement.Member(value);
            if (member.Count != 2 || member[0].Word != BlockKinds.Own || member[1].Word != Format.Toolbar)
            {
                trouble.Say(statement, $"no function {Quotes.Spelling(member)} giving back a signal; expected ext.toolbar(<toolbar>, <page>, <slot>).");
                return false;
            }

            var given = statement.Arguments(value);
            if (given.Count != 3 || Statement.Counted(given[0]) == 0 || Statement.Counted(given[1]) == 0 || Statement.Counted(given[2]) == 0)
            {
                trouble.Say(statement, "expected ext.toolbar(<toolbar>, <page>, <slot>), each a whole number from 1.");
                return false;
            }

            var on = statement.On(value);
            if (on.Count == 0)
            {
                trouble.Say(statement, "expected the blocks the signal is sent from.");
                return false;
            }

            for (var i = 0; i < on.Count; i++)
            {
                if (!Quotes.Named(on[i]))
                {
                    trouble.Say(statement, "expected names in the list.");
                    return false;
                }

                if (program.FindRef(on[i].Word) == null)
                {
                    trouble.Say(statement, $"no variable '{on[i].Word}' holding a block above this line.");
                    return false;
                }

                blocks.Add(on[i].Word);
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
                trouble.Say(statement, $"no system called {Quotes.Spelled(statement, 2)}.");
                return;
            }

            if (!system.IsBlueprint)
            {
                trouble.Say(statement, $"system {Quotes.Spelled(statement, 2)} is not a system blueprint; only a blueprint has instances.");
                return;
            }

            if (system.FindInstance((int)number.Whole) != null)
            {
                trouble.Say(statement, $"instance {number.Whole} of this system is already declared.");
                return;
            }

            var name = "";
            var at = 3;
            if (statement.Says(3, Keyword.As))
            {
                if (!Quotes.Named(statement.ValueAt(4)))
                {
                    trouble.Say(statement, "expected a name after 'as'.");
                    return;
                }

                name = statement.Word(4);
                at = 5;
            }

            if (!Quotes.Ends(statement, at, trouble))
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

                if (!Quotes.Ends(inner, 2, trouble))
                    continue;

                Part part = system.FindPart(inner.Head);
                if (part == null)
                {
                    trouble.Say(inner, $"system blueprint '{system.Name}' has no slot '{inner.Head}'.");
                    continue;
                }

                if (instance.FilledWith(inner.Head) != null)
                {
                    trouble.Say(inner, $"slot '{inner.Head}' is already answered.");
                    continue;
                }

                instance.Note(inner.Head, inner.Annotation);

                if (part.IsSignal)
                {
                    Answered(inner, system, instance, program, trouble);
                    continue;
                }

                var held = HostTypes.Named(part.Holds);
                if (held != null)
                {
                    Facets(inner, held, instance, program, trouble);
                    continue;
                }

                var filled = new List<string>();
                if (!Names(inner, 1, filled, trouble, "expected variables holding blocks in the list."))
                    continue;

                foreach (var variable in filled)
                {
                    if (program.FindRef(variable) == null)
                        trouble.Say(inner, $"no variable '{variable}' holding a block above this line.");
                }

                instance.Fill(inner.Head, filled);
            }
        }

        /// <summary>
        /// <c>status = [ ext.screen(1) on [ board ] ]</c>: what answers a slot of the type <paramref name="type" /> in one use of
        /// a blueprint, each written in place by the function returning one, a thing of each block it is on.
        /// </summary>
        static void Facets(Statement statement, HostType type, SystemInstance instance, Automation program, Trouble trouble)
        {
            var items = statement.Items(1);
            var blocks = new List<string>();
            var facets = new List<int>();
            var like = $"{type.Id}(1) on [ <block> ]";
            for (var i = 0; i < items.Count; i++)
            {
                var member = statement.Member(items[i]);
                if (items[i].Form != ValueForm.Reference || member.Count != 2 || $"{member[0].Word}.{member[1].Word}" != type.Id)
                {
                    trouble.Say(statement, $"expected {type.Id} in the list, like [ {like} ].");
                    return;
                }

                var given = statement.Arguments(items[i]);
                var number = given.Count == 1 ? Statement.Counted(given[0]) : 0;
                if (number == 0)
                {
                    trouble.Say(statement, $"expected {type.Id}(<number>), a whole number from 1.");
                    return;
                }

                var on = statement.On(items[i]);
                if (on.Count == 0)
                {
                    trouble.Say(statement, "expected the blocks it is on.");
                    return;
                }

                for (var n = 0; n < on.Count; n++)
                {
                    if (!Quotes.Named(on[n]))
                    {
                        trouble.Say(statement, "expected names in the list.");
                        return;
                    }

                    if (program.FindRef(on[n].Word) == null)
                    {
                        trouble.Say(statement, $"no variable '{on[n].Word}' holding a block above this line.");
                        return;
                    }

                    blocks.Add(on[n].Word);
                    facets.Add(number);
                }
            }

            instance.Fill(statement.Head, blocks, facets);
        }

        /// <summary>
        /// <c>button = [ ext.toolbar(1, 1, 4) on [ panel1 ] ]</c>: the signals that answer a signal slot in one use of a
        /// blueprint, each written in place or named by a variable above.
        /// </summary>
        static void Answered(Statement statement, AutomationSystem system, SystemInstance instance, Automation program,
            Trouble trouble)
        {
            var items = statement.Items(1);
            var blocks = new List<string>();
            var at = new List<ToolbarSlot>();
            for (var i = 0; i < items.Count; i++)
            {
                if (Quotes.Named(items[i]))
                {
                    var declared = program.FindSignalRef(items[i].Word);
                    if (declared == null)
                    {
                        trouble.Say(statement, $"no variable '{items[i].Word}' holding a signal above this line.");
                        return;
                    }

                    for (var n = 0; n < declared.Blocks.Count; n++)
                    {
                        blocks.Add(declared.Blocks[n]);
                        at.Add(declared.At);
                    }

                    continue;
                }

                if (items[i].Form != ValueForm.Reference)
                {
                    trouble.Say(statement, "expected signals in the list, like [ ext.toolbar(1, 1, 4) on [ panel1 ] ].");
                    return;
                }

                var from = blocks.Count;
                ToolbarSlot slot;
                if (!Signal(statement, items[i], program, trouble, blocks, out slot))
                    return;

                for (var n = from; n < blocks.Count; n++)
                    at.Add(slot);
            }

            // Each answer to a slot an on names is a toolbar slot that starts it, and holds nothing else.
            for (var i = 0; Systems.Started(system, statement.Head) && i < blocks.Count; i++)
                trouble.Takes(statement, blocks[i], at[i]);

            instance.Fill(statement.Head, blocks, at);
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

        /// <summary>The names in the list at <paramref name="at" />, into <paramref name="into" />; or false, and trouble saying <paramref name="what" />, where one is not a name.</summary>
        static bool Names(Statement statement, int at, IList<string> into, Trouble trouble, string what)
        {
            into.Clear();
            var items = statement.Items(at);
            for (var i = 0; i < items.Count; i++)
            {
                if (!Quotes.Named(items[i]))
                {
                    trouble.Say(statement, what);
                    return false;
                }

                into.Add(items[i].Word);
            }

            return true;
        }
    }
}
