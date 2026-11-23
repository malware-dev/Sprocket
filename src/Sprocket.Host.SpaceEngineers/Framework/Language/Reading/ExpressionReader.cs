using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>Reads the expressions a statement holds where it takes a value, out of the terms the parser wrote.</summary>
    public static class ExpressionReader
    {
        /// <summary>Whether <paramref name="written" /> is read as an expression: operators or functions, or a member read on blocks.</summary>
        public static bool Is(Value written)
        {
            return written.Form == ValueForm.Expression || written.Form == ValueForm.Reference;
        }

        /// <summary>
        /// The value <paramref name="words" /> say, read as the value of a step of <paramref name="system" /> is: an expression, or a
        /// literal or setting standing alone; empty where it reads, else what is wrong with it, without a line number.
        /// </summary>
        /// <remarks>Read with the parser and readers a program is read with, so the words mean what they would mean in it.</remarks>
        public static string FromText(string words, AutomationSystem system, out Literal value)
        {
            value = default(Literal);
            const string head = "set x on [ x ] to ";
            var text = head + (words ?? "") + "\n";
            var parser = new Parser(text);
            var statements = parser.Read();
            var trouble = new ScriptTrouble(text, parser.Trouble);
            if (trouble.What.Length == 0 && (statements.Count != 1 || statements[0].Count < 3))
                trouble.Say("expected a value.");

            if (trouble.What.Length == 0)
            {
                var statement = statements[0];
                var given = statement.ValueAt(2);
                Duration length;
                if (statement.Count > 3)
                    trouble.Say(statement, "unexpected words after the value.");
                else if (StepReader.Lasting(given, 0, out length))
                    value = Literal.Of(length);
                else if (!StepReader.ReadArgument(statement, given, system, trouble, out value))
                    value = default(Literal);
            }

            return Unlined(trouble.What);
        }

        /// <summary><paramref name="what" /> without the line number a script's trouble starts with, which one line of words has no use for.</summary>
        static string Unlined(string what)
        {
            if (!what.StartsWith("Line "))
                return what;

            var colon = what.IndexOf(": ", StringComparison.Ordinal);
            return colon < 0 ? what : what.Substring(colon + 2);
        }

        /// <summary>
        /// The value <paramref name="written" />, an expression, as the literal that holds it; false, and trouble, where it is not
        /// one a statement of <paramref name="system" /> can have.
        /// </summary>
        public static bool Read(Statement statement, Value written, AutomationSystem system, ScriptTrouble trouble, out Literal value)
        {
            value = default(Literal);
            Expression tree;
            if (!Tree(statement, written, system, trouble, out tree))
                return false;

            var operand = tree as Operand;
            value = operand != null ? operand.Value : Literal.Worked(tree);
            return true;
        }

        /// <summary>
        /// The condition <paramref name="written" />, an expression an <c>if</c> or a <c>when</c> tests, into <paramref name="tree" />;
        /// false, and trouble, where it is not one a statement of <paramref name="system" /> can have, or comes to a fixed value
        /// that is no flag.
        /// </summary>
        public static bool Condition(Statement statement, Value written, AutomationSystem system, ScriptTrouble trouble, out Expression tree)
        {
            if (!Tree(statement, written, system, trouble, out tree))
                return false;

            var operand = tree as Operand;
            if (operand == null || operand.Value.IsSetting || operand.Value.Kind == ValueKind.Flag)
                return true;

            trouble.Say(statement, "expected a condition, like ready or count >= 5, that comes to on or off.");
            return false;
        }

        static bool Tree(Statement statement, Value written, AutomationSystem system, ScriptTrouble trouble, out Expression tree)
        {
            tree = null;
            if (written.Form == ValueForm.Expression)
                return Terms(statement, written, system, trouble, out tree);

            if (written.Form == ValueForm.Reference)
                return Reading(statement, written, Combination.None, system, trouble, out tree);

            return Single(statement, written, system, trouble, out tree);
        }

        /// <summary>The terms of <paramref name="written" />, postfix, worked into one expression.</summary>
        static bool Terms(Statement statement, Value written, AutomationSystem system, ScriptTrouble trouble, out Expression tree)
        {
            tree = null;
            var terms = statement.Terms(written);
            var stack = new List<Expression>();
            for (var i = 0; i < terms.Count; i++)
            {
                var term = terms[i];
                if (term.Form != ValueForm.Operator)
                {
                    // A check of any or all of a reading's blocks follows the reading and the value it compares with.
                    Combination checks;
                    Relation relation;
                    if (term.Form == ValueForm.Reference && i + 2 < terms.Count && terms[i + 2].Form == ValueForm.Operator
                        && Checks(terms[i + 2].Word, out checks, out relation))
                    {
                        Expression compared, check;
                        if (!Tree(statement, terms[i + 1], system, trouble, out compared)
                            || !Reading(statement, term, checks, system, trouble, out check, relation, compared))
                            return false;

                        stack.Add(check);
                        i += 2;
                        continue;
                    }

                    // A combining word follows the member it combines the readings of.
                    var combine = Combination.None;
                    if (term.Form == ValueForm.Reference && i + 1 < terms.Count && terms[i + 1].Form == ValueForm.Operator
                        && Combining(terms[i + 1].Word, out combine))
                        i++;

                    Expression operand;
                    if (!(term.Form == ValueForm.Reference
                            ? Reading(statement, term, combine, system, trouble, out operand)
                            : Tree(statement, term, system, trouble, out operand)))
                        return false;

                    stack.Add(operand);
                    continue;
                }

                if (!Applied(term, stack, statement, trouble))
                    return false;
            }

            if (stack.Count != 1)
            {
                trouble.Say(statement, "expected one value in the expression.");
                return false;
            }

            tree = stack[0];
            return true;
        }

        /// <summary>Has the operator or function <paramref name="term" /> take its operands off <paramref name="stack" /> and put what it makes of them back.</summary>
        static bool Applied(Value term, List<Expression> stack, Statement statement, ScriptTrouble trouble)
        {
            var count = term.Integer > int.MaxValue ? 0 : (int)term.Integer;
            if (stack.Count < count)
            {
                trouble.Say(statement, "expected a value after the operator.");
                return false;
            }

            var operands = stack.GetRange(stack.Count - count, count);
            stack.RemoveRange(stack.Count - count, count);
            switch (term.Word)
            {
                case ExpressionWords.Negate:
                    stack.Add(new Negation(operands[0]));
                    return true;
                case ExpressionWords.Add:
                    stack.Add(new Arithmetic(Operator.Add, operands[0], operands[1]));
                    return true;
                case ExpressionWords.Subtract:
                    stack.Add(new Arithmetic(Operator.Subtract, operands[0], operands[1]));
                    return true;
                case ExpressionWords.Multiply:
                    stack.Add(new Arithmetic(Operator.Multiply, operands[0], operands[1]));
                    return true;
                case ExpressionWords.Divide:
                    stack.Add(new Arithmetic(Operator.Divide, operands[0], operands[1]));
                    return true;
                case ExpressionWords.MinFunction:
                    stack.Add(new Function(FunctionKind.Min, operands));
                    return true;
                case ExpressionWords.MaxFunction:
                    stack.Add(new Function(FunctionKind.Max, operands));
                    return true;
                case ExpressionWords.RoundFunction:
                    stack.Add(new Function(FunctionKind.Round, operands));
                    return true;
                case ExpressionWords.AbsFunction:
                    stack.Add(new Function(FunctionKind.Abs, operands));
                    return true;
                case ExpressionWords.Not:
                    stack.Add(new Inversion(operands[0]));
                    return true;
                case ExpressionWords.And:
                    stack.Add(new Logical(Connective.And, operands[0], operands[1]));
                    return true;
                case ExpressionWords.Or:
                    stack.Add(new Logical(Connective.Or, operands[0], operands[1]));
                    return true;
            }

            Relation relation;
            if (count == 2 && Relating(term.Word, out relation))
            {
                stack.Add(new Comparing(relation, operands[0], operands[1]));
                return true;
            }

            trouble.Say(statement, "unexpected operator in the expression.");
            return false;
        }

        /// <summary>The comparison the word <paramref name="word" /> spells, <c>==</c> or <c>&lt;=</c>, into <paramref name="relation" />; false where it spells none.</summary>
        static bool Relating(string word, out Relation relation)
        {
            relation = Relation.Is;
            return StepReader.Related(new Value(ValueForm.Keyword, null, 0, 0, Keywords.Find(word)), out relation);
        }

        /// <summary>Whether the term <paramref name="word" /> checks a comparison on any or all of a reading's blocks, into <paramref name="checks" /> and <paramref name="relation" />.</summary>
        static bool Checks(string word, out Combination checks, out Relation relation)
        {
            checks = Combination.None;
            relation = Relation.Is;
            var space = word.IndexOf(' ');
            if (space < 0)
                return false;

            var quantifier = word.Substring(0, space);
            if (quantifier == Keywords.Name(Keyword.Any))
                checks = Combination.Any;
            else if (quantifier == Keywords.Name(Keyword.All))
                checks = Combination.All;
            else
                return false;

            return Relating(word.Substring(space + 1), out relation);
        }

        static bool Combining(string word, out Combination combine)
        {
            combine = Combination.None;
            switch (word)
            {
                case ExpressionWords.Sum: combine = Combination.Sum; break;
                case ExpressionWords.Average: combine = Combination.Average; break;
                case ExpressionWords.Min: combine = Combination.Min; break;
                case ExpressionWords.Max: combine = Combination.Max; break;
            }

            return combine != Combination.None;
        }

        /// <summary>A member read on blocks, <c>gearbox.fill on [ tanks ]</c>, its arguments each a value or an expression.</summary>
        static bool Reading(Statement statement, Value written, Combination combine, AutomationSystem system, ScriptTrouble trouble, out Expression tree,
            Relation relation = Relation.Is, Expression compared = null)
        {
            tree = null;
            var member = statement.Member(written);
            if (!ScriptQuotes.Whole(member))
            {
                trouble.Say(statement, "expected a member on a list of names.");
                return false;
            }

            var key = new MemberKey(ScriptQuotes.Namespace(member), member[member.Count - 1].Word);
            var arguments = new List<Literal>();
            var given = statement.Arguments(written);
            for (var i = 0; i < given.Count; i++)
            {
                Literal one;
                if (!StepReader.ReadArgument(statement, given[i], system, trouble, out one))
                    return false;

                arguments.Add(one);
            }

            var targets = new List<string>();
            if (!StepReader.Names(statement.On(written), targets, statement, system, trouble))
                return false;

            // Whether several blocks are read is also asked of what a slot holds, when the program is bound; where it names several
            // outright, no combining word before the member is plain.
            if (combine == Combination.None && targets.Count > 1)
            {
                trouble.Say(statement, "a member read on more than one block needs sum, average, min or max before it.");
                return false;
            }

            tree = compared != null ? new BlockReading(key, combine, arguments, targets, relation, compared) : new BlockReading(key, combine, arguments, targets);
            return true;
        }

        /// <summary>One operand: a state, a setting, or a literal.</summary>
        static bool Single(Statement statement, Value written, AutomationSystem system, ScriptTrouble trouble, out Expression tree)
        {
            tree = null;
            var state = StateNamed(statement, written, system);
            if (state != null)
            {
                tree = new StateOperand(state);
                return true;
            }

            // A length of time carries its unit, which a value given to a member does not.
            Duration length;
            if (written.Form == ValueForm.Duration && StepReader.Lasting(written, long.MinValue, out length))
            {
                tree = new Operand(Literal.Of(length));
                return true;
            }

            Literal value;
            if (!StepReader.ReadArgument(statement, written, system, trouble, out value))
                return false;

            if (value.Kind == ValueKind.Slots)
            {
                trouble.Say(statement, "unexpected a list of names; an expression takes values.");
                return false;
            }

            tree = new Operand(value);
            return true;
        }

        /// <summary>The state of <paramref name="system" /> that <paramref name="written" /> names, bare or as <c>state(where)</c>, or null where it names none.</summary>
        static string StateNamed(Statement statement, Value written, AutomationSystem system)
        {
            var name = written.Form == ValueForm.Kinded
                ? Kinds.Named(statement, written, Kinds.State)
                : ScriptQuotes.Named(written) && system.FindPart(written.Word) == null ? written.Word : null;

            return name != null && system.FindState(name) != null ? name : null;
        }
    }
}
