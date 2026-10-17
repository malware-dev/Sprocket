namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>A client asking for the buttons of a panel somebody has just picked to be written, whatever their slots hold.</summary>
    public struct WiringRequest
    {
        /// <summary>The block that carries the Sprocket, by entity id.</summary>
        public long Block;

        /// <summary>The button panel, by entity id.</summary>
        public long Panel;

        /// <summary>How many bytes <paramref name="wiring" /> takes on the wire.</summary>
        public static int SizeOf(ref WiringRequest wiring)
        {
            return sizeof(long) * 2;
        }

        /// <summary>Writes <paramref name="wiring" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref WiringRequest wiring, byte[] data, ref int offset)
        {
            var start = offset;
            ByteCodec.Write(wiring.Block, data, ref offset);
            ByteCodec.Write(wiring.Panel, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads a wiring at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out WiringRequest wiring)
        {
            var start = offset;
            wiring = new WiringRequest();
            ByteCodec.TryReadInt64(data, ref offset, out wiring.Block);
            ByteCodec.TryReadInt64(data, ref offset, out wiring.Panel);
            return offset - start;
        }
    }
}
