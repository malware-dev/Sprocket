using System;
using System.Collections.Generic;
using System.Globalization;
using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Codex;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Lumel.Styling;

namespace Sprocket.Host.SpaceEngineers.Features.Library
{
    /// <summary>The library: the blueprints on this construct, the backups of constructs that lost their automation, and the blueprints the player keeps for every world.</summary>
    /// <remarks>The list is made again only when the screen is rebuilt, by something done on this page or a change to the program.</remarks>
    public sealed partial class LibraryPage : EditorPage
    {
        readonly List<LibraryListing> _entries = new List<LibraryListing>();
        readonly List<LibraryEntry> _volumes = new List<LibraryEntry>();
        readonly List<Backup> _group = new List<Backup>();
        readonly HashSet<Backup> _listed = new HashSet<Backup>();

        LibraryListing _chosen;
        Guid _opening;

        /// <summary>The library, in the editor <paramref name="editor" />, with the blueprint <paramref name="opening" /> chosen first, or the first one where that is empty.</summary>
        public LibraryPage(SprocketEditor editor, Guid opening) : base(editor)
        {
            _opening = opening;
        }

        /// <inheritdoc />
        public override string Title => Editor.Texts.SystemsLoad;

        /// <inheritdoc />
        public override bool ScrollsItself => true;

        /// <summary>The list in the order it reads, its headings and lines included and a construct's rows among them, for whoever reads it back.</summary>
        public IEnumerable<Element> Rows => InOrder(_list);

        /// <summary>The box that narrows the list.</summary>
        public TextBox SearchBox => _search;

        /// <summary>Makes the list again, once the view is made; until then <see cref="ViewCreated" /> does.</summary>
        public override void Build()
        {
            if (_list != null)
                List();
        }

        /// <summary>Gives the search its hint and makes the list for the first time.</summary>
        protected override void ViewCreated()
        {
            _search.Hint = Editor.Texts.PickSearch;
            _search.Edited += Narrow;
            List();
        }

        /// <summary>Lists the blueprints on this construct, the backups, and the blueprints in the library.</summary>
        void List()
        {
            var texts = Editor.Texts;
            var was = _chosen;
            _chosen = null;
            _entries.Clear();
            _list.Children.Clear();

            var constructs = Part(TermHeading(CodexPages.Within(texts.LibraryOnConstructs, "constructs", texts.LibraryOnConstructsTerm, CodexPages.Construct)), Gaps.Sections);
            var held = Construct(constructs, Template.Fill(texts.LibraryThisConstruct, "name", Editor.World.Name));
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
                Add(new LibraryListing { Section = LibrarySection.OnConstruct, Identity = system.Identity }, row, held);
            }

            if (here == 0)
                Line(texts.LibraryNoneHere, Styles.InnerEmpty, held);

            ListBackups(texts);
            ListKept(texts);

            LibraryListing again = null;
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
        /// <remarks>Drawn from what the index says of each, without reading a blueprint.</remarks>
        void ListBackups(Texts texts)
        {
            var backups = Editor.Backups;
            if (backups == null || backups.All.Count == 0)
                return;

            var lost = Part(TermHeading(CodexPages.Link(texts.LibraryBackups.ToUpperInvariant(), CodexPages.Backup)), Gaps.Sections);
            _listed.Clear();

            var all = backups.All;
            while (_listed.Count < all.Count)
            {
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

                    // Latest loss first; those lost together keep the order they were taken in.
                    var at = _group.Count;
                    while (at > 0 && _group[at - 1].Lost < all[i].Lost)
                        at--;

                    _group.Insert(at, all[i]);
                }

                _listed.UnionWith(_group);
                var held = Construct(lost, latest.Construct);
                foreach (var backup in _group)
                    ListBackup(texts, backup, held);
            }
        }

