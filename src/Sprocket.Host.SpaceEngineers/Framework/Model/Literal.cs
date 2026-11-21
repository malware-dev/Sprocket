using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// A value written down in a program, carrying what kind of value it is.
    /// </summary>
    /// <remarks>Made when somebody writes a sequence, and never on a tick.</remarks>
    public struct Literal
    {
        // The text of a length of time written in seconds.
        const string InSeconds = "s";

        Literal(ValueKind kind, EntityValue value, string text) : this()
        {
            Kind = kind;
            Value = value;
            Text = text ?? "";
        }

        /// <summary>A flag, on or off.</summary>
        public static Literal Of(bool flag)
        {
            return new Literal(ValueKind.Flag, EntityValue.Of(flag), null);
        }

        /// <summary>An integer.</summary>
        public static Literal Of(long integer)
        {
            return new Literal(ValueKind.Integer, EntityValue.Of(integer), null);
        }

        /// <summary>A real number.</summary>
        public static Literal Of(double real)
        {
            return new Literal(ValueKind.Real, EntityValue.Of(real), null);
        }

        /// <summary>Text.</summary>
        public static Literal Of(string text)
        {
            return new Literal(ValueKind.Text, new EntityValue(0), text);
        }

        /// <summary>One of a list of names a member gives, written as the name.</summary>
        public static Literal Name(string name)
        {
            return new Literal(ValueKind.Name, new EntityValue(0), name);
        }

        /// <summary>One of the choices of the host type <paramref name="typed" />, written with it: <c>door_status(closed)</c>.</summary>
        public static Literal Name(string name, ChoiceType typed)
        {
            var named = Name(name);
            named.Typed = typed;
            return named;
        }

        /// <summary>The slots <paramref name="names" /> name, kept as that list, for a parameter that takes slots.</summary>
        public static Literal Slots(IList<string> names)
        {
            var slots = new Literal(ValueKind.Slots, new EntityValue(0), null);
            slots.Names = names;
            return slots;
        }

        /// <summary>The slots <paramref name="names" /> name, kept as that list, with what they hold in a step that has been worked out: <paramref name="held" />.</summary>
        public static Literal Reaching(IList<string> names, IList<HeldThing> held)
        {
            var slots = Slots(names);
            slots.Held = held;
            return slots;
        }

        /// <summary>A color, by its red, green, blue and alpha.</summary>
        public static Literal Color(byte r, byte g, byte b, byte a)
        {
            return new Literal(ValueKind.Color, EntityValue.Of(r, g, b, a), null);
        }

        /// <summary>A length of time, kept in the unit it was written in.</summary>
        /// <remarks>Its ticks where it is in ticks, and its seconds where it is in seconds, marked by the text <see cref="InSeconds" />.</remarks>
        public static Literal Of(Duration length)
        {
            return length.InSeconds
                ? new Literal(ValueKind.Duration, EntityValue.Of(length.Seconds), InSeconds)
                : new Literal(ValueKind.Duration, EntityValue.Of(length.Ticks), null);
        }

        /// <summary>
        /// The setting called <paramref name="name" />, standing in for the value each installation gives it, written with its
        /// kind, <c>setting('Open angle')</c>, where <paramref name="kinded" /> says.
        /// </summary>
        /// <remarks>Where it stands among a member's values the spelling is kept, since only the member's blocks say whether alone it could be one of its words.</remarks>
        public static Literal Setting(string name, bool kinded = false)
        {
            return new Literal(ValueKind.Setting, new EntityValue(kinded ? 1 : 0), name);
        }

        /// <summary>An expression, worked out when the step holding it is reached.</summary>
        /// <remarks>A plain literal is never wrapped: only a value that holds an operator, a function or a reading is an expression.</remarks>
        public static Literal Worked(Expression tree)
        {
            var worked = new Literal(ValueKind.Expression, new EntityValue(0), null);
            worked.Tree = tree;
            return worked;
        }

        /// <summary>Whether any of <paramref name="given" /> is an expression.</summary>
        public static bool HoldsExpression(IList<Literal> given)
        {
            for (var i = 0; i < given.Count; i++)
            {
                if (given[i].IsExpression)
                    return true;
            }

            return false;
        }

        /// <summary>The kind of value it is.</summary>
        public ValueKind Kind { get; private set; }

        /// <summary>The expression, for <see cref="ValueKind.Expression" />, and null for the rest.</summary>
        public Expression Tree { get; private set; }

        /// <summary>Whether it is an expression, worked out when its step is reached, rather than a value.</summary>
        public bool IsExpression => Kind == ValueKind.Expression;

        /// <summary>What it is, for every kind but <see cref="ValueKind.Text" />.</summary>
        public EntityValue Value { get; private set; }

        /// <summary>
        /// The text, for <see cref="ValueKind.Text" />, or the name, for <see cref="ValueKind.Name" /> and <see cref="ValueKind.Setting" />,
        /// and empty for the rest.
        /// </summary>
        public string Text { get; private set; }

        /// <summary>Whether it is a setting standing in for a value, rather than a value.</summary>
        public bool IsSetting => Kind == ValueKind.Setting;

        /// <summary>Whether it is a setting written with its kind, <c>setting('Open angle')</c>.</summary>
        public bool IsKinded => Kind == ValueKind.Setting && Value.Bits != 0;

        /// <summary>It as a length of time, for <see cref="ValueKind.Duration" />.</summary>
        public Duration Length => Text == InSeconds ? Duration.OfSeconds(Real) : Duration.OfTicks((int)Integer);

        /// <summary>The names, for <see cref="ValueKind.Slots" />, and null for the rest.</summary>
        public IList<string> Names { get; private set; }

        /// <summary>What the slots hold, for <see cref="ValueKind.Slots" /> in a step that has been worked out, and null otherwise.</summary>
        public IList<HeldThing> Held { get; private set; }

        /// <summary>The host type a choice was written with, or null where it was written alone.</summary>
        public ChoiceType Typed { get; private set; }

        /// <summary>This again, with a list of names of its own where it has one, so editing the copy cannot reach it.</summary>
        public Literal Copied => Kind == ValueKind.Slots ? Slots(new List<string>(Names)) : Kind == ValueKind.Expression ? Worked(Tree.Copied()) : this;

        /// <summary>It as a flag, for <see cref="ValueKind.Flag" />.</summary>
        public bool Flag => Value.Bits != 0;

        /// <summary>It as an integer, for <see cref="ValueKind.Integer" />.</summary>
        public long Integer => Value.Bits;

        /// <summary>It as a real number, for <see cref="ValueKind.Real" />.</summary>
        public double Real => BitConverter.Int64BitsToDouble(Value.Bits);

        /// <summary>What it is as a number, whether it was written whole or with a point, and nought for anything else.</summary>
        /// <remarks><see cref="Real" /> read over an integer is not the number at all.</remarks>
        public double Number
        {
            get
            {
                if (Kind == ValueKind.Integer)
                    return Integer;

                return Kind == ValueKind.Real ? Real : 0;
            }
        }

        /// <summary>The red of a color.</summary>
        public byte Red => (byte)(Value.Bits >> 24);

        /// <summary>The green of a color.</summary>
        public byte Green => (byte)(Value.Bits >> 16);

        /// <summary>The blue of a color.</summary>
        public byte Blue => (byte)(Value.Bits >> 8);

        /// <summary>The alpha of a color.</summary>
        public byte Alpha => (byte)Value.Bits;

        /// <summary>
        /// How it reads to a person, and to anything that wanted text.
        /// </summary>
        /// <remarks>In the invariant culture: the game's own actions are handed this, and parse it back.</remarks>
        public string Said
        {
            get
            {
                if (Kind == ValueKind.Text || Kind == ValueKind.Name || Kind == ValueKind.Setting)
                    return Text;

                if (Kind == ValueKind.Duration)
                {
                    var length = Length;
                    return length.InSeconds
                        ? $"{length.Seconds.ToString("R", CultureInfo.InvariantCulture)}s"
                        : $"{length.Ticks.ToString(CultureInfo.InvariantCulture)}t";
                }

                if (Kind == ValueKind.Expression)
                    return Tree.Written;

                if (Kind == ValueKind.Slots)
                    return string.Join(", ", Names);

                if (Kind == ValueKind.Flag)
                    return Flag ? "on" : "off";

                if (Kind == ValueKind.Integer)
                    return Integer.ToString(CultureInfo.InvariantCulture);

                if (Kind == ValueKind.Real)
                    return Real.ToString("R", CultureInfo.InvariantCulture);

                return $"#{Hex(Red)}{Hex(Green)}{Hex(Blue)}{(Alpha == 255 ? "" : Hex(Alpha))}";
            }
        }

        /// <summary>
        /// This as the game will take it as <paramref name="type" />, or false where it cannot be one.
        /// </summary>
        /// <remarks>An integer also fits a real type, and a value must be within what the type holds: 300 is no byte.</remarks>
        public bool Fits(Primitive type, out Literal became)
        {
            became = this;
            switch (type)
            {
                case Primitive.None:
                    return true;
                case Primitive.Boolean:
                    return Kind == ValueKind.Flag;
                case Primitive.Byte:
                    return Within(byte.MinValue, byte.MaxValue);
                case Primitive.SByte:
                    return Within(sbyte.MinValue, sbyte.MaxValue);
                case Primitive.Int16:
                    return Within(short.MinValue, short.MaxValue);
                case Primitive.UInt16:
                    return Within(ushort.MinValue, ushort.MaxValue);
                case Primitive.Int32:
                    return Within(int.MinValue, int.MaxValue);
                case Primitive.UInt32:
                    return Within(uint.MinValue, uint.MaxValue);
                case Primitive.Int64:
                    return Kind == ValueKind.Integer;
                case Primitive.UInt64:
                    return Within(0, long.MaxValue);
                case Primitive.Single:
                    return Becomes(ValueKind.Real, out became) && Math.Abs(became.Real) <= float.MaxValue;
                case Primitive.Double:
                    return Becomes(ValueKind.Real, out became);
                case Primitive.Char:
                    return (Kind == ValueKind.Text || Kind == ValueKind.Name) && Text.Length == 1;
                case Primitive.String:
                    return Kind == ValueKind.Text || Kind == ValueKind.Name;
            }

            return false;
        }

        bool Within(long least, long most)
        {
            return Kind == ValueKind.Integer && Integer >= least && Integer <= most;
        }

        /// <summary>
        /// This as a value of <paramref name="type" /> itself, boxed, for one that <see cref="Fits" />.
        /// </summary>
        /// <remarks>Made when a program is bound, never when it runs, so the boxing is paid once.</remarks>
        public object Native(Primitive type)
        {
            switch (type)
            {
                case Primitive.Boolean:
                    return Flag;
                case Primitive.Byte:
                    return (byte)Integer;
                case Primitive.SByte:
                    return (sbyte)Integer;
                case Primitive.Int16:
                    return (short)Integer;
                case Primitive.UInt16:
                    return (ushort)Integer;
                case Primitive.Int32:
                    return (int)Integer;
                case Primitive.UInt32:
                    return (uint)Integer;
                case Primitive.Int64:
                    return Integer;
                case Primitive.UInt64:
                    return (ulong)Integer;
                case Primitive.Single:
                    return (float)Number;
                case Primitive.Double:
                    return Number;
                case Primitive.Char:
                    return Text[0];
                case Primitive.String:
                    return Text;
            }

            return Said;
        }

        /// <summary>This as <paramref name="kind" />, or false where it cannot be one.</summary>
        /// <remarks>An integer also becomes a real one, and a name also becomes text; every other kind only becomes itself.</remarks>
        public bool Becomes(ValueKind kind, out Literal became)
        {
            became = this;
            if (Kind == kind)
                return true;

            if (Kind == ValueKind.Name && kind == ValueKind.Text)
            {
                became = Of(Text);
                return true;
            }

            if (Kind != ValueKind.Integer || kind != ValueKind.Real)
                return false;

            became = Of((double)Integer);
            return true;
        }

        static string Hex(byte value)
        {
            return value.ToString("X2", CultureInfo.InvariantCulture);
        }
    }
}
