using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Features.Selector.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Input;
using Lumel.Layout;

namespace Sprocket.Host.SpaceEngineers.Features.Mods
{
    /// <summary>
    /// The mods the host has, side by side with whether an if or an otherwise if checks that one is installed or is not: the
    /// namespaces the mods registered, each shown by the mod's title and author, then the choice between is installed and is
    /// not installed.
    /// </summary>
    public sealed class ModsPage : EditorPage
    {
        const string Installed = "installed";
        const string NotInstalled = "not-installed";

        readonly string _lead;
        readonly SourceSwitch _source;
        readonly Action<HostTest> _done;
        readonly string _unlisted;
        readonly List<string> _keys = new List<string>();
        readonly List<HostNamespace> _mods = new List<HostNamespace>();

        string _chosen;
        bool _installed;
        string _search = "";
        PickStrip _strip;
        StageColumn _list;
        StageColumn _check;
        KeyedChildren<string, PickRow> _rows;
        FieldSet _form;
        List<Field> _asking;
        ICommand _finish;

        /// <summary>
        /// The mods of the host, opened on <paramref name="start" /> where there is one, its sentence led by <paramref name="lead" />,
        /// the first column's heading choosing between this and the other sources through <paramref name="source" />; hands the
        /// test chosen to <paramref name="done" />.
        /// </summary>
        public ModsPage(SprocketEditor editor, string lead, HostTest start, SourceSwitch source, Action<HostTest> done) : base(editor)
        {
            _lead = lead;
            _source = source;
            _done = done;
            _chosen = start?.Namespace ?? "";
            _installed = start?.Installed ?? true;

            // A namespace the program names that the host lacks is listed by the name it is written with, to be opened and changed.
            _unlisted = _chosen.Length != 0 && HostNamespaces.Find(_chosen) == null ? _chosen : "";
        }

        /// <inheritdoc />
        public override string Title
        {
            get
            {
                var said = SentenceText.Capitalised(_lead);
                return Chosen.Length == 0 ? $"{said} {SentenceText.Empty}" : $"{said} {SentenceText.Installed(Test())}";
            }
        }

        /// <inheritdoc />
        public override bool Asks => true;

        /// <inheritdoc />
        public override string PrimaryText => Editor.Texts.ChromeDone;

        /// <inheritdoc />
        public override bool CanPrimary => Chosen.Length != 0;

        /// <inheritdoc />
        public override string PrimaryAbout => "";

        /// <inheritdoc />
        public override string PrimaryWaits => Editor.Texts.Codex.PickDoneFirst;

        /// <inheritdoc />
        public override bool ScrollsItself => true;

        /// <summary>The strip the pick is drawn in.</summary>
        public PickStrip Strip => _strip;

        /// <summary>The column listing the mods, once the page is built.</summary>
        public StageColumn List => _list;

        /// <summary>The choice between is installed and is not installed, once the page is built.</summary>
        public FieldSet Form => _form;

        /// <summary>The namespace chosen, or empty where none is.</summary>
        string Chosen => _chosen;

        /// <summary>Whether the test checks that the namespace is installed, as the choice stands now.</summary>
        bool Checks => _asking == null ? _installed : _asking[0].Meaning(_form.Fields[0].Text ?? "") != NotInstalled;

        /// <summary>The test the pick comes to as it stands.</summary>
        HostTest Test()
        {
            return new HostTest { Namespace = _chosen, Installed = Checks };
        }

        /// <inheritdoc />
        public override void Primary()
        {
            if (!CanPrimary)
                return;

            _done(Test());
        }

