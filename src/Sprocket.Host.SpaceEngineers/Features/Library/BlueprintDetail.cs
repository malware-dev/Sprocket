using System;
using System.Collections.Generic;
using System.Text;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Codex;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;

namespace Sprocket.Host.SpaceEngineers.Features.Library
{
    /// <summary>The blueprint chosen in the library: its name, the ways to put it on this construct, keep it, reset or copy it, and what it holds.</summary>
    public partial class BlueprintDetail : VStack
    {
        readonly SprocketEditor _editor;
        readonly List<string> _names = new List<string>();
        readonly Relay _adds;
        readonly Relay _keeps;
        readonly Relay _copies;

        LibraryListing _chosen;
        string _refused = "";

        /// <summary>The details of no blueprint, in the editor <paramref name="editor" />.</summary>
        public BlueprintDetail(SprocketEditor editor)
        {
            _editor = editor;
            _adds = new Relay(Add);
            _keeps = new Relay(Keep);
            _copies = new Relay(Copy);

            CreateView();

            var texts = editor.Texts;
            editor.Codex.Explain(_firstHeading);
            editor.Codex.Explain(_secondHeading);
            _add.Text = texts.LibraryAdd;
            _add.Command = _adds;
            _add.Tip = Lumel.Tip.For(_add.Tip, texts.Codex.LibraryPutOn);
            _keep.Command = _keeps;
            _reset.Command = new Relay(Reset);
            _copy.Command = _copies;
        }

        /// <summary>Shows the details of <paramref name="chosen" />, or of nothing where it is null; why the last action on another was refused is forgotten.</summary>
        public void Show(LibraryListing chosen)
        {
            if (!ReferenceEquals(_chosen, chosen))
                _refused = "";

            _chosen = chosen;
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
            Say(_firstSection, _firstHeading, _first, null, null, null);
            Say(_secondSection, _secondHeading, _second, null, null, null);

            if (entry == null)
                return;

            _name.Text = entry.Row.Summary;
            DescribeBlueprint(entry);
        }

        /// <summary>The details of a blueprint: whether it is on this construct, how it stands with the library, and what it holds.</summary>
        void DescribeBlueprint(LibraryListing entry)
        {
            // Read from its file only when chosen, so the list is drawn without reading any.
            if (entry.Backup != null && entry.Read == null && entry.Trouble.Length == 0 && _editor.Backups != null)
                entry.Trouble = _editor.Backups.Open(entry.Backup, _editor.World.Values, out entry.Read);

            var texts = _editor.Texts;
            var shelf = _editor.Shelf;
            if (entry.Read != null && shelf != null)
                shelf.Link(entry.Read);
            var live = Live(entry);
            var volume = Shelved(entry, live);
            var system = entry.Section == LibrarySection.OnConstruct ? live : entry.Read;
            var free = _editor.Holder == null;

            _here.IsVisible = true;
            _add.IsVisible = live == null;
            _adds.CanExecute = free && system != null;

            var here = entry.Section == LibrarySection.OnConstruct;
            var kept = here ? live : entry.Section == LibrarySection.BackedUp ? entry.Read : null;
            var same = shelf != null && kept != null && volume != null && shelf.Holds(kept);
            var newer = here && live != null && volume != null && !same && !_editor.Holdings.ChangedSince(live);
            _keep.IsVisible = kept != null && shelf != null;
            _keep.Text = volume == null ? texts.SystemsMakeBlueprint : newer ? texts.SystemsUpdateFromLibrary : texts.SystemsUpdateBlueprint;
            _keeps.CanExecute = !same;
            _keep.Tip = Lumel.Tip.For(_keep.Tip, same ? texts.Codex.LibrarySame
                : newer ? texts.Codex.SystemsUpdateFromLibraryAbout
                : volume == null ? texts.Codex.SystemsMakeBlueprintAbout : texts.Codex.SystemsUpdateBlueprintAbout);

            _reset.IsVisible = here && live != null && volume != null && !same && !newer;
            _reset.Text = texts.LibraryReset;
            _reset.Tip = Lumel.Tip.For(_reset.Tip, texts.Codex.LibraryResetAbout);

            // A blueprint in storage is copied by putting it on a construct, so it offers no copy of itself.
            _copy.IsVisible = entry.Section != LibrarySection.Kept;
            _copy.Text = texts.LibraryCopy;
            _copies.CanExecute = free && system != null;
            _copy.Tip = Lumel.Tip.For(_copy.Tip, texts.Codex.LibraryCopyAbout);

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

            Say(_firstSection, _firstHeading, _first, texts.BlueprintParts, CodexPages.Slot, _names);

            _names.Clear();
            for (var i = 0; live != null && i < live.Instances.Count; i++)
                _names.Add(SystemSummary.DescribeInstance(texts, _editor.Runner.Bindings, live, live.Instances[i]));

            Say(_secondSection, _secondHeading, _second, texts.LibraryInstallationsHeading, CodexPages.Installation, _names);
        }

