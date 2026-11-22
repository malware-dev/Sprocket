using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Writing
{
    /// <summary>What an expression is written into, in order: its words, and each operand that reads from outside it.</summary>
    public interface IExpressionPen
    {
        /// <summary>Writes <paramref name="words" />: a literal, an operator, a parenthesis or a function's name and punctuation.</summary>
        void Words(string words);

        /// <summary>Writes <paramref name="operand" />, a setting, a state or a reading, which a script spells <paramref name="written" />.</summary>
        void Operand(Expression operand, string written);
    }
}
