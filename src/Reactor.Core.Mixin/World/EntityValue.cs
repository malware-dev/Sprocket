using System;

namespace Reactor.World
{
    /// <summary>
    /// One reading off an entity, or one literal written in a script.
    /// </summary>
    /// <remarks>
    /// Eight bytes, and opaque. What is inside depends on the kind, and the kind is not here - it is
    /// a property of the <i>declaration</i>, which is where a value always comes from. A reading is a
    /// reading of a declared property; an operand is attached to a declared state. A value never
    /// floats free, so it never has to carry what it is.
    /// <para>
    /// Anything that fits in sixty-four bits is here directly, and can be made without anything else:
    /// a flag, a whole number, a real through <c>BitConverter</c>, a packed colour. Anything wider is
    /// a slot in <see cref="Values" /> - so a matrix would live unboxed inside an array rather than as
    /// a heap object of its own, and the value naming it is still eight bytes. Adding a kind never
    /// grows this, whatever the kind is made of.
    /// </para>
    /// <para>
    /// Reading one goes through <see cref="Values" />, which has both the kind and the lists, so a
    /// colour cannot be read off a text by accident.
    /// </para>
    /// </remarks>
    public struct EntityValue
    {
        internal EntityValue(long bits) : this()
        {
            Bits = bits;
        }

        public static EntityValue Of(bool flag)
        {
            return new EntityValue(flag ? 1 : 0);
        }

        /// <summary>
        /// A whole number, held exactly.
        /// </summary>
        /// <remarks>
        /// Why <see cref="ValueKind.Integer" /> is not <see cref="ValueKind.Real" />: a double holds
        /// integers exactly only to 2^53, and an entity id runs to seventeen digits. One that went
        /// through a double would come back a different id, silently.
        /// </remarks>
        public static EntityValue Of(long integer)
        {
            return new EntityValue(integer);
        }

        public static EntityValue Of(double real)
        {
            return new EntityValue(BitConverter.DoubleToInt64Bits(real));
        }

        /// <summary>A colour, packed rather than given a list of its own. It fits.</summary>
        public static EntityValue Of(byte r, byte g, byte b, byte a)
        {
            return new EntityValue(((long)r << 24) | ((long)g << 16) | ((long)b << 8) | a);
        }

        /// <summary>Meaningless without the kind. See <see cref="Values" />.</summary>
        internal long Bits { get; private set; }
    }
}
