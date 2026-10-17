using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>The blueprints a player keeps for every world, each in a file of its own, and the index naming them.</summary>
    /// <remarks>
    /// The index is a list of file names, one to a line; a line starting <c>#</c> is a comment, and a blank line is
    /// nothing. A player adds a blueprint somebody gave them by putting its file beside the index and its name on a line
    /// of its own. Each file holds one blueprint as <see cref="Scribe.Blueprint(AutomationSystem, Guid)" /> writes it.
    /// <para>
    /// The index is edited line by line, so whatever a player wrote in it stays where they wrote it. A file is only ever
    /// written where it read as a blueprint, or where the library made it.
    /// </para>
    /// </remarks>
    public sealed class Shelf
    {
        const string Index = "sprocket-bp.index";

        const string Lead = "sprocket-bp-";

        const string Kind = ".cog";

        /// <summary>What makes a line of the index name a folder.</summary>
        static readonly char[] Folders = { '\\', '/', ':' };

        /// <summary>The characters the game refuses in a file name in mod storage.</summary>
        static readonly char[] Refused = { '\\', '/', '"', '<', '>', '|', ':', '*', '?' };

        /// <summary>What a new index says, so a player opening it for the first time sees how it is used.</summary>
        static readonly string[] Explained =
        {
            "# Blueprints for the automation library: one file name a line, each file in this folder.",
            "# A line starting with # is ignored."
        };

        readonly IFiles _files;

        readonly Action<string> _say;

        readonly Values _values = new Values();

        readonly List<string> _lines = new List<string>();

        readonly List<Volume> _all = new List<Volume>();

        /// <summary>A library kept in <paramref name="files" />, saying through <paramref name="say" /> what would not write.</summary>
        public Shelf(IFiles files, Action<string> say)
        {
            _files = files;
            _say = say;
        }

        /// <summary>Every line of the index that names a file, in the order the index gives them.</summary>
        public IReadOnlyList<Volume> All => _all;

        /// <summary>Whether the index read; where it did not, nothing is written over it or beside it.</summary>
        public bool Intact { get; private set; } = true;

        /// <summary>The copy of the blueprint <paramref name="identity" />, or null where the library has none.</summary>
        public Volume Find(Guid identity)
        {
            foreach (var volume in _all)
            {
                if (volume.State == VolumeState.Read && volume.Identity == identity)
                    return volume;
            }

            return null;
        }

        /// <summary>
        /// Puts the logic system <paramref name="blueprint" /> in the library as it is now, in place of the blueprint it is
        /// linked to, or as a new blueprint where it links to none here, and links it to the blueprint as it now is.
        /// </summary>
        /// <remarks>
        /// A file the library named after the blueprint is renamed with it; a file a player named keeps the name they gave
        /// it.
        /// </remarks>
        public Volume Keep(AutomationSystem blueprint)
        {
            var had = blueprint.HasOrigin ? Find(blueprint.Origin) : null;
            var identity = had?.Identity ?? Guid.NewGuid();
            var text = Scribe.Blueprint(blueprint, identity);
            if (had == null)
            {
                had = new Volume(Free(blueprint.Name), identity, blueprint.Namespace, blueprint.Name, blueprint.Changed, text);
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
        /// <remarks>A logic system with the identity of a stored blueprint was put on from it or made into it.</remarks>
        public void Link(AutomationSystem system)
        {
            if (system.HasOrigin || !system.IsBlueprint)
                return;

            var had = Find(system.Identity);
            if (had != null)
                Link(system, had);
        }

        /// <summary>Takes <paramref name="volume" /> out of the library, and deletes its file where it held a blueprint.</summary>
        public void Forget(Volume volume)
        {
            if (!_all.Remove(volume))
                return;

            var line = LineOf(volume.File);
            if (line >= 0)
                _lines.RemoveAt(line);

            WriteIndex();
            if (volume.State == VolumeState.Read)
                Delete(volume.File);
        }

        /// <summary>Whether the blueprint the logic system <paramref name="blueprint" /> is linked to is the logic system as it is now.</summary>
        public bool Holds(AutomationSystem blueprint)
        {
            var had = blueprint.HasOrigin ? Find(blueprint.Origin) : null;
            return had != null && had.Text == Scribe.Blueprint(blueprint, had.Identity);
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
                    // Made at once, so the index is always there beside the blueprints for a player to add a line to.
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
                if (volume.State != VolumeState.Read)
                    trouble.Append(trouble.Length > 0 ? "; " : "").Append(name).Append(": ").Append(volume.Trouble);
            }

            return trouble.ToString();
        }

        /// <summary>The file <paramref name="name" /> read as a blueprint, or why it would not.</summary>
        Volume Open(string name)
        {
            if (name == "." || name == ".." || name.IndexOfAny(Folders) >= 0)
                return new Volume(name, VolumeState.Path, "a line of the index names a folder; it may only name a file.");

            string text;
            try
            {
                if (!_files.Has(name))
                    return new Volume(name, VolumeState.Missing, "there is no file of that name.");

                text = _files.Read(name);
            }
            catch (Exception failed)
            {
                return new Volume(name, VolumeState.Missing, failed.Message);
            }

            AutomationSystem read;
            var trouble = Reader.Lone(text, _values, out read);
            if (trouble.Length > 0)
                return new Volume(name, VolumeState.Unreadable, trouble);

            if (Find(read.Identity) != null)
                return new Volume(name, VolumeState.Twice, $"system {read.Identity:D} is already in the library.");

            return new Volume(name, read.Identity, read.Namespace, read.Name, read.Changed, text);
        }

        static void Link(AutomationSystem system, Volume volume)
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