        /// <summary>Shows <paramref name="names" /> under <paramref name="title" />, the term whose codex page is <paramref name="term" />, or neither where there are none.</summary>
        static void Say(Section section, RichText heading, Label list, string title, string term, List<string> names)
        {
            var shown = names != null && names.Count > 0;
            section.IsVisible = shown;
            if (!shown)
                return;

            heading.Markdown = CodexPages.Link(title.ToUpperInvariant(), term);
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
        void Add()
        {
            var entry = _chosen;
            if (entry == null || Live(entry) != null || (entry.Backup != null && entry.Read == null))
                return;

            if (entry.Backup != null)
            {
                Run(CommandKind.PutOn, () => _refused = _editor.Holdings.PutOn(entry.Read));
            }
            else
            {
                var volume = _editor.Shelf?.Find(entry.Identity);
                if (volume != null)
                    Run(CommandKind.PutOn, () => _refused = _editor.Holdings.PutOn(volume));
            }

            Describe();
        }

        /// <summary>Puts the chosen logic system, as it is on this construct, in the library; or a backed up one, as its backup keeps it.</summary>
        void Keep()
        {
            var entry = _chosen;
            if (entry == null || entry.Section == LibrarySection.Kept)
                return;

            if (entry.Section == LibrarySection.BackedUp)
            {
                var backed = entry.Read;
                if (backed != null)
                    Run(CommandKind.Shelve, () => _editor.Holdings.ShelveBackup(backed));

                return;
            }

            var live = Live(entry);
            if (live == null)
                return;

            if (!_editor.Holdings.Stored(live))
                Run(CommandKind.Shelve, () => _editor.Holdings.Shelve(live));
            else if (_editor.Holdings.ChangedSince(live))
                Run(CommandKind.Shelve, () => _editor.Holdings.AskUpdate(live));
            else
                Run(CommandKind.Renew, () => _editor.Holdings.AskRenew(live));
        }

        /// <summary>Puts the chosen blueprint in the library back in the place of the logic system on this construct, after asking.</summary>
        void Reset()
        {
            var live = _chosen == null || _chosen.Section != LibrarySection.OnConstruct ? null : Live(_chosen);
            if (live != null)
                Run(CommandKind.Renew, () => _editor.Holdings.AskReset(live));
        }

        /// <summary>Puts a copy of the chosen blueprint on the construct, as a blueprint of its own, and opens it to be changed.</summary>
        void Copy()
        {
            var entry = _chosen;
            var text = entry == null ? null : Source(entry);
            if (text == null)
                return;

            Run(CommandKind.PutOn, delegate
            {
                var made = _editor.Holdings.Copy(text, out _refused);
                if (made != null)
                    _editor.Push(new LogicSystemPage(_editor, made));
            });

            Describe();
        }

        void Run(CommandKind kind, Action action)
        {
            var command = _editor.Act(kind, action);
            if (command != null)
                command.Execute();
        }

        /// <summary>
        /// The logic system on this construct <paramref name="entry" /> is of, or null: itself, the one the backup keeps, or the
        /// one made from the blueprint in the library.
        /// </summary>
        AutomationSystem Live(LibraryListing entry)
        {
            var program = _editor.Program;
            var system = entry.Section == LibrarySection.Kept ? program.MadeFrom(entry.Identity) : program.FindSystem(entry.Identity);
            return system != null && system.IsBlueprint ? system : null;
        }

        /// <summary>
        /// The blueprint in the library <paramref name="entry" /> is, or the one its logic system, <paramref name="live" /> or the
        /// backup's, is linked to; null where the library has none.
        /// </summary>
        LibraryEntry Shelved(LibraryListing entry, AutomationSystem live)
        {
            var shelf = _editor.Shelf;
            if (shelf == null)
                return null;

            if (entry.Section == LibrarySection.Kept)
                return shelf.Find(entry.Identity);

            var linked = entry.Section == LibrarySection.BackedUp ? entry.Read : live;
            return linked != null && linked.HasOrigin ? shelf.Find(linked.Origin) : null;
        }

        /// <summary>The text <paramref name="entry" /> keeps its blueprint as, or null where it has none that reads.</summary>
        string Source(LibraryListing entry)
        {
            if (entry.Section == LibrarySection.Kept)
            {
                var volume = _editor.Shelf?.Find(entry.Identity);
                return volume?.Text;
            }

            var system = entry.Section == LibrarySection.BackedUp ? entry.Read : Live(entry);
            return system == null ? null : ProgramWriter.Blueprint(system);
        }

        /// <summary>Who made <paramref name="system" /> and when it last changed, as far as either is known.</summary>
        static string Made(Texts texts, AutomationSystem system)
        {
            var by = system.Namespace;
            if (!system.Changed.HasValue)
                return by.Length == 0 ? "" : Template.Fill(texts.LibraryBy, "name", by);

            var time = LibraryPage.Time(system.Changed.Value);
            return by.Length == 0
                ? Template.Fill(texts.LibraryChanged, "time", time)
                : Template.Fill(Template.Fill(texts.LibraryByChanged, "name", by), "time", time);
        }
    }
}
