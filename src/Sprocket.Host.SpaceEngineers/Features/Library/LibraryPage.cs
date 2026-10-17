using Sprocket.Host.SpaceEngineers.Features.Codex;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Lumel;
using Lumel.Input;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;

namespace Sprocket.Host.SpaceEngineers.Features.Library
{
    /// <summary>The library: the blueprints on this construct, the backups of constructs that lost their automation, and the blueprints the player keeps for every world.</summary>
    /// <remarks>
    /// A list beside the details of what is chosen from it. The list is made again whenever the screen is rebuilt, which
    /// only something done on this page or a change to the program brings about. The elements it is drawn with are declared
    /// in <c>LibraryPage.View.cs</c>.
    /// </remarks>
    public sealed partial class LibraryPage : EditorPage
    {
        const int OnConstruct = 0;
        const int BackedUp = 1;
        const int Kept = 2;

        readonly List<Entry> _entries = new List<Entry>();
        readonly List<LibraryEntry> _volumes = new List<LibraryEntry>();
        readonly List<string> _names = new List<string>();
        readonly List<Backup> _group = new List<Backup>();
        readonly HashSet<Backup> _listed = new HashSet<Backup>();
        readonly Relay _adds;
        readonly Relay _keeps;
        readonly Relay _resets;
        readonly Relay _copies;

        Entry _chosen;
        string _refused = "";
        Guid _opening;

        /// <summary>The library, in the editor <paramref name="editor" />, with the blueprint <paramref name="opening" /> chosen first, or the first one where that is empty.</summary>
        public LibraryPage(SprocketEditor editor, Guid opening) : base(editor)
        {
            _opening = opening;
            _adds = new Relay(Add);
            _keeps = new Relay(Keep);
            _resets = new Relay(Reset);
            _copies = new Relay(Copy);
        }

        /// <inheritdoc />
        public override string Title => Editor.Texts.SystemsLoad;

        /// <inheritdoc />
        public override string Help => Terms.Library;

        /// <inheritdoc />
        public override bool ScrollsItself => true;

        /// <summary>The list, its headings and lines included, for whoever reads it back.</summary>
        public Element.Collection Rows => _list.Children;

        /// <summary>The box that narrows the list.</summary>
        public TextBox SearchBox => _search;

        /// <summary>Makes the list again, once the view is made; until then <see cref="ViewCreated" /> does.</summary>
        public override void Build()
        {
            if (_list != null)
                List();
        }

        /// <summary>Gives the details their fixed words and makes the list for the first time.</summary>
        protected override void ViewCreated()
        {
            var texts = Editor.Texts;
            _search.Hint = texts.PickSearch;
            _search.Edited += Narrow;
            _add.Text = texts.LibraryAdd;
            _add.Command = _adds;
            _add.Tip = Tip.For(_add.Tip, texts.LibraryPutOn);
            _keep.Command = _keeps;
            _reset.Command = _resets;
            _copy.Command = _copies;
            List();
        }

        /// <summary>Lists the blueprints on this construct, the backups, and the blueprints in the library.</summary>
        /// <remarks>What was chosen stays chosen where it is still listed; the first row is chosen where it is not.</remarks>
        void List()
        {
            var texts = Editor.Texts;
            var was = _chosen;
            _chosen = null;
            _entries.Clear();
            _list.Children.Clear();

            Heading(texts.LibraryOnConstructs, Styles.Heading);
            _list.Children.Add(new Label
            {
                Style = Styles.InnerName,
                Text = Template.Fill(texts.LibraryThisConstruct, "name", Editor.World.Name),
                Padding = new Thickness(16f, 8f, 0f, 4f)
            });

            var here = 0;
            var systems = Editor.Program.Systems;
            for (var i = 0; i < systems.Count; i++)
            {
                if (!systems[i].IsBlueprint)
                    continue;

                here++;
                var system = systems[i];
                var row = new StatusRow { Mark = Symbols.LogicSystem, Summary = system.Name, Depth = 1 };
                row.Status = Installations(texts, system.Instances.Count);
                Add(new Entry { Section = OnConstruct, Identity = system.Identity }, row);
            }

            if (here == 0)
                Line(texts.LibraryNoneHere, Styles.Empty);

            ListBackups(texts);
            ListKept(texts);

            Entry again = null;
            for (var i = 0; i < _entries.Count && again == null; i++)
            {
                if (_opening != Guid.Empty ? _entries[i].Identity == _opening : _entries[i].Same(was))
                    again = _entries[i];
            }

            _opening = Guid.Empty;

            Choose(again ?? (_entries.Count > 0 ? _entries[0] : null));
            Narrow();
        }

