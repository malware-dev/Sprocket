using Mal.MdkModMixin.Network;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Shares;

namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>A client asking the server for something about shared blueprints.</summary>
    public struct ShareOrder
    {
        /// <summary>What is asked.</summary>
        public ShareAct Act;

        /// <summary>Who a blueprint being shared is for.</summary>
        public ShareReach Reach;

        /// <summary>The player it is about: the sharer of a share being taken back, or the player being blocked or barred.</summary>
        public long Target;

        /// <summary>The identity of the blueprint of a share being taken back.</summary>
        public string Identity;

        /// <summary>The identities of the players a blueprint is shared with, separated by spaces.</summary>
        public string Players;

        /// <summary>The blueprint being shared.</summary>
        public string Blueprint;

        /// <summary>How many bytes <paramref name="order" /> takes on the wire.</summary>
        public static int SizeOf(ref ShareOrder order)
        {
            return sizeof(int) * 2 + sizeof(long) + ByteData.SizeOfString(order.Identity) + ByteData.SizeOfString(order.Players)
                   + ByteData.SizeOfString(order.Blueprint);
        }

        /// <summary>Writes <paramref name="order" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref ShareOrder order, byte[] data, ref int offset)
        {
            var start = offset;
            ByteData.Write((int)order.Act, data, ref offset);
            ByteData.Write((int)order.Reach, data, ref offset);
            ByteData.Write(order.Target, data, ref offset);
            ByteData.WriteString(order.Identity, data, ref offset);
            ByteData.WriteString(order.Players, data, ref offset);
            ByteData.WriteString(order.Blueprint, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads an order at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out ShareOrder order)
        {
            var start = offset;
            order = new ShareOrder();
            int act;
            int reach;
            ByteData.TryReadInt32(data, ref offset, out act);
            ByteData.TryReadInt32(data, ref offset, out reach);
            order.Act = (ShareAct)act;
            order.Reach = (ShareReach)reach;
            ByteData.TryReadInt64(data, ref offset, out order.Target);
            ByteData.TryReadString(data, ref offset, out order.Identity);
            ByteData.TryReadString(data, ref offset, out order.Players);
            ByteData.TryReadString(data, ref offset, out order.Blueprint);
            return offset - start;
        }
    }
}
