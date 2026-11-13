using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Codex;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>One stage of a pick as a column: what it asks, whatever narrows it, and the things to choose from.</summary>
    /// <remarks>
    /// Its rows, kinds and options are the children of <see cref="Rows" />, <see cref="Kinds" /> and <see cref="Options" />,
    /// kept by whoever fills the column.
    /// </remarks>
    public partial class StageColumn : VStack
    {
        readonly List<Button> _views = new List<Button>();

        float _width;
        string _titleWords = "";
        string _titleTerm;

        string _showHidden;
        string _hideHidden;

        /// <summary>
        /// A column <paramref name="width" /> across, narrowed by typing where <paramref name="canNarrow" /> says, looked at
        /// in any of <paramref name="views" />, saying its fixed words in <paramref name="texts" />.
        /// </summary>
        public StageColumn(float width, bool canNarrow, IList<string> views, Texts texts)
        {
            CreateView();

            Width = width;
            CanNarrow = canNarrow;
            _search.IsVisible = canNarrow;
            Say(texts);
            ShowsHidden = false;

            _narrow.Edited += () => Typed(_narrow, _narrowClear, Narrowed);
            _narrowClear.Command = new RelayCommand(() => Emptied(_narrow, _narrowClear, Narrowed));
            _kindNarrow.Edited += () => Typed(_kindNarrow, _kindNarrowClear, KindsNarrowed);
            _kindNarrowClear.Command = new RelayCommand(() => Emptied(_kindNarrow, _kindNarrowClear, KindsNarrowed));

            // The list hangs off this line and is reachable only while it hangs there, so its rows cannot be pressed through it.
            _kindLine.Command = new RelayCommand(delegate
            {
                _kinds.Toggle();
                if (_kinds.IsOpen && KindsOpened != null)
                    KindsOpened();
            });

            if (views == null || views.Count < 2)
                return;

            for (var i = 0; i < views.Count; i++)
            {
                var link = new Button { Style = Styles.ViewLink, Text = views[i] };
                _views.Add(link);
                _viewLinks.Children.Add(link);
            }
        }

        /// <summary>Says the column's own words, those of its search and its buttons, in <paramref name="texts" />.</summary>
        public void Say(Texts texts)
        {
            _narrow.Hint = texts.PickSearch;
            _kindNarrow.Hint = texts.PickSearch;
            _narrowClear.Tip = KeptTip.For(_narrowClear.Tip, texts.Codex.PickClearTyped);
            _kindNarrowClear.Tip = KeptTip.For(_kindNarrowClear.Tip, texts.Codex.PickClearTyped);
            _kindClear.Tip = KeptTip.For(_kindClear.Tip, texts.Codex.PickEveryKind);
            _showHidden = texts.Codex.PickShowHidden;
            _hideHidden = texts.Codex.PickHideHidden;
            ShowsHidden = ShowsHidden;
            _kindLine.Tip = CodexTip.For(_kindLine.Tip, texts.Codex.PickKindAbout);
        }

        /// <summary>Names the views the column is looked at in <paramref name="views" />, in the order it was made with, each link saying what it shows by the text beside it in <paramref name="abouts" />.</summary>
        public void NameViews(IList<string> views, IList<string> abouts)
        {
            for (var i = 0; i < _views.Count && i < views.Count; i++)
            {
                _views[i].Text = views[i];
                _views[i].Tip = CodexTip.For(_views[i].Tip, i < abouts.Count ? abouts[i] : null);
            }
        }

        /// <summary>Raised with what is typed into the box that narrows the column, as it is typed.</summary>
        public event Action<string> Narrowed;

        /// <summary>Raised with what is typed into the box that narrows the list of kinds, as it is typed.</summary>
        public event Action<string> KindsNarrowed;

        /// <summary>Raised as the list of kinds is hung open, for the keyboard to be given to the box that narrows it.</summary>
        public event Action KindsOpened;

        /// <summary>How wide the column is.</summary>
        public float Width
        {
            get { return _width; }
            set
            {
                if (_width == value)
                    return;

                _width = value;
                Placement = FixedSize.Width(value);
            }
        }

        /// <summary>What the column asks, in capitals.</summary>
        public string Title
        {
            get { return _titleWords; }
            set
            {
                _titleWords = (value ?? "").ToUpperInvariant();
                Retitle();
            }
        }

        /// <summary>The codex page the title is the term of, its words then a link to it as a term's heading is; null for words alone.</summary>
        public string TitleTerm
        {
            set
            {
                _titleTerm = value;
                Retitle();
            }
        }

        /// <summary>Makes a link in the title explain its term with the page <paramref name="codex" /> has on it.</summary>
        public void Explain(CodexPages codex)
        {
            codex.Explain(_title);
        }

        /// <summary>Writes the title's words, as a link to its term's page where it has one.</summary>
        void Retitle()
        {
            _title.Markdown = _titleTerm == null ? Markdown.Escape(_titleWords) : CodexPages.Term(_titleWords, _titleTerm);
        }

        /// <summary>A quiet line under what the column asks, such as how many are picked, or empty.</summary>
        public string Subtitle
        {
            get { return _subtitle.Text; }
            set { _subtitle.Text = value ?? ""; }
        }

        /// <summary>Whether the column can be narrowed by typing.</summary>
        public bool CanNarrow { get; }

        /// <summary>The box the column is narrowed by typing into.</summary>
        public TextBox Narrow => _narrow;

        /// <summary>What the column is narrowed to, put in its box only while nobody is typing there.</summary>
        public string NarrowText
        {
            get { return _narrow.Text; }
            set { Put(_narrow, _narrowClear, value); }
        }

        /// <summary>Whether the column can be narrowed to one kind of block.</summary>
        public bool HasKinds
        {
            get { return _kindHead.IsVisible; }
            set { _kindHead.IsVisible = value; }
        }

        /// <summary>The kind the column is narrowed to, or the word for every kind.</summary>
        public string Kind
        {
            get { return _kindWord.Text; }
            set { _kindWord.Text = value; }
        }

        /// <summary>What putting every kind back does, or null while there is nothing to put back.</summary>
        public ICommand ClearKind
        {
            get { return _kindClear.Command; }
            set
            {
                _kindClear.Command = value;
                _kindClear.IsVisible = value != null;
            }
        }

        /// <summary>What showing or hiding the blocks hidden from toolbars does, or null where the column offers no such switch.</summary>
        public ICommand ShowHidden
        {
            get { return _hidden.Command; }
            set
            {
                _hidden.Command = value;
                _hidden.IsVisible = value != null;
            }
        }

        /// <summary>Whether the blocks hidden from toolbars are shown: the eye open, or shut.</summary>
        public bool ShowsHidden
        {
            get { return _hidden.Symbol == Symbols.HiddenShown; }
            set
            {
                _hidden.Symbol = value ? Symbols.HiddenShown : Symbols.HiddenHidden;
                _hidden.Tip = CodexTip.For(_hidden.Tip, value ? _hideHidden : _showHidden);
            }
        }

        /// <summary>The kinds the column can be narrowed to, one row each, in the list hanging from its line.</summary>
        public Collection Kinds => _kindRows.Children;

        /// <summary>The box the list of kinds is narrowed by typing into.</summary>
        public TextBox KindNarrow => _kindNarrow;

        /// <summary>What the list of kinds is narrowed to, put in its box only while nobody is typing there.</summary>
        public string KindText
        {
            get { return _kindNarrow.Text; }
            set { Put(_kindNarrow, _kindNarrowClear, value); }
        }

        /// <summary>The links to each way of looking at what the column offers, in order, or none where there is only one way.</summary>
        /// <remarks>Only the links to the views not showing are shown.</remarks>
        public IReadOnlyList<Button> Views => _views;

        /// <summary>Which of <see cref="Views" /> is showing.</summary>
        public int ViewShown { get; private set; }

        /// <summary>How many of the things there are the column is showing, or empty.</summary>
        public string Showing
        {
            get { return _showing.Text; }
            set
            {
                _showing.Text = value ?? "";
                _showing.IsVisible = _showing.Text.Length != 0;
            }
        }

        /// <summary>The choices about the picking itself, over the rows they describe.</summary>
        public Collection Options => _options.Children;

        /// <summary>The things to choose from, one row each.</summary>
        public Collection Rows => _rows.Children;

        /// <summary>A line over the rows saying what they are narrowed by, or empty.</summary>
        public string Note
        {
            get { return _note.Text; }
            set
            {
                _note.Text = value ?? "";
                _note.IsVisible = _note.Text.Length != 0;
            }
        }

        /// <summary>What the column says where there is nothing to choose.</summary>
        public string EmptyText { get; set; }



        /// <summary>Shows the view at <paramref name="current" /> as the one showing, the link to each other view carrying out the command beside it.</summary>
        public void ShowViews(int current, IList<ICommand> commands)
        {
            ViewShown = current;
            for (var i = 0; i < _views.Count; i++)
            {
                _views[i].IsVisible = i != current;
                _views[i].Command = i == current || i >= commands.Count ? null : commands[i];
            }
        }

        /// <summary>Says what the column says where there is nothing to choose, if there is nothing, and shows the choices about the picking where there are any.</summary>
        public void ShowEmpty()
        {
            _options.IsVisible = _options.Children.Count != 0;

            var empty = _rows.Children.Count == 0;
            _empty.IsVisible = empty;
            if (empty)
                _empty.Text = EmptyText ?? "";
        }

        /// <summary>Asks what the chosen thing is given in <paramref name="given" />, over the choices and the rows, or nothing there where it is null.</summary>
        public void Ask(Element given)
        {
            if (given != null && given.Parent == _given)
                return;

            _given.Children.Clear();
            if (given != null)
                _given.Children.Add(given);

            _given.IsVisible = given != null;
        }

        /// <summary>Puts <paramref name="choice" /> after what the column asks, as the answer to it, or nothing there where it is null.</summary>
        public void Heads(Element choice)
        {
            if (choice != null && choice.Parent == _heads)
                return;

            _heads.Children.Clear();
            if (choice != null)
                _heads.Children.Add(choice);

            _heads.IsVisible = choice != null;
        }

        /// <summary>Closes the list of kinds, where it hangs open.</summary>
        public void CloseKinds()
        {
            _kinds.Close();
        }

        /// <summary>Puts <paramref name="text" /> in <paramref name="box" /> unless somebody is typing there, and arms its cross.</summary>
        static void Put(TextBox box, Glyph clear, string text)
        {
            if (!box.IsFocused)
                box.Text = text ?? "";

            clear.IsVisible = box.Text.Length != 0;
        }

        static void Typed(TextBox box, Glyph clear, Action<string> told)
        {
            clear.IsVisible = box.Text.Length != 0;
            if (told != null)
                told(box.Text);
        }

        static void Emptied(TextBox box, Glyph clear, Action<string> told)
        {
            box.Text = "";
            Typed(box, clear, told);
        }
    }
}
