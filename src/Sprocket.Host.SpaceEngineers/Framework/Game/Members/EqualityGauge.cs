using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>Whether a value read with no options is one thing, as a flag.</summary>
    public sealed class EqualityGauge<T, TOptions> : Reading where T : class
    {
        readonly TypedOwnValue<T, TOptions> _being;
        readonly TOptions _options;
        readonly Literal _operand;

        /// <summary>Whether <paramref name="being" /> reads as <paramref name="operand" />.</summary>
        public EqualityGauge(TypedOwnValue<T, TOptions> being, Literal operand)
        {
            _being = being;
            _options = being.Options(Array.Empty<Literal>());
            _operand = operand;
        }

        /// <inheritdoc />
        public override bool Remembers => _being.Remembers;

        /// <inheritdoc />
        public override bool Holds(IMyTerminalBlock block, ReaderMemory seen)
        {
            return Relations.Holds(_being.Read((T)block, _options, seen), Relation.Is, _operand);
        }
    }
}
