using System;
using Mal.MdkModMixin.Network;

namespace Sprocket.Host.SpaceEngineers.Framework.Network
{
    /// <summary>
    /// A client changing by hand the value a state has now in one installation of the Sprocket it is editing, while that
    /// installation or its logic system is switched off.
    /// </summary>
    public struct StateOrder
    {
        /// <summary>The block that carries the Sprocket, by entity id.</summary>
        public long Block;

        /// <summary>The system, by its identity.</summary>
        public Guid System;

        /// <summary>Which one of the system, by number.</summary>
        public int Instance;

        /// <summary>The state, by name.</summary>
        public string State;

        /// <summary>The choice it is given, or empty for a flag.</summary>
        public string Choice;

        /// <summary>Whether a flag is given on.</summary>
        public bool Flag;

        /// <summary>How many bytes <paramref name="order" /> takes on the wire.</summary>
        public static int SizeOf(ref StateOrder order)
        {
            return sizeof(long) + ByteData.SizeOfString(order.System.ToString("N")) + sizeof(int) * 2
                   + ByteData.SizeOfString(order.State ?? "") + ByteData.SizeOfString(order.Choice ?? "");
        }

        /// <summary>Writes <paramref name="order" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref StateOrder order, byte[] data, ref int offset)
        {
            var start = offset;
            ByteData.Write(order.Block, data, ref offset);
            ByteData.WriteString(order.System.ToString("N"), data, ref offset);
            ByteData.Write(order.Instance, data, ref offset);
            ByteData.WriteString(order.State ?? "", data, ref offset);
            ByteData.WriteString(order.Choice ?? "", data, ref offset);
            ByteData.Write(order.Flag ? 1 : 0, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads an order at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out StateOrder order)
        {
            var start = offset;
            order = new StateOrder();
            ByteData.TryReadInt64(data, ref offset, out order.Block);

            string system;
            ByteData.TryReadString(data, ref offset, out system);
            Guid.TryParseExact(system ?? "", "N", out order.System);

            ByteData.TryReadInt32(data, ref offset, out order.Instance);
            ByteData.TryReadString(data, ref offset, out order.State);
            ByteData.TryReadString(data, ref offset, out order.Choice);

            int flag;
            ByteData.TryReadInt32(data, ref offset, out flag);
            order.Flag = flag != 0;
            return offset - start;
        }
    }
}
