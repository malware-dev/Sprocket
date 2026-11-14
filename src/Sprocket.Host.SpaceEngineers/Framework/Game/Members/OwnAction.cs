using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
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
            return new TypedOwnAction<T, object>(
                Nothing,
                does == null ? null : new Action<T, object, ReaderMemory>((block, options, seen) => does(block)),
                until,
                null,
                trouble == null ? null : new Func<T, object, ReaderMemory, BlockTrouble>((block, options, seen) => trouble(block)),
                stop,
                expected == null ? null : new Func<T, object, int>((block, options) => expected(block)));
        }

        /// <summary>An action on a <typeparamref name="T" /> that takes no options, whose <paramref name="trouble" /> is judged against what was seen of the block before.</summary>
        /// <remarks>Where <paramref name="does" /> is null, the game's own action the entry names does it.</remarks>
        public static OwnAction Watched<T>(Action<T> does, Reading until, Func<T, ReaderMemory, BlockTrouble> trouble, Action<T> stop,
            Func<T, int> expected) where T : class
        {
            return new TypedOwnAction<T, object>(
                Nothing,
                does == null ? null : new Action<T, object, ReaderMemory>((block, options, seen) => does(block)),
                until,
                null,
                (block, options, seen) => trouble(block, seen),
                stop,
                (block, options) => expected(block));
        }

        /// <summary>An action on a <typeparamref name="T" /> with options of its own, read into a <typeparamref name="TOptions" />.</summary>
        public static OwnAction Of<T, TOptions>(Func<IList<Literal>, TOptions> options, Action<T, TOptions, ReaderMemory> does = null,
            Reading until = null, Func<T, TOptions, ReaderMemory, bool> done = null, Func<T, TOptions, ReaderMemory, BlockTrouble> trouble = null,
            Action<T> stop = null, Func<T, TOptions, int> expected = null, Func<TOptions, bool> forced = null) where T : class
        {
            return new TypedOwnAction<T, TOptions>(options, does, until, done, trouble, stop, expected, forced);
        }

        static object Nothing(IList<Literal> arguments)
        {
            return null;
        }
    }
}