        /// <summary>
        /// Lists the backups, where this machine keeps them: the logic systems kept of each construct as it lost its automation,
        /// under the construct's name, the latest loss first.
        /// </summary>
        /// <remarks>
        /// Drawn from what the index says of each, without reading a blueprint. The name is only a label of where a backup came
        /// from, so backups under the same name are listed together whichever construct they came from.
        /// </remarks>
        void ListBackups(Texts texts)
        {
            var backups = Editor.Backups;
            if (backups == null || backups.All.Count == 0)
                return;

            Heading(texts.LibraryBackups, Styles.NextHeading);
            _listed.Clear();

            var all = backups.All;
            while (_listed.Count < all.Count)
            {
                // The name lost most lately leads; among those lost together, the one taken last.
                Backup latest = null;
                for (var i = 0; i < all.Count; i++)
                {
                    if (!_listed.Contains(all[i]) && (latest == null || all[i].Lost >= latest.Lost))
                        latest = all[i];
                }

                if (latest == null)
                    break;

                _group.Clear();
                for (var i = 0; i < all.Count; i++)
                {
                    if (all[i].Construct != latest.Construct)
                        continue;

                    // In order of loss, the latest first; among those lost together, as they were taken.
                    var at = _group.Count;
                    while (at > 0 && _group[at - 1].Lost < all[i].Lost)
                        at--;

                    _group.Insert(at, all[i]);
                }

                _listed.UnionWith(_group);
                _list.Children.Add(new Label
                {
                    Style = Styles.InnerName,
                    Text = latest.Construct,
                    Padding = new Thickness(16f, 8f, 0f, 4f)
                });

                foreach (var backup in _group)
                    ListBackup(texts, backup);
            }
        }

        /// <summary>Lists <paramref name="backup" /> as a row, chosen and used as a blueprint from storage is.</summary>
        void ListBackup(Texts texts, Backup backup)
        {
            Add(new Entry { Section = BackedUp, Backup = backup, Identity = backup.Identity }, new StatusRow
            {
                Mark = Symbols.Blueprint,
                Summary = backup.Name,
                Depth = 1,
                Status = Template.Fill(texts.LibraryLost, "time", Time(backup.Lost)),
                Deletes = Editor.Act(CommandKind.ForgetBackup, () => Editor.Holdings.ForgetBackup(backup)),
                DeleteText = texts.LibraryForgetBackup
            });
        }

        /// <summary>Lists the blueprints in the library, by name, reading each to know it will read.</summary>
        void ListKept(Texts texts)
        {
            var shelf = Editor.Shelf;
            if (shelf == null)
                return;

            Heading(texts.LibraryInStorage, Styles.NextHeading);

            _volumes.Clear();
            _volumes.AddRange(shelf.All);
            _volumes.Sort((a, b) => string.Compare(Called(a), Called(b), StringComparison.CurrentCultureIgnoreCase));

            foreach (var volume in _volumes)
            {
                var entry = new Entry { Section = Kept, Identity = volume.Identity };
                if (volume.State == LibraryEntryState.Read)
                {
                    var trial = new Automation();
                    entry.Trouble = ProgramReader.Load(volume.Text, trial, Editor.World.Values);
                    if (entry.Trouble.Length == 0)
                        entry.Read = trial.FindSystem(volume.Identity);
                }
                else
                {
                    entry.Trouble = volume.Trouble;
                }

                var row = new StatusRow
                {
                    Mark = Symbols.Blueprint,
                    Summary = Called(volume),
                    Depth = 1,
                    Status = Made(volume, texts),
                    Deletes = Editor.Act(CommandKind.Unshelve, () => Editor.Holdings.Unshelve(volume)),
                    DeleteText = texts.LibraryForget
                };

                if (entry.Read == null)
                {
                    row.Problem = Unkept(volume, texts);
                    row.Problems = 1;
                }

                Add(entry, row);
            }

            if (_volumes.Count == 0)
                Line(texts.LibraryNoneSaved, Styles.Empty);
        }

        /// <summary>Puts <paramref name="row" /> in the list for <paramref name="entry" />, pressed to choose it.</summary>
        void Add(Entry entry, StatusRow row)
        {
            entry.Row = row;
            row.Opens = Editor.Act(CommandKind.Expand, () => Choose(entry));
            row.Show(Editor.Texts);
            _entries.Add(entry);
            _list.Children.Add(row);
        }

