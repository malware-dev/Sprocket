using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;

namespace Sprocket.Host.SpaceEngineers.Framework.Running
{
    /// <summary>What kinds of value the settings, states and readings of an expression hold on one installation.</summary>
    /// <remarks>Asked when a program is checked, never on a tick.</remarks>
    public sealed class ExpressionTypes : ITypeScope
    {
        readonly AutomationSystem _system;
        readonly SystemInstance _through;
        readonly Bindings _bindings;

        /// <summary>The kinds in <paramref name="system" />, its blocks as <paramref name="through" /> fills the slots and <paramref name="bindings" /> finds them.</summary>
        public ExpressionTypes(AutomationSystem system, SystemInstance through, Bindings bindings)
        {
            _system = system;
            _through = through;
            _bindings = bindings;
        }

        /// <inheritdoc />
        public ValueKind? Setting(string name)
        {
            var part = _system?.FindPart(name);
            if (part == null || !part.IsSetting)
                return null;

            switch (part.Setting)
            {
                case SettingReader.Flag: return ValueKind.Flag;
                case SettingReader.Text: return ValueKind.Text;
                case SettingReader.Integer: return ValueKind.Integer;
                case SettingReader.Real: return ValueKind.Real;
                case SettingReader.Duration: return ValueKind.Duration;
                case SettingReader.Color: return ValueKind.Color;
            }

            return ValueKind.Name;
        }

        /// <inheritdoc />
        public ValueKind? State(string name)
        {
            var state = _system?.FindState(name);
            if (state == null)
                return null;

            return state.IsFlag ? ValueKind.Flag : ValueKind.Name;
        }

        /// <inheritdoc />
        public ValueKind? Reading(BlockReading reading)
        {
            foreach (var name in SystemInstance.Through(_through, reading.Targets))
            {
                var kind = Probe.KindOf(_bindings.Entity(name), reading.Key);
                if (kind != null)
                    return kind;
            }

            return null;
        }

        /// <summary>A value of the kind <paramref name="kind" />, to stand in for what an expression of that kind comes to; null where the kind is not one a member's options are checked with.</summary>
        public static Literal? Sample(ValueKind? kind)
        {
            switch (kind)
            {
                case ValueKind.Integer: return Literal.Of(0L);
                case ValueKind.Real: return Literal.Of(0d);
                case ValueKind.Text: return Literal.Of("");
                case ValueKind.Flag: return Literal.Of(false);
                case ValueKind.Duration: return Literal.Of(0L);
            }

            return null;
        }
    }
}
