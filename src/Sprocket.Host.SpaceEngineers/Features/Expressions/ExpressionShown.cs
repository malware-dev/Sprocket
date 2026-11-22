using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using System.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    /// <summary>
    /// An expression as a player reads it wherever it is shown as one value: its calculations written as mathematics writes
    /// them, and each setting, state and block value by its name, never in the script's own words.
    /// </summary>
    public static class ExpressionShown
    {
        // How tightly a negation binds: tighter than any calculation written between its values.
        const int Tightest = 5;

        /// <summary><paramref name="expression" />, of <paramref name="system" />, as <paramref name="editor" /> shows it.</summary>
        public static string Of(SprocketEditor editor, AutomationSystem system, Expression expression)
        {
            var said = new StringBuilder();
            Write(editor, system, expression, 0, said);
            return said.ToString();
        }

        // Writes expression into said, in brackets where it binds more loosely than needed.
        static void Write(SprocketEditor editor, AutomationSystem system, Expression expression, int needed, StringBuilder said)
        {
            var arithmetic = expression as Arithmetic;
            if (arithmetic != null)
            {
                var kind = JoinKinds.Of(arithmetic.Op);
                var binds = kind == JoinKind.Add || kind == JoinKind.Subtract ? 3 : 4;
                if (binds < needed)
                    said.Append('(');

                Write(editor, system, arithmetic.Left, binds, said);
                said.Append(' ').Append(ExpressionWords.Symbol(editor.Texts, kind)).Append(' ');
                Write(editor, system, arithmetic.Right, binds + 1, said);
                if (binds < needed)
                    said.Append(')');

                return;
            }

            var negation = expression as Negation;
            if (negation != null)
            {
                said.Append('-');
                Write(editor, system, negation.Operand, Tightest, said);
                return;
            }

            var function = expression as Function;
            if (function != null)
            {
                said.Append(ExpressionWords.Symbol(editor.Texts, JoinKinds.Of(function.Kind))).Append('(');
                for (var i = 0; i < function.Arguments.Count; i++)
                {
                    if (i > 0)
                        said.Append(", ");

                    Write(editor, system, function.Arguments[i], 0, said);
                }

                said.Append(')');
                return;
            }

            // A text is quoted, so where it ends among the rest can be seen.
            var operand = expression as Operand;
            if (operand != null && !operand.Value.IsSetting && operand.Value.Kind == ValueKind.Text)
            {
                said.Append('"').Append(operand.Value.Text).Append('"');
                return;
            }

            string kindWord;
            said.Append(ExpressionNames.Of(editor, system, expression, out kindWord));
        }
    }
}