        void Heading(string text, TextStyle style)
        {
            _list.Children.Add(new Label { Style = style, Text = text.ToUpperInvariant() });
        }

        void Line(string text, TextStyle style)
        {
            _list.Children.Add(new Label { Style = style, Text = text, Wraps = true });
        }

        /// <summary>Shows only the rows whose name holds the words typed in the search box, as every search box matches them.</summary>
        void Narrow()
        {
            foreach (var entry in _entries)
            {
                var row = entry.Row;
                row.IsVisible = EntityQuery.MatchesText(row.Summary, _search.Text);
            }
        }

        /// <summary>Marks <paramref name="entry" /> as chosen and shows its details.</summary>
        void Choose(Entry entry)
        {
            var texts = Editor.Texts;
            if (_chosen != null)
            {
                _chosen.Row.Chosen = false;
                _chosen.Row.Show(texts);
            }

            if (!ReferenceEquals(_chosen, entry))
                _refused = "";

            _chosen = entry;
            if (entry != null)
            {
                entry.Row.Chosen = true;
                entry.Row.Show(texts);
            }

            Describe();
        }

        /// <summary>Shows the details of what is chosen: a blueprint, wherever it is listed.</summary>
        void Describe()
        {
            var entry = _chosen;
            _name.IsVisible = entry != null;
            _here.IsVisible = false;
            _line.IsVisible = false;
            _about.IsVisible = false;
            _trouble.IsVisible = false;
            Say(_firstHeading, _first, null, null);
            Say(_secondHeading, _second, null, null);

            if (entry == null)
                return;

            _name.Text = entry.Row.Summary;
            DescribeBlueprint(entry);
        }

        /// <summary>The details of a blueprint: whether it is on this construct, how it stands with the library, and what it holds.</summary>
        /// <remarks>What it holds is the entry's own: the logic system here, the backup, or the blueprint in the library.</remarks>
        void DescribeBlueprint(Entry entry)
        {
            // A backup's blueprint is read from its file as it is chosen, so the list is drawn without reading any.
            if (entry.Backup != null && entry.Read == null && entry.Trouble.Length == 0 && Editor.Backups != null)
                entry.Trouble = Editor.Backups.Open(entry.Backup, Editor.World.Values, out entry.Read);

            var texts = Editor.Texts;
            var shelf = Editor.Shelf;
            if (entry.Read != null && shelf != null)
                shelf.Link(entry.Read);
            var live = Live(entry);
            var volume = Shelved(entry, live);
            var system = entry.Section == OnConstruct ? live : entry.Read;
            var free = Editor.Holder == null;

            // Only where it is not on this construct already: it comes off from the home screen.
            _here.IsVisible = true;
            _add.IsVisible = live == null;
            _adds.CanExecute = free && system != null;

            // A backup is kept in the library as it keeps it, and a logic system here as it is; a blueprint in the library is
            // there already, so its row has no button between it and the library.
            var here = entry.Section == OnConstruct;
            var kept = here ? live : entry.Section == BackedUp ? entry.Read : null;
            var same = shelf != null && kept != null && volume != null && shelf.Holds(kept);
            var newer = here && live != null && volume != null && !same && !Editor.Holdings.ChangedSince(live);
            _keep.IsVisible = kept != null && shelf != null;
            _keep.Text = volume == null ? texts.SystemsMakeBlueprint : newer ? texts.SystemsUpdateFromLibrary : texts.SystemsUpdateBlueprint;
            _keeps.CanExecute = !same;
            _keep.Tip = Tip.For(_keep.Tip, same ? texts.LibrarySame
                : newer ? texts.SystemsUpdateFromLibraryAbout
                : volume == null ? texts.SystemsMakeBlueprintAbout : texts.SystemsUpdateBlueprintAbout);

            // Back the other way while the logic system has moved on from its blueprint, beside the button that would copy it over.
            _reset.IsVisible = here && live != null && volume != null && !same && !newer;
            _reset.Text = texts.LibraryReset;
            _reset.Tip = Tip.For(_reset.Tip, texts.LibraryResetAbout);

            _copy.Text = texts.LibraryCopy;
            _copies.CanExecute = free && system != null;
            _copy.Tip = Tip.For(_copy.Tip, texts.LibraryCopyAbout);

            var trouble = _refused.Length > 0 ? _refused : system == null ? entry.Trouble : "";
            _trouble.Text = trouble;
            _trouble.IsVisible = trouble.Length > 0;

            if (system == null)
                return;

            _line.Text = Made(texts, system);
            _line.IsVisible = _line.Text.Length > 0;
            _about.Markdown = system.Annotation;
            _about.IsVisible = system.Annotation.Length > 0;

            _names.Clear();
            var parts = system.Parts;
            for (var i = 0; i < parts.Count; i++)
            {
                if (!parts[i].Optional)
                    _names.Add(parts[i].Id);
            }

            Say(_firstHeading, _first, texts.BlueprintParts, _names);

            _names.Clear();
            for (var i = 0; live != null && i < live.Instances.Count; i++)
                _names.Add(SystemSummary.DescribeInstance(texts, Editor.Runner.Bindings, live, live.Instances[i]));

            Say(_secondHeading, _second, texts.LibraryInstallationsHeading, _names);
        }

