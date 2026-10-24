using Sprocket.Network;

namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>
    /// The server's answer to a <see cref="EditClaim" />: the Sprocket to edit, or who is editing it already.
    /// </summary>
    public struct EditGrant
    {
        /// <summary>The block that carries the Sprocket, by entity id.</summary>
        public long Block;

        /// <summary>Whether it is lent to the one who asked.</summary>
        public bool Given;

        /// <summary>Whether it is refused because the one who asked is none of the construct's big owners.</summary>
        public bool Foreign;

        /// <summary>Who holds it where it is not given, or empty where nobody can have it.</summary>
        public string Holder;

        /// <summary>The program, as the block holds it, where it is given.</summary>
        public string Program;

        /// <summary>What is switched off, as the block holds it, where it is given.</summary>
        public string State;

        /// <summary>Whether the one who asked gives lengths of time in ticks, by their settings in this world.</summary>
        public bool Ticks;

        /// <summary>Which way the one who asked gives a color, as the place of its tab, by their settings in this world.</summary>
        public int ColorTab;

        /// <summary>Whether the pickers of the one who asked list the blocks hidden from toolbars, by their settings in this world.</summary>
        public bool ShowsHidden;

        /// <summary>The colors the one who asked last chose, as their codes separated by spaces, by their settings in this world.</summary>
        public string RecentColors;

        /// <summary>The settings the server runs the world by, written as one text, which the client takes as its own.</summary>
        public string Settings;

        /// <summary>The construct's memory, packed: the runs, triggers, owners, pause and versions waiting that the copy shows.</summary>
        public string Memory;

        /// <summary>How many bytes <paramref name="grant" /> takes on the wire.</summary>
        public static int SizeOf(ref EditGrant grant)
        {
            return sizeof(long) + sizeof(int) + ByteCodec.SizeOfString(grant.Holder)
                + ByteCodec.SizeOfString(grant.Program) + ByteCodec.SizeOfString(grant.State) + ByteCodec.SizeOfString(grant.Settings)
                + ByteCodec.SizeOfString(grant.RecentColors) + ByteCodec.SizeOfString(grant.Memory);
        }

        /// <summary>Writes <paramref name="grant" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref EditGrant grant, byte[] data, ref int offset)
        {
            var start = offset;
            ByteCodec.Write(grant.Block, data, ref offset);
            ByteCodec.Write((grant.Given ? 1 : 0) | (grant.Foreign ? 2 : 0) | (grant.Ticks ? 4 : 0) | (grant.ShowsHidden ? 16 : 0) | ((grant.ColorTab & 0xFF) << 8), data, ref offset);
            ByteCodec.WriteString(grant.Holder, data, ref offset);
            ByteCodec.WriteString(grant.Program, data, ref offset);
            ByteCodec.WriteString(grant.State, data, ref offset);
            ByteCodec.WriteString(grant.Settings, data, ref offset);
            ByteCodec.WriteString(grant.RecentColors, data, ref offset);
            ByteCodec.WriteString(grant.Memory, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads a grant at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out EditGrant grant)
        {
            var start = offset;
            grant = new EditGrant();
            ByteCodec.TryReadInt64(data, ref offset, out grant.Block);

            int given;
            ByteCodec.TryReadInt32(data, ref offset, out given);
            grant.Given = (given & 1) != 0;
            grant.Foreign = (given & 2) != 0;
            grant.Ticks = (given & 4) != 0;
            grant.ColorTab = (given >> 8) & 0xFF;
            grant.ShowsHidden = (given & 16) != 0;

            ByteCodec.TryReadString(data, ref offset, out grant.Holder);
            ByteCodec.TryReadString(data, ref offset, out grant.Program);
            ByteCodec.TryReadString(data, ref offset, out grant.State);
            ByteCodec.TryReadString(data, ref offset, out grant.Settings);
            ByteCodec.TryReadString(data, ref offset, out grant.RecentColors);
            ByteCodec.TryReadString(data, ref offset, out grant.Memory);
            return offset - start;
        }
    }
}
