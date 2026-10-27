using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using Lumel.Windows;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>A <see cref="SprocketEditor" /> on the screen: the chrome around the page, the page's own view, and whatever hangs over them.</summary>
    public partial class EditorWorkspace : SprocketWorkspace
    {
        /// <summary>How many ticks the pointer rests on something before its tip opens.</summary>
        public const int TipDelay = 30;

        /// <summary>How many ticks a tip stays open while the pointer crosses from what it explains onto it.</summary>
        public const int TipGrace = 15;

        /// <summary>Lifts a symbol in the heading so its middle is level with the middle of the heading's capitals.</summary>
        /// <remarks>Measured in the game.</remarks>
        public static readonly Thickness LevelWithCapitals = new Thickness(0f, -2.5f, 0f, 2.5f);

        readonly SprocketEditor _editor;

        readonly VStack _head;
        readonly Glyph _back;
        readonly HeadingRow _title;
        readonly Glyph _undo;
        readonly Glyph _redo;
        readonly Glyph _tips;
        readonly Switch _switch;
        readonly Button _done;
        readonly HazardButton _admin;
        readonly Glyph _close;
        readonly Button _library;
        readonly Glyph _run;
        readonly Glyph _delete;
        readonly Label _subtitle;
        readonly Banner _banner = new Banner { Base = { Padding = new Thickness(0f, 0f, Sizes.Bar, 0f) } };
        readonly Grid _page;
        readonly ScrollView _scroll;
        readonly VStack _body;
        readonly VStack _foot;
        readonly HStack _actions;

        readonly Relay _undoes;
        readonly Relay _redoes;
        readonly Relay _libraries;
        readonly Relay _flips;
        readonly Relay _dones;
        readonly Relay _runs;
        readonly Relay _deletes;

        readonly Arguments _undone = new Arguments();
        readonly Arguments _redone = new Arguments();

        readonly MessageBox _dialog;
        readonly ConfirmPopup _confirm = new ConfirmPopup();
        readonly CodexTip _codexTip;
        readonly List<ChoiceEntry> _actionsShown = new List<ChoiceEntry>();

        EditorPage _shownPage;
        int _seen = -1;
        int _asked;
        bool _hanging;

        Element _raisedBy;

        readonly Relay _commits;
        readonly Relay _ends;

        // The plate whose name is being typed, a heading's or a row's, and what giving it does.
        NamePlate _plate;
        Naming _renamed;
        HeadingPart _namedArrival;

        // Whether the name being typed is the one a page arrived to be given, which belongs to making what it is on.
        bool _arriving;
        string _checkedText;
        bool _checkedTaken;

        /// <summary>The chrome for <paramref name="editor" />, which it hands its input root and window layer to.</summary>
        public EditorWorkspace(SprocketEditor editor)
        {
            _editor = editor;
            _title = new HeadingRow(Rename);
            _commits = new Relay(CommitRename);
            _ends = new Relay(EndRename);

            _undoes = new Relay(() => Run(_editor.Screen.Undo));
            _redoes = new Relay(() => Run(_editor.Screen.Redo));
            _libraries = new Relay(() => Run(_editor.Screen.Library));
            _flips = new Relay(() => Run(_editor.Screen.Flip));
            _dones = new Relay(() => Run(_editor.Screen.Primary));
            _runs = new Relay(() => Run(_editor.Screen.Running ? _editor.Screen.Stops : _editor.Screen.Runs));
            _deletes = new Relay(() => Run(_editor.Screen.Deletes));

            _scroll = new ScrollView { Style = Styles.ScrollView, Content = new VStack().As(out _body) };

            var root = new VStack { Base = { Background = Palette.Page } };
            Children.Add(root);
            root.Children.Add(new VStack
            {
                Base = { Background = Palette.Chrome, Padding = new Thickness(16f, 8f) },
                Spacing = 4f,
                Children =
                {
                    new HStack
                    {
                        Spacing = 8f,
                        Children =
                        {
                            new Glyph { Symbol = Symbols.Left, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f), Base = { Padding = LevelWithCapitals }, Command = new Relay(Back) }.As(out _back),
                            _title,
                            // Quietest first, Done or the close cross last: the row stands against the right edge, so a
                            // control's place depends only on what stands to its right.
                            new HazardButton { Style = Styles.Button, Command = new Relay(OpenAdmin) }.As(out _admin),
                            new Glyph { Symbol = "?", Style = Styles.Tips, Placement = FixedSize.Size(32f, 32f), Command = new Relay(ToggleTips) }.As(out _tips),
                            new Glyph { Symbol = Symbols.Play, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f), Command = _runs }.As(out _run),
                            new Glyph { Symbol = Symbols.Undo, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f), Command = _undoes }.As(out _undo),
                            new Glyph { Symbol = Symbols.Redo, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f), Command = _redoes }.As(out _redo),
                            new Button { Style = Styles.Button, Command = _libraries }.As(out _library),
                            new Glyph { Symbol = Symbols.Delete, Style = Styles.Bin, Placement = FixedSize.Size(32f, 32f), Command = _deletes }.As(out _delete),
                            new Switch { Style = Styles.SystemSwitch, Command = _flips }.As(out _switch),
                            new Button { Style = Styles.Primary, Command = _dones }.As(out _done),
                            new Glyph { Symbol = Symbols.Close, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f), Command = new Relay(Back) }.As(out _close)
                        }
                    },
                    new Label { Style = Styles.Detail }.As(out _subtitle)
                }
            }.As(out _head));
            root.Children.Add(new Rule(1f, Palette.Line));
            root.Children.Add(new Grid
            {
                Base = { Padding = new Thickness(16f) },
                Placement = Flex.Grow,
                Columns = { GridLength.Flex(1f) },
                Rows = { GridLength.Auto, GridLength.Flex(1f) },
                Cells = { [0, 0] = _banner }
            }.As(out _page));
            // Shown only while the screen has actions, which only developer mode gives it.
            root.Children.Add(new VStack
            {
                IsVisible = false,
                Children =
                {
                    new Rule(1f, Palette.Line),
                    new HStack { Base = { Background = Palette.Chrome, Padding = new Thickness(16f, 8f) }, Spacing = 8f }.As(out _actions)
                }
            }.As(out _foot));

            _dialog = new MessageBox
            {
                Style = Styles.Dialog,
                Placement = FixedSize.Wide(Sizes.Popup),
                MessageStyle = Styles.Question,
                ButtonStyle = Styles.Button,
                DefaultStyle = Styles.Primary,
                Cancel = new Relay(() => _editor.Unask()),
                ShowsCancel = false
            };

            _codexTip = new CodexTip
            {
                Style = Styles.CodexTip,
                Shadow = Styles.ShortShadow,
                Nests = true,
                Placement = FixedSize.Wide(Sizes.Popup),
                MaxHeight = Sizes.TipHigh,
                Spacing = 8f,
                Delay = TipDelay,
                Grace = TipGrace,
                BodyStyle = Styles.Tip,
                StepStyle = Styles.Icon,
                BackSymbol = Symbols.Left,
                ForwardSymbol = Symbols.Right,
                Pages = _editor.Codex.Find
            };

            Tips = _codexTip;
            Input.FocusChanged += Refocused;

            _editor.Input = Input;
            _editor.Windows = Windows;
            _editor.Renamer = Rename;
        }

        /// <summary>Whether a question with answers is waiting, which owns Enter and Escape.</summary>
        public override bool IsQuestioning => Windows.IsOpen(_dialog) || Windows.IsOpen(_confirm);

        /// <inheritdoc />
        protected override bool ShowsTips => _editor.TipsShown;

        /// <inheritdoc />
        protected override int Now => _editor.CurrentTick;

        /// <summary>Why the name in hand cannot be given as it stands, or empty when it can or none is.</summary>
        string Blocked
        {
            get
            {
                if (_plate == null)
                    return "";

                var part = _renamed;
                var text = _plate.Box.Text;
                if (part.NameRequired && string.IsNullOrEmpty(text.Trim()))
                    return _editor.Texts.RenameEmpty;

                return Taken(part, text) ? _editor.Texts.RenameTaken : "";
            }
        }

        /// <summary>Brings the chrome into line with the screen, and puts the page's view on it where the page changed.</summary>
        public override void Update()
        {
            var screen = _editor.Screen;

            Ask(screen);

            if (_plate != null && _title.IndexOf(_plate) >= 0 && !screen.CanRename)
                EndRename();

            if (_editor.Current != _shownPage)
                Show(screen);

            _banner.Show(_editor.Notice, _editor.CurrentTick);

            if (screen.Revision == _seen)
                return;

            _seen = screen.Revision;
            Wrote();
            Dress(screen);
        }

        /// <inheritdoc />
        /// <remarks>Nothing scrolls while a name is being typed.</remarks>
        public override void ScrollBy(Vector2 point, float amount)
        {
            if (_plate == null)
                base.ScrollBy(point, amount);
        }

        /// <summary>Escape, answering whether the host should close the editor.</summary>
        public override bool Escape()
        {
            if (IsQuestioning)
            {
                Dismiss();
                return false;
            }

            if (CloseTop())
                return false;

            if (_plate != null)
            {
                CommitRename();
                EndRename();
            }

            Input.Focus(null);

            if (!_editor.Screen.CanGoBack)
                return true;

            Run(_editor.Screen.Back);
            return false;
        }

        /// <summary>Takes the answer the page said Enter takes, and says whether there was one.</summary>
        public override bool TakeDefault()
        {
            if (Windows.IsOpen(_dialog))
                return _dialog.TakeAccept();

            var taken = _editor.Screen.Default;
            if (!Windows.IsOpen(_confirm) || taken == null)
                return false;

            Run(taken.Command);
            return true;
        }

        /// <summary>Puts the standing question away without answering it.</summary>
        public void Dismiss()
        {
            if (Windows.IsOpen(_dialog))
            {
                _dialog.TakeCancel();
                return;
            }

            Windows.Close(_confirm);
            _editor.Unask();
        }

        /// <inheritdoc />
        /// <remarks>While the name being typed cannot be given, only a press on its plate goes on.</remarks>
        protected override bool Admits(Vector2 point)
        {
            return Blocked.Length == 0 || _plate.Reaches(point);
        }

        /// <inheritdoc />
        protected override void Pressing(Element target)
        {
            // A question this press raises hangs under the target.
            _raisedBy = target;
        }

        /// <inheritdoc />
        protected override Element RootUnder(Vector2 point)
        {
            return Blocked.Length > 0 ? _plate.Under(point) : base.RootUnder(point);
        }

        /// <inheritdoc />
        protected override Element RootSayingAt(Vector2 point)
        {
            return Blocked.Length > 0 ? _plate.SayingAt(point) : base.RootSayingAt(point);
        }

        /// <inheritdoc />
        /// <remarks>The screen's scroller takes the wheel unless the page scrolls itself.</remarks>
        protected override void ScrollRoot(Vector2 point, float amount)
        {
            if (_shownPage == null || !_shownPage.ScrollsItself)
            {
                _scroll.ScrollBy(amount);
                return;
            }

            base.ScrollRoot(point, amount);
        }

        /// <summary>Opens the question the screen raised, or puts away the one it no longer asks.</summary>
        void Ask(EditorScreen screen)
        {
            if (!screen.HasQuestion)
            {
                Windows.Close(_dialog);
                Windows.Close(_confirm);
                _hanging = false;
                return;
            }

            if (_asked == screen.Asked)
            {
                if (_hanging && !Windows.IsOpen(_confirm))
                {
                    _hanging = false;
                    _editor.Unask();
                }

                return;
            }

            _asked = screen.Asked;
            Windows.Close(_dialog);
            Windows.Close(_confirm);

            _hanging = screen.QuestionIsHere && _raisedBy != null;
            if (_hanging)
            {
                _confirm.Ask(screen.Question, screen.QuestionArguments, screen.Answers, screen.Default, _raisedBy, Windows);
                return;
            }

            var yes = screen.Answers.Count > 0 ? screen.Answers[0] : null;
            var no = screen.Answers.Count > 1 ? screen.Answers[1] : null;

            _dialog.Message = screen.QuestionArguments == null ? Markdown.Parse(screen.Question) : Template.Read(screen.Question);
            _dialog.MessageArguments = screen.QuestionArguments;
            _dialog.YesText = yes != null ? yes.Text : "";
            _dialog.YesCommand = yes?.Command;
            _dialog.NoText = no != null ? no.Text : "";
            _dialog.NoCommand = no?.Command;
            _dialog.Default = yes != null && yes == screen.Default ? MessageButton.Yes
                : no != null && no == screen.Default ? MessageButton.No
                : (MessageButton?)null;

            Windows.Open(_dialog);
        }

        /// <summary>Puts the page on top on the screen, in the screen's scroller unless it scrolls itself.</summary>
        void Show(EditorScreen screen)
        {
            EndRename();
            Windows.Close(_confirm);

            // Each page keeps how far down it was read, so coming back from a page opened from it finds the place again.
            if (_shownPage != null && !_shownPage.ScrollsItself)
                _shownPage.Scrolled = _scroll.Scrolled;

            _shownPage = _editor.Current;

            // Set once for the life of the page: changing it later would move the page under whoever is reading it.
            _subtitle.IsVisible = !string.IsNullOrEmpty(screen.Subtitle);
            _seen = -1;

            var drawn = _shownPage.View;
            _body.Children.Clear();

            // A second scroller around one the page has would fight it over one gesture.
            if (_shownPage.ScrollsItself)
            {
                _page.Cells[1, 0] = drawn;
                return;
            }

            _body.Children.Add(drawn);
            _page.Cells[1, 0] = _scroll;
            _scroll.Scrolled = _shownPage.Scrolled;
        }

        /// <summary>Writes what the screen says onto the chrome: the heading, the words and states beside it, what each control does, and the way on.</summary>
        void Dress(EditorScreen screen)
        {
            _title.Update(screen.Heading, _plate == null ? -1 : _title.IndexOf(_plate));
            NameArrival(screen);

            _subtitle.Text = screen.Subtitle;

            if (_plate != null)
            {
                var wrong = Blocked;
                _plate.Refuse(wrong.Length > 0 ? wrong : null);
            }

            var texts = _editor.Texts;

            var closes = !screen.CanGoBack;
            _back.IsVisible = screen.CanGoBack;
            _back.Tip = Tip.For(_back.Tip, _back.IsVisible ? texts.Codex.ChromeBack : null);
            _close.IsVisible = closes;
            _close.Tip = Tip.For(_close.Tip, !closes ? null : texts.Codex.ChromeClose);

            // Asked as the screen is dressed, which is enough: creative tools are switched on and off with the editor put away.
            _admin.IsVisible = !screen.CanGoBack && _editor.MayAdminister;
            _admin.Text = texts.AdminButton;

            _tips.Style = _editor.TipsShown ? Styles.Tips : Styles.TipsOff;
            _tips.Tip = Tip.For(_tips.Tip, texts.Codex.ChromeTips);

            _undone.Set("action", screen.UndoText);
            _redone.Set("action", screen.RedoText);

            _undo.IsVisible = screen.HasHistory;
            _redo.IsVisible = screen.HasHistory;
            _undoes.CanExecute = screen.Undo != null;
            _undo.Tip = Tip.For(_undo.Tip, screen.Undo != null ? texts.Codex.ChromeUndo : null, _undone);
            _redoes.CanExecute = screen.Redo != null;
            _redo.Tip = Tip.For(_redo.Tip, screen.Redo != null ? texts.Codex.ChromeRedo : null, _redone);

            _library.IsVisible = screen.Library != null;
            _library.Text = texts.SystemsLoad;
            _libraries.CanExecute = screen.Library != null;
            _library.Tip = Tip.For(_library.Tip, screen.Library != null ? texts.Codex.TermLibrary : null);

            var doing = screen.Running ? screen.Stops : screen.Runs;
            _run.IsVisible = doing != null;
            _run.Symbol = screen.Running ? Symbols.Stop : Symbols.Play;
            _runs.CanExecute = doing != null;
            _run.Tip = Tip.For(_run.Tip, doing == null ? null : screen.Running ? SequenceText.Stops(texts) : SequenceText.Starts(texts));

            // A bin held back keeps its place, so nothing beside it moves when a switch is flipped.
            _delete.IsVisible = screen.Deletes != null || screen.DeleteLocked != null;
            _deletes.CanExecute = screen.Deletes != null;
            _delete.Tip = Tip.For(_delete.Tip, screen.Deletes != null ? screen.DeleteAbout : screen.DeleteLocked);

            _switch.IsVisible = screen.HasSwitch;
            _switch.IsOn = screen.SwitchIsOn;

            // The heading stands as the subject's row on the home screen does: on red where something is wrong, on amber where it
            // carries a warning or is switched off.
            var broken = screen.Standing == NoticeLevel.Problem;
            var warned = !broken && (screen.Standing == NoticeLevel.Warning || screen.HasSwitch && !screen.SwitchIsOn);
            RemarkLook.Ground(_head, broken ? NoticeLevel.Problem : warned ? NoticeLevel.Warning : NoticeLevel.Info, Palette.Chrome);
            _switch.Says(texts.SwitchOn, texts.SwitchOff);
            _flips.CanExecute = screen.HasSwitch && !screen.SwitchLocked;
            _switch.Tip = Tip.For(_switch.Tip, screen.HasSwitch ? screen.SwitchAbout : null);

            _done.IsVisible = screen.HasPrimary;
            _done.Text = screen.PrimaryText;
            _dones.CanExecute = screen.Primary != null;
            _done.Tip = Tip.For(_done.Tip, screen.PrimaryAbout.Length != 0 ? screen.PrimaryAbout : null);

            ShowActions(screen);
        }

        /// <summary>Puts a button at the foot for each of the screen's actions, where they are not the ones already there, and the foot away where there are none.</summary>
        void ShowActions(EditorScreen screen)
        {
            var actions = screen.Actions;
            _foot.IsVisible = actions.Count > 0;
            if (Same(actions))
                return;

            _actionsShown.Clear();
            _actions.Children.Clear();

            for (var i = 0; i < actions.Count; i++)
            {
                var entry = actions[i];
                _actionsShown.Add(entry);
                _actions.Children.Add(new Button
                {
                    Style = entry.IsCurrent ? Styles.Current : Styles.Plain,
                    Text = Caps(entry.Text),
                    Command = entry.Command
                });
            }
        }

        /// <summary>Whether <paramref name="actions" /> are the very entries already at the foot.</summary>
        bool Same(IList<ChoiceEntry> actions)
        {
            if (actions.Count != _actionsShown.Count)
                return false;

            for (var i = 0; i < actions.Count; i++)
            {
                if (!ReferenceEquals(actions[i], _actionsShown[i]))
                    return false;
            }

            return true;
        }

        /// <summary>Whether <paramref name="part" /> would refuse <paramref name="text" />, asked once per change of text.</summary>
        bool Taken(Naming part, string text)
        {
            if (!ReferenceEquals(text, _checkedText))
            {
                _checkedText = text;
                _checkedTaken = !string.IsNullOrEmpty(text) && !part.Takes(text);
            }

            return _checkedTaken;
        }

        /// <summary>Goes back a page, or asks to leave from the first one.</summary>
        void Back()
        {
            var screen = _editor.Screen;
            if (screen.CanGoBack)
                Run(screen.Back);
            else
                Close();
        }

        /// <summary>Begins renaming the part of the heading at <paramref name="index" />.</summary>
        void Rename(int index)
        {
            var parts = _editor.Screen.Heading;
            if (index >= parts.Count || !parts[index].CanRename)
                return;

            Rename(_title.Plate(index), parts[index]);
        }

        /// <summary>Begins giving the name <paramref name="naming" /> holds in <paramref name="plate" />, in place, where it can be changed.</summary>
        /// <remarks>Enter gives the name, Escape puts the box away, and the keyboard leaving gives it too; a name that cannot be given keeps the keyboard and every press outside the plate away.</remarks>
        public void Rename(NamePlate plate, Naming naming)
        {
            if (!naming.CanRename)
                return;

            EndRename();
            _plate = plate;
            _renamed = naming;
            _checkedText = null;
            plate.Box.Submit = _commits;
            plate.Box.Cancel = _ends;
            plate.Box.Edited += Retyped;
            plate.Begin(naming.Name);
            Input.Focus(plate.Box);
            _seen = -1;
        }

        /// <summary>Begins renaming the part of the heading that arrives to be named, once for that part.</summary>
        void NameArrival(EditorScreen screen)
        {
            if (_plate != null)
                return;

            for (var i = 0; i < screen.Heading.Count && i < _title.Count; i++)
            {
                var part = screen.Heading[i];
                if (!part.NamedOnArrival || !part.CanRename || part == _namedArrival)
                    continue;

                _namedArrival = part;
                Rename(i);
                _arriving = _plate != null;
                return;
            }
        }

        /// <summary>Gives the name in hand, where it can be given, and puts the box away.</summary>
        void CommitRename()
        {
            if (_plate == null || Blocked.Length > 0)
                return;

            // The part the name was begun on, which a press that changed the screen has taken out of the heading.
            var part = _renamed;
            var name = _plate.Box.Text;
            var arriving = _arriving;

            EndRename();

            var renames = part.Renames;
            if (renames != null)
                renames(name);

            // The name given on arrival is part of making the thing, so leaving the page does not ask whether to keep it.
            if (arriving)
                _editor.Keep(_editor.Current);
        }

        /// <summary>Puts the box away, giving nothing.</summary>
        void EndRename()
        {
            if (_plate == null)
                return;

            var plate = _plate;
            _plate = null;
            _arriving = false;
            _renamed = null;
            plate.Box.Edited -= Retyped;
            plate.End();
            _seen = -1;

            if (Input.Focused == plate.Box)
                Input.Focus(null);
        }

        /// <summary>Says the name in hand changed, which decides whether it can be given.</summary>
        void Retyped()
        {
            _seen = -1;
        }

        /// <summary>Takes the keyboard leaving a name being typed as the press away it is, which gives the name.</summary>
        void Refocused()
        {
            if (_plate == null)
                return;

            var box = _plate.Box;
            if (Input.Focused == box)
                return;

            CommitRename();

            if (_plate != null)
                Input.Focus(box);
        }

        /// <summary>Turns tips off where they are shown and on where they are not, closing a tip that is open.</summary>
        void ToggleTips()
        {
            _codexTip.Close();
            _editor.ToggleTips();
        }

        /// <summary>Opens the world's settings over the screen, for an admin.</summary>
        void OpenAdmin()
        {
            if (_editor.MayAdminister)
                Windows.Open(new Admin.AdminDialog(_editor));
        }

        /// <summary>Carries out <paramref name="command" />, where there is one that can be.</summary>
        static void Run(ICommand command)
        {
            if (command != null && command.CanExecute)
                command.Execute();
        }

        /// <summary>The words of a button, in capitals as the game's own are.</summary>
        static string Caps(string text)
        {
            return text == null ? "" : text.ToUpperInvariant();
        }

        /// <summary>The chrome has just written what the screen says onto its controls.</summary>
        /// <remarks>Has no body in anything packed for the game, where the call compiles to nothing.</remarks>
        partial void Wrote();
    }
}
