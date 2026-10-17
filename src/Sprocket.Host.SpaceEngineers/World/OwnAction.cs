using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>What one of Sprocket's own actions does to a block, what it waits for, and how it finishes.</summary>
    public abstract class OwnAction
    {
        /// <summary>What the action waits for, or null where it is done the moment it is told.</summary>
        public abstract Reading Until { get; }

        /// <summary>The action as a step gives it <paramref name="arguments" />, already in the kinds its fields take.</summary>
        /// <remarks>Asked when a step is compiled, and never while it runs.</remarks>
        public abstract BoundAction Bind(IList<Literal> arguments);

        /// <summary>An action on a <typeparamref name="T" /> that takes no options.</summary>
        /// <remarks>Where <paramref name="does" /> is null, the game's own action the entry names does it.</remarks>
        public static OwnAction Of<T>(Action<T> does = null, Reading until = null, Func<T, BlockTrouble> trouble = null,
            Action<T> stop = null, Func<T, int> expected = null) where T : class
        {
            return new Typed<T, object>(
                Nothing,
                does == null ? null : new Action<T, object, ReaderMemory>((block, options, seen) => does(block)),
                until,
                null,
                trouble == null ? null : new Func<T, object, ReaderMemory, BlockTrouble>((block, options, seen) => trouble(block)),
                stop,
                expected == null ? null : new Func<T, object, int>((block, options) => expected(block)));
        }

        /// <summary>An action on a <typeparamref name="T" /> with options of its own, read into a <typeparamref name="TOptions" />.</summary>
        public static OwnAction Of<T, TOptions>(Func<IList<Literal>, TOptions> options, Action<T, TOptions, ReaderMemory> does = null,
            Reading until = null, Func<T, TOptions, ReaderMemory, bool> done = null, Func<T, TOptions, ReaderMemory, BlockTrouble> trouble = null,
            Action<T> stop = null, Func<T, TOptions, int> expected = null) where T : class
        {
            return new Typed<T, TOptions>(options, does, until, done, trouble, stop, expected);
        }

        static object Nothing(IList<Literal> arguments)
        {
            return null;
        }

        /// <summary>An action on a <typeparamref name="T" />, with options read into a <typeparamref name="TOptions" />.</summary>
        sealed class Typed<T, TOptions> : OwnAction where T : class
        {
            readonly Func<IList<Literal>, TOptions> _options;
            readonly Reading _until;

            public Typed(Func<IList<Literal>, TOptions> options, Action<T, TOptions, ReaderMemory> does, Reading until,
                Func<T, TOptions, ReaderMemory, bool> done, Func<T, TOptions, ReaderMemory, BlockTrouble> trouble, Action<T> stop,
                Func<T, TOptions, int> expected)
            {
                _options = options;
                _until = until;
                Does = does;
                Finished = done;
                Troubled = trouble;
                Stops = stop;
                Takes = expected;
            }

            public Action<T, TOptions, ReaderMemory> Does { get; private set; }

            public Func<T, TOptions, ReaderMemory, bool> Finished { get; private set; }

            public Func<T, TOptions, ReaderMemory, BlockTrouble> Troubled { get; private set; }

            public Action<T> Stops { get; private set; }

            public Func<T, TOptions, int> Takes { get; private set; }

            public override Reading Until => _until;

            public override BoundAction Bind(IList<Literal> arguments)
            {
                return new Given<T, TOptions>(this, _options(arguments));
            }
        }

        /// <summary>An action with the options one step gave it.</summary>
        sealed class Given<T, TOptions> : BoundAction where T : class
        {
            readonly Typed<T, TOptions> _doing;
            readonly TOptions _options;

            public Given(Typed<T, TOptions> doing, TOptions options)
            {
                _doing = doing;
                _options = options;
            }

            public override bool Acts => _doing.Does != null;

            public override void Do(IMyTerminalBlock block, ReaderMemory seen)
            {
                if (_doing.Does != null)
                    _doing.Does((T)block, _options, seen);
            }

            public override bool Done(IMyTerminalBlock block, ReaderMemory seen)
            {
                return _doing.Finished != null && _doing.Finished((T)block, _options, seen);
            }

            public override BlockTrouble Trouble(IMyTerminalBlock block, ReaderMemory seen)
            {
                return _doing.Troubled?.Invoke((T)block, _options, seen) ?? BlockTrouble.None;
            }

            public override void Stop(IMyTerminalBlock block)
            {
                if (_doing.Stops != null)
                    _doing.Stops((T)block);
            }

            public override int Expected(IMyTerminalBlock block)
            {
                return _doing.Takes?.Invoke((T)block, _options) ?? GridEntity.Unestimated;
            }
        }
    }
}
