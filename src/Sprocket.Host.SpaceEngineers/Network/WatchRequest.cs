using Sprocket.Network;

namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>A client asking the server to say how a construct's automation stands, and every change to it, or to stop.</summary>
    public struct WatchRequest
    {
        /// <summary>The construct, by the entity id of the grid that stands for it.</summary>
        public long Construct;

        /// <summary>Whether the client starts watching, rather than stops.</summary>
        public bool Watching;

        /// <summary>How many bytes <paramref name="request" /> takes on the wire.</summary>
        public static int SizeOf(ref WatchRequest request)
        {
            return sizeof(long) + sizeof(int);
        }

        /// <summary>Writes <paramref name="request" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref WatchRequest request, byte[] data, ref int offset)
        {
            var start = offset;
            ByteCodec.Write(request.Construct, data, ref offset);
            ByteCodec.Write(request.Watching ? 1 : 0, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads a request at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out WatchRequest request)
        {
            var start = offset;
            request = new WatchRequest();
            int watching;
            ByteCodec.TryReadInt64(data, ref offset, out request.Construct);
            ByteCodec.TryReadInt32(data, ref offset, out watching);
            request.Watching = watching != 0;
            return offset - start;
        }
    }
}
