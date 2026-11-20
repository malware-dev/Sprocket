using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage
{
    /// <summary>The logic systems stored of each construct as it lost its automation, each blueprint in a file of its own, and the index listing them.</summary>
    /// <remarks>
    /// Persisted: the index is an ini file with a section for each blueprint file:
    /// <code>
    /// [backup-1.cog]
    /// system=6f1c0f0e-3c2b-4e8a-9d0f-1a2b3c4d5e6f
    /// name=Cycle In
    /// construct=Hangar Airlock
    /// lost=2026-10-02T14:30:00.0000000Z
    /// </code>
    /// Each file holds one logic system as <see cref="ProgramWriter.Linked" /> writes it. Nothing is written until <see cref="Read" />
    /// has been called.
    /// </remarks>
    public sealed class Backups
    {
        const string Index = "backups.ini";

        const string Lead = "backup-";

        const string Kind = ".cog";

        const string SystemKey = "system";

        const string NameKey = "name";

        const string ConstructKey = "construct";

        const string LostKey = "lost";

        readonly IFiles _files;

        readonly List<Backup> _all = new List<Backup>();

        readonly List<string> _sections = new List<string>();

        bool _open;

        /// <summary>Backups stored in <paramref name="files" />, once they have been read from it.</summary>
        public Backups(IFiles files)
        {
            _files = files;
        }

        /// <summary>Every backup, in the order they were first taken.</summary>
        public IReadOnlyList<Backup> All => _all;

        /// <summary>Whether the index read whole; where it did not, nothing is written over it.</summary>
        public bool Intact { get; private set; } = true;

        /// <summary>Whether what changes is written: once the index has been read, and only where it read whole.</summary>
        bool Writes => _open && Intact;

        /// <summary>Reads the index, answering what was wrong with it, or empty.</summary>
        /// <remarks>Where any of it will not read, none of it is taken and it is never written over.</remarks>
        public string Read()
        {
            _all.Clear();
            Intact = true;
            _open = true;

            string text;
            try
            {
                if (!_files.Has(Index))
                    return "";

                text = _files.Read(Index);
            }
            catch (Exception failed)
            {
                Intact = false;
                return $"{Index} will not open - {failed.Message}";
            }

            var ini = new MyIni();
            MyIniParseResult parsed;
            if (!ini.TryParse(text, out parsed))
            {
                Intact = false;
                return $"{Index} has a problem on line {parsed.LineNo} - {parsed.Error}";
            }

            _sections.Clear();
            ini.GetSections(_sections);
            var read = new List<Backup>();
            foreach (var section in _sections)
            {
                var backup = Entry(ini, section);
                if (backup == null)
                {
                    Intact = false;
                    return $"{Index} says too little of {section}";
                }

                read.Add(backup);
            }

            _all.AddRange(read);
            return "";
        }

        /// <summary>
        /// Stores <paramref name="systems" />, the logic systems of the construct called <paramref name="construct" />, as it
        /// loses its automation at <paramref name="lost" />; one already backed up as it is, by its identity and the date it last
        /// changed, is not stored twice. <paramref name="values" /> reads the backups already stored.
        /// </summary>
        public void Take(string construct, DateTime lost, IList<AutomationSystem> systems, Values values)
        {
            for (var i = 0; i < systems.Count; i++)
            {
                var system = systems[i];
                if (Stored(system, values) != null)
                    continue;

                var backup = new Backup(Free(), system.Identity, system.Name, construct, lost);
                _all.Add(backup);
                if (Writes)
                    _files.Write(backup.File, ProgramWriter.Linked(system));
            }

            if (Writes)
                WriteIndex();
        }

        /// <summary>The backup that stores <paramref name="system" /> as it is, the same logic system last changed at the same time, or null; <paramref name="values" /> reads the backups.</summary>
        /// <remarks>Reads the file of each backup of the same logic system, so it is for a construct losing its automation or a blueprint being saved, never for a repeating path.</remarks>
        public Backup Stored(AutomationSystem system, Values values)
        {
            foreach (var backup in _all)
            {
                if (backup.Identity != system.Identity)
                    continue;

                AutomationSystem read;
                if (Open(backup, values, out read).Length == 0 && read.Changed == system.Changed)
                    return backup;
            }

            return null;
        }

        /// <summary>The logic system <paramref name="backup" /> stores, read from its file, answering what was wrong with it, or empty.</summary>
        public string Open(Backup backup, Values values, out AutomationSystem read)
        {
            read = null;
            string text;
            try
            {
                text = _files.Read(backup.File);
            }
            catch (Exception failed)
            {
                return $"{backup.File} will not open - {failed.Message}";
            }

            return ProgramReader.Lone(text, values, out read);
        }

        /// <summary>Forgets <paramref name="backup" />, deleting its file.</summary>
        public void Forget(Backup backup)
        {
            if (!_all.Remove(backup) || !Writes)
                return;

            // The index first, so a file a failed delete leaves behind is one nothing lists.
            WriteIndex();
            _files.Delete(backup.File);
        }

        /// <summary>A file name no backup has and storage does not hold, numbered after the highest in use.</summary>
        string Free()
        {
            var number = 1;
            foreach (var backup in _all)
            {
                var file = backup.File;
                int had;
                if (file.StartsWith(Lead, StringComparison.Ordinal) && file.EndsWith(Kind, StringComparison.Ordinal)
                    && int.TryParse(file.Substring(Lead.Length, file.Length - Lead.Length - Kind.Length), NumberStyles.None, CultureInfo.InvariantCulture, out had)
                    && had >= number)
                {
                    number = had + 1;
                }
            }

            while (Writes && _files.Has(Name(number)))
                number++;

            return Name(number);
        }

        static string Name(int number)
        {
            return Lead + number.ToString(CultureInfo.InvariantCulture) + Kind;
        }

        /// <summary>The backup the index's section <paramref name="file" /> describes, or null where it says too little.</summary>
        static Backup Entry(MyIni ini, string file)
        {
            Guid identity;
            DateTime lost;
            var name = ini.Get(file, NameKey).ToString();
            var construct = ini.Get(file, ConstructKey).ToString();
            if (!Guid.TryParse(ini.Get(file, SystemKey).ToString(""), out identity)
                || !DateTime.TryParse(ini.Get(file, LostKey).ToString(""), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out lost)
                || !ini.ContainsKey(file, NameKey) || !ini.ContainsKey(file, ConstructKey))
            {
                return null;
            }

            return new Backup(file, identity, name, construct, lost);
        }

        void WriteIndex()
        {
            var ini = new MyIni();
            foreach (var backup in _all)
            {
                ini.Set(backup.File, SystemKey, backup.Identity.ToString("D"));
                ini.Set(backup.File, NameKey, backup.Name);
                ini.Set(backup.File, ConstructKey, backup.Construct);
                ini.Set(backup.File, LostKey, backup.Lost.ToString("o", CultureInfo.InvariantCulture));
            }

            _files.Write(Index, ini.ToString());
        }
    }
}
