using System.Collections.Generic;
using Mal.MdkModMixin.Network;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Shares;

namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>The server telling one client how things stand with shared blueprints, as it sees them.</summary>
    public struct ShareNews
    {
        /// <summary>What the server said to a blueprint just offered, or null where none was.</summary>
        public ShareAnswer? Answer;

        /// <summary>How many blueprints each player may share.</summary>
        public int Limit;

        /// <summary>Whether this player is stopped from sharing.</summary>
        public bool IsBarred;

        /// <summary>The shares that reach this player, each with its blueprint.</summary>
        public List<ShareLine> Reaching;

        /// <summary>This player's own shares.</summary>
        public List<ShareLine> Mine;

        /// <summary>Every share in the world, for an admin; empty for anyone else.</summary>
        public List<ShareLine> All;

        /// <summary>The players of this world.</summary>
        public List<KnownPlayer> Known;

        /// <summary>How many bytes <paramref name="news" /> takes on the wire.</summary>
        public static int SizeOf(ref ShareNews news)
        {
            return sizeof(int) * 3 + SizeOf(news.Reaching) + SizeOf(news.Mine) + SizeOf(news.All) + SizeOf(news.Known);
        }

        /// <summary>Writes <paramref name="news" /> at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Write(ref ShareNews news, byte[] data, ref int offset)
        {
            var start = offset;
            ByteData.Write(news.Answer.HasValue ? (int)news.Answer.Value : -1, data, ref offset);
            ByteData.Write(news.Limit, data, ref offset);
            ByteData.Write(news.IsBarred ? 1 : 0, data, ref offset);
            Write(news.Reaching, data, ref offset);
            Write(news.Mine, data, ref offset);
            Write(news.All, data, ref offset);
            Write(news.Known, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads news at <paramref name="offset" />, and answers how many bytes it took.</summary>
        public static int Read(byte[] data, ref int offset, out ShareNews news)
        {
            var start = offset;
            news = new ShareNews();
            int answer;
            int barred;
            ByteData.TryReadInt32(data, ref offset, out answer);
            ByteData.TryReadInt32(data, ref offset, out news.Limit);
            ByteData.TryReadInt32(data, ref offset, out barred);
            news.Answer = answer < 0 ? (ShareAnswer?)null : (ShareAnswer)answer;
            news.IsBarred = barred != 0;
            news.Reaching = ReadLines(data, ref offset);
            news.Mine = ReadLines(data, ref offset);
            news.All = ReadLines(data, ref offset);
            news.Known = ReadPlayers(data, ref offset);
            return offset - start;
        }

        static int SizeOf(List<ShareLine> lines)
        {
            var size = sizeof(int);
            foreach (var line in lines)
            {
                size += sizeof(long) + sizeof(int) * 2 + ByteData.SizeOfString(line.SharerName) + ByteData.SizeOfString(line.Identity)
                        + ByteData.SizeOfString(line.Name) + ByteData.SizeOfString(line.Players) + ByteData.SizeOfString(line.Blueprint);
            }

            return size;
        }

        static int SizeOf(List<KnownPlayer> players)
        {
            var size = sizeof(int);
            foreach (var player in players)
                size += sizeof(long) + sizeof(int) + ByteData.SizeOfString(player.Name);

            return size;
        }

        static void Write(List<ShareLine> lines, byte[] data, ref int offset)
        {
            ByteData.Write(lines.Count, data, ref offset);
            foreach (var line in lines)
            {
                ByteData.Write(line.Sharer, data, ref offset);
                ByteData.Write((int)line.Reach, data, ref offset);
                ByteData.Write(line.IsNew ? 1 : 0, data, ref offset);
                ByteData.WriteString(line.SharerName, data, ref offset);
                ByteData.WriteString(line.Identity, data, ref offset);
                ByteData.WriteString(line.Name, data, ref offset);
                ByteData.WriteString(line.Players, data, ref offset);
                ByteData.WriteString(line.Blueprint, data, ref offset);
            }
        }

        static void Write(List<KnownPlayer> players, byte[] data, ref int offset)
        {
            ByteData.Write(players.Count, data, ref offset);
            foreach (var player in players)
            {
                ByteData.Write(player.Identity, data, ref offset);
                ByteData.Write((player.IsBlocked ? 1 : 0) | (player.IsBarred ? 2 : 0), data, ref offset);
                ByteData.WriteString(player.Name, data, ref offset);
            }
        }

        static List<ShareLine> ReadLines(byte[] data, ref int offset)
        {
            int count;
            ByteData.TryReadInt32(data, ref offset, out count);
            var lines = new List<ShareLine>(count);
            for (var i = 0; i < count; i++)
            {
                var line = new ShareLine();
                int reach;
                int isNew;
                ByteData.TryReadInt64(data, ref offset, out line.Sharer);
                ByteData.TryReadInt32(data, ref offset, out reach);
                ByteData.TryReadInt32(data, ref offset, out isNew);
                line.IsNew = isNew != 0;
                line.Reach = (ShareReach)reach;
                ByteData.TryReadString(data, ref offset, out line.SharerName);
                ByteData.TryReadString(data, ref offset, out line.Identity);
                ByteData.TryReadString(data, ref offset, out line.Name);
                ByteData.TryReadString(data, ref offset, out line.Players);
                ByteData.TryReadString(data, ref offset, out line.Blueprint);
                lines.Add(line);
            }

            return lines;
        }

        static List<KnownPlayer> ReadPlayers(byte[] data, ref int offset)
        {
            int count;
            ByteData.TryReadInt32(data, ref offset, out count);
            var players = new List<KnownPlayer>(count);
            for (var i = 0; i < count; i++)
            {
                var player = new KnownPlayer();
                int flags;
                ByteData.TryReadInt64(data, ref offset, out player.Identity);
                ByteData.TryReadInt32(data, ref offset, out flags);
                ByteData.TryReadString(data, ref offset, out player.Name);
                player.IsBlocked = (flags & 1) != 0;
                player.IsBarred = (flags & 2) != 0;
                players.Add(player);
            }

            return players;
        }
    }
}
