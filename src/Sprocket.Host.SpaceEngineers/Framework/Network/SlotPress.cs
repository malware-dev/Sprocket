using Mal.MdkModMixin.Network;

namespace Sprocket.Host.SpaceEngineers.Framework.Network
{
    /// <summary>A client telling the server that a toolbar slot holding Sprocket's action was pressed, where a ship's toolbar runs what it holds.</summary>
    public struct SlotPress
    {
        /// <summary>The block the slot's action is on, by entity id.</summary>
        public long Block;

        /// <summary>The key the slot carries, which says which trigger it starts.</summary>
        public string Key;

        /// <summary>How many bytes <paramref name="press" /> takes on the wire.</summary>
        public static int SizeOf(ref SlotPress press)
        {
            return sizeof(long) + ByteData.SizeOfString(press.Key);
        }

        /// <summary>Writes <paramref name="press" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref SlotPress press, byte[] data, ref int offset)
        {
            var start = offset;
            ByteData.Write(press.Block, data, ref offset);
            ByteData.WriteString(press.Key, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads a press at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out SlotPress press)
        {
            var start = offset;
            press = new SlotPress();
            ByteData.TryReadInt64(data, ref offset, out press.Block);
            ByteData.TryReadString(data, ref offset, out press.Key);
            return offset - start;
        }
    }
}
