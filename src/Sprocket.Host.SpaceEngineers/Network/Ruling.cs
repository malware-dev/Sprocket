namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>The world's settings, written as one text: proposed by an admin's client to the server, and told by the server to every client.</summary>
    public struct Ruling
    {
        /// <summary>The settings, as <c>Settings.Written</c> writes them.</summary>
        public string Settings;

        /// <summary>How many bytes <paramref name="ruling" /> takes on the wire.</summary>
        public static int SizeOf(ref Ruling ruling)
        {
            return ByteData.SizeOfString(ruling.Settings);
        }

        /// <summary>Writes <paramref name="ruling" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref Ruling ruling, byte[] data, ref int offset)
        {
            var start = offset;
            ByteData.WriteString(ruling.Settings, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads a ruling at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out Ruling ruling)
        {
            var start = offset;
            ruling = new Ruling();
            ByteData.TryReadString(data, ref offset, out ruling.Settings);
            return offset - start;
        }
    }
}
