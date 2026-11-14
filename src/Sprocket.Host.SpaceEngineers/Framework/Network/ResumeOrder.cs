using Mal.MdkModMixin.Network;

namespace Sprocket.Host.SpaceEngineers.Framework.Network
{
    /// <summary>A client resuming the paused automation of the Sprocket it is editing.</summary>
    public struct ResumeOrder
    {
        /// <summary>The block that carries the Sprocket, by entity id.</summary>
        public long Block;

        /// <summary>How many bytes <paramref name="order" /> takes on the wire.</summary>
        public static int SizeOf(ref ResumeOrder order)
        {
            return sizeof(long);
        }

        /// <summary>Writes <paramref name="order" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref ResumeOrder order, byte[] data, ref int offset)
        {
            var start = offset;
            ByteData.Write(order.Block, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads an order at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out ResumeOrder order)
        {
            var start = offset;
            order = new ResumeOrder();
            ByteData.TryReadInt64(data, ref offset, out order.Block);
            return offset - start;
        }
    }
}
