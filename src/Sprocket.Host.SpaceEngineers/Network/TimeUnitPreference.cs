namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>A client's player choosing the unit they give lengths of time in, for the server to keep with their settings in the world.</summary>
    public struct TimeUnitPreference
    {
        /// <summary>Whether they give lengths in ticks rather than seconds.</summary>
        public bool Ticks;

        /// <summary>How many bytes <paramref name="preference" /> takes on the wire.</summary>
        public static int SizeOf(ref TimeUnitPreference preference)
        {
            return sizeof(int);
        }

        /// <summary>Writes <paramref name="preference" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref TimeUnitPreference preference, byte[] data, ref int offset)
        {
            var start = offset;
            ByteCodec.Write(preference.Ticks ? 1 : 0, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads a preference at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out TimeUnitPreference preference)
        {
            var start = offset;
            preference = new TimeUnitPreference();
            int ticks;
            ByteCodec.TryReadInt32(data, ref offset, out ticks);
            preference.Ticks = (ticks & 1) != 0;
            return offset - start;
        }
    }
}
