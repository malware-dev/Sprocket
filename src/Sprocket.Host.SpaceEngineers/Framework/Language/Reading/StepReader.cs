using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>Reads a sequence's steps out of the script, its ifs among them, and the members and values steps and triggers name.</summary>
    public static class StepReader
    {
        /// <summary>The quantifier at <paramref name="at" />, or false and trouble naming <paramref name="after" /> where there is none.</summary>
        public static bool Quantified(Statement statement, int at, string after, ScriptTrouble trouble, out Quantifier quantifier)
        {
            quantifier = Quantifier.Any;
            if (statement.Says(at, Keyword.All))
                quantifier = Quantifier.All;
            else if (statement.Says(at, Keyword.Sum))
                quantifier = Quantifier.Sum;
            else if (statement.Says(at, Keyword.Average))
                quantifier = Quantifier.Average;
            else if (statement.Says(at, Keyword.Min))
                quantifier = Quantifier.Min;
            else if (statement.Says(at, Keyword.Max))
                quantifier = Quantifier.Max;
            else if (!statement.Says(at, Keyword.Any))
            {
                trouble.Say(statement, $"expected any, all, sum, average, min or max after '{after}'.");
                return false;
            }

            return true;
        }

        /// <summary>Whether the value at <paramref name="at" /> is a quantifier, <c>any</c>, <c>all</c>, <c>sum</c> or <c>average</c>, which a test of blocks starts with.</summary>
        /// <remarks>Asked before a bare name there is read as a state's: the test's construct reads these words where it starts.</remarks>
        public static bool IsQuantifier(Statement statement, int at)
        {
            // A name spelled min or max is a quantifier only before a member, as it may also name a state.
            return statement.Says(at, Keyword.Any) || statement.Says(at, Keyword.All) || statement.Says(at, Keyword.Sum)
                || statement.Says(at, Keyword.Average) || (statement.Says(at, Keyword.Min) || statement.Says(at, Keyword.Max)) && statement.IsMember(at + 1);
        }

        /// <summary>
        /// Whether <paramref name="written" /> is a flag, into <paramref name="value" />; false where it is not, and also, with
        /// trouble and <paramref name="wrong" />, where a bare one could also mean a thing of <paramref name="system" /> so named.
        /// </summary>
        static bool Flagged(Statement statement, Value written, AutomationSystem system, ScriptTrouble trouble, out Literal value, out bool wrong)
        {
            value = default(Literal);
            wrong = false;
            bool on, bare;
            if (!Kinds.Flag(statement, written, out on, out bare))
                return false;

            if (bare && !Kinds.FlagAlone(statement, written.Word, system, trouble))
            {
                wrong = true;
                return false;
            }

            value = Literal.Of(on);
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
        /// The member at <paramref name="index" />, bare or qualified by a namespace, its arguments and its blocks; or false and
        /// trouble where it is not one, <paramref name="what" /> saying what was expected.
        /// </summary>
        public static bool Member(Statement statement, AutomationSystem system, int index, string what, ScriptTrouble trouble, out string provider, out string id,
            IList<Literal> arguments, IList<string> targets)
        {
            provider = id = "";
            if (!statement.IsMember(index))
            {
                trouble.Say(statement, $"expected {what}.");
                return false;
            }

            var member = statement.Parts(index);
            if (!ScriptQuotes.Whole(member))
            {
                trouble.Say(statement, $"expected {what}.");
                return false;
            }

            provider = ScriptQuotes.Namespace(member);
            id = member[member.Count - 1].Word;
            // A namespace no mod has registered reads all the same, and its member is not offered until one does: a mod may
            // load after the world's programs are read, or not be installed.

            var given = statement.Arguments(index);
            for (var i = 0; i < given.Count; i++)
            {
                Literal one;
                if (!ReadArgument(statement, given[i], system, trouble, out one))
                    return false;

                arguments.Add(one);
            }

            return Names(statement.On(index), targets, statement, system, trouble);
        }

        /// <summary>
        /// One value a member is given or compared with, <paramref name="argument" />, as the literal it is: a flag, a list of
        /// slots, a setting, a word of the member or a host type, or a value; or an expression. False, and trouble, where it is none of them.
        /// </summary>
        public static bool ReadArgument(Statement statement, Value argument, AutomationSystem system, ScriptTrouble trouble, out Literal value)
        {
            value = default(Literal);
            if (ExpressionReader.Is(argument))
                return ExpressionReader.Read(statement, argument, system, trouble, out value);

            bool shared;
            if (Flagged(statement, argument, system, trouble, out value, out shared))
                return true;

            if (shared || !Kinds.Literal(statement, ref argument, trouble))
                return false;

            if (argument.Form == ValueForm.List)
            {
                var slots = new List<string>();
                if (!Names(statement.Within(argument), slots, statement, system, trouble))
                    return false;

                value = Literal.Slots(slots);
                return true;
            }

            var setting = SettingReader.Referenced(statement, argument, system);
            if (setting != null)
            {
                if (!SettingReader.Owe(statement, setting, system, null, null, trouble))
                    return false;

                value = Literal.Setting(setting, argument.Form == ValueForm.Kinded);
                return true;
            }

            bool wrong;
            if (Word(statement, argument, system, trouble, out value, out wrong))
                return true;

            if (wrong || !Argument(statement, argument, trouble))
                return false;

            value = Given(argument);
            return true;
        }

        /// <summary>
        /// A choice of a host type written with it, <c>door_status(closed)</c> or <c>sprocket.door_status(closed)</c>, into
        /// <paramref name="word" />; false where <paramref name="written" /> is not written so, and also, with trouble and
        /// <paramref name="wrong" />, where it names none of the type's choices, or the type's bare name is also a thing of
        /// <paramref name="system" />'s.
        /// </summary>
        static bool Word(Statement statement, Value written, AutomationSystem system, ScriptTrouble trouble, out Literal word, out bool wrong)
        {
            word = default(Literal);
            wrong = false;
            if (written.Form != ValueForm.Kinded)
                return false;

            var kind = statement.KindOf(written);
            var thing = statement.ThingOf(written);
            string space = null;
            string id = null;
            if (kind.Form == ValueForm.Qualified)
            {
                var parts = statement.PartsOf(kind);
                if (ScriptQuotes.Whole(parts))
                {
                    space = ScriptQuotes.Namespace(parts);
                    id = parts[parts.Count - 1].Word;
                }
            }
            else if (ScriptQuotes.Named(kind))
                id = kind.Word;

            if (id == null)
                return false;

            var type = space != null ? ChoiceTypes.Named(space, id) : ChoiceTypes.Named(id);
            if (type == null && space == null)
            {
                // Bare, and called so in more than one of the host's namespaces.
                var hosts = new List<string>();
                ChoiceTypes.Spellings(id, hosts);
                if (hosts.Count > 1)
                {
                    wrong = true;
                    var meanings = new List<string>();
                    foreach (var host in hosts)
                        meanings.Add($"the host's type '{host}'");

                    Kinds.Ambiguous(statement, id, meanings, hosts, trouble);
                    return false;
                }
            }

            if (type == null)
                return false;

            wrong = true;
            if (kind.Form != ValueForm.Qualified)
            {
                var meanings = new List<string>();
                var spellings = new List<string>();
                Kinds.Meanings(system, id, meanings, spellings);
                if (meanings.Count > 0)
                {
                    meanings.Add($"the host's type '{id}'");
                    spellings.Add($"{type.Namespace}.{id}");
                    Kinds.Ambiguous(statement, id, meanings, spellings, trouble);
                    return false;
                }
            }

            if (!ScriptQuotes.Named(thing) || !type.Has(thing.Word))
            {
                trouble.Say(statement, $"no choice {ScriptQuotes.Spelled(statement, thing)} in {type.Namespace}.{type.Id}.");
                return false;
            }

            wrong = false;
            word = Literal.Name(thing.Word, type);
            return true;
        }

        /// <summary>Whether <paramref name="value" /> is one a member can be given or compared with, and trouble where it is not.</summary>
        static bool Argument(Statement statement, Value value, ScriptTrouble trouble)
        {
            var form = value.Form;
            if (form != ValueForm.List && form != ValueForm.Vector && form != ValueForm.Reference && form != ValueForm.Qualified && form != ValueForm.Kinded)
                return true;

            trouble.Say(statement, $"unexpected {ScriptQuotes.Spelled(statement, value)}; expected a value.");
            return false;
        }

        /// <summary>
        /// The comparison and the value from <paramref name="at" />, into <paramref name="relation" /> and
        /// <paramref name="value" />, a word of the member <paramref name="id" /> of <paramref name="provider" /> written bare or
        /// qualified by it; or false and trouble where they are not that, or a number is wanted and not given.
        /// </summary>
        /// <remarks>An ordering compares numbers, and so does a sum, so either is given one.</remarks>
        public static bool Compared(Statement statement, AutomationSystem system, string provider, string id, int at, Quantifier quantifier, ScriptTrouble trouble, out Relation relation,
            out Literal value)
        {
            value = default(Literal);
            if (!Related(statement.ValueAt(at), out relation) || statement.Count <= at + 1)
            {
                trouble.Say(statement, "expected a comparison and a value after the names, like == on.");
                return false;
            }

            var written = statement.ValueAt(at + 1);
            if (ExpressionReader.Is(written))
                return ExpressionReader.Read(statement, written, system, trouble, out value);

            var ordered = relation != Relation.Is && relation != Relation.IsNot;
            bool shared;
            if (Flagged(statement, written, system, trouble, out value, out shared))
            {
                if (!ordered && !Quantifiers.Numeric(quantifier))
                    return true;

                trouble.Say(statement, $"expected a number after '{Keywords.Name(ProgramWriter.Said(relation))}'.");
                return false;
            }

            if (shared || !Kinds.Literal(statement, ref written, trouble))
                return false;

            var setting = SettingReader.Referenced(statement, written, system);
            if (setting != null)
            {
                value = Literal.Setting(setting, written.Form == ValueForm.Kinded);
                return ordered || Quantifiers.Numeric(quantifier)
                    ? SettingReader.Owe(statement, setting, system, SettingReader.Numeric, "a number", trouble)
                    : SettingReader.Owe(statement, setting, system, null, null, trouble);
            }

            Literal word;
            bool wrong;
            if (Word(statement, written, system, trouble, out word, out wrong))
            {
                value = word;
                if (!ordered && !Quantifiers.Numeric(quantifier))
                    return true;

                trouble.Say(statement, $"expected a number after '{Keywords.Name(ProgramWriter.Said(relation))}'.");
                return false;
            }

            if (wrong)
                return false;

            if (!Argument(statement, written, trouble))
                return false;

            value = Given(written);

            if ((ordered || Quantifiers.Numeric(quantifier)) && value.Kind != ValueKind.Integer && value.Kind != ValueKind.Real)
            {
                trouble.Say(statement, $"expected a number after '{Keywords.Name(ProgramWriter.Said(relation))}'.");
                return false;
            }

            return true;
        }

        /// <summary>The statements of a block, as steps added to <paramref name="into" />: an <c>if</c> as its line, its branches and its end.</summary>
        public static void ReadSteps(IList<Statement> body, IList<Step> into, AutomationSystem system, Automation program, ScriptTrouble trouble)
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

                var step = ReadStep(statement, system, trouble);
                if (step == null)
                    continue;

                step.IsOff = statement.IsOff;
                SystemReader.Uses(statement, step, system, program, trouble);
                into.Add(step);
            }
        }

        /// <summary>
        /// <c>if … then { … }</c> and the <c>else if</c> and <c>else</c> after it, as the lines they are, answering where the
        /// last statement of it stands in <paramref name="body" />.
        /// </summary>
        static int ReadIf(IList<Statement> body, int at, IList<Step> into, AutomationSystem system, Automation program,
            ScriptTrouble trouble)
        {
            var statement = body[at];
            var opens = new If(Tested(statement, 0, system, program, trouble) ?? new Comparison());
            opens.IsOff = statement.IsOff;
            opens.Annotation = statement.Annotation;
            opens.Attributes = ScriptAttributes.Unused(statement);
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

                BranchTest test = null;
                if (branch.Says(0, Keyword.If))
                    test = Tested(branch, 1, system, program, trouble) ?? new Comparison();
                else
                {
                    ScriptQuotes.Ends(branch, 0, trouble);
                    last = true;
                }

                var other = new Else(test);
                other.Annotation = branch.Annotation;
                other.Attributes = ScriptAttributes.Unused(branch);
                into.Add(other);
                Blocked(branch, into, system, program, trouble);
            }

            into.Add(new EndIf());
            return at;
        }

        /// <summary>The steps of the block of an <c>if</c> or an <c>else</c>, or trouble where it has none.</summary>
        static void Blocked(Statement statement, IList<Step> into, AutomationSystem system, Automation program, ScriptTrouble trouble)
        {
            if (!statement.HasBlock)
            {
                trouble.Say(statement, "expected '{' to open the block.");
                return;
            }

            ReadSteps(statement.Body, into, system, program, trouble);
        }

        /// <summary>
        /// <c>all sprocket.filled on [ vent ] == on then</c>, <c>where == inside then</c> or <c>gearbox.lifts is installed then</c>,
        /// from <paramref name="at" />: what an <c>if</c> or an <c>else if</c> tests, or null and trouble where it is not that.
        /// </summary>
        static BranchTest Tested(Statement statement, int at, AutomationSystem system, Automation program, ScriptTrouble trouble)
        {
            if (statement.Says(at + 1, Keyword.Is))
                return HostTested(statement, at, trouble);

            if (!IsQuantifier(statement, at) && StateReader.Called(statement, statement.ValueAt(at)) != null)
                return StateTested(statement, at, system, trouble);

            var test = new Comparison();
            Quantifier quantifier;
            if (!Quantified(statement, at, "if", trouble, out quantifier))
                return null;

            string provider, id;
            if (!Member(statement, system, at + 1, "a value on a list of names, like sprocket.closed on [ inner ]", trouble, out provider,
                    out id, test.Arguments, test.Targets))
                return null;

            Relation relation;
            Literal value;
            if (!Compared(statement, system, provider, id, at + 2, quantifier, trouble, out relation, out value))
                return null;

            if (!statement.Says(at + 4, Keyword.Then))
            {
                trouble.Say(statement, "expected 'then' after the value.");
                return null;
            }

            if (!ScriptQuotes.Ends(statement, at + 5, trouble))
                return null;

            test.Quantifier = quantifier;
            test.Provider = provider;
            test.MemberId = id;
            test.Relation = relation;
            test.Value = value;
            for (var i = 0; i < test.Targets.Count; i++)
                SystemReader.Uses(statement, test.Targets[i], system, program, false, trouble);

            if (value.IsExpression)
            {
                var lists = new List<IList<string>>();
                value.Tree.NameLists(lists);
                foreach (var names in lists)
                {
                    for (var i = 0; i < names.Count; i++)
                        SystemReader.Uses(statement, names[i], system, program, false, trouble);
                }
            }

            return test;
        }

        /// <summary>
        /// <c>gearbox.lifts is installed then</c> or <c>gearbox.lifts is not installed then</c> from <paramref name="at" />: whether
        /// the host has a namespace, which an <c>if</c> tests; or null and trouble where it is not that.
        /// </summary>
        /// <remarks>A namespace no mod has registered reads all the same: the test is how a program asks.</remarks>
        static HostTest HostTested(Statement statement, int at, ScriptTrouble trouble)
        {
            var written = statement.ValueAt(at);
            var parts = statement.Parts(at);
            if ((written.Form != ValueForm.Identifier && written.Form != ValueForm.Qualified) || !ScriptQuotes.Whole(parts))
            {
                trouble.Say(statement, "expected a namespace before 'is', like gearbox.lifts is installed.");
                return null;
            }

            var lacks = statement.Says(at + 2, Keyword.Not);
            var installed = at + (lacks ? 3 : 2);
            if (!statement.Says(installed, Keyword.Installed))
            {
                trouble.Say(statement, "expected installed or not installed after 'is'.");
                return null;
            }

            if (!statement.Says(installed + 1, Keyword.Then))
            {
                trouble.Say(statement, "expected 'then' after 'installed'.");
                return null;
            }

            if (!ScriptQuotes.Ends(statement, installed + 2, trouble))
                return null;

            return new HostTest { Namespace = ScriptQuotes.Spelling(parts), Installed = !lacks };
        }

        /// <summary><c>where == inside then</c> from <paramref name="at" />: a state an <c>if</c> tests, or null and trouble where it is not that.</summary>
        static StateTest StateTested(Statement statement, int at, AutomationSystem system, ScriptTrouble trouble)
        {
            var test = StateReader.Test(statement, at, system, trouble);
            if (test == null)
                return null;

            if (!statement.Says(at + 3, Keyword.Then))
            {
                trouble.Say(statement, "expected 'then' after the value.");
                return null;
            }

            return ScriptQuotes.Ends(statement, at + 4, trouble) ? test : null;
        }

        /// <summary>
        /// A duration as it was written, or false where it was no length of time that can be kept.
        /// </summary>
        /// <remarks>Ticks are counted in an int, and seconds are held to the same bound by the ticks they come to.</remarks>
        public static bool Lasting(Value value, long least, out Duration length)
        {
            length = default(Duration);
            if (value.Form != ValueForm.Duration || value.Integer < least || value.Integer > int.MaxValue)
                return false;

            length = value.Word == Parser.Seconds ? Duration.OfSeconds(value.Real) : Duration.OfTicks((int)value.Integer);
            return true;
        }

        /// <summary>A step, or nothing where the statement was not one, which is trouble.</summary>
        public static Step ReadStep(Statement statement, AutomationSystem system, ScriptTrouble trouble)
        {
            if (statement.Key == Keyword.Await)
                return ScriptQuotes.Ends(statement, 0, trouble) ? Noted(new Await(), statement) : null;

            if (statement.Key == Keyword.Run || statement.Key == Keyword.Stop)
                return ScriptQuotes.Ends(statement, 1, trouble) ? Called(statement, system, trouble) : null;

            if (statement.Key == Keyword.Wait)
            {
                Duration length;
                if (Lasting(statement.ValueAt(0), 0, out length))
                    return ScriptQuotes.Ends(statement, 1, trouble) ? Noted(new Wait(length), statement) : null;

                if (ExpressionReader.Is(statement.ValueAt(0)))
                {
                    Literal worked;
                    if (!ExpressionReader.Read(statement, statement.ValueAt(0), system, trouble, out worked) || !ScriptQuotes.Ends(statement, 1, trouble))
                        return null;

                    return Noted(new Wait(0) { Expression = worked.Tree }, statement);
                }

                var name = SettingReader.Referenced(statement, statement.ValueAt(0), system);
                if (name != null)
                {
                    if (!SettingReader.Owe(statement, name, system, SettingReader.Lasting, "a duration", trouble))
                        return null;

                    return ScriptQuotes.Ends(statement, 1, trouble) ? Noted(new Wait(0) { Setting = name }, statement) : null;
                }

                trouble.Say(statement, "expected a duration after 'wait', like 60t, or a setting or an expression holding one.");
                return null;
            }

            if (statement.Key == Keyword.Call)
                return Acted(statement, system, trouble);

            if (statement.Key == Keyword.Set && StateReader.Called(statement, statement.ValueAt(0)) != null)
            {
                var sets = StateReader.Set(statement, system, trouble);
                return sets == null ? null : Noted(sets, statement);
            }

            if (statement.Key == Keyword.Set)
                return ReadSet(statement, system, trouble);

            if (statement.Key == Keyword.None && statement.Head == AbortWords.Abort)
                return Aborted(statement, system, trouble);

            if (SequenceWords.Handles(statement))
            {
                trouble.Say(statement, $"unexpected '{statement.Head}'; a catch or finally stands last in the sequence's own body, never inside an if.");
                return null;
            }

            trouble.Say(statement, $"unexpected {ScriptQuotes.Headed(statement)}; expected call, set, run, stop, await, wait or abort.");
            return null;
        }

        /// <summary>
        /// <c>abort saying "Door jammed" as warning</c>: an abort, its message, written or a text setting of
        /// <paramref name="system" />, and its level each given or left out.
        /// </summary>
        static Step Aborted(Statement statement, AutomationSystem system, ScriptTrouble trouble)
        {
            var abort = new Abort();
            var at = 0;
            if (statement.ValueAt(0).Form == ValueForm.Identifier && statement.Word(0) == AbortWords.Saying)
            {
                var message = statement.ValueAt(1);
                var setting = message.Form == ValueForm.Text || ExpressionReader.Is(message) ? null : SettingReader.Referenced(statement, message, system);
                if (ExpressionReader.Is(message))
                {
                    Literal worked;
                    if (!ExpressionReader.Read(statement, message, system, trouble, out worked))
                        return null;

                    abort.Expression = worked.Tree;
                }
                else if (setting != null)
                {
                    if (!SettingReader.Owe(statement, setting, system, SettingReader.Worded, "text", trouble))
                        return null;

                    abort.Setting = setting;
                }
                else if (message.Form == ValueForm.Text)
                    abort.Message = message.Word ?? "";
                else
                {
                    trouble.Say(statement, "expected text in double quotes after 'saying', like \"Door jammed\", or a setting holding text.");
                    return null;
                }

                at = 2;
            }

            if (statement.Says(at, Keyword.As))
            {
                AbortLevel level;
                if (!AbortWords.Level(statement.Word(at + 1), out level))
                {
                    trouble.Say(statement, "expected note, warning or problem after 'as'.");
                    return null;
                }

                abort.Level = level;
                at += 2;
            }

            return ScriptQuotes.Ends(statement, at, trouble) ? Noted(abort, statement) : null;
        }

        /// <summary>
        /// <c>call sprocket.close on [ inner ]</c> or <c>call SetColor(255, 0, 0) on [ light ]</c>: an action, bare or qualified by
        /// its namespace, on some blocks, given its arguments.
        /// </summary>
        static Step Acted(Statement statement, AutomationSystem system, ScriptTrouble trouble)
        {
            var act = new Act("");
            string provider, id;
            if (!Member(statement, system, 0, "an action on a list of names after 'call', like sprocket.close on [ inner ]", trouble,
                    out provider, out id, act.Arguments, act.Targets))
                return null;

            if (!ScriptQuotes.Ends(statement, 1, trouble))
                return null;

            act.ActionId = id;
            act.Form = provider.Length == 0 ? ReachKind.Action : ReachKind.Native;
            if (provider.Length > 0)
                act.Provider = provider;

            return Noted(act, statement);
        }

        /// <summary><c>set Radius on [ light ] to 4.5</c>: one of the block's own values.</summary>
        /// <remarks>The value is taken by position, so a flag written <c>on</c> is a value.</remarks>
        static Step ReadSet(Statement statement, AutomationSystem system, ScriptTrouble trouble)
        {
            var act = new Act("");
            string provider, id;
            if (!Member(statement, system, 0, "a value on a list of names after 'set', like Radius on [ light ]", trouble,
                    out provider, out id, act.Arguments, act.Targets))
                return null;

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

            if (!ScriptQuotes.Ends(statement, 3, trouble))
                return null;

            var given = statement.ValueAt(2);
            if (ExpressionReader.Is(given))
            {
                Literal worked;
                if (!ExpressionReader.Read(statement, given, system, trouble, out worked))
                    return null;

                act.ActionId = id;
                act.Form = ReachKind.Value;
                act.Provider = provider;
                act.Arguments.Add(worked);
                return Noted(act, statement);
            }

            Literal flag;
            bool shared;
            if (Flagged(statement, given, system, trouble, out flag, out shared))
            {
                act.ActionId = id;
                act.Form = ReachKind.Value;
                act.Provider = provider;
                act.Arguments.Add(flag);
                return Noted(act, statement);
            }

            if (shared || !Kinds.Literal(statement, ref given, trouble))
                return null;

            var setting = SettingReader.Referenced(statement, given, system);
            var word = default(Literal);
            var wrong = false;
            var worded = setting == null && Word(statement, given, system, trouble, out word, out wrong);
            if (setting != null ? !SettingReader.Owe(statement, setting, system, null, null, trouble) : !worded && (wrong || !Argument(statement, given, trouble)))
                return null;

            act.ActionId = id;
            act.Form = ReachKind.Value;
            act.Provider = provider;
            act.Arguments.Add(setting != null ? Literal.Setting(setting, given.Form == ValueForm.Kinded)
                : worded ? word : Given(given));
            return Noted(act, statement);
        }

        /// <summary>One value, as whatever kind it was written as.</summary>
        /// <remarks>A bare word that is not a flag is a name: one of a list of names a member gives.</remarks>
        static Literal Given(Value value)
        {
            if (value.Form == ValueForm.Text)
                return Literal.Of(value.Word);

            if (value.Form == ValueForm.Integer || value.Form == ValueForm.Duration)
                return Literal.Of(value.Integer);

            if (value.Form == ValueForm.Real)
                return Literal.Of(value.Real);

            if (value.Form == ValueForm.Color)
            {
                return Literal.Color((byte)(value.Integer >> 24), (byte)(value.Integer >> 16),
                    (byte)(value.Integer >> 8), (byte)value.Integer);
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
            step.Attributes = ScriptAttributes.Unused(statement);
            return step;
        }

        /// <summary>
        /// <c>run 2</c>, <c>stop 2</c>, <c>run 'Let me out'</c> or <c>run sequence(cycle)</c>: another sequence of the same
        /// system; trouble where a bare name could also be another thing of <paramref name="system" />.
        /// </summary>
        static Step Called(Statement statement, AutomationSystem system, ScriptTrouble trouble)
        {
            var stops = statement.Key == Keyword.Stop;
            var named = statement.ValueAt(0);
            var kinded = Kinds.Named(statement, named, Keywords.Name(Keyword.Sequence));
            if (kinded != null)
                named = new Value(ValueForm.Identifier, kinded);
            else if (ScriptQuotes.Named(named))
            {
                var meanings = new List<string>();
                var spellings = new List<string>();
                Kinds.Meanings(system, named.Word, meanings, spellings);
                if (meanings.Count > 1)
                {
                    Kinds.Ambiguous(statement, named.Word, meanings, spellings, trouble);
                    return null;
                }
            }

            if (named.Form == ValueForm.Integer && named.Integer >= 1 && named.Integer <= int.MaxValue)
            {
                var numbered = new Run((int)named.Integer);
                numbered.Stops = stops;
                trouble.Wrote(statement, numbered);
                return Noted(numbered, statement);
            }

            if (ScriptQuotes.Named(named))
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
        /// The names in <paramref name="items" />, bare or as <c>slot(inner)</c>, into <paramref name="into" />; or false, and
        /// trouble, where one is not a name, or a bare slot's name is also another thing's of <paramref name="system" />.
        /// </summary>
        public static bool Names(ValueRange items, IList<string> into, Statement statement, AutomationSystem system, ScriptTrouble trouble)
        {
            into.Clear();
            for (var i = 0; i < items.Count; i++)
            {
                var slot = Kinds.Named(statement, items[i], Keywords.Name(Keyword.Slot));
                if (slot != null)
                {
                    into.Add(slot);
                    continue;
                }

                if (!ScriptQuotes.Named(items[i]))
                {
                    trouble.Say(statement, "expected names in the list.");
                    return false;
                }

                var name = items[i].Word;
                if (system.FindPart(name) != null)
                {
                    var meanings = new List<string>();
                    var spellings = new List<string>();
                    Kinds.Meanings(system, name, meanings, spellings);
                    if (meanings.Count > 1)
                    {
                        Kinds.Ambiguous(statement, name, meanings, spellings, trouble);
                        return false;
                    }
                }

                into.Add(name);
            }

            return true;
        }
    }
}
