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
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;

namespace Sprocket.Host.SpaceEngineers.Features.Library
{
    /// <summary>The library: the blueprints on this construct, the constructs that are lost, and the blueprints the player keeps for every world.</summary>
    /// <remarks>
    /// A list beside the details of what is chosen from it. The list is made again whenever the screen is rebuilt, which
    /// only something done on this page or a change to the program brings about. The elements it is drawn with are declared
    /// in <c>LibraryPage.View.cs</c>.
    /// </remarks>
    public sealed partial class LibraryPage : EditorPage
    {
        const int OnConstruct = 0;
        const int Lost = 1;
        const int Kept = 2;

        readonly List<Entry> _entries = new List<Entry>();
        readonly List<Volume> _volumes = new List<Volume>();
        readonly List<string> _names = new List<string>();
        readonly Relay _flips;
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
            _flips = new Relay(Flip);
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
            _hereLabel.Text = texts.LibraryHere;
            _switch.Command = _flips;
            _switch.Says(texts.SwitchOn, texts.SwitchOff);
            _keep.Command = _keeps;
            _reset.Command = _resets;
            _copy.Command = _copies;
            List();
        }

        /// <summary>Lists the blueprints on this construct, the constructs that are lost, and the blueprints in the library.</summary>
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

            ListLost(texts);
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

