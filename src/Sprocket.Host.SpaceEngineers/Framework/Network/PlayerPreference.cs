using Mal.MdkModMixin.Network;

namespace Sprocket.Host.SpaceEngineers.Framework.Network
{
    /// <summary>A client's player changing their own settings in this world, for the server to keep with them.</summary>
    public struct PlayerPreference
    {
        /// <summary>Whether they give lengths in ticks rather than seconds.</summary>
        public bool Ticks;

        /// <summary>Which way they give a color in a color picker, as the place of its tab.</summary>
        public int ColorTab;

        /// <summary>Whether the pickers list the blocks hidden from toolbars.</summary>
        public bool ShowsHidden;

        /// <summary>The colors they last chose, as their codes separated by spaces, the newest first.</summary>
        public string RecentColors;

        /// <summary>How many bytes <paramref name="preference" /> takes on the wire.</summary>
        public static int SizeOf(ref PlayerPreference preference)
        {
            return sizeof(int) + ByteData.SizeOfString(preference.RecentColors);
        }

        /// <summary>Writes <paramref name="preference" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref PlayerPreference preference, byte[] data, ref int offset)
        {
            var start = offset;
            ByteData.Write((preference.Ticks ? 1 : 0) | (preference.ShowsHidden ? 4 : 0) | ((preference.ColorTab & 0xFF) << 8), data, ref offset);
            ByteData.WriteString(preference.RecentColors, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads a preference at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out PlayerPreference preference)
        {
            var start = offset;
            preference = new PlayerPreference();
            int flags;
            ByteData.TryReadInt32(data, ref offset, out flags);
            preference.Ticks = (flags & 1) != 0;
            preference.ColorTab = (flags >> 8) & 0xFF;
            preference.ShowsHidden = (flags & 4) != 0;
            ByteData.TryReadString(data, ref offset, out preference.RecentColors);
            return offset - start;
        }
    }
}
