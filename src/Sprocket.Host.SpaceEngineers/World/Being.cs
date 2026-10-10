using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>What one of Sprocket's own values reads off a block, and how a change in it is heard.</summary>
    /// <remarks>
    /// Declared once, beside the value, by what knows the block, as <see cref="Doing" /> is for an action, and never changed
    /// after: how a change in it is heard belongs to the value a kind lists, <see cref="EntityAction.Announced" />. A function is a
    /// value with options of its own: it reads them into a type of its own when a test is compiled, and is handed that
    /// from then on. See <see cref="Gauge" />.
    /// </remarks>
    public abstract class Being
    {
        static readonly string[] None = Array.Empty<string>();

        readonly List<Field> _parameters = new List<Field>();

        Action<IMyTerminalBlock, Literal> _write;

        /// <summary>What a function is given, one field per option in the order they are written; empty for a value.</summary>
        public IReadOnlyList<Field> Parameters => _parameters;

        /// <summary>Whether a step can set it, with <c>set</c>.</summary>
        public bool Settable => _write != null;

        /// <summary>The kind of value it gives back.</summary>
        public abstract ValueKind Kind { get; }

        /// <summary>The names a "one of" value is one of, in order; empty for any other kind.</summary>
        public abstract IReadOnlyList<string> Names { get; }

        /// <summary>Whether a reading needs a <see cref="Recall" /> of its own for each block it reads.</summary>
        public abstract bool Remembers { get; }

        /// <summary>The value on <paramref name="block" /> as a test given <paramref name="arguments" /> reads it, made ready once.</summary>
        /// <remarks>Asked when a test is compiled, and never while it runs. <paramref name="seen" /> is the block's own, or null where this remembers nothing.</remarks>
        public abstract Gauge Gauge(IMyTerminalBlock block, IList<Literal> arguments, Recall seen);

        /// <summary>The flag that the value, read with no arguments, is <paramref name="becomes" />: on or off for a flag, a name for one of a list.</summary>
        /// <remarks>What a signal that is a change in this value is a change of: a door finishing closing is its status coming to be closed.</remarks>
        public abstract Reading Is(Literal becomes);

        /// <summary>Says this is a function, given an argument for each of <paramref name="parameters" />.</summary>
        /// <remarks>Part of making it, in the same initializer: a Being is shared by every kind that lists it.</remarks>
        public Being Taking(params Field[] parameters)
        {
            _parameters.AddRange(parameters);
            return this;
        }

        /// <summary>
        /// Says a step can set this, by <paramref name="write" />, handed the value as the kind it is: on or off, a number,
        /// text, a color, and a one of a list as its position among the names.
        /// </summary>
        /// <remarks>Part of making it, as <see cref="Taking" /> is. Only what a player could set at the block in the game.</remarks>
        public Being Writing<T>(Action<T, Literal> write) where T : class
        {
            _write = (block, value) => write((T)block, value);
            return this;
        }

        /// <summary>
        /// What sets this on <paramref name="block" /> to <paramref name="value" />, made ready once; null where it cannot be
        /// set, or a one of a list is given none of its names.
        /// </summary>
        /// <remarks>Made when a step is compiled, and given when it runs, which only calls the writer.</remarks>
        public Order Order(IMyTerminalBlock block, Literal value)
        {
            if (_write == null || block == null)
                return null;

            if (Kind == ValueKind.Name)
            {
                var at = Position(value.Text);
                if (at < 0)
                    return null;

                value = Literal.Of(at);
            }

            return new Setting(_write, block, value);
        }

        /// <summary>Where <paramref name="name" /> stands among the names, or -1 where it is none of them.</summary>
        public int Position(string name)
        {
            var names = Names;
            for (var i = 0; i < names.Count; i++)
            {
                if (names[i] == name)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// What the first parameter <paramref name="given" /> arguments leave out is called; empty where nothing names it, and
        /// null where none is missing.
        /// </summary>
        /// <remarks>A function is given an argument for every parameter it declares.</remarks>
        public string Missing(int given)
        {
            var covered = 0;
            for (var i = 0; i < _parameters.Count; i++)
            {
                covered += _parameters[i].Covers;
                if (given < covered)
                    return _parameters[i].Name ?? "";
            }

            return null;
        }

        /// <summary>
        /// What the first of <paramref name="arguments" /> it cannot take is called; empty where nothing names it or it is one
        /// too many, and null where it takes them all.
        /// </summary>
        /// <remarks>
        /// An argument is taken where it is the kind its field is for, one of the field's words where it lists some, and
        /// within its bounds where it has them. Asked when a test is checked or compiled, never while it runs.
        /// </remarks>
        public string Refused(IList<Literal> arguments)
        {
            var at = 0;
            foreach (var field in _parameters)
            {
                if (field.Covers != 1)
                {
                    at += field.Covers;
                    continue;
                }

                if (at < arguments.Count && !Takes(field, arguments[at]))
                    return field.Name ?? "";

                at++;
            }

            return arguments.Count > at ? "" : null;
        }

        /// <summary>Whether <paramref name="field" /> takes <paramref name="given" />: its kind, one of its words, within its bounds.</summary>
        static bool Takes(Field field, Literal given)
        {
            ValueKind kind;
            if (EntityAction.Kind(field.Entry, out kind) && !given.Becomes(kind, out given))
                return false;

            return field.Admits(given, true);
        }

        /// <summary>A flag of Sprocket's own, read by <paramref name="flag" />.</summary>
        public static Being Flag(Reading flag)
        {
            return new Flagged(flag);
        }

        /// <summary>One of <paramref name="names" />, read off a <typeparamref name="T" /> as a position among them.</summary>
        public static Being OneOf<T>(string[] names, Func<T, int> position) where T : class
        {
            return new Typed<T, object>(ValueKind.Name, names, false, Nothing,
                (block, options, seen) => Literal.Of(position(block)));
        }

        /// <summary>One of <paramref name="names" />, read off a <typeparamref name="T" /> as a position among them, remembering between looks.</summary>
        public static Being OneOf<T>(string[] names, Func<T, Recall, int> position) where T : class
        {
            return new Typed<T, object>(ValueKind.Name, names, true, Nothing,
                (block, options, seen) => Literal.Of(position(block, seen)));
        }

        /// <summary>A real number read off a <typeparamref name="T" />.</summary>
        public static Being Number<T>(Func<T, double> number) where T : class
        {
            return new Typed<T, object>(ValueKind.Real, None, false, Nothing,
                (block, options, seen) => Literal.Of(number(block)));
        }

        /// <summary>A real number read off a <typeparamref name="T" />, remembering between looks.</summary>
        public static Being Number<T>(Func<T, Recall, double> number) where T : class
        {
            return new Typed<T, object>(ValueKind.Real, None, true, Nothing,
                (block, options, seen) => Literal.Of(number(block, seen)));
        }

        /// <summary>A whole number read off a <typeparamref name="T" />.</summary>
        public static Being Whole<T>(Func<T, long> whole) where T : class
        {
            return new Typed<T, object>(ValueKind.Integer, None, false, Nothing,
                (block, options, seen) => Literal.Of(whole(block)));
        }

        /// <summary>Text read off a <typeparamref name="T" />.</summary>
        public static Being Text<T>(Func<T, string> text) where T : class
        {
            return new Typed<T, object>(ValueKind.Text, None, false, Nothing,
                (block, options, seen) => Literal.Of(text(block) ?? ""));
        }

        /// <summary>A color read off a <typeparamref name="T" />.</summary>
        public static Being Color<T>(Func<T, Color> color) where T : class
        {
            return new Typed<T, object>(ValueKind.Color, None, false, Nothing, (block, options, seen) =>
            {
                var read = color(block);
                return Literal.Colour(read.R, read.G, read.B, read.A);
            });
        }

        /// <summary>A function of a <typeparamref name="T" /> giving back a real number, with options read into a <typeparamref name="TOptions" />.</summary>
        public static Being Number<T, TOptions>(Func<IList<Literal>, TOptions> options, Func<T, TOptions, Recall, double> number)
            where T : class
        {
            return new Typed<T, TOptions>(ValueKind.Real, None, true, options,
                (block, given, seen) => Literal.Of(number(block, given, seen)));
        }

        /// <summary>A function of a <typeparamref name="T" /> giving back one of <paramref name="names" /> as a position among them, with options read into a <typeparamref name="TOptions" />.</summary>
        public static Being OneOf<T, TOptions>(string[] names, Func<IList<Literal>, TOptions> options,
            Func<T, TOptions, Recall, int> position) where T : class
        {
            return new Typed<T, TOptions>(ValueKind.Name, names, true, options,
                (block, given, seen) => Literal.Of(position(block, given, seen)));
        }

        /// <summary>A function of a <typeparamref name="T" /> giving back a color, with options read into a <typeparamref name="TOptions" />.</summary>
        public static Being Color<T, TOptions>(Func<IList<Literal>, TOptions> options, Func<T, TOptions, Recall, Color> color)
            where T : class
        {
            return new Typed<T, TOptions>(ValueKind.Color, None, true, options, (block, given, seen) =>
            {
                var read = color(block, given, seen);
                return Literal.Colour(read.R, read.G, read.B, read.A);
            });
        }

        /// <summary>A function of a <typeparamref name="T" /> giving back a flag, with options read into a <typeparamref name="TOptions" />.</summary>
        public static Being Flag<T, TOptions>(Func<IList<Literal>, TOptions> options, Func<T, TOptions, Recall, bool> flag)
            where T : class
        {
            return new Typed<T, TOptions>(ValueKind.Flag, None, true, options,
                (block, given, seen) => Literal.Of(flag(block, given, seen)));
        }

        /// <summary>A function of a <typeparamref name="T" /> giving back a whole number, with options read into a <typeparamref name="TOptions" />.</summary>
        public static Being Whole<T, TOptions>(Func<IList<Literal>, TOptions> options, Func<T, TOptions, Recall, long> whole)
            where T : class
        {
            return new Typed<T, TOptions>(ValueKind.Integer, None, true, options,
                (block, given, seen) => Literal.Of(whole(block, given, seen)));
        }

        /// <summary>A function of a <typeparamref name="T" /> giving back text, with options read into a <typeparamref name="TOptions" />.</summary>
        public static Being Text<T, TOptions>(Func<IList<Literal>, TOptions> options, Func<T, TOptions, Recall, string> text)
            where T : class
        {
            return new Typed<T, TOptions>(ValueKind.Text, None, true, options,
                (block, given, seen) => Literal.Of(text(block, given, seen) ?? ""));
        }

        static object Nothing(IList<Literal> arguments)
        {
            return null;
        }

        /// <summary>A flag read by a <see cref="Reading" />.</summary>
        sealed class Flagged : Being
        {
            readonly Reading _flag;

            public Flagged(Reading flag)
            {
                _flag = flag;
            }

            public override ValueKind Kind => ValueKind.Flag;

            public override IReadOnlyList<string> Names => None;

            public override bool Remembers => _flag.Remembers;

            public override Gauge Gauge(IMyTerminalBlock block, IList<Literal> arguments, Recall seen)
            {
                return new FlagGauge(_flag, block, seen);
            }

            public override Reading Is(Literal becomes)
            {
                return becomes.Flag ? _flag : new Not(_flag);
            }
        }

        /// <summary>A value read off a <typeparamref name="T" />, with options read into a <typeparamref name="TOptions" />.</summary>
        sealed class Typed<T, TOptions> : Being where T : class
        {
            readonly ValueKind _kind;
            readonly string[] _names;
            readonly Func<IList<Literal>, TOptions> _options;
            readonly Func<T, TOptions, Recall, Literal> _read;

            public Typed(ValueKind kind, string[] names, bool remembers, Func<IList<Literal>, TOptions> options,
                Func<T, TOptions, Recall, Literal> read)
            {
                _kind = kind;
                _names = names;
                Remembers = remembers;
                _options = options;
                _read = read;
            }

            public override ValueKind Kind => _kind;

            public override IReadOnlyList<string> Names => _names;

            public override bool Remembers { get; }

            public override Gauge Gauge(IMyTerminalBlock block, IList<Literal> arguments, Recall seen)
            {
                return new Measured<T, TOptions>(this, _options(arguments), block, seen);
            }

            /// <remarks>
            /// A one of a list is compared by its position among the names, worked out here, once. A name that is none of
            /// them is a mistake in the declaration, and stops it.
            /// </remarks>
            public override Reading Is(Literal becomes)
            {
                var operand = becomes;
                if (_kind == ValueKind.Name)
                {
                    var at = Array.IndexOf(_names, becomes.Text);
                    if (at < 0)
                        throw new ArgumentException("'" + becomes.Text + "' is none of " + string.Join(", ", _names) + ".");

                    operand = Literal.Of(at);
                }

                return new Equal<T, TOptions>(this, operand);
            }

            public Literal Read(T block, TOptions options, Recall seen)
            {
                return _read(block, options, seen);
            }

            public TOptions Options(IList<Literal> arguments)
            {
                return _options(arguments);
            }
        }

        /// <summary>One block told what this is to be, by one step.</summary>
        sealed class Setting : Order
        {
            readonly Action<IMyTerminalBlock, Literal> _write;
            readonly IMyTerminalBlock _block;
            readonly Literal _value;

            public Setting(Action<IMyTerminalBlock, Literal> write, IMyTerminalBlock block, Literal value)
            {
                _write = write;
                _block = block;
                _value = value;
            }

            public override void Give()
            {
                _write(_block, _value);
            }
        }

        /// <summary>A value of a block read as it reads now, with the options one test gave it.</summary>
        /// <remarks>The cast never fails: a test is compiled only against blocks of the kind that declared the value.</remarks>
        sealed class Measured<T, TOptions> : Gauge where T : class
        {
            readonly Typed<T, TOptions> _being;
            readonly TOptions _options;
            readonly T _block;
            readonly Recall _seen;

            public Measured(Typed<T, TOptions> being, TOptions options, IMyTerminalBlock block, Recall seen)
            {
                _being = being;
                _options = options;
                _block = (T)block;
                _seen = seen;
            }

            /// <remarks>A one of a list reads as its position, so a comparison on a tick compares whole numbers.</remarks>
            public override ValueKind Kind => _being.Kind == ValueKind.Name ? ValueKind.Integer : _being.Kind;

            public override Literal Read()
            {
                return _being.Read(_block, _options, _seen);
            }
        }

        /// <summary>A flag of a block read as it reads now.</summary>
        sealed class FlagGauge : Gauge
        {
            readonly Reading _flag;
            readonly IMyTerminalBlock _block;
            readonly Recall _seen;

            public FlagGauge(Reading flag, IMyTerminalBlock block, Recall seen)
            {
                _flag = flag;
                _block = block;
                _seen = seen;
            }

            public override ValueKind Kind => ValueKind.Flag;

            public override Literal Read()
            {
                return Literal.Of(_flag.Holds(_block, _seen));
            }
        }

        /// <summary>Whether a value read with no options is one thing, as a flag.</summary>
        sealed class Equal<T, TOptions> : Reading where T : class
        {
            readonly Typed<T, TOptions> _being;
            readonly TOptions _options;
            readonly Literal _operand;

            public Equal(Typed<T, TOptions> being, Literal operand)
            {
                _being = being;
                _options = being.Options(Array.Empty<Literal>());
                _operand = operand;
            }

            public override bool Remembers => _being.Remembers;

            public override bool Holds(IMyTerminalBlock block, Recall seen)
            {
                return Relations.Holds(_being.Read((T)block, _options, seen), Relation.Is, _operand);
            }
        }

        /// <summary>A flag read the other way round.</summary>
        sealed class Not : Reading
        {
            readonly Reading _flag;

            public Not(Reading flag)
            {
                _flag = flag;
            }

            public override bool Remembers => _flag.Remembers;

            public override bool Holds(IMyTerminalBlock block, Recall seen)
            {
                return !_flag.Holds(block, seen);
            }
        }
    }
}
