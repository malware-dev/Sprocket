using Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.World;
using Mal.MdkModMixin.Network;

namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>How a construct's automation stands, as the server tells a client that watches it.</summary>
    public struct StatusReport
    {
        /// <summary>The construct, by the entity id of the grid that stands for it.</summary>
        public long Construct;

        /// <summary>What the construct is called.</summary>
        public string Name;

        /// <summary>How many logic systems it holds.</summary>
        public int Systems;

        /// <summary>How many of them need no looking at and are switched on.</summary>
        public int Normal;

        /// <summary>The installations that want looking at, most wanting first.</summary>
        public StatusItem[] Items;

        /// <summary>The blocks that logic systems left out of <see cref="Systems" /> are kept to.</summary>
        public long[] KeptBlocks;

        /// <summary>How many logic systems are kept to each of <see cref="KeptBlocks" />.</summary>
        public int[] KeptCounts;

        /// <summary>How many bytes <paramref name="report" /> takes on the wire.</summary>
        public static int SizeOf(ref StatusReport report)
        {
            var size = sizeof(long) + ByteData.SizeOfString(report.Name ?? "") + sizeof(int) * 4 + report.KeptBlocks.Length * (sizeof(long) + sizeof(int));
            foreach (var item in report.Items)
            {
                size += sizeof(int) * 11 + ByteData.SizeOfString(item.Logic ?? "") + ByteData.SizeOfString(item.Installation ?? "")
                    + ByteData.SizeOfString(item.Sequence ?? "") + ByteData.SizeOfString(item.Fault.Which ?? "")
                    + ByteData.SizeOfString(item.Fault.Because.About ?? "");
            }

            return size;
        }

        /// <summary>Writes <paramref name="report" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref StatusReport report, byte[] data, ref int offset)
        {
            var start = offset;
            ByteData.Write(report.Construct, data, ref offset);
            ByteData.WriteString(report.Name ?? "", data, ref offset);
            ByteData.Write(report.Systems, data, ref offset);
            ByteData.Write(report.Normal, data, ref offset);
            ByteData.Write(report.KeptBlocks.Length, data, ref offset);
            for (var i = 0; i < report.KeptBlocks.Length; i++)
            {
                ByteData.Write(report.KeptBlocks[i], data, ref offset);
                ByteData.Write(report.KeptCounts[i], data, ref offset);
            }

            ByteData.Write(report.Items.Length, data, ref offset);
            foreach (var item in report.Items)
            {
                ByteData.Write((int)item.Level, data, ref offset);
                ByteData.WriteString(item.Logic ?? "", data, ref offset);
                ByteData.Write(item.Number, data, ref offset);
                ByteData.Write(item.Blueprint ? 1 : 0, data, ref offset);
                ByteData.WriteString(item.Installation ?? "", data, ref offset);
                ByteData.Write(item.Position, data, ref offset);
                ByteData.WriteString(item.Sequence ?? "", data, ref offset);
                ByteData.Write(item.Alone ? 1 : 0, data, ref offset);
                ByteData.Write(item.Row, data, ref offset);
                ByteData.Write(item.Halted ? 1 : 0, data, ref offset);
                ByteData.Write((int)item.Fault.Problem, data, ref offset);
                ByteData.Write(item.Fault.Step, data, ref offset);
                ByteData.WriteString(item.Fault.Which ?? "", data, ref offset);
                ByteData.Write((int)item.Fault.Unwired, data, ref offset);
                ByteData.Write((int)item.Fault.Because.Cause, data, ref offset);
                ByteData.WriteString(item.Fault.Because.About ?? "", data, ref offset);
            }

            return offset - start;
        }

        /// <summary>Reads a report at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out StatusReport report)
        {
            var start = offset;
            report = new StatusReport();
            int count;
            ByteData.TryReadInt64(data, ref offset, out report.Construct);
            ByteData.TryReadString(data, ref offset, out report.Name);
            ByteData.TryReadInt32(data, ref offset, out report.Systems);
            ByteData.TryReadInt32(data, ref offset, out report.Normal);
            int kept;
            ByteData.TryReadInt32(data, ref offset, out kept);
            report.KeptBlocks = new long[kept < 0 ? 0 : kept];
            report.KeptCounts = new int[report.KeptBlocks.Length];
            for (var i = 0; i < report.KeptBlocks.Length; i++)
            {
                ByteData.TryReadInt64(data, ref offset, out report.KeptBlocks[i]);
                ByteData.TryReadInt32(data, ref offset, out report.KeptCounts[i]);
            }

            ByteData.TryReadInt32(data, ref offset, out count);
            report.Items = new StatusItem[count < 0 ? 0 : count];
            for (var i = 0; i < report.Items.Length; i++)
                report.Items[i] = Item(data, ref offset);

            return offset - start;
        }

        /// <summary>One item, as it was written.</summary>
        static StatusItem Item(byte[] data, ref int offset)
        {
            int level, number, blueprint, position, alone, row, halted, problem, step, unwired, cause;
            string logic, installation, sequence, which, about;
            ByteData.TryReadInt32(data, ref offset, out level);
            ByteData.TryReadString(data, ref offset, out logic);
            ByteData.TryReadInt32(data, ref offset, out number);
            ByteData.TryReadInt32(data, ref offset, out blueprint);
            ByteData.TryReadString(data, ref offset, out installation);
            ByteData.TryReadInt32(data, ref offset, out position);
            ByteData.TryReadString(data, ref offset, out sequence);
            ByteData.TryReadInt32(data, ref offset, out alone);
            ByteData.TryReadInt32(data, ref offset, out row);
            ByteData.TryReadInt32(data, ref offset, out halted);
            ByteData.TryReadInt32(data, ref offset, out problem);
            ByteData.TryReadInt32(data, ref offset, out step);
            ByteData.TryReadString(data, ref offset, out which);
            ByteData.TryReadInt32(data, ref offset, out unwired);
            ByteData.TryReadInt32(data, ref offset, out cause);
            ByteData.TryReadString(data, ref offset, out about);

            var said = (SequenceProblem)problem;
            var because = (Cause)cause == Cause.None ? BlockTrouble.None : BlockTrouble.Kept((Cause)cause, string.IsNullOrEmpty(about) ? null : about);
            var fault = said == SequenceProblem.NeverTriggered
                ? SequenceFault.Unstarted((Unwired)unwired, which)
                : new SequenceFault(said, step, because, which);

            return new StatusItem
            {
                Level = (StatusLevel)level,
                Logic = logic ?? "",
                Number = number,
                Blueprint = blueprint != 0,
                Installation = installation ?? "",
                Position = position,
                Sequence = sequence ?? "",
                Alone = alone != 0,
                Row = row,
                Halted = halted != 0,
                Fault = fault
            };
        }
    }
}
