using System;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>
    /// One reading off an entity, or one literal written in a script.
    /// </summary>
    /// <remarks>
    /// Eight bytes, holding no kind: anything wider than 64 bits is a slot in <see cref="Model.Values" />, and every value is read
    /// through <see cref="Model.Values" />, which has the kind.
    /// </remarks>
    public struct EntityValue
    {
        /// <summary>A value held as the 64 bits <paramref name="bits" />.</summary>
        public EntityValue(long bits) : this()
        {
            Bits = bits;
        }

        /// <summary>The value <paramref name="flag" />, as one or nought.</summary>
        public static EntityValue Of(bool flag)
        {
            return new EntityValue(flag ? 1 : 0);
        }

        /// <summary>
        /// An integer, held exactly.
        /// </summary>
        /// <remarks>An entity id runs to seventeen digits, past the 2^53 a double holds exactly.</remarks>
        public static EntityValue Of(long integer)
        {
            return new EntityValue(integer);
        }

        /// <summary>The value <paramref name="real" />, held as its bits.</summary>
        public static EntityValue Of(double real)
        {
            return new EntityValue(BitConverter.DoubleToInt64Bits(real));
        }

        /// <summary>The color <paramref name="r" />, <paramref name="g" />, <paramref name="b" />, <paramref name="a" />, packed into the 64 bits.</summary>
        public static EntityValue Of(byte r, byte g, byte b, byte a)
        {
            return new EntityValue(((long)r << 24) | ((long)g << 16) | ((long)b << 8) | a);
        }

        /// <summary>The 64 bits the value is held as, meaningless without its kind.</summary>
        public long Bits { get; private set; }
    }
}