        /// <summary>Lists the constructs whose automation was lost, where this machine keeps the backups: a backup is only taken as it is lost.</summary>
        void ListLost(Texts texts)
        {
            var backups = Editor.Backups;
            if (backups == null)
                return;

            var all = backups.All;
            for (var i = 0; i < all.Count; i++)
            {
                var backup = all[i];
                var row = new StatusRow
                {
                    Summary = backup.Name,
                    Problem = Template.Fill(texts.LibraryLost, "time", Time(backup.Saved)),
                    Problems = 1,
                    Deletes = Editor.Act(Cmd.ForgetBackup, delegate { Editor.ForgetBackup(backup); }),
                    DeleteText = texts.LibraryForgetLost
                };

                Add(new Entry { Section = Lost, Backup = backup }, row);
            }
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
            _volumes.Sort(delegate(Volume a, Volume b) { return string.Compare(Called(a), Called(b), StringComparison.CurrentCultureIgnoreCase); });

            for (var i = 0; i < _volumes.Count; i++)
            {
                var volume = _volumes[i];
                var entry = new Entry { Section = Kept, Identity = volume.Identity };
                if (volume.State == VolumeState.Read)
                {
                    var trial = new Automation();
                    entry.Trouble = ProgramText.Load(volume.Text, trial, Editor.World.Values);
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
                    Deletes = Editor.Act(Cmd.Unshelve, delegate { Editor.Unshelve(volume); }),
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
            row.Opens = Editor.Act(Cmd.Expand, delegate { Choose(entry); });
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

        /// <summary>Shows only the rows whose name holds what is typed in the search box.</summary>
        void Narrow()
        {
            var typed = _search.Text.Trim();
            for (var i = 0; i < _entries.Count; i++)
            {
                var row = _entries[i].Row;
                row.IsVisible = typed.Length == 0 || row.Summary.IndexOf(typed, StringComparison.CurrentCultureIgnoreCase) >= 0;
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

        /// <summary>Shows the details of what is chosen: a blueprint, wherever it is listed, or a construct that is lost.</summary>
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
            if (entry.Backup != null)
                DescribeLost(entry.Backup);
            else
                DescribeBlueprint(entry);
        }

        /// <summary>The details of a blueprint: whether it is on this construct, how it stands with the library, and what it holds.</summary>
        /// <remarks>The copy on this construct where there is one, chosen from either list; the library's where there is not.</remarks>
        void DescribeBlueprint(Entry entry)
        {
            var texts = Editor.Texts;
            var shelf = Editor.Shelf;
            var live = Live(entry.Identity);
            var volume = shelf == null ? null : shelf.Find(entry.Identity);
            var system = live ?? entry.Read;
            var free = Editor.Holder == null;

            _here.IsVisible = true;
            _switch.IsOn = live != null;
            _flips.CanExecute = free && system != null;
            _switch.Tip = Tip.For(_switch.Tip, live == null
                ? texts.LibraryPutOn
                : volume != null ? texts.LibraryTakeOff : texts.LibraryDeletes);

            var same = live != null && volume != null && shelf.Holds(live);
            var newer = live != null && volume != null && !same && !Editor.ChangedSince(live);
            _keep.IsVisible = live != null && shelf != null;
            _keep.Text = volume == null ? texts.SystemsMakeBlueprint : newer ? texts.SystemsUpdateFromLibrary : texts.SystemsUpdateBlueprint;
            _keeps.CanExecute = !same;
            _keep.Tip = Tip.For(_keep.Tip, same ? texts.LibrarySame
                : newer ? texts.SystemsUpdateFromLibraryAbout
                : volume == null ? texts.SystemsMakeBlueprintAbout : texts.SystemsUpdateBlueprintAbout);

            // Back the other way while the logic system has moved on from its blueprint, beside the button that would copy it over.
            _reset.IsVisible = live != null && volume != null && !same && !newer;
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

        /// <summary>The details of a construct that is lost: when its automation was kept, and the logic systems it had.</summary>
        void DescribeLost(Backup backup)
        {
            var texts = Editor.Texts;
            _line.Text = Template.Fill(texts.LibraryLost, "time", Time(backup.Saved));
            _line.IsVisible = true;

            var trial = new Automation();
            var trouble = ProgramText.Load(backup.Program, trial, Editor.World.Values);
            _trouble.Text = trouble;
            _trouble.IsVisible = trouble.Length > 0;

            _names.Clear();
            var systems = trial.Systems;
            for (var i = 0; i < systems.Count; i++)
                _names.Add(systems[i].Name);

            Say(_firstHeading, _first, texts.LibrarySystems, _names);
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

        /// <summary>Puts the chosen blueprint on the construct, or takes it off.</summary>
        void Flip()
        {
            var entry = _chosen;
            if (entry == null || entry.Backup != null)
                return;

            var live = Live(entry.Identity);
            if (live != null && Editor.Stored(live))
            {
                Run(Cmd.TakeOff, delegate { Editor.AskTakeOff(live); });
            }
            else if (live != null)
            {
                Run(Cmd.DeleteSystem, delegate { Editor.DeleteSystem(live); });
            }
            else
            {
                var volume = Editor.Shelf == null ? null : Editor.Shelf.Find(entry.Identity);
                if (volume != null)
                    Run(Cmd.PutOn, delegate { _refused = Editor.PutOn(volume); });
            }

            Describe();
        }

        /// <summary>Puts the chosen blueprint, as it is on this construct, in the library.</summary>
        void Keep()
        {
            var live = _chosen == null ? null : Live(_chosen.Identity);
            if (live == null)
                return;

            // Either update replaces something, so it asks first: the blueprint where the logic system is the newer, and the
            // logic system where the blueprint is.
            if (!Editor.Stored(live))
                Run(Cmd.Shelve, delegate { Editor.Shelve(live); });
            else if (Editor.ChangedSince(live))
                Run(Cmd.Shelve, delegate { Editor.AskUpdate(live); });
            else
                Run(Cmd.Renew, delegate { Editor.AskRenew(live); });
        }

        /// <summary>Puts the chosen blueprint in the library back in the place of the logic system on this construct, after asking.</summary>
        void Reset()
        {
            var live = _chosen == null ? null : Live(_chosen.Identity);
            if (live != null)
                Run(Cmd.Renew, delegate { Editor.AskReset(live); });
        }

        /// <summary>Puts a copy of the chosen blueprint on the construct, as a blueprint of its own, and opens it to be changed.</summary>
        /// <remarks>Copied as it is on this construct where it is here; as the library keeps it where it is not.</remarks>
        void Copy()
        {
            var entry = _chosen;
            if (entry == null || entry.Backup != null)
                return;

            var live = Live(entry.Identity);
            var volume = Editor.Shelf == null ? null : Editor.Shelf.Find(entry.Identity);
            var text = live != null ? ProgramText.Blueprint(live) : volume == null ? null : volume.Text;
            if (text == null)
                return;

            Run(Cmd.PutOn, delegate
            {
                var made = Editor.Copy(text, out _refused);
                if (made != null)
                    Editor.Push(new LogicSystemPage(Editor, made));
            });

            Describe();
        }

        void Run(Cmd kind, Action action)
        {
            var command = Editor.Act(kind, action);
            if (command != null)
                command.Execute();
        }

        /// <summary>The blueprint <paramref name="identity" /> on this construct, or null.</summary>
        AutomationSystem Live(Guid identity)
        {
            var system = Editor.Program.FindSystem(identity);
            return system != null && system.IsBlueprint ? system : null;
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
        static string Called(Volume volume)
        {
            return volume.State == VolumeState.Read ? volume.Name : volume.File;
        }

        /// <summary>Who made the blueprint in <paramref name="volume" /> and when it last changed, as far as its text says.</summary>
        string Made(Volume volume, Texts texts)
        {
            if (volume.State != VolumeState.Read)
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
        static string Unkept(Volume volume, Texts texts)
        {
            switch (volume.State)
            {
                case VolumeState.Missing:
                    return texts.LibraryMissing;
                case VolumeState.Path:
                    return texts.LibraryFolder;
                case VolumeState.Twice:
                    return texts.LibraryTwice;
                default:
                    return texts.LibraryUnreadable;
            }
        }

        /// <summary>A time in UTC, said as the player's clock has it.</summary>
        static string Time(DateTime utc)
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
