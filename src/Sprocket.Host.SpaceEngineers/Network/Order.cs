using System;

namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>A client running a sequence by hand, or stopping one, on the Sprocket it is editing.</summary>
    public struct Order
    {
        /// <summary>The block that carries the Sprocket, by entity id.</summary>
        public long Block;

        /// <summary>The system, by its identity, which is the one name that does not change.</summary>
        public Guid System;

        /// <summary>Which one of the system, by number.</summary>
        public int Instance;

        /// <summary>The sequence, by number.</summary>
        public int Sequence;

        /// <summary>Whether it stops the sequence rather than running it.</summary>
        public bool Stops;

        /// <summary>How many bytes <paramref name="order" /> takes on the wire.</summary>
        public static int SizeOf(ref Order order)
        {
            return sizeof(long) + ByteData.SizeOfString(order.System.ToString("N")) + sizeof(int) * 3;
        }

        /// <summary>Writes <paramref name="order" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref Order order, byte[] data, ref int offset)
        {
            var start = offset;
            ByteData.Write(order.Block, data, ref offset);
            ByteData.WriteString(order.System.ToString("N"), data, ref offset);
            ByteData.Write(order.Instance, data, ref offset);
            ByteData.Write(order.Sequence, data, ref offset);
            ByteData.Write(order.Stops ? 1 : 0, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads an order at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out Order order)
        {
            var start = offset;
            order = new Order();
            ByteData.TryReadInt64(data, ref offset, out order.Block);

            string system;
            ByteData.TryReadString(data, ref offset, out system);
            Guid.TryParseExact(system ?? "", "N", out order.System);

            ByteData.TryReadInt32(data, ref offset, out order.Instance);
            ByteData.TryReadInt32(data, ref offset, out order.Sequence);

            int stops;
            ByteData.TryReadInt32(data, ref offset, out stops);
            order.Stops = stops != 0;
            return offset - start;
        }
    }
}
