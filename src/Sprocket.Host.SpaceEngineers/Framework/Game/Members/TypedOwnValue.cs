using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>A value read off a <typeparamref name="T" />, with options read into a <typeparamref name="TOptions" />.</summary>
    public sealed class TypedOwnValue<T, TOptions> : OwnValue where T : class
    {
        readonly ValueKind _kind;
        readonly string[] _names;
        readonly Func<IList<Literal>, TOptions> _options;
        readonly Func<T, TOptions, ReaderMemory, Literal> _read;

        /// <summary>A value of the kind <paramref name="kind" />, read with <paramref name="read" /> and its options with <paramref name="options" />.</summary>
        public TypedOwnValue(ValueKind kind, string[] names, bool remembers, Func<IList<Literal>, TOptions> options,
            Func<T, TOptions, ReaderMemory, Literal> read)
        {
            _kind = kind;
            _names = names;
            Remembers = remembers;
            _options = options;
            _read = read;
        }

        /// <inheritdoc />
        public override ValueKind Kind => _kind;

        /// <inheritdoc />
        public override IReadOnlyList<string> Names => _names;

        /// <inheritdoc />
        public override bool Remembers { get; }

        /// <inheritdoc />
        public override Gauge Gauge(IMyTerminalBlock block, IList<Literal> arguments, ReaderMemory seen)
        {
            return new OwnValueGauge<T, TOptions>(this, _options(arguments), block, seen);
        }

        /// <inheritdoc />
        public override Reading Is(Literal becomes)
        {
            var operand = becomes;
            if (_kind == ValueKind.Name)
            {
                var at = Array.IndexOf(_names, becomes.Text);
                if (at < 0)
                    throw new ArgumentException($"'{becomes.Text}' is none of {string.Join(", ", _names)}.");

                operand = Literal.Of(at);
            }

            return new EqualityGauge<T, TOptions>(this, operand);
        }

        /// <summary>The value of <paramref name="block" /> with <paramref name="options" />, remembering between looks in <paramref name="seen" />.</summary>
        public Literal Read(T block, TOptions options, ReaderMemory seen)
        {
            return _read(block, options, seen);
        }

        /// <summary>The options <paramref name="arguments" /> give.</summary>
        public TOptions Options(IList<Literal> arguments)
        {
            return _options(arguments);
        }
    }
}
