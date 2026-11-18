using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>What one of Sprocket's own values reads off a block, and how a change in it is heard.</summary>
    public abstract class OwnValue
    {
        Func<IMyTerminalBlock, IList<Literal>, bool> _answers;

        static readonly string[] None = Array.Empty<string>();

        List<Field> _parameters = new List<Field>();

        Action<IMyTerminalBlock, Literal> _write;
        IReadOnlyList<string> _said;

        /// <summary>What a function is given, one field per option in the order they are written; empty for a value.</summary>
        public IReadOnlyList<Field> Parameters => _parameters;

        /// <summary>Whether a step can set it, with <c>set</c>.</summary>
        public bool Settable => _write != null;

        /// <summary>The kind of value it gives back.</summary>
        public abstract ValueKind Kind { get; }

        /// <summary>The names a "one of" value is one of, in order, or the ids a text value offers; empty for any other.</summary>
        public abstract IReadOnlyList<string> Names { get; }

        /// <summary>The type whose choices a "one of" value gives, or null for any other kind.</summary>
        public ChoiceType Choices { get; private set; }

        /// <summary>The words a person reads for <see cref="Names" />, one each in order, or null where each name is read as it is written.</summary>
        public IReadOnlyList<string> Said => _said;

        /// <summary>Whether a reading needs a <see cref="ReaderMemory" /> of its own for each block it reads.</summary>
        public abstract bool Remembers { get; }

        /// <summary>The value on <paramref name="block" /> as a test given <paramref name="arguments" /> reads it, made ready once.</summary>
        /// <remarks>Asked when a test is compiled, and never while it runs. <paramref name="seen" /> is the block's own, or null where this remembers nothing.</remarks>
        public abstract Gauge Gauge(IMyTerminalBlock block, IList<Literal> arguments, ReaderMemory seen);

        /// <summary>The flag that the value, read with no arguments, is <paramref name="becomes" />: on or off for a flag, a name for one of a list.</summary>
        public abstract Reading Is(Literal becomes);

        /// <summary>Says this is a function, given an argument for each of <paramref name="parameters" />.</summary>
        /// <remarks>Called only in the initializer that makes it: an OwnValue is shared by every kind that lists it.</remarks>
        public OwnValue Taking(params Field[] parameters)
        {
            _parameters.AddRange(parameters);
            return this;
        }

        /// <summary>The word joining this value to its blocks where no part of them is named, or null for the usual "on".</summary>
        public string Join { get; private set; }

        /// <summary>The word joining this value to its blocks where a part of them is named, or null for <see cref="Join" />.</summary>
        public string PartJoin { get; private set; }

        /// <summary>The name of the option whose answer decides this value's unit, or null where its unit is fixed.</summary>
        public string UnitFollows { get; private set; }

        /// <summary>This value's unit for an answer to the option <see cref="UnitFollows" /> names, for one where the flag says so.</summary>
        public Func<string, bool, string> UnitOf { get; private set; }

        /// <summary>Says this value's unit follows the answer to the option <paramref name="follows" />, by <paramref name="unitOf" />.</summary>
        /// <remarks>Called only in the initializer that makes it.</remarks>
        public OwnValue MeasuredBy(string follows, Func<string, bool, string> unitOf)
        {
            UnitFollows = follows;
            UnitOf = unitOf;
            return this;
        }

        /// <summary>A copy of this value that asks for each part of its blocks by number.</summary>
        public OwnValue WithPartsNumbered()
        {
            var copy = (OwnValue)MemberwiseClone();
            copy._parameters = new List<Field>(_parameters.Count);
            foreach (var field in _parameters)
                copy._parameters.Add(field.IsPart ? field.Numbered() : field);

            return copy;
        }

        /// <summary>Says how a sentence joins this value to its blocks: <paramref name="join" />, or <paramref name="partJoin" /> where it names a part of them.</summary>
        /// <remarks>Called only in the initializer that makes it.</remarks>
        public OwnValue Joined(string join, string partJoin)
        {
            Join = join;
            PartJoin = partJoin;
            return this;
        }

        /// <summary>Says a block of a set may be unable to answer: one <paramref name="answers" /> says no of is passed over.</summary>
        /// <remarks>Called only in the initializer that makes it.</remarks>
        public OwnValue AnsweredBy<T>(Func<T, IList<Literal>, bool> answers) where T : class
        {
            _answers = (block, given) =>
            {
                var read = block as T;
                return read != null && answers(read, given);
            };
            return this;
        }

        /// <summary>Whether <paramref name="block" /> can answer this value read with <paramref name="arguments" />.</summary>
        public bool Answers(IMyTerminalBlock block, IList<Literal> arguments)
        {
            return _answers == null || _answers(block, arguments);
        }

        /// <summary>Says a person reads <paramref name="words" /> for the names, one each in order, where the program names things by id.</summary>
        /// <remarks>Called only in the initializer that makes it.</remarks>
        public OwnValue Saying(IReadOnlyList<string> words)
        {
            _said = words;
            return this;
        }

        /// <summary>
        /// Says a step can set this, by <paramref name="write" />, handed the value as the kind it is: on or off, a number,
        /// text, a color, and a one of a list as its position among the names.
        /// </summary>
        /// <remarks>Called only in the initializer that makes it.</remarks>
        public OwnValue Writing<T>(Action<T, Literal> write) where T : class
        {
            _write = (block, value) => write((T)block, value);
            return this;
        }

        /// <summary>
        /// What sets this on <paramref name="block" /> to <paramref name="value" />, made ready once; null where it cannot be
        /// set, or a one of a list is given none of its names.
        /// </summary>
        /// <remarks>Asked when a step is compiled, and never while it runs.</remarks>
        public BlockOrder Order(IMyTerminalBlock block, Literal value)
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

            return new OwnValueSetting(_write, block, value);
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
        /// <remarks>Asked when a test is checked or compiled, and never while it runs.</remarks>
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
        public static OwnValue Flag(Reading flag)
        {
            return new FlagOwnValue(flag);
        }

        /// <summary>One of the choices of <paramref name="type" />, read off a <typeparamref name="T" /> as a position among them.</summary>
        public static OwnValue OneOf<T>(ChoiceType type, Func<T, int> position) where T : class
        {
            return Of(type, new TypedOwnValue<T, object>(ValueKind.Name, type.Choices, false, Nothing,
                (block, options, seen) => Literal.Of(position(block))));
        }

        /// <summary>One of the choices of <paramref name="type" />, read off a <typeparamref name="T" /> as a position among them, remembering between looks.</summary>
        public static OwnValue OneOf<T>(ChoiceType type, Func<T, ReaderMemory, int> position) where T : class
        {
            return Of(type, new TypedOwnValue<T, object>(ValueKind.Name, type.Choices, true, Nothing,
                (block, options, seen) => Literal.Of(position(block, seen))));
        }

        /// <summary><paramref name="value" />, its choices those of <paramref name="type" />.</summary>
        static OwnValue Of(ChoiceType type, OwnValue value)
        {
            value.Choices = type;
            return value;
        }

        /// <summary>A real number read off a <typeparamref name="T" />.</summary>
        public static OwnValue Number<T>(Func<T, double> number) where T : class
        {
            return new TypedOwnValue<T, object>(ValueKind.Real, None, false, Nothing,
                (block, options, seen) => Literal.Of(number(block)));
        }

        /// <summary>A real number read off a <typeparamref name="T" />, remembering between looks.</summary>
        public static OwnValue Number<T>(Func<T, ReaderMemory, double> number) where T : class
        {
            return new TypedOwnValue<T, object>(ValueKind.Real, None, true, Nothing,
                (block, options, seen) => Literal.Of(number(block, seen)));
        }

        /// <summary>A whole number read off a <typeparamref name="T" />.</summary>
        public static OwnValue Whole<T>(Func<T, long> whole) where T : class
        {
            return new TypedOwnValue<T, object>(ValueKind.Integer, None, false, Nothing,
                (block, options, seen) => Literal.Of(whole(block)));
        }

        /// <summary>Text read off a <typeparamref name="T" />.</summary>
        public static OwnValue Text<T>(Func<T, string> text) where T : class
        {
            return new TypedOwnValue<T, object>(ValueKind.Text, None, false, Nothing,
                (block, options, seen) => Literal.Of(text(block) ?? ""));
        }

        /// <summary>Text read off a <typeparamref name="T" />, an id the game gives one of <paramref name="offered" /> for, which an editor offers to choose among.</summary>
        public static OwnValue TextAmong<T>(string[] offered, Func<T, string> text) where T : class
        {
            return new TypedOwnValue<T, object>(ValueKind.Text, offered, false, Nothing,
                (block, options, seen) => Literal.Of(text(block) ?? ""));
        }

        /// <summary>Text read off a <typeparamref name="T" /> with options read into a <typeparamref name="TOptions" />, an id the game gives one of <paramref name="offered" /> for, which an editor offers to choose among.</summary>
        public static OwnValue TextAmong<T, TOptions>(string[] offered, Func<IList<Literal>, TOptions> options, Func<T, TOptions, string> text)
            where T : class
        {
            return new TypedOwnValue<T, TOptions>(ValueKind.Text, offered, false, options,
                (block, given, seen) => Literal.Of(text(block, given) ?? ""));
        }

        /// <summary>A color read off a <typeparamref name="T" />.</summary>
        public static OwnValue Color<T>(Func<T, Color> color) where T : class
        {
            return new TypedOwnValue<T, object>(ValueKind.Color, None, false, Nothing, (block, options, seen) =>
            {
                var read = color(block);
                return Literal.Color(read.R, read.G, read.B, read.A);
            });
        }

        /// <summary>A function of a <typeparamref name="T" /> giving back a real number, with options read into a <typeparamref name="TOptions" />.</summary>
        public static OwnValue Number<T, TOptions>(Func<IList<Literal>, TOptions> options, Func<T, TOptions, double> number)
            where T : class
        {
            return new TypedOwnValue<T, TOptions>(ValueKind.Real, None, false, options,
                (block, given, seen) => Literal.Of(number(block, given)));
        }

        /// <summary>A function of a <typeparamref name="T" /> giving back one of the choices of <paramref name="type" /> as a position among them, with options read into a <typeparamref name="TOptions" />.</summary>
        public static OwnValue OneOf<T, TOptions>(ChoiceType type, Func<IList<Literal>, TOptions> options,
            Func<T, TOptions, int> position) where T : class
        {
            return Of(type, new TypedOwnValue<T, TOptions>(ValueKind.Name, type.Choices, false, options,
                (block, given, seen) => Literal.Of(position(block, given))));
        }

        /// <summary>A function of a <typeparamref name="T" /> giving back a color, with options read into a <typeparamref name="TOptions" />.</summary>
        public static OwnValue Color<T, TOptions>(Func<IList<Literal>, TOptions> options, Func<T, TOptions, Color> color)
            where T : class
        {
            return new TypedOwnValue<T, TOptions>(ValueKind.Color, None, false, options, (block, given, seen) =>
            {
                var read = color(block, given);
                return Literal.Color(read.R, read.G, read.B, read.A);
            });
        }

        /// <summary>A function of a <typeparamref name="T" /> giving back a flag, with options read into a <typeparamref name="TOptions" />.</summary>
        public static OwnValue Flag<T, TOptions>(Func<IList<Literal>, TOptions> options, Func<T, TOptions, bool> flag)
            where T : class
        {
            return new TypedOwnValue<T, TOptions>(ValueKind.Flag, None, false, options,
                (block, given, seen) => Literal.Of(flag(block, given)));
        }

        /// <summary>A function of a <typeparamref name="T" /> giving back a whole number, with options read into a <typeparamref name="TOptions" />.</summary>
        public static OwnValue Whole<T, TOptions>(Func<IList<Literal>, TOptions> options, Func<T, TOptions, long> whole)
            where T : class
        {
            return new TypedOwnValue<T, TOptions>(ValueKind.Integer, None, false, options,
                (block, given, seen) => Literal.Of(whole(block, given)));
        }

        /// <summary>A function of a <typeparamref name="T" /> giving back text, with options read into a <typeparamref name="TOptions" />.</summary>
        public static OwnValue Text<T, TOptions>(Func<IList<Literal>, TOptions> options, Func<T, TOptions, string> text)
            where T : class
        {
            return new TypedOwnValue<T, TOptions>(ValueKind.Text, None, false, options,
                (block, given, seen) => Literal.Of(text(block, given) ?? ""));
        }

        static object Nothing(IList<Literal> arguments)
        {
            return null;
        }
    }
}
