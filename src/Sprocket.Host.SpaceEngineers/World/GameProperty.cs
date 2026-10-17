using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System.Text;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>One of a block's own values, read through the property the game hands out for it.</summary>
    /// <remarks>
    /// Made once, when the test is compiled. Text is compared where the block keeps it, so a <c>when</c> on a text looks
    /// without building a string.
    /// </remarks>
    public sealed class GameProperty : Gauge
    {
        readonly IMyTerminalBlock _block;
        readonly ValueKind _kind;
        readonly ITerminalProperty<bool> _flag;
        readonly ITerminalProperty<float> _real;
        readonly ITerminalProperty<long> _whole;
        readonly ITerminalProperty<Color> _color;
        readonly ITerminalProperty<StringBuilder> _text;

        /// <summary>The property <paramref name="property" /> of <paramref name="block" />, read as a value of the kind <paramref name="kind" />.</summary>
        public GameProperty(IMyTerminalBlock block, ITerminalProperty property, ValueKind kind)
        {
            _block = block;
            _kind = kind;
            switch (kind)
            {
                case ValueKind.Flag:
                    _flag = property.AsBool();
                    break;
                case ValueKind.Real:
                    _real = property.AsFloat();
                    break;
                case ValueKind.Integer:
                    _whole = property.As<long>();
                    break;
                case ValueKind.Color:
                    _color = property.AsColor();
                    break;
                default:
                    _text = property.As<StringBuilder>();
                    break;
            }
        }

        /// <inheritdoc />
        public override ValueKind Kind => _kind;

        /// <inheritdoc />
        public override Literal Read()
        {
            switch (_kind)
            {
                case ValueKind.Flag:
                    return Literal.Of(_flag.GetValue(_block));
                case ValueKind.Real:
                    return Literal.Of(_real.GetValue(_block));
                case ValueKind.Integer:
                    return Literal.Of(_whole.GetValue(_block));
                case ValueKind.Color:
                    var color = _color.GetValue(_block);
                    return Literal.Colour(color.R, color.G, color.B, color.A);
            }

            return Literal.Of(_text.GetValue(_block).ToString());
        }

        /// <remarks>A real is compared in the single the block keeps it as, so 0.1 written matches the 0.1 it holds.</remarks>
        public override bool Holds(Relation relation, Literal operand)
        {
            if (_kind == ValueKind.Real)
                return Relations.Holds(Read(), relation, Literal.Of((float)operand.Number));

            if (_kind == ValueKind.Text && (relation == Relation.Is || relation == Relation.IsNot))
                return Same(_text.GetValue(_block), operand.Text) == (relation == Relation.Is);

            return base.Holds(relation, operand);
        }

        static bool Same(StringBuilder held, string text)
        {
            if (held == null)
                return text.Length == 0;

            if (held.Length != text.Length)
                return false;

            for (var i = 0; i < text.Length; i++)
            {
                if (held[i] != text[i])
                    return false;
            }

            return true;
        }
    }
}
