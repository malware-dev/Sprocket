using Sprocket.Host.SpaceEngineers.Framework.Language.Format;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Sharing.Text
{
    /// <summary>
    /// A blueprint as text a player can copy anywhere and paste into a block's Custom Data in another world: compact, an ini
    /// section named <c>[sprocket-blueprint</c> and the blueprint's identity<c>]</c>, with its name, the version of this form,
    /// when it was last changed, and the packed blueprint; or plain, the blueprint itself as the whole of the Custom Data.
    /// </summary>
    /// <remarks>
    /// The identity in the section's name keeps every section's name its own, so a script reading the same Custom Data with
    /// MyIni, which refuses a name used twice, still reads it. <c>data</c> is the section's last key and runs to the section's
    /// end, written as MyIni's multi-line value: lines of <see cref="Width" /> characters, each after a <c>|</c>. Spaces, line
    /// breaks and <c>|</c> in it are ignored, so a text wrapped again by a chat or a forum still reads.
    /// </remarks>
    public static class BlueprintText
    {
        /// <summary>What a section's name starts with, before the blueprint's identity.</summary>
        public const string Section = "sprocket-blueprint";

        /// <summary>How many characters of data go on a line, narrow enough for the game's text editor.</summary>
        public const int Width = 64;

        /// <summary>The version of the packing this build writes and reads.</summary>
        public const int Version = 1;

        /// <summary>
        /// The compact section holding <paramref name="blueprint" />, called <paramref name="name" />; its identity is in the
        /// section's name and when it was last changed is a key, both for a player to read, and neither is part of the data.
        /// </summary>
        public static string Write(string name, string blueprint)
        {
            AutomationSystem system;
            DateTime? changed = null;
            var identity = Guid.Empty;
            if (ProgramReader.Lone(blueprint, new Values(), out system).Length == 0)
            {
                changed = system.Changed;
                identity = system.Identity;
                system.Changed = null;
                blueprint = ProgramWriter.Blueprint(system, Guid.Empty);
            }

            var coded = TokenCode.Encode(blueprint);
            var check = Check(coded);
            var data = new List<byte> { (byte)(check >> 8), (byte)check };
            data.AddRange(BytePacker.Pack(coded));
            var typed = TypedBytes.Write(data.ToArray());
            var text = new StringBuilder("[" + Section + " " + identity.ToString("D") + "]\nname=" + name.Replace('\n', ' ').Replace('\r', ' ') + "\n");
            if (changed.HasValue)
                text.Append("changed=").Append(changed.Value.ToString(ScriptFormat.Time, CultureInfo.InvariantCulture)).Append('\n');
            text.Append("version=").Append(Version).Append("\ndata=\n");
            for (var at = 0; at < typed.Length; at += Width)
                text.Append('|').Append(typed, at, Math.Min(Width, typed.Length - at)).Append('\n');

            return text.ToString();
        }

        /// <summary>The CRC-16 (CCITT, from 0xFFFF) of <paramref name="bytes" />, which the data starts with, so damage is found before reading.</summary>
        static int Check(byte[] bytes)
        {
            var check = 0xFFFF;
            foreach (var b in bytes)
            {
                check ^= b << 8;
                for (var bit = 0; bit < 8; bit++)
                    check = (check & 0x8000) != 0 ? ((check << 1) ^ 0x1021) & 0xFFFF : (check << 1) & 0xFFFF;
            }

            return check;
        }

        /// <summary>
        /// The blueprints in <paramref name="text" />, such as a block's Custom Data: the one plain blueprint it is as a whole, or
        /// every compact section in it, in the order they are written.
        /// </summary>
        public static List<SharedText> Read(string text)
        {
            var found = new List<SharedText>();
            if (text.TrimStart().StartsWith(Keywords.Name(Keyword.Sprocket) + " ", StringComparison.Ordinal))
            {
                found.Add(new SharedText { Blueprint = text.Trim() });
                return found;
            }

            SharedText open = null;
            StringBuilder data = null;
            foreach (var raw in text.Replace("\r", "").Split('\n'))
            {
                var line = raw.Trim();
                if (line.StartsWith("[", StringComparison.Ordinal))
                {
                    Close(open, data);
                    open = null;
                    data = null;
                    Guid identity;
                    if (Named(line, out identity))
                        found.Add(open = new SharedText { Identity = identity });
                    continue;
                }

                if (open == null)
                    continue;

                if (data != null)
                {
                    data.Append(Data(line));
                    continue;
                }

                var equals = line.IndexOf('=');
                if (equals < 0)
                    continue;

                var key = line.Substring(0, equals).Trim();
                var value = line.Substring(equals + 1).Trim();
                if (key == "name")
                    open.Name = value;
                else if (key == "version")
                    open.Version = value;
                else if (key == "changed")
                    open.Changed = Changed(value);
                else if (key == "data")
                    data = new StringBuilder(Data(value));
            }

            Close(open, data);
            return found;
        }

        /// <summary>Whether <paramref name="line" /> starts a compact section, whose name gives the blueprint's <paramref name="identity" />.</summary>
        static bool Named(string line, out Guid identity)
        {
            identity = Guid.Empty;
            var start = "[" + Section + " ";
            return line.StartsWith(start, StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal) &&
                   Guid.TryParseExact(line.Substring(start.Length, line.Length - start.Length - 1).Trim(), "D", out identity);
        }

        /// <summary>The time <paramref name="value" /> writes, in UTC, or null where it is not one.</summary>
        static DateTime? Changed(string value)
        {
            DateTime changed;
            return DateTime.TryParseExact(value, ScriptFormat.Time, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out changed)
                ? changed
                : (DateTime?)null;
        }

        /// <summary>The characters of data in <paramref name="line" />, without spaces and line markers.</summary>
        static string Data(string line)
        {
            return line.Replace(" ", "").Replace("\t", "").Replace("|", "");
        }

        static void Close(SharedText open, StringBuilder data)
        {
            if (open == null)
                return;

            if (open.Version != Version.ToString(CultureInfo.InvariantCulture) || data == null)
                return;

            var read = TypedBytes.Read(data.ToString());
            if (read == null || read.Length < 2)
                return;

            var packed = new byte[read.Length - 2];
            Array.Copy(read, 2, packed, 0, packed.Length);
            var bytes = BytePacker.Unpack(packed);
            if (bytes == null || Check(bytes) != ((read[0] << 8) | read[1]))
                return;

            // The data holds the blueprint under the empty identity; its own is the one the section's name gives.
            var decoded = TokenCode.Decode(bytes);
            open.Blueprint = decoded?.Replace("\"" + Guid.Empty.ToString("D") + "\"", "\"" + (open.Identity ?? Guid.Empty).ToString("D") + "\"");
        }
    }
}
