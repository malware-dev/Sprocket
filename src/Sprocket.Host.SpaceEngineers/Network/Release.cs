namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>A client handing back a Sprocket it was editing, so somebody else may.</summary>
    public struct Release
    {
        /// <summary>The block that carries the Sprocket, by entity id.</summary>
        public long Block;

        /// <summary>How many bytes <paramref name="release" /> takes on the wire.</summary>
        public static int SizeOf(ref Release release)
        {
            return sizeof(long);
        }

        /// <summary>Writes <paramref name="release" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref Release release, byte[] data, ref int offset)
        {
            var start = offset;
            ByteData.Write(release.Block, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads a release at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out Release release)
        {
            var start = offset;
            release = new Release();
            ByteData.TryReadInt64(data, ref offset, out release.Block);
            return offset - start;
        }
    }
}
