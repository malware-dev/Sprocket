using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using System.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Writing
{
    /// <summary>An expression written out as one text, operands as a script spells them.</summary>
    public sealed class ExpressionText : IExpressionPen
    {
        readonly StringBuilder _written = new StringBuilder();

        /// <inheritdoc />
        public void Words(string words)
        {
            _written.Append(words);
        }

        /// <inheritdoc />
        public void Operand(Expression operand, string written)
        {
            _written.Append(written);
        }

        /// <summary>What has been written.</summary>
        public override string ToString()
        {
            return _written.ToString();
        }
    }
}
