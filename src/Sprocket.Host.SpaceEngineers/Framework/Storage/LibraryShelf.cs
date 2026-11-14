using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage
{
    /// <summary>The blueprints a player keeps for every world, each in a file of its own, and the index naming them.</summary>
    /// <remarks>
    /// Persisted: the index is a list of file names, one to a line; a line starting <c>#</c> is a comment, and a blank line
    /// is nothing. Each file holds one blueprint as <see cref="ProgramWriter.Blueprint(AutomationSystem, Guid)" /> writes it.
    /// </remarks>
    public sealed class LibraryShelf
    {
        const string Index = "sprocket-bp.index";

        const string Lead = "sprocket-bp-";

        const string Kind = ".cog";

        static readonly char[] Folders = { '\\', '/', ':' };

        static readonly char[] Refused = { '\\', '/', '"', '<', '>', '|', ':', '*', '?' };

        static readonly string[] Explained =
        {
            "# Blueprints for the automation library: one file name a line, each file in this folder.",
            "# A line starting with # is ignored."
        };

        readonly IFiles _files;

        readonly Action<string> _say;

        readonly Values _values = new Values();

        readonly List<string> _lines = new List<string>();

        readonly List<LibraryEntry> _all = new List<LibraryEntry>();

        /// <summary>A library kept in <paramref name="files" />, saying through <paramref name="say" /> what would not write.</summary>
        public LibraryShelf(IFiles files, Action<string> say)
        {
            _files = files;
            _say = say;
        }

        /// <summary>Every line of the index that names a file, in the order the index gives them.</summary>
        public IReadOnlyList<LibraryEntry> All => _all;

        /// <summary>Whether the index read; where it did not, nothing is written over it or beside it.</summary>
        public bool Intact { get; private set; } = true;

        /// <summary>The copy of the blueprint <paramref name="identity" />, or null where the library has none.</summary>
        public LibraryEntry Find(Guid identity)
        {
            foreach (var volume in _all)
            {
                if (volume.State == LibraryEntryState.Read && volume.Identity == identity)
                    return volume;
            }

            return null;
        }

        /// <summary>
        /// Puts the logic system <paramref name="blueprint" /> in the library as it is now, in place of the blueprint it is
        /// linked to, or as a new blueprint where it links to none here, and links it to the blueprint as it now is.
        /// </summary>
        /// <remarks>A file the library named after the blueprint is renamed with it; a file a player named keeps its name.</remarks>
        public LibraryEntry Keep(AutomationSystem blueprint)
        {
            var had = blueprint.HasOrigin ? Find(blueprint.Origin) : null;
            var identity = had?.Identity ?? Guid.NewGuid();
            var text = ProgramWriter.Blueprint(blueprint, identity);
            if (had == null)
            {
                had = new LibraryEntry(Free(blueprint.Name), identity, blueprint.Namespace, blueprint.Name, blueprint.Changed, text);
                _all.Add(had);
                _lines.Add(had.File);
                Write(had.File, text);
                WriteIndex();
                Link(blueprint, had);
                return had;
            }

            var file = had.File;
            if (blueprint.Name != had.Name && file == Lead + Clean(had.Name) + Kind)
                file = Free(blueprint.Name);

            had.Replace(blueprint.Namespace, blueprint.Name, blueprint.Changed, text);
            Write(file, text);
            if (file != had.File)
            {
                var was = had.File;
                _lines[LineOf(was)] = file;
                had.Move(file);
                WriteIndex();
                Delete(was);
            }

            Link(blueprint, had);
            return had;
        }

        /// <summary>
        /// Links <paramref name="system" />, where it links to no blueprint, to the blueprint here that has its identity, at that
        /// blueprint's version.
        /// </summary>
        public void Link(AutomationSystem system)
        {
            if (system.HasOrigin || !system.IsBlueprint)
                return;

            var had = Find(system.Identity);
            if (had != null)
                Link(system, had);
        }

        /// <summary>Takes <paramref name="volume" /> out of the library, and deletes its file where it held a blueprint.</summary>
        public void Forget(LibraryEntry volume)
        {
            if (!_all.Remove(volume))
                return;

            var line = LineOf(volume.File);
            if (line >= 0)
                _lines.RemoveAt(line);

            WriteIndex();
            if (volume.State == LibraryEntryState.Read)
                Delete(volume.File);
        }

        /// <summary>
        /// Whether the blueprint the logic system <paramref name="blueprint" /> is linked to is the logic system as it is now, by their
        /// dates: the blueprint is still the version the logic system was made from or last brought up to date with, and the logic
        /// system has not changed since.
        /// </summary>
        public bool Holds(AutomationSystem blueprint)
        {
            var had = blueprint.HasOrigin ? Find(blueprint.Origin) : null;
            if (had == null || had.Changed != blueprint.OriginChanged)
                return false;

            return !blueprint.Changed.HasValue || blueprint.OriginChanged.HasValue && blueprint.Changed.Value <= blueprint.OriginChanged.Value;
        }

        /// <summary>Reads the index and every file it names, answering what was wrong with any of them, or empty.</summary>
        /// <remarks>A file that will not read is listed with what was wrong; the rest are read all the same.</remarks>
        public string Read()
        {
            _lines.Clear();
            _all.Clear();
            Intact = true;

            string index;
            try
            {
                if (!_files.Has(Index))
                {
                    // Made at once, so a player always finds the index to add a line to.
                    _lines.AddRange(Explained);
                    WriteIndex();
                    return "";
                }

                index = _files.Read(Index);
            }
            catch (Exception failed)
            {
                Intact = false;
                return $"{Index} will not open - {failed.Message}";
            }

            var trouble = new StringBuilder();
            var lines = index.Split('\n');
            var last = lines.Length > 0 && lines[lines.Length - 1].Length == 0 ? lines.Length - 1 : lines.Length;
            for (var i = 0; i < last; i++)
            {
                var line = lines[i].TrimEnd('\r');
                _lines.Add(line);

                var name = line.Trim();
                if (name.Length == 0 || name[0] == '#')
                    continue;

                var volume = Open(name);
                _all.Add(volume);
                if (volume.State != LibraryEntryState.Read)
                    trouble.Append(trouble.Length > 0 ? "; " : "").Append(name).Append(": ").Append(volume.Trouble);
            }

            return trouble.ToString();
        }

        /// <summary>The file <paramref name="name" /> read as a blueprint, or why it would not.</summary>
        LibraryEntry Open(string name)
        {
            if (name == "." || name == ".." || name.IndexOfAny(Folders) >= 0)
                return new LibraryEntry(name, LibraryEntryState.Path, "a line of the index names a folder; it may only name a file.");

            string text;
            try
            {
                if (!_files.Has(name))
                    return new LibraryEntry(name, LibraryEntryState.Missing, "there is no file of that name.");

                text = _files.Read(name);
            }
            catch (Exception failed)
            {
                return new LibraryEntry(name, LibraryEntryState.Missing, failed.Message);
            }

            AutomationSystem read;
            var trouble = ProgramReader.Lone(text, _values, out read);
            if (trouble.Length > 0)
                return new LibraryEntry(name, LibraryEntryState.Unreadable, trouble);

            if (Find(read.Identity) != null)
                return new LibraryEntry(name, LibraryEntryState.Twice, $"system {read.Identity:D} is already in the library.");

            return new LibraryEntry(name, read.Identity, read.Namespace, read.Name, read.Changed, text);
        }

        static void Link(AutomationSystem system, LibraryEntry volume)
        {
            system.Origin = volume.Identity;
            system.OriginChanged = volume.Changed;
        }

        /// <summary>The line of the index naming <paramref name="file" />, or -1.</summary>
        int LineOf(string file)
        {
            for (var i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].Trim() == file)
                    return i;
            }

            return -1;
        }

        /// <summary>The name the library gives a new file for the blueprint <paramref name="name" />, numbered where it is taken.</summary>
        string Free(string name)
        {
            var clean = Clean(name);
            var file = Lead + clean + Kind;
            for (var number = 2; Taken(file); number++)
                file = $"{Lead}{clean} {number.ToString(CultureInfo.InvariantCulture)}{Kind}";

            return file;
        }

        /// <summary>Whether the index names <paramref name="file" /> or storage holds a file of that name.</summary>
        /// <remarks>Storage that cannot be asked counts as holding none, so the write that follows is what says it failed.</remarks>
        bool Taken(string file)
        {
            if (LineOf(file) >= 0)
                return true;

            try
            {
                return _files.Has(file);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary><paramref name="name" /> with every character a file name cannot hold put as <c>_</c>.</summary>
        static string Clean(string name)
        {
            var clean = new StringBuilder(name.Length);
            foreach (var character in name)
            {
                // Refused holds the characters the game refuses in a file name in mod storage.
                clean.Append(character < ' ' || Array.IndexOf(Refused, character) >= 0 ? '_' : character);
            }

            return clean.ToString().Trim().TrimEnd('.');
        }

        void WriteIndex()
        {
            var text = new StringBuilder();
            foreach (var line in _lines)
                text.Append(line).Append('\n');

            Write(Index, text.ToString());
        }

        /// <summary>Writes <paramref name="text" /> to <paramref name="file" />, saying in the log that it did, or why it did not.</summary>
        void Write(string file, string text)
        {
            if (!Intact)
            {
                _say($"{file} is not written, because the library's index did not read");
                return;
            }

            try
            {
                _files.Write(file, text);
                _say($"wrote {file} in the library");
            }
            catch (Exception failed)
            {
                _say($"{file} will not write - {failed.Message}");
            }
        }

        void Delete(string file)
        {
            if (!Intact)
                return;

            try
            {
                _files.Delete(file);
            }
            catch (Exception failed)
            {
                _say($"{file} will not delete - {failed.Message}");
            }
        }
    }
}
