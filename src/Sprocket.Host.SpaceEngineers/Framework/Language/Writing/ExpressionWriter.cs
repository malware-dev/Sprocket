using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Writing
{
    /// <summary>Writes an expression as a program holds it, with only the parentheses its precedence needs.</summary>
    public static class ExpressionWriter
    {
        /// <summary>How <paramref name="tree" /> reads without a system around it to ask what a bare name could also mean.</summary>
        public static string Of(Expression tree)
        {
            return Text(null, tree);
        }

        /// <summary><paramref name="tree" /> as a script holds it, its names written with their kinds where <paramref name="system" /> has them mean more than one thing.</summary>
        public static string Text(AutomationSystem system, Expression tree)
        {
            var written = new ExpressionText();
            Write(written, system, tree);
            return written.ToString();
        }

        /// <summary>Writes <paramref name="tree" /> into <paramref name="pen" /> as <see cref="Text" /> spells it, each setting, state and reading as an operand of its own.</summary>
        public static void Write(IExpressionPen pen, AutomationSystem system, Expression tree)
        {
            Append(pen, system, tree, 0);
        }

        /// <summary>Writes <paramref name="tree" /> into <paramref name="into" />, in parentheses where it binds less tightly than <paramref name="needed" />.</summary>
        static void Append(IExpressionPen into, AutomationSystem system, Expression tree, int needed)
        {
            if (tree.Precedence < needed)
            {
                into.Words("(");
                Append(into, system, tree, 0);
                into.Words(")");
                return;
            }

            var arithmetic = tree as Arithmetic;
            if (arithmetic != null)
            {
                var precedence = arithmetic.Precedence;
                Append(into, system, arithmetic.Left, precedence);
                into.Words(" " + Spelling(arithmetic.Op) + " ");
                Append(into, system, arithmetic.Right, precedence + 1);
                return;
            }

            var negation = tree as Negation;
            if (negation != null)
            {
                into.Words("-");
                Append(into, system, negation.Operand, Expression.AtomPrecedence);
                return;
            }

            var function = tree as Function;
            if (function != null)
            {
                into.Words(Function.Spelling(function.Kind) + "(");
                for (var i = 0; i < function.Arguments.Count; i++)
                {
                    if (i > 0)
                        into.Words(", ");

                    Append(into, system, function.Arguments[i], 0);
                }

                into.Words(")");
                return;
            }

            var reading = tree as BlockReading;
            if (reading != null)
            {
                into.Operand(reading, Reading(system, reading));
                return;
            }

            var state = tree as StateOperand;
            if (state != null)
            {
                var pen = new ScriptWriter();
                ProgramWriter.Named(pen, system, Kinds.State, state.Name, false);
                into.Operand(state, pen.ToString().Trim());
                return;
            }

            var operand = (Operand)tree;
            if (operand.Value.IsSetting)
                into.Operand(operand, Written(system, operand.Value));
            else
                into.Words(Written(system, operand.Value));
        }

        static string Reading(AutomationSystem system, BlockReading reading)
        {
            var pen = new ScriptWriter();
            switch (reading.Combine)
            {
                case Combination.Sum: pen.Says(Keyword.Sum); break;
                case Combination.Average: pen.Says(Keyword.Average); break;
                case Combination.Min: pen.Says(Keyword.Min); break;
                case Combination.Max: pen.Says(Keyword.Max); break;
            }

            ProgramWriter.WriteMember(pen, system, reading.Key.Namespace, reading.Key.Id, reading.Arguments, reading.Targets);
            return pen.ToString().Trim();
        }

        /// <summary>A literal or setting standing as an operand; a setting spelled like one of the format's words is written with its kind, since that word alone reads as the word.</summary>
        static string Written(AutomationSystem system, Literal value)
        {
            var pen = new ScriptWriter();
            if (value.IsSetting && !value.IsKinded && Keywords.Find(value.Text) != Keyword.None)
                pen.Kinded("", Kinds.Setting, value.Text);
            else
                ProgramWriter.Word(pen, system, value);

            return pen.ToString().Trim();
        }

        static string Spelling(Operator op)
        {
            switch (op)
            {
                case Operator.Add: return ExpressionWords.Add;
                case Operator.Subtract: return ExpressionWords.Subtract;
                case Operator.Multiply: return ExpressionWords.Multiply;
            }

            return ExpressionWords.Divide;
        }
    }
}
