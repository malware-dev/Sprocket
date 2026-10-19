using Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Network;

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

        /// <summary>How many bytes <paramref name="report" /> takes on the wire.</summary>
        public static int SizeOf(ref StatusReport report)
        {
            var size = sizeof(long) + ByteCodec.SizeOfString(report.Name ?? "") + sizeof(int) * 3;
            foreach (var item in report.Items)
            {
                size += sizeof(int) * 11 + ByteCodec.SizeOfString(item.Logic ?? "") + ByteCodec.SizeOfString(item.Installation ?? "")
                    + ByteCodec.SizeOfString(item.Sequence ?? "") + ByteCodec.SizeOfString(item.Fault.Which ?? "")
                    + ByteCodec.SizeOfString(item.Fault.Because.About ?? "");
            }

            return size;
        }

        /// <summary>Writes <paramref name="report" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref StatusReport report, byte[] data, ref int offset)
        {
            var start = offset;
            ByteCodec.Write(report.Construct, data, ref offset);
            ByteCodec.WriteString(report.Name ?? "", data, ref offset);
            ByteCodec.Write(report.Systems, data, ref offset);
            ByteCodec.Write(report.Normal, data, ref offset);
            ByteCodec.Write(report.Items.Length, data, ref offset);
            foreach (var item in report.Items)
            {
                ByteCodec.Write((int)item.Level, data, ref offset);
                ByteCodec.WriteString(item.Logic ?? "", data, ref offset);
                ByteCodec.Write(item.Number, data, ref offset);
                ByteCodec.Write(item.Blueprint ? 1 : 0, data, ref offset);
                ByteCodec.WriteString(item.Installation ?? "", data, ref offset);
                ByteCodec.Write(item.Position, data, ref offset);
                ByteCodec.WriteString(item.Sequence ?? "", data, ref offset);
                ByteCodec.Write(item.Alone ? 1 : 0, data, ref offset);
                ByteCodec.Write(item.Row, data, ref offset);
                ByteCodec.Write(item.Halted ? 1 : 0, data, ref offset);
                ByteCodec.Write((int)item.Fault.Problem, data, ref offset);
                ByteCodec.Write(item.Fault.Step, data, ref offset);
                ByteCodec.WriteString(item.Fault.Which ?? "", data, ref offset);
                ByteCodec.Write((int)item.Fault.Unwired, data, ref offset);
                ByteCodec.Write((int)item.Fault.Because.Cause, data, ref offset);
                ByteCodec.WriteString(item.Fault.Because.About ?? "", data, ref offset);
            }

            return offset - start;
        }

        /// <summary>Reads a report at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out StatusReport report)
        {
            var start = offset;
            report = new StatusReport();
            int count;
            ByteCodec.TryReadInt64(data, ref offset, out report.Construct);
            ByteCodec.TryReadString(data, ref offset, out report.Name);
            ByteCodec.TryReadInt32(data, ref offset, out report.Systems);
            ByteCodec.TryReadInt32(data, ref offset, out report.Normal);
            ByteCodec.TryReadInt32(data, ref offset, out count);
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
            ByteCodec.TryReadInt32(data, ref offset, out level);
            ByteCodec.TryReadString(data, ref offset, out logic);
            ByteCodec.TryReadInt32(data, ref offset, out number);
            ByteCodec.TryReadInt32(data, ref offset, out blueprint);
            ByteCodec.TryReadString(data, ref offset, out installation);
            ByteCodec.TryReadInt32(data, ref offset, out position);
            ByteCodec.TryReadString(data, ref offset, out sequence);
            ByteCodec.TryReadInt32(data, ref offset, out alone);
            ByteCodec.TryReadInt32(data, ref offset, out row);
            ByteCodec.TryReadInt32(data, ref offset, out halted);
            ByteCodec.TryReadInt32(data, ref offset, out problem);
            ByteCodec.TryReadInt32(data, ref offset, out step);
            ByteCodec.TryReadString(data, ref offset, out which);
            ByteCodec.TryReadInt32(data, ref offset, out unwired);
            ByteCodec.TryReadInt32(data, ref offset, out cause);
            ByteCodec.TryReadString(data, ref offset, out about);

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
