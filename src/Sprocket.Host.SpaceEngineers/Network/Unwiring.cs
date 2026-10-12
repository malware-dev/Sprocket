namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>A client letting go of the buttons it asked to have written and the server has not written yet, as a pick is backed out of.</summary>
    public struct Unwiring
    {
        /// <summary>The block that carries the Sprocket, by entity id.</summary>
        public long Block;

        /// <summary>How many bytes <paramref name="unwiring" /> takes on the wire.</summary>
        public static int SizeOf(ref Unwiring unwiring)
        {
            return sizeof(long);
        }

        /// <summary>Writes <paramref name="unwiring" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref Unwiring unwiring, byte[] data, ref int offset)
        {
            var start = offset;
            ByteData.Write(unwiring.Block, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads an unwiring at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out Unwiring unwiring)
        {
            var start = offset;
            unwiring = new Unwiring();
            ByteData.TryReadInt64(data, ref offset, out unwiring.Block);
            return offset - start;
        }
    }
}
