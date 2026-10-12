namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>A client's player choosing the unit they give lengths of time in, for the server to keep with their settings in the world.</summary>
    public struct Preference
    {
        /// <summary>Whether they give lengths in ticks rather than seconds.</summary>
        public bool Ticks;

        /// <summary>How many bytes <paramref name="preference" /> takes on the wire.</summary>
        public static int SizeOf(ref Preference preference)
        {
            return sizeof(int);
        }

        /// <summary>Writes <paramref name="preference" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref Preference preference, byte[] data, ref int offset)
        {
            var start = offset;
            ByteData.Write(preference.Ticks ? 1 : 0, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads a preference at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out Preference preference)
        {
            var start = offset;
            preference = new Preference();
            int ticks;
            ByteData.TryReadInt32(data, ref offset, out ticks);
            preference.Ticks = (ticks & 1) != 0;
            return offset - start;
        }
    }
}