        /// <summary>Shows <paramref name="names" /> under <paramref name="title" />, or neither where there are none.</summary>
        static void Say(Label heading, Label list, string title, List<string> names)
        {
            var shown = names != null && names.Count > 0;
            heading.IsVisible = shown;
            list.IsVisible = shown;
            if (!shown)
                return;

            heading.Text = title.ToUpperInvariant();
            var joined = new StringBuilder();
            for (var i = 0; i < names.Count; i++)
            {
                if (i > 0)
                    joined.Append(", ");

                joined.Append(names[i]);
            }

            list.Text = joined.ToString();
        }

        /// <summary>Puts the chosen blueprint on this construct as a logic system, with its first installation.</summary>
        /// <remarks>As its backup keeps it where it is a backup's; as the library keeps it where it is not.</remarks>
        void Add()
        {
            var entry = _chosen;
            if (entry == null || Live(entry) != null || (entry.Backup != null && entry.Read == null))
                return;

            if (entry.Backup != null)
            {
                Run(CommandKind.PutOn, () => _refused = Editor.Holdings.PutOn(entry.Read));
            }
            else
            {
                var volume = Editor.Shelf?.Find(entry.Identity);
                if (volume != null)
                    Run(CommandKind.PutOn, () => _refused = Editor.Holdings.PutOn(volume));
            }

            Describe();
        }

        /// <summary>Puts the chosen logic system, as it is on this construct, in the library; or a backed up one, as its backup keeps it.</summary>
        void Keep()
        {
            var entry = _chosen;
            if (entry == null || entry.Section == Kept)
                return;

            if (entry.Section == BackedUp)
            {
                var backed = entry.Read;
                if (backed != null)
                    Run(CommandKind.Shelve, () => Editor.Holdings.ShelveBackup(backed));

                return;
            }

            var live = Live(entry);
            if (live == null)
                return;

            // Either update replaces something, so it asks first: the blueprint where the logic system is the newer, and the
            // logic system where the blueprint is.
            if (!Editor.Holdings.Stored(live))
                Run(CommandKind.Shelve, () => Editor.Holdings.Shelve(live));
            else if (Editor.Holdings.ChangedSince(live))
                Run(CommandKind.Shelve, () => Editor.Holdings.AskUpdate(live));
            else
                Run(CommandKind.Renew, () => Editor.Holdings.AskRenew(live));
        }

        /// <summary>Puts the chosen blueprint in the library back in the place of the logic system on this construct, after asking.</summary>
        void Reset()
        {
            var live = _chosen == null || _chosen.Section != OnConstruct ? null : Live(_chosen);
            if (live != null)
                Run(CommandKind.Renew, () => Editor.Holdings.AskReset(live));
        }

        /// <summary>Puts a copy of the chosen blueprint on the construct, as a blueprint of its own, and opens it to be changed.</summary>
        /// <remarks>Copied as the entry itself keeps it: the logic system here, the backup, or the blueprint in the library.</remarks>
        void Copy()
        {
            var entry = _chosen;
            var text = entry == null ? null : Source(entry);
            if (text == null)
                return;

            Run(CommandKind.PutOn, delegate
            {
                var made = Editor.Holdings.Copy(text, out _refused);
                if (made != null)
                    Editor.Push(new LogicSystemPage(Editor, made));
            });

            Describe();
        }

        void Run(CommandKind kind, Action action)
        {
            var command = Editor.Act(kind, action);
            if (command != null)
                command.Execute();
        }

