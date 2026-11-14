using Mal.MdkModMixin.Network;

namespace Sprocket.Host.SpaceEngineers.Framework.Network
{
    /// <summary>
    /// A Sprocket's program, and what it has switched off, as they stand after its latest change.
    /// </summary>
    /// <remarks>
    /// Sent both ways: the holder sends the server its edits, and the server sends every client what it wrote into the
    /// block.
    /// </remarks>
    public struct ProgramChange
    {
        /// <summary>The block that carries the Sprocket, by entity id.</summary>
        public long Block;

        /// <summary>The program, as the block holds it.</summary>
        public string Program;

        /// <summary>What is switched off, and nothing about what is running.</summary>
        public string Switches;

        /// <summary>How many bytes <paramref name="change" /> takes on the wire.</summary>
        public static int SizeOf(ref ProgramChange change)
        {
            return sizeof(long) + ByteData.SizeOfString(change.Program) + ByteData.SizeOfString(change.Switches);
        }

        /// <summary>Writes <paramref name="change" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref ProgramChange change, byte[] data, ref int offset)
        {
            var start = offset;
            ByteData.Write(change.Block, data, ref offset);
            ByteData.WriteString(change.Program, data, ref offset);
            ByteData.WriteString(change.Switches, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads a change at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out ProgramChange change)
        {
            var start = offset;
            change = new ProgramChange();
            ByteData.TryReadInt64(data, ref offset, out change.Block);
            ByteData.TryReadString(data, ref offset, out change.Program);
            ByteData.TryReadString(data, ref offset, out change.Switches);
            return offset - start;
        }
    }
}
