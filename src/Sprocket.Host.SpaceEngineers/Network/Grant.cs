
namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>
    /// The server's answer to a <see cref="Claim" />: the Sprocket to edit, or who is editing it already.
    /// </summary>
    public struct Grant
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

        /// <summary>How many bytes <paramref name="grant" /> takes on the wire.</summary>
        public static int SizeOf(ref Grant grant)
        {
            return sizeof(long) + sizeof(int) + ByteData.SizeOfString(grant.Holder)
                + ByteData.SizeOfString(grant.Program) + ByteData.SizeOfString(grant.State);
        }

        /// <summary>Writes <paramref name="grant" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref Grant grant, byte[] data, ref int offset)
        {
            var start = offset;
            ByteData.Write(grant.Block, data, ref offset);
            ByteData.Write((grant.Given ? 1 : 0) | (grant.Foreign ? 2 : 0), data, ref offset);
            ByteData.WriteString(grant.Holder, data, ref offset);
            ByteData.WriteString(grant.Program, data, ref offset);
            ByteData.WriteString(grant.State, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads a grant at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out Grant grant)
        {
            var start = offset;
            grant = new Grant();
            ByteData.TryReadInt64(data, ref offset, out grant.Block);

            int given;
            ByteData.TryReadInt32(data, ref offset, out given);
            grant.Given = (given & 1) != 0;
            grant.Foreign = (given & 2) != 0;

            ByteData.TryReadString(data, ref offset, out grant.Holder);
            ByteData.TryReadString(data, ref offset, out grant.Program);
            ByteData.TryReadString(data, ref offset, out grant.State);
            return offset - start;
        }
    }
}
