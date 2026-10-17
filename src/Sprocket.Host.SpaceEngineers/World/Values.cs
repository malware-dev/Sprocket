using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// Makes values and reads them back, and holds the ones too wide to fit in one.
    /// </summary>
    /// <remarks>Allocates nothing once a slot exists: the lists grow to what is live at once, and then stop.</remarks>
    public sealed class Values
    {
        readonly List<string> _texts = new List<string>();

        /// <summary>Text in a slot of its own, for a literal, which is written once and kept.</summary>
        public EntityValue Text(string text)
        {
            _texts.Add(text ?? "");

            // One-based: default(EntityValue) is zero, and a reading not yet given a slot must be told apart from one
            // holding the first.
            return new EntityValue(_texts.Count);
        }

        /// <summary>
        /// Text, into the slot a reading already has.
        /// </summary>
        /// <remarks>
        /// Handed the value the reading gave last time, so a property read on every tick takes one slot for the life of the
        /// world.
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

        /// <summary>The text, if that is what this is, and never null.</summary>
        public bool TryText(ValueKind kind, EntityValue value, out string text)
        {
            text = kind == ValueKind.Text ? Read(value) : "";
            return kind == ValueKind.Text;
        }

        /// <summary>The color, if that is what this is.</summary>
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
        /// This measured as a number, whatever it is.
        /// </summary>
        /// <remarks>A flag reads one or zero. Never a way to get a whole number back: past 2^53 a double cannot carry one.</remarks>
        public double AsNumber(ValueKind kind, EntityValue value)
        {
            return kind == ValueKind.Real ? BitConverter.Int64BitsToDouble(value.Bits) : value.Bits;
        }

        /// <summary>This as text where it is text, and empty for anything else.</summary>
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
