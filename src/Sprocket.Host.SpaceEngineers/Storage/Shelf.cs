using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>The blueprints a player keeps for every world, and the text they are kept as.</summary>
    /// <remarks>
    /// The same for every world the player opens, and kept on their own machine; where it is kept is not this type's
    /// business. One copy per blueprint, known by the blueprint's identity.
    /// <para>
    /// The text is a line saying what it is, then each blueprint as a line of facts followed by its namespace, name and
    /// text written out at the lengths that line gives, so nothing inside them needs escaping:
    /// <code>
    /// library 1
    /// volume 3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43 2026-10-02T14:30:00.0000000Z 7 7 412
    /// MalwareAirlocksprocket 1 alpha 9 ...
    /// </code>
    /// </para>
    /// </remarks>
    public sealed class Shelf
    {
        const string Head = "library 1";
        const string Line = "volume ";

        readonly List<Volume> _all = new List<Volume>();

        /// <summary>Raised whenever a blueprint is put in or taken out, so whoever keeps the library writes it.</summary>
        public event Action Changed;

        /// <summary>Every blueprint, in the order they were first put in.</summary>
        public IReadOnlyList<Volume> All => _all;

        /// <summary>Whether what was read was read whole; where it was not, nothing is written over it.</summary>
        public bool Intact { get; private set; } = true;

        /// <summary>The copy of the blueprint <paramref name="identity" />, or null where the library has none.</summary>
        public Volume Find(Guid identity)
        {
            for (var i = 0; i < _all.Count; i++)
            {
                if (_all[i].Identity == identity)
                    return _all[i];
            }

            return null;
        }

        /// <summary>Puts <paramref name="blueprint" /> in the library as it is now, in place of any copy already there.</summary>
        public Volume Keep(AutomationSystem blueprint, DateTime saved)
        {
            var text = ProgramText.Blueprint(blueprint);
            var had = Find(blueprint.Identity);
            if (had != null)
                had.Replace(blueprint.Namespace, blueprint.Name, saved, text);
            else
                _all.Add(had = new Volume(blueprint.Identity, blueprint.Namespace, blueprint.Name, saved, text));

            Tell();
            return had;
        }

        /// <summary>Takes <paramref name="volume" /> out of the library.</summary>
        public void Forget(Volume volume)
        {
            if (_all.Remove(volume))
                Tell();
        }

        /// <summary>Whether the library's copy of <paramref name="blueprint" /> is the blueprint as it is now.</summary>
        public bool Holds(AutomationSystem blueprint)
        {
            var had = Find(blueprint.Identity);
            return had != null && had.Text == ProgramText.Blueprint(blueprint);
        }

        /// <summary>The library as text, to be kept.</summary>
        public string Write()
        {
            var text = new StringBuilder(Head).Append('\n');
            for (var i = 0; i < _all.Count; i++)
            {
                var volume = _all[i];
                text.Append(Line).Append(volume.Identity.ToString("D")).Append(' ')
                    .Append(volume.Saved.ToString("o", CultureInfo.InvariantCulture)).Append(' ')
                    .Append(volume.Namespace.Length).Append(' ').Append(volume.Name.Length).Append(' ')
                    .Append(volume.Text.Length).Append('\n');
                text.Append(volume.Namespace).Append(volume.Name).Append(volume.Text).Append('\n');
            }

            return text.ToString();
        }

        /// <summary>Reads a library kept as <paramref name="text" />, answering what was wrong with it, or empty.</summary>
        /// <remarks>Whole or not at all: where any of it will not read, none of it is taken, and it is never written over.</remarks>
        public string Read(string text)
        {
            _all.Clear();
            Intact = true;

            var read = new List<Volume>();
            var trouble = Parse(text ?? "", read);
            if (trouble.Length > 0)
            {
                Intact = false;
                return trouble;
            }

            _all.AddRange(read);
            return "";
        }

        void Tell()
        {
            if (Changed != null)
                Changed();
        }

        static string Parse(string text, List<Volume> into)
        {
            var at = 0;
            var head = Next(text, ref at);
            if (head != Head)
                return "the library does not start with \"" + Head + "\"";

            while (at < text.Length)
            {
                var line = Next(text, ref at);
                if (!line.StartsWith(Line, StringComparison.Ordinal))
                    return "a line of the library is not a blueprint: " + line;

                var words = line.Substring(Line.Length).Split(' ');
                Guid identity;
                DateTime saved;
                int space;
                int name;
                int body;
                if (words.Length != 5
                    || !Guid.TryParseExact(words[0], "D", out identity)
                    || !DateTime.TryParse(words[1], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out saved)
                    || !int.TryParse(words[2], NumberStyles.None, CultureInfo.InvariantCulture, out space)
                    || !int.TryParse(words[3], NumberStyles.None, CultureInfo.InvariantCulture, out name)
                    || !int.TryParse(words[4], NumberStyles.None, CultureInfo.InvariantCulture, out body)
                    || at + space + name + body + 1 > text.Length
                    || text[at + space + name + body] != '\n')
                {
                    return "a blueprint's line in the library will not read: " + line;
                }

                into.Add(new Volume(identity, text.Substring(at, space), text.Substring(at + space, name), saved,
                    text.Substring(at + space + name, body)));
                at += space + name + body + 1;
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
