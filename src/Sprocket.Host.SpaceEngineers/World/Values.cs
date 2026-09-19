using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// Makes values and reads them back, and holds the ones too wide to fit in one.
    /// </summary>
    /// <remarks>
    /// An <see cref="EntityValue" /> is eight opaque bytes. Everything that knows what those bytes
    /// mean is here, because two things are needed to read one and neither belongs in the value: the
    /// <see cref="ValueKind" />, which comes from the declaration, and the lists, which hold anything
    /// wider than sixty-four bits.
    /// <para>
    /// <b>Wide kinds are lists rather than references.</b> A list of matrices keeps them unboxed and
    /// next to each other; a reference per value would put a heap object in a loop that runs sixty
    /// times a second. Nothing here allocates once a slot exists - see
    /// <see cref="Text(EntityValue,string)" />, which writes into the slot a reading already owns
    /// rather than taking a new one.
    /// So the lists grow to hold what is live at once and then stop.
    /// </para>
    /// <para>
    /// One store serves a whole world: both the readings its entities produce and the operands
    /// declared on their kinds, because a comparison meets one of each.
    /// </para>
    /// </remarks>
    public sealed class Values
    {
        /// <summary>
        /// Wide values, one list per kind. Only text so far.
        /// </summary>
        /// <remarks>
        /// A slot is an index into the list for its own kind, so two kinds never collide and neither
        /// list has to hold anything but its own sort of thing.
        /// </remarks>
        readonly List<string> _texts = new List<string>();

        /// <summary>Text, in a slot of its own. For a literal, which is written once and kept.</summary>
        public EntityValue Text(string text)
        {
            _texts.Add(text ?? "");

            // One-based, because default(EntityValue) is zero and a reading that has not been given a
            // slot yet must be told apart from one holding the first. Getting that wrong means the
            // first reading overwrites the first literal.
            return new EntityValue(_texts.Count);
        }

        /// <summary>
        /// Text, into the slot a reading already has.
        /// </summary>
        /// <remarks>
        /// What a host calls when a property reads as text, handing back the value it gave last time.
        /// The slot belongs to that reading rather than to the moment, so a property read on every
        /// tick takes one slot for the life of the world instead of one per tick.
        /// </remarks>
        public EntityValue Text(EntityValue slot, string text)
        {
            if (slot.Bits > 0 && slot.Bits <= _texts.Count)
            {
                _texts[(int)slot.Bits - 1] = text ?? "";
                return slot;
            }

            return Text(text);
        }

        /// <summary>The whole number, if that is what this is.</summary>
        public bool TryInteger(ValueKind kind, EntityValue value, out long integer)
        {
            integer = value.Bits;
            return kind == ValueKind.Integer;
        }

        /// <summary>The number, if that is what this is.</summary>
        public bool TryReal(ValueKind kind, EntityValue value, out double real)
        {
            real = BitConverter.Int64BitsToDouble(value.Bits);
            return kind == ValueKind.Real;
        }

        /// <summary>The text, if that is what this is. Never null.</summary>
        public bool TryText(ValueKind kind, EntityValue value, out string text)
        {
            text = kind == ValueKind.Text ? Read(value) : "";
            return kind == ValueKind.Text;
        }

        /// <summary>The colour, if that is what this is.</summary>
        public bool TryColor(ValueKind kind, EntityValue value, out byte r, out byte g, out byte b, out byte a)
        {
            r = (byte)(value.Bits >> 24);
            g = (byte)(value.Bits >> 16);
            b = (byte)(value.Bits >> 8);
            a = (byte)value.Bits;

            return kind == ValueKind.Color;
        }

        /// <summary>
        /// Whether there is anything here.
        /// </summary>
        /// <remarks>
        /// The same question of every kind - a number not zero, a text not empty, a colour not fully
        /// transparent - which is why it can be asked without knowing which one it is first.
        /// </remarks>
        public bool Flag(ValueKind kind, EntityValue value)
        {
            if (kind == ValueKind.Text)
                return !string.IsNullOrEmpty(Read(value));

            // Negative zero has bits set, and is still nothing.
            if (kind == ValueKind.Real)
                return BitConverter.Int64BitsToDouble(value.Bits) != 0;

            if (kind == ValueKind.Color)
                return (byte)value.Bits != 0;

            return value.Bits != 0;
        }

        /// <summary>
        /// This measured as a number, whatever it is. A deliberate coercion.
        /// </summary>
        /// <remarks>
        /// What a relation compares, which is why a flag reads one or zero and "is true" needs no case
        /// of its own. Not every kind has a meaningful answer, and that is what
        /// <see cref="Relations.Suits" /> refuses rather than this pretending otherwise.
        /// <para>
        /// Never a way to get a whole number back. Past 2^53 a double cannot carry one, which is the
        /// entire reason <see cref="ValueKind.Integer" /> exists.
        /// </para>
        /// </remarks>
        public double AsNumber(ValueKind kind, EntityValue value)
        {
            return kind == ValueKind.Real ? BitConverter.Int64BitsToDouble(value.Bits) : value.Bits;
        }

        /// <summary>This as text, whatever it is. Empty for anything that is not text.</summary>
        /// <remarks>
        /// A number gives back nothing rather than a rendering of itself: how a value is written -
        /// decimals, a unit, the words for true and false - is presentation, and core deciding it once
        /// for everybody is exactly what the host is meant to own.
        /// </remarks>
        public string AsText(ValueKind kind, EntityValue value)
        {
            return kind == ValueKind.Text ? Read(value) : "";
        }

        string Read(EntityValue value)
        {
            return value.Bits > 0 && value.Bits <= _texts.Count ? _texts[(int)value.Bits - 1] : "";
        }
    }
}
