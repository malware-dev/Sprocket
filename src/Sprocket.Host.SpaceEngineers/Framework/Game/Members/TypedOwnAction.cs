using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.ModSupport;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>An action on a <typeparamref name="T" />, with options read into a <typeparamref name="TOptions" />.</summary>
    public sealed class TypedOwnAction<T, TOptions> : OwnAction where T : class
    {
        readonly Func<IList<Literal>, TOptions> _options;
        readonly Reading _until;

        /// <summary>An action that reads its options with <paramref name="options" />, acts with <paramref name="does" />, and is done when <paramref name="done" /> says so or <paramref name="until" /> holds.</summary>
        public TypedOwnAction(Func<IList<Literal>, TOptions> options, Action<T, TOptions, ReaderMemory> does, Reading until,
            Func<T, TOptions, ReaderMemory, bool> done, Func<T, TOptions, ReaderMemory, BlockTrouble> trouble, Action<T> stop,
            Func<T, TOptions, int> expected,
            Func<TOptions, bool> forced = null, Action<IList<T>, TOptions> begins = null)
        {
            _options = options;
            _until = until;
            Does = does;
            Finished = done;
            Troubled = trouble;
            Stops = stop;
            Takes = expected;
            Forced = forced;
            Begun = begins;
        }

        /// <summary>What the action does to a block.</summary>
        public Action<T, TOptions, ReaderMemory> Does { get; private set; }

        /// <summary>Whether the action has finished on a block; null reads as not finished.</summary>
        public Func<T, TOptions, ReaderMemory, bool> Finished { get; private set; }

        /// <summary>What stops the action on a block, or null where nothing can.</summary>
        public Func<T, TOptions, ReaderMemory, BlockTrouble> Troubled { get; private set; }

        /// <summary>What stops the action on a block part way, or null where stopping does nothing.</summary>
        public Action<T> Stops { get; private set; }

        /// <summary>What stops the action on a block, told why, or null where <see cref="Stops" /> does.</summary>
        public Action<T, SprocketCancelCause> StopsFor { get; set; }

        /// <summary>Whether the action is forced by its options, or null where it never is.</summary>
        public Func<TOptions, bool> Forced { get; private set; }

        /// <summary>What the action is told of every block of a step together, as the step starts, or null where it needs nothing.</summary>
        public Action<IList<T>, TOptions> Begun { get; private set; }

        /// <summary>How many ticks the action is expected to take on a block, or null where it cannot say.</summary>
        public Func<T, TOptions, int> Takes { get; private set; }

        /// <inheritdoc />
        public override Reading Until => _until;

        /// <inheritdoc />
        public override BoundAction Bind(IList<Literal> arguments)
        {
            return new GivenOwnAction<T, TOptions>(this, _options(arguments));
        }
    }
}
