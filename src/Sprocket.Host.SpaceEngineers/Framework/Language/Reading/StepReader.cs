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
        public static bool Member(Statement statement, int index, string what, ScriptTrouble trouble, out string provider, out string id,
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
                if (given[i].Form == ValueForm.List)
                {
                    var slots = new List<string>();
                    if (!Names(statement.Within(given[i]), slots, statement, trouble))
                        return false;

                    arguments.Add(Literal.Slots(slots));
                    continue;
                }

                if (!Argument(statement, given[i], trouble))
                    return false;

                arguments.Add(Given(given[i]));
            }

            return Names(statement.On(index), targets, statement, trouble);
        }

        /// <summary>Whether <paramref name="value" /> is one a member can be given or compared with, and trouble where it is not.</summary>
        static bool Argument(Statement statement, Value value, ScriptTrouble trouble)
        {
            var form = value.Form;
            if (form != ValueForm.List && form != ValueForm.Vector && form != ValueForm.Reference && form != ValueForm.Qualified)
                return true;

            trouble.Say(statement, $"unexpected {ScriptQuotes.Spelled(statement, value)}; expected a value.");
            return false;
        }

        /// <summary>
        /// The comparison and the value from <paramref name="at" />, into <paramref name="relation" /> and
        /// <paramref name="value" />; or false and trouble where they are not that, or a number is wanted and not given.
        /// </summary>
        /// <remarks>An ordering compares numbers, and so does a sum, so either is given one.</remarks>
        public static bool Compared(Statement statement, int at, Quantifier quantifier, ScriptTrouble trouble, out Relation relation,
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
                SystemReader.Uses(statement, step as Act, system, program, trouble);
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
        /// <c>all ext.filled on [ vent ] == on then</c> or <c>where == inside then</c>, from <paramref name="at" />: what an
        /// <c>if</c> or an <c>else if</c> tests, or null and trouble where it is not that.
        /// </summary>
        static BranchTest Tested(Statement statement, int at, AutomationSystem system, Automation program, ScriptTrouble trouble)
        {
            if (ScriptQuotes.Named(statement.ValueAt(at)))
                return StateTested(statement, at, system, trouble);

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

            if (!ScriptQuotes.Ends(statement, at + 5, trouble))
                return null;

            test.Quantifier = quantifier;
            test.Provider = provider;
            test.MemberId = id;
            test.Relation = relation;
            test.Value = value;
            for (var i = 0; i < test.Targets.Count; i++)
                SystemReader.Uses(statement, test.Targets[i], system, program, false, trouble);

            return test;
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
            if (value.Form != ValueForm.Duration || value.Whole < least || value.Whole > int.MaxValue)
                return false;

            length = value.Word == Parser.Seconds ? Duration.OfSeconds(value.Real) : Duration.OfTicks((int)value.Whole);
            return true;
        }

        /// <summary>A step, or nothing where the statement was not one, which is trouble.</summary>
        public static Step ReadStep(Statement statement, AutomationSystem system, ScriptTrouble trouble)
        {
            if (statement.Key == Keyword.Await)
                return ScriptQuotes.Ends(statement, 0, trouble) ? Noted(new Await(), statement) : null;

            if (statement.Key == Keyword.Run || statement.Key == Keyword.Stop)
                return ScriptQuotes.Ends(statement, 1, trouble) ? Called(statement, trouble) : null;

            if (statement.Key == Keyword.Wait)
            {
                Duration length;
                if (Lasting(statement.ValueAt(0), 0, out length))
                    return ScriptQuotes.Ends(statement, 1, trouble) ? Noted(new Wait(length), statement) : null;

                trouble.Say(statement, "expected a duration after 'wait', like 60t.");
                return null;
            }

            if (statement.Key == Keyword.Call)
                return Acted(statement, trouble);

            if (statement.Key == Keyword.Set && !statement.IsMember(0) && ScriptQuotes.Named(statement.ValueAt(0)))
            {
                var sets = StateReader.Set(statement, system, trouble);
                return sets == null ? null : Noted(sets, statement);
            }

            if (statement.Key == Keyword.Set)
                return ReadSet(statement, trouble);

            if (statement.Key == Keyword.None && statement.Head == AbortWords.Abort)
                return Aborted(statement, trouble);

            trouble.Say(statement, $"unexpected {ScriptQuotes.Headed(statement)}; expected call, set, run, stop, await, wait or abort.");
            return null;
        }

        /// <summary><c>abort saying "Door jammed" as warning</c>: an abort, its message and its level each given or left out.</summary>
        static Step Aborted(Statement statement, ScriptTrouble trouble)
        {
            var abort = new Abort();
            var at = 0;
            if (statement.ValueAt(0).Form == ValueForm.Identifier && statement.Word(0) == AbortWords.Saying)
            {
                var message = statement.ValueAt(1);
                if (message.Form != ValueForm.Text)
                {
                    trouble.Say(statement, "expected text in double quotes after 'saying', like \"Door jammed\".");
                    return null;
                }

                abort.Message = message.Word ?? "";
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
        /// <c>call ext.close on [ inner ]</c> or <c>call SetColor(255, 0, 0) on [ light ]</c>: an action, bare or qualified by
        /// its namespace, on some blocks, given its arguments.
        /// </summary>
        static Step Acted(Statement statement, ScriptTrouble trouble)
        {
            var act = new Act("");
            string provider, id;
            if (!Member(statement, 0, "an action on a list of names after 'call', like ext.close on [ inner ]", trouble,
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
        static Step ReadSet(Statement statement, ScriptTrouble trouble)
        {
            var act = new Act("");
            string provider, id;
            if (!Member(statement, 0, "a value on a list of names after 'set', like Radius on [ light ]", trouble,
                    out provider, out id, act.Arguments, act.Targets))
                return null;

            if (provider.Length > 0 && provider != BlockKinds.Own)
            {
                trouble.Say(statement, $"no value '{ScriptWriter.Spelling(provider, id)}' to set; expected one of the block's own, written bare, or one qualified by {BlockKinds.Own}.");
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

            if (!ScriptQuotes.Ends(statement, 3, trouble) || !Argument(statement, statement.ValueAt(2), trouble))
                return null;

            act.ActionId = id;
            act.Form = ReachKind.Value;
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
                return Literal.Color((byte)(value.Whole >> 24), (byte)(value.Whole >> 16),
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
            step.Attributes = ScriptAttributes.Unused(statement);
            return step;
        }

        /// <summary><c>run 2</c>, <c>stop 2</c> or <c>run 'Let me out'</c>: another sequence of the same system.</summary>
        static Step Called(Statement statement, ScriptTrouble trouble)
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

        /// <summary>The names in <paramref name="items" />, into <paramref name="into" />; or false, and trouble, where one is not a name.</summary>
        public static bool Names(ValueRange items, IList<string> into, Statement statement, ScriptTrouble trouble)
        {
            into.Clear();
            for (var i = 0; i < items.Count; i++)
            {
                if (!ScriptQuotes.Named(items[i]))
                {
                    trouble.Say(statement, "expected names in the list.");
                    return false;
                }

                into.Add(items[i].Word);
            }

            return true;
        }
    }
}
