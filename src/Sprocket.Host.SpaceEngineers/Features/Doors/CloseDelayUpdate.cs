using Sprocket.Host.SpaceEngineers.Network;

namespace Sprocket.Host.SpaceEngineers.Features.Doors
{
    /// <summary>A door's auto-close delay, as somebody set it in the terminal.</summary>
    /// <remarks>
    /// Sent both ways. A client sends the server what its player set, and the server sends every client what the door now
    /// keeps, because a door's storage reaches a client only with the grid.
    /// </remarks>
    public struct CloseDelayUpdate
    {
        /// <summary>The door, by entity id.</summary>
        public long Door;

        /// <summary>The delay in whole seconds; none is off.</summary>
        public int Seconds;

        /// <summary>How many bytes <paramref name="closing" /> takes on the wire.</summary>
        public static int SizeOf(ref CloseDelayUpdate closing)
        {
            return sizeof(long) + sizeof(int);
        }

        /// <summary>Writes <paramref name="closing" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref CloseDelayUpdate closing, byte[] data, ref int offset)
        {
            var start = offset;
            ByteCodec.Write(closing.Door, data, ref offset);
            ByteCodec.Write(closing.Seconds, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads a closing at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out CloseDelayUpdate closing)
        {
            var start = offset;
            closing = new CloseDelayUpdate();
            ByteCodec.TryReadInt64(data, ref offset, out closing.Door);
            ByteCodec.TryReadInt32(data, ref offset, out closing.Seconds);
            return offset - start;
        }
    }
}