        /// <inheritdoc />
        public override void Build()
        {
            var texts = Editor.Texts;
            if (_strip == null)
            {
                _strip = new PickStrip(texts);
                _finish = Editor.Act(CommandKind.Primary, Primary);

                _list = new StageColumn(_strip.Width, true, null, texts);
                _list.Title = texts.PickSource;
                _list.Heads(_source.Box(PickSource.Mods));
                _list.EmptyText = texts.PickModsEmpty;
                _list.Narrowed += Narrow;
                _rows = new KeyedChildren<string, PickRow>(_list.Rows, Row);

                _check = new StageColumn(_strip.Width, false, null, texts);
                _check.Title = texts.PickModCheck;
                _form = new FieldSet { Placement = Flex.Grow, IsVisible = true };
                _form.Edited += Edited;
                _form.Submitted += delegate
                {
                    if (_finish != null && CanPrimary)
                        _finish.Execute();
                };
                _check.Options.Add(_form);

                _asking = Conditions();
                _form.Show(_asking, new List<string> { _asking[0].Wording(_installed ? Installed : NotInstalled) }, null, texts);
            }

            _strip.Clear();
            _strip.Add(_list);
            _strip.Add(_check);
            _strip.ShowsArguments = false;

            FillMods();
            _check.ShowEmpty();
            _strip.Show();
        }

        /// <inheritdoc />
        public override void Refresh()
        {
            _strip.Tick();
        }

        /// <summary>The strip of the pick.</summary>
        protected override Element CreateView()
        {
            return _strip;
        }

        /// <summary>The one field of the check: whether the namespace is installed or is not.</summary>
        static List<Field> Conditions()
        {
            var field = Field.Among("condition", new[] { SentenceText.InstalledWords, SentenceText.NotInstalledWords }, new[] { Installed, NotInstalled });
            field.Worded = texts => texts.PickComparison;
            field.Entry = FieldInput.Name;
            field.Default = SentenceText.InstalledWords;
            return new List<Field> { field };
        }

        /// <summary>A row for the namespace <paramref name="key" />, choosing it when pressed and going on when pressed twice.</summary>
        PickRow Row(string key)
        {
            var row = new PickRow();
            row.Picked += delegate(HeldModifier held, int presses)
            {
                var choose = Editor.Act(CommandKind.PickChoice, () => Choose(key));
                if (choose == null)
                    return;

                choose.Execute();
                if (presses > 1 && _finish != null && CanPrimary)
                    _finish.Execute();
            };
            return row;
        }

        /// <summary>Chooses the namespace <paramref name="key" /> names.</summary>
        void Choose(string key)
        {
            if (key == _chosen)
                return;

            _installed = Checks;
            _chosen = key;
            Editor.Invalidate();
        }

        /// <summary>Says again what the pick has come to, as the choice is changed.</summary>
        void Edited()
        {
            if (Editor.Current == this)
                Editor.Invalidate();
        }

        /// <summary>Narrows the mods listed to those answering <paramref name="said" />.</summary>
        void Narrow(string said)
        {
            said = said ?? "";
            if (said == _search)
                return;

            var narrow = Editor.Act(CommandKind.NarrowChoices, delegate
            {
                _search = said;
                Editor.Invalidate();
            });

            if (narrow != null)
                narrow.Execute();
        }

        /// <summary>Lists the mods that answer the search, by title with the author under it, the one chosen lit.</summary>
        void FillMods()
        {
            _mods.Clear();
            foreach (var space in HostNamespaces.All)
            {
                if (space.Name != BlockKinds.Own)
                    _mods.Add(space);
            }

            _mods.Sort((one, other) =>
            {
                var order = string.Compare(one.Title, other.Title, StringComparison.OrdinalIgnoreCase);
                return order != 0 ? order : string.Compare(one.Name, other.Name, StringComparison.Ordinal);
            });

            _keys.Clear();
            foreach (var space in _mods)
            {
                if (Answers(space.Title, space.Author, space.Name))
                    _keys.Add(space.Name);
            }

            if (_unlisted.Length != 0 && Answers("", "", _unlisted))
                _keys.Add(_unlisted);

            _rows.Show(_keys);
            foreach (var key in _keys)
            {
                var space = HostNamespaces.Find(key);
                var row = _rows[key];
                row.Text = space == null || space.Title.Length == 0 ? key : space.Title;
                row.Secondary = space?.Author ?? "";
                row.IsChosen = key == _chosen;
                row.Show();
            }

            _list.NarrowText = _search;
            _list.ShowEmpty();
        }

        /// <summary>Whether the search is found in the mod's title, its author or its namespace.</summary>
        bool Answers(string title, string author, string name)
        {
            return EntityQuery.MatchesText(title, _search) || EntityQuery.MatchesText(author, _search) || EntityQuery.MatchesText(name, _search);
        }
    }
}