        /// <summary>
        /// The logic system on this construct <paramref name="entry" /> is of, or null: itself, the one the backup keeps, or the
        /// one made from the blueprint in the library.
        /// </summary>
        AutomationSystem Live(Entry entry)
        {
            var program = Editor.Program;
            var system = entry.Section == Kept ? program.MadeFrom(entry.Identity) : program.FindSystem(entry.Identity);
            return system != null && system.IsBlueprint ? system : null;
        }

        /// <summary>
        /// The blueprint in the library <paramref name="entry" /> is, or the one its logic system, <paramref name="live" /> or the
        /// backup's, is linked to; null where the library has none.
        /// </summary>
        LibraryEntry Shelved(Entry entry, AutomationSystem live)
        {
            var shelf = Editor.Shelf;
            if (shelf == null)
                return null;

            if (entry.Section == Kept)
                return shelf.Find(entry.Identity);

            var linked = entry.Section == BackedUp ? entry.Read : live;
            return linked != null && linked.HasOrigin ? shelf.Find(linked.Origin) : null;
        }

        /// <summary>The text <paramref name="entry" /> keeps its blueprint as, or null where it has none that reads.</summary>
        string Source(Entry entry)
        {
            if (entry.Section == Kept)
            {
                var volume = Editor.Shelf?.Find(entry.Identity);
                return volume?.Text;
            }

            var system = entry.Section == BackedUp ? entry.Read : Live(entry);
            return system == null ? null : ProgramWriter.Blueprint(system);
        }

        /// <summary>Who made <paramref name="system" /> and when it last changed, as far as either is known.</summary>
        static string Made(Texts texts, AutomationSystem system)
        {
            var by = system.Namespace;
            if (!system.Changed.HasValue)
                return by.Length == 0 ? "" : Template.Fill(texts.LibraryBy, "name", by);

            var time = Time(system.Changed.Value);
            return by.Length == 0
                ? Template.Fill(texts.LibraryChanged, "time", time)
                : Template.Fill(Template.Fill(texts.LibraryByChanged, "name", by), "time", time);
        }

        static string Installations(Texts texts, int count)
        {
            return count == 0
                ? texts.LibraryNoInstallations
                : count == 1
                    ? texts.LibraryOneInstallation
                    : Template.Fill(texts.LibraryInstallations, "count", count.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>What <paramref name="volume" /> is called on its row: the blueprint's name, or its file's where it did not read.</summary>
        static string Called(LibraryEntry volume)
        {
            return volume.State == LibraryEntryState.Read ? volume.Name : volume.File;
        }

        /// <summary>Who made the blueprint in <paramref name="volume" /> and when it last changed, as far as its text says.</summary>
        string Made(LibraryEntry volume, Texts texts)
        {
            if (volume.State != LibraryEntryState.Read)
                return "";

            var own = volume.Namespace.Length == 0 || volume.Namespace == Editor.Author;
            if (!volume.Changed.HasValue)
                return own ? "" : Template.Fill(texts.LibraryBy, "name", volume.Namespace);

            var time = Time(volume.Changed.Value);
            return own
                ? Template.Fill(texts.LibrarySaved, "time", time)
                : Template.Fill(Template.Fill(texts.LibrarySavedBy, "name", volume.Namespace), "time", time);
        }

        /// <summary>Why <paramref name="volume" /> gave no blueprint.</summary>
        static string Unkept(LibraryEntry volume, Texts texts)
        {
            switch (volume.State)
            {
                case LibraryEntryState.Missing:
                    return texts.LibraryMissing;
                case LibraryEntryState.Path:
                    return texts.LibraryFolder;
                case LibraryEntryState.Twice:
                    return texts.LibraryTwice;
                default:
                    return texts.LibraryUnreadable;
            }
        }

        /// <summary>A time in UTC, said as the player's clock has it.</summary>
        public static string Time(DateTime utc)
        {
            return utc.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }

        /// <summary>One row of the list, and what it stands for.</summary>
        sealed class Entry
        {
            public int Section;
            public Guid Identity;
            public Backup Backup;
            public AutomationSystem Read;
            public string Trouble = "";
            public StatusRow Row;

            /// <summary>Whether <paramref name="other" /> stands for the same thing in the same list.</summary>
            public bool Same(Entry other)
            {
                return other != null && other.Section == Section && other.Identity == Identity && other.Backup == Backup;
            }
        }
    }
}
