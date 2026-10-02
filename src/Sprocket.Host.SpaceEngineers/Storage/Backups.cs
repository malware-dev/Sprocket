using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>Every construct's backup in the world, and the text they are kept as.</summary>
    /// <remarks>
    /// One backup per construct, taken again each time the world saves; one whose construct is gone stays until somebody
    /// deletes it. Where they are kept is not this type's business - see <c>Carrier</c>.
    /// <para>
    /// The text is a line saying what it is, then each backup as a line of numbers followed by its name, program and
    /// switches written out at the lengths that line gives, so nothing inside them needs escaping:
    /// <code>
    /// backups 1
    /// backup 2026-10-02T14:30:00.0000000Z 2 118 119 14 5021 0
    /// Hangar Airlocksprocket 1 alpha 9 ...
    /// </code>
    /// </para>
    /// </remarks>
    public sealed class Backups
    {
        const string Head = "backups 1";
        const string Line = "backup ";

        readonly List<Backup> _all = new List<Backup>();

        /// <summary>Every backup, in the order they were first taken.</summary>
        public IReadOnlyList<Backup> All => _all;

        /// <summary>Whether what was read was read whole; where it was not, nothing is written over it.</summary>
        public bool Intact { get; private set; } = true;

        /// <summary>The backup of the construct made of <paramref name="grids" />, or null where it has none.</summary>
        public Backup Of(IList<long> grids)
        {
            for (var i = 0; i < _all.Count; i++)
            {
                if (_all[i].Of(grids))
                    return _all[i];
            }

            return null;
        }

        /// <summary>Takes the backup of the construct made of <paramref name="grids" /> again, or takes its first.</summary>
        public void Take(string name, DateTime saved, IList<long> grids, string program, string switches)
        {
            var had = Of(grids);
            if (had != null)
                had.Retake(name, saved, grids, program, switches);
            else
                _all.Add(new Backup(name, saved, grids, program, switches));
        }

        /// <summary>Forgets <paramref name="backup" />, which is no longer kept from the next save on.</summary>
        public void Forget(Backup backup)
        {
            _all.Remove(backup);
        }

        /// <summary>The backups as text, to be kept.</summary>
        public string Write()
        {
            var text = new StringBuilder(Head).Append('\n');
            for (var i = 0; i < _all.Count; i++)
            {
                var backup = _all[i];
                text.Append(Line).Append(backup.Saved.ToString("o", CultureInfo.InvariantCulture));
                text.Append(' ').Append(backup.Grids.Count);
                for (var g = 0; g < backup.Grids.Count; g++)
                    text.Append(' ').Append(backup.Grids[g].ToString(CultureInfo.InvariantCulture));

                text.Append(' ').Append(backup.Name.Length).Append(' ').Append(backup.Program.Length).Append(' ')
                    .Append(backup.Switches.Length).Append('\n');
                text.Append(backup.Name).Append(backup.Program).Append(backup.Switches).Append('\n');
            }

            return text.ToString();
        }

        /// <summary>Reads backups kept as <paramref name="text" />, answering what was wrong with it, or empty.</summary>
        /// <remarks>Whole or not at all: where any of it will not read, none of it is taken, and it is never written over.</remarks>
        public string Read(string text)
        {
            _all.Clear();
            Intact = true;

            var read = new List<Backup>();
            var trouble = Parse(text ?? "", read);
            if (trouble.Length > 0)
            {
                Intact = false;
                return trouble;
            }

            _all.AddRange(read);
            return "";
        }

        static string Parse(string text, List<Backup> into)
        {
            var at = 0;
            var head = Next(text, ref at);
            if (head != Head)
                return "the backups do not start with \"" + Head + "\"";

            while (at < text.Length)
            {
                var line = Next(text, ref at);
                if (!line.StartsWith(Line, StringComparison.Ordinal))
                    return "a line of the backups is not a backup: " + line;

                var words = line.Substring(Line.Length).Split(' ');
                DateTime saved;
                int grids;
                if (words.Length < 2
                    || !DateTime.TryParse(words[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out saved)
                    || !int.TryParse(words[1], NumberStyles.None, CultureInfo.InvariantCulture, out grids)
                    || words.Length != 2 + grids + 3)
                {
                    return "a backup's line will not read: " + line;
                }

                var ids = new List<long>();
                for (var g = 0; g < grids; g++)
                {
                    long id;
                    if (!long.TryParse(words[2 + g], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out id))
                        return "a backup's grid will not read: " + words[2 + g];

                    ids.Add(id);
                }

                int name;
                int program;
                int switches;
                if (!int.TryParse(words[2 + grids], NumberStyles.None, CultureInfo.InvariantCulture, out name)
                    || !int.TryParse(words[3 + grids], NumberStyles.None, CultureInfo.InvariantCulture, out program)
                    || !int.TryParse(words[4 + grids], NumberStyles.None, CultureInfo.InvariantCulture, out switches)
                    || at + name + program + switches + 1 > text.Length
                    || text[at + name + program + switches] != '\n')
                {
                    return "a backup's lengths do not match what follows: " + line;
                }

                into.Add(new Backup(text.Substring(at, name), saved, ids, text.Substring(at + name, program),
                    text.Substring(at + name + program, switches)));
                at += name + program + switches + 1;
            }

            return "";
        }

        /// <summary>The line from <paramref name="at" />, moving past its end.</summary>
        static string Next(string text, ref int at)
        {
            var end = text.IndexOf('\n', at);
            if (end < 0)
                end = text.Length;

            var line = text.Substring(at, end - at);
            at = Math.Min(end + 1, text.Length);
            return line;
        }
    }
}
