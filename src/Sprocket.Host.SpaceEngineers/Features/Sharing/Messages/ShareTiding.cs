using Mal.MdkModMixin.Network;

namespace Sprocket.Host.SpaceEngineers.Features.Sharing.Messages
{
    /// <summary>The server telling a player that a blueprint has been shared with them, or that one shared with them has changed.</summary>
    public struct ShareTiding
    {
        /// <summary>Whether a blueprint already shared with them has changed, rather than a new one reaching them.</summary>
        public bool IsUpdate;

        /// <summary>What the blueprint is called.</summary>
        public string Name;

        /// <summary>The name of the player who shared it.</summary>
        public string Sharer;

        /// <summary>How many bytes <paramref name="tiding" /> takes on the wire.</summary>
        public static int SizeOf(ref ShareTiding tiding)
        {
            return sizeof(int) + ByteData.SizeOfString(tiding.Name) + ByteData.SizeOfString(tiding.Sharer);
        }

        /// <summary>Writes <paramref name="tiding" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref ShareTiding tiding, byte[] data, ref int offset)
        {
            var start = offset;
            ByteData.Write(tiding.IsUpdate ? 1 : 0, data, ref offset);
            ByteData.WriteString(tiding.Name, data, ref offset);
            ByteData.WriteString(tiding.Sharer, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads a tiding at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out ShareTiding tiding)
        {
            var start = offset;
            tiding = new ShareTiding();
            int update;
            ByteData.TryReadInt32(data, ref offset, out update);
            tiding.IsUpdate = update != 0;
            ByteData.TryReadString(data, ref offset, out tiding.Name);
            ByteData.TryReadString(data, ref offset, out tiding.Sharer);
            return offset - start;
        }
    }
}