        /// <summary>Lists <paramref name="backup" /> as a row, chosen and used as a blueprint from storage is.</summary>
        void ListBackup(Texts texts, Backup backup, Panel into)
        {
            Add(new LibraryListing { Section = LibrarySection.BackedUp, Backup = backup, Identity = backup.Identity }, new StatusRow
            {
                Mark = Symbols.Blueprint,
                Summary = backup.Name,
                Depth = 1,
                Status = Template.Fill(texts.LibraryLost, "time", Time(backup.Lost)),
                Deletes = Editor.Act(CommandKind.ForgetBackup, () => Editor.Holdings.ForgetBackup(backup)),
                DeleteText = texts.Codex.LibraryForgetBackup
            }, into);
        }

        /// <summary>Lists the blueprints in the library, by name, reading each to know it will read.</summary>
        void ListKept(Texts texts)
        {
            var shelf = Editor.Shelf;
            if (shelf == null)
                return;

            var kept = Part(new Label { Style = Styles.H2, Text = texts.LibraryInStorage.ToUpperInvariant() }, Gaps.Items);

            _volumes.Clear();
            _volumes.AddRange(shelf.All);
            _volumes.Sort((a, b) => string.Compare(Called(a), Called(b), StringComparison.CurrentCultureIgnoreCase));

            foreach (var volume in _volumes)
            {
                var entry = new LibraryListing { Section = LibrarySection.Kept, Identity = volume.Identity };
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
                    DeleteText = texts.Codex.LibraryForget
                };

                if (entry.Read == null)
                {
                    row.Problem = Unkept(volume, texts);
                    row.Problems = 1;
                }

                Add(entry, row, kept);
            }

            if (_volumes.Count == 0)
                Line(texts.LibraryNoneSaved, Styles.InnerEmpty, kept);
        }

        /// <summary>Puts <paramref name="row" /> in the list for <paramref name="entry" />, pressed to choose it.</summary>
        void Add(LibraryListing entry, StatusRow row, Panel into)
        {
            entry.Row = row;
            row.Opens = Editor.Act(CommandKind.Expand, () => Choose(entry));
            row.Show(Editor.Texts);
            _entries.Add(entry);
            into.Children.Add(row);
        }

        /// <summary>A heading that holds a term, written as <paramref name="markdown" /> with the term a link to its codex page, which resting on it shows.</summary>
        RichText TermHeading(string markdown)
        {
            var heading = new RichText { Style = Styles.H2Term, Align = Align.Left, Markdown = markdown };
            Editor.Codex.Explain(heading);
            return heading;
        }

        /// <summary>Adds a part of the list under <paramref name="heading" />, and gives back where what it heads goes, <paramref name="apart" /> from each other.</summary>
        VStack Part(Element heading, float apart)
        {
            var body = new VStack { Spacing = apart };
            _list.Children.Add(new Section { Children = { heading, body } });
            return body;
        }

        /// <summary>Adds a construct's name to <paramref name="part" />, and gives back where its rows go, set in under the name.</summary>
        VStack Construct(Panel part, string name)
        {
            var held = new VStack { Spacing = Gaps.Items, Base = { Padding = new Thickness(Sizes.Indent, 0f, 0f, 0f) } };
            part.Children.Add(new Section { Children = { new Label { Style = Styles.InnerName, Text = name }, held } });
            return held;
        }

        /// <summary>The headings, names, lines and rows inside <paramref name="holder" />, in the order they read.</summary>
        static IEnumerable<Element> InOrder(Panel holder)
        {
            foreach (var child in holder.Children)
            {
                var inner = child as VStack;
                if (inner == null)
                {
                    yield return child;
                    continue;
                }

                foreach (var line in InOrder(inner))
                    yield return line;
            }
        }

        void Line(string text, IStyle<Label> style, Panel into)
        {
            into.Children.Add(new Label { Style = style, Text = text, Wraps = true });
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
        void Choose(LibraryListing entry)
        {
            var texts = Editor.Texts;
            if (_chosen != null)
            {
                _chosen.Row.Chosen = false;
                _chosen.Row.Show(texts);
            }

            _chosen = entry;
            if (entry != null)
            {
                entry.Row.Chosen = true;
                entry.Row.Show(texts);
            }

            _detail.Show(entry);
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
    }
}
