using Mal.MdkModMixin.Network;

namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>A client asking the server for a Sprocket to edit.</summary>
    public struct EditClaim
    {
        /// <summary>The block that carries the Sprocket, by entity id.</summary>
        public long Block;

        /// <summary>Who is asking, as the server tells anybody else who asks while they hold it.</summary>
        public string Name;

        /// <summary>How many bytes <paramref name="claim" /> takes on the wire.</summary>
        public static int SizeOf(ref EditClaim claim)
        {
            return sizeof(long) + ByteData.SizeOfString(claim.Name);
        }

        /// <summary>Writes <paramref name="claim" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref EditClaim claim, byte[] data, ref int offset)
        {
            var start = offset;
            ByteData.Write(claim.Block, data, ref offset);
            ByteData.WriteString(claim.Name, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads a claim at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out EditClaim claim)
        {
            var start = offset;
            claim = new EditClaim();
            ByteData.TryReadInt64(data, ref offset, out claim.Block);
            ByteData.TryReadString(data, ref offset, out claim.Name);
            return offset - start;
        }
    }
}
