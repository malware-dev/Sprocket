using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using Lumel.Windows;
using VRage.Input;
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

        readonly VStack _frame;
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

        readonly RelayCommand _undoes;
        readonly RelayCommand _redoes;
        readonly RelayCommand _libraries;
        readonly RelayCommand _flips;
        readonly RelayCommand _dones;
        readonly RelayCommand _runs;
        readonly RelayCommand _deletes;

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

        readonly RelayCommand _commits;
        readonly RelayCommand _ends;

        // The plate whose name is being typed, a heading's or a row's, and what giving it does.
        NamePlate _plate;
        Naming _renamed;
        HeadingPart _namedArrival;

        // Whether the name being typed is the one a page arrived to be given, which belongs to making what it is on.
        bool _arriving;

        // Whether the terms on the screen are plain, so the whole screen is visited only when tips are turned off or on.
        bool _plain;
        string _checkedText;
        bool _checkedTaken;

        /// <summary>The chrome for <paramref name="editor" />, which it hands its input root and window layer to.</summary>
        public EditorWorkspace(SprocketEditor editor) : base("Sprocket", () => editor.CurrentTick)
        {
            _editor = editor;
            _title = new HeadingRow(Rename);
            _commits = new RelayCommand(CommitRename);
            _ends = new RelayCommand(EndRename);

            _undoes = new RelayCommand(() => Run(_editor.Screen.Undo));
            _redoes = new RelayCommand(() => Run(_editor.Screen.Redo));
            _libraries = new RelayCommand(() => Run(_editor.Screen.Library));
            _flips = new RelayCommand(() => Run(_editor.Screen.Flip));
            _dones = new RelayCommand(() => Run(_editor.Screen.Primary));
            _runs = new RelayCommand(() => Run(_editor.Screen.Running ? _editor.Screen.Stops : _editor.Screen.Runs));
            _deletes = new RelayCommand(() => Run(_editor.Screen.Deletes));

            _scroll = new ScrollView { Style = Styles.ScrollView, Content = new VStack().As(out _body) };

            var root = _frame = new VStack { Base = { Background = Palette.Page } };
            Children.Add(root);
            root.Children.Add(new VStack
            {
                Base = { Background = Palette.Chrome, Padding = new Thickness(16f, 8f), Spacing = Gaps.Within },
                Children =
                {
                    new HStack
                    {
                        Base = { Spacing = Gaps.Parts },
                        Children =
                        {
                            new Glyph { Symbol = Symbols.Left, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f), Base = { Padding = LevelWithCapitals }, Command = new RelayCommand(Back) }.As(out _back),
                            _title,
                            // Quietest first, Done or the close cross last: the row stands against the right edge, so a
                            // control's place depends only on what stands to its right.
                            new HazardButton { Style = Styles.Button, Command = new RelayCommand(OpenAdmin) }.As(out _admin),
                            new Glyph { Symbol = "?", Style = Styles.Tips, Placement = FixedSize.Size(32f, 32f), Command = new RelayCommand(ToggleTips) }.As(out _tips),
                            new Glyph { Symbol = Symbols.Play, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f), Command = _runs }.As(out _run),
                            new Glyph { Symbol = Symbols.Undo, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f), Command = _undoes }.As(out _undo),
                            new Glyph { Symbol = Symbols.Redo, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f), Command = _redoes }.As(out _redo),
                            new Button { Style = Styles.Button, Command = _libraries }.As(out _library),
                            new Glyph { Symbol = Symbols.Delete, Style = Styles.Bin, Placement = FixedSize.Size(32f, 32f), Command = _deletes }.As(out _delete),
                            new Switch { Style = Styles.SystemSwitch, Command = _flips }.As(out _switch),
                            new Button { Style = Styles.Primary, Command = _dones }.As(out _done),
                            new Glyph { Symbol = Symbols.Close, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f), Command = new RelayCommand(Back) }.As(out _close)
                        }
                    },
                    new Label { Style = Styles.Detail }.As(out _subtitle)
                }
            }.As(out _head));
            root.Children.Add(new Separator());
            // The banner over the page, which fills what is left.
            root.Children.Add(new VStack
            {
                Base = { Padding = new Thickness(16f) },
                Placement = Flex.Grow,
                Children =
                {
                    _banner,
                    new Grid
                    {
                        Placement = Flex.Grow,
                        Columns = { GridLength.Flex(1f) },
                        Rows = { GridLength.Flex(1f) }
                    }.As(out _page)
                }
            });
            // Shown only while the screen has actions, which only developer mode gives it.
            root.Children.Add(new VStack
            {
                IsVisible = false,
                Children =
                {
                    new Separator(),
                    new HStack { Base = { Spacing = Gaps.Parts, Background = Palette.Chrome, Padding = new Thickness(16f, 8f) }}.As(out _actions)
                }
            }.As(out _foot));

            _dialog = new MessageBox
            {
                Style = Styles.Asking,
                Placement = FixedSize.Width(Sizes.Popup),
                CancelCommand = new RelayCommand(() => _editor.Unask()),
                ShowsCancel = false
            };

            _codexTip = new CodexTip
            {
                Style = Styles.CodexTip,
                OpensNestedTips = true,
                Placement = FixedSize.Width(Sizes.Popup),
                ShowDelay = TipDelay,
                CloseDelay = TipGrace,
                FnResolvePage = _editor.Codex.Find
            };

            CodexTip = _codexTip;
            Input.FocusChanged += Refocused;
            Input.PreviewPointerDown += PressReaching;
            Input.PreviewWheel += Turning;

            _editor.Input = Input;
            _editor.Windows = Windows;
            _editor.Renamer = Rename;
        }

        /// <summary>Whether a question with answers is waiting, which owns Enter and Escape.</summary>
        public bool IsQuestioning => Windows.IsOpen(_dialog) || Windows.IsOpen(_confirm);

        /// <inheritdoc />
        /// <remarks>While the player has turned tips off, only a <see cref="KeptTip" /> opens.</remarks>
        protected override Tip TipFor(Element owner, Tip tip)
        {
            return _editor.TipsShown || tip is KeptTip ? tip : null;
        }

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

            _banner.Show(_editor.Notice);

            if (screen.Revision == _seen)
                return;

            _seen = screen.Revision;
            Wrote();
            Dress(screen);
        }

        /// <inheritdoc />
        /// <remarks>While a question is open it takes Enter as its default answer and no other key; Escape is <see cref="Escape" /> whatever is open.</remarks>
        public override bool Key(MyKeys key, bool shift, bool ctrl)
        {
            if (key == MyKeys.Escape)
                return !Escape();

            if (IsQuestioning)
                return key != MyKeys.Enter || TakeDefault();

            return base.Key(key, shift, ctrl);
        }

        /// <summary>Escape, answering whether the host should close the editor.</summary>
        public bool Escape()
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
        public bool TakeDefault()
        {
            if (Windows.IsOpen(_dialog))
                return _dialog.PerformAccept();

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
                _dialog.PerformCancel();
                return;
            }

            Windows.Close(_confirm);
            _editor.Unask();
        }

        /// <summary>Notes what a press is about to reach, under which a question it raises hangs.</summary>
        void PressReaching(Element target)
        {
            _raisedBy = target;
        }

        /// <summary>Holds the wheel while a name is being typed, and gives the screen's scroller the wheel turned anywhere on the screen unless the page scrolls itself.</summary>
        void Turning(WheelEventArgs wheel)
        {
            if (_plate != null)
            {
                wheel.Handled = true;
                return;
            }

            if (_shownPage != null && _shownPage.ScrollsItself || !Within(wheel.Target, _frame))
                return;

            _scroll.ScrollBy(wheel.Amount);
            wheel.Handled = true;
        }

        /// <summary>Whether <paramref name="element" /> is <paramref name="holder" /> or inside it.</summary>
        static bool Within(Element element, Element holder)
        {
            for (var at = element; at != null; at = at.Parent)
            {
                if (at == holder)
                    return true;
            }

            return false;
        }

        /// <summary>Keeps presses to the plate while the name being typed on it cannot be given as it stands.</summary>
        void Confine()
        {
            Input.Confined = Blocked.Length > 0 ? _plate : null;
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

            _dialog.Message = screen.QuestionArguments == null ? Markdown.Parse(screen.Question) : Template.Parse(screen.Question);
            _dialog.MessageArguments = screen.QuestionArguments;
            _dialog.YesText = yes != null ? yes.Text : "";
            _dialog.YesCommand = yes?.Command;
            _dialog.NoText = no != null ? no.Text : "";
            _dialog.NoCommand = no?.Command;
            _dialog.DefaultButton = yes != null && yes == screen.Default ? MessageBoxAnswer.Yes
                : no != null && no == screen.Default ? MessageBoxAnswer.No
                : (MessageBoxAnswer?)null;

            Windows.Open(_dialog);
        }

        /// <summary>Puts the page on top on the screen, in the screen's scroller unless it scrolls itself.</summary>
        void Show(EditorScreen screen)
        {
            EndRename();
            Windows.Close(_confirm);

            // Each page keeps how far down it was read, so coming back from a page opened from it finds the place again.
            if (_shownPage != null && !_shownPage.ScrollsItself)
                _shownPage.Scrolled = _scroll.VerticalOffset;

            _shownPage = _editor.Current;

            // Set once for the life of the page: changing it later would move the page under whoever is reading it.
            _subtitle.IsVisible = !string.IsNullOrEmpty(screen.Subtitle);
            _seen = -1;

            var drawn = _shownPage.View;
            _body.Children.Clear();

            // A second scroller around one the page has would fight it over one gesture.
            if (_shownPage.ScrollsItself)
            {
                _page.Cells[0, 0] = drawn;
                return;
            }

            _body.Children.Add(drawn);
            _page.Cells[0, 0] = _scroll;
            _scroll.ScrollToVerticalOffset(_shownPage.Scrolled);
        }

        /// <summary>Puts every text in <paramref name="element" /> in the look its terms have while tips are off where <paramref name="plain" />, and takes it out otherwise.</summary>
        static void Plain(Element element, bool plain)
        {
            if (element is RichText)
                element.Set(LookStates.Plain, plain);

            for (var i = 0; i < element.VisualChildCount; i++)
                Plain(element.GetVisualChild(i), plain);
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
            _back.Tip = KeptTip.For(_back.Tip, _back.IsVisible ? texts.Codex.ChromeBack : null);
            _close.IsVisible = closes;
            _close.Tip = KeptTip.For(_close.Tip, !closes ? null : texts.Codex.ChromeClose);

            // Asked as the screen is dressed, which is enough: creative tools are switched on and off with the editor put away.
            _admin.IsVisible = !screen.CanGoBack && _editor.MayAdminister;
            _admin.Text = texts.AdminButton;

            _tips.Set(LookStates.Off, !_editor.TipsShown);
            _tips.Tip = KeptTip.For(_tips.Tip, _editor.TipsShown ? texts.Codex.ChromeTips : texts.Codex.ChromeTipsOn);
            if (_plain == _editor.TipsShown)
            {
                _plain = !_editor.TipsShown;
                Plain(Root, _plain);
            }

            _undone.Set("action", screen.UndoText);
            _redone.Set("action", screen.RedoText);

            _undo.IsVisible = screen.HasHistory;
            _redo.IsVisible = screen.HasHistory;
            _undoes.CanExecute = screen.Undo != null;
            _undo.Tip = KeptTip.For(_undo.Tip, screen.Undo != null ? texts.Codex.ChromeUndo : null, _undone);
            _redoes.CanExecute = screen.Redo != null;
            _redo.Tip = KeptTip.For(_redo.Tip, screen.Redo != null ? texts.Codex.ChromeRedo : null, _redone);

            _library.IsVisible = screen.Library != null;
            _library.Text = texts.SystemsLoad;
            _libraries.CanExecute = screen.Library != null;
            _library.Tip = Tip.For(_library.Tip, screen.Library != null ? texts.Codex.TermLibrary : null);

            var doing = screen.Running ? screen.Stops : screen.Runs;
            _run.IsVisible = doing != null;
            _run.Symbol = screen.Running ? Symbols.Stop : Symbols.Play;
            _runs.CanExecute = doing != null;
            _run.Tip = KeptTip.For(_run.Tip, doing == null ? null : screen.Running ? SequenceText.Stops(texts) : SequenceText.Starts(texts));

            // A bin held back keeps its place, so nothing beside it moves when a switch is flipped.
            _delete.IsVisible = screen.Deletes != null || screen.DeleteLocked != null;
            _deletes.CanExecute = screen.Deletes != null;
            _delete.Tip = KeptTip.For(_delete.Tip, screen.Deletes != null ? screen.DeleteAbout : screen.DeleteLocked);

            _switch.IsVisible = screen.HasSwitch;
            _switch.IsOn = screen.SwitchIsOn;

            // The heading stands as the subject's row on the home screen does: on red where something is wrong, on amber where it
            // carries a warning or is switched off.
            var broken = screen.Standing == NoticeLevel.Problem;
            var warned = !broken && (screen.Standing == NoticeLevel.Warning || screen.HasSwitch && !screen.SwitchIsOn);
            RemarkLook.Ground(_head, broken ? NoticeLevel.Problem : warned ? NoticeLevel.Warning : NoticeLevel.Info, Palette.Chrome);
            _switch.OnText = texts.SwitchOn;
            _switch.OffText = texts.SwitchOff;
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
                var action = new Button { Style = Styles.Plain, Text = Caps(entry.Text), Command = entry.Command };
                action.Set(LookStates.Chosen, entry.IsCurrent);
                _actions.Children.Add(action);
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
                RequestClose();
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
            Confine();
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
            Confine();

            if (Input.Focused == plate.Box)
                Input.Focus(null);
        }

        /// <summary>Says the name in hand changed, which decides whether it can be given.</summary>
        void Retyped()
        {
            _seen = -1;
            Confine();
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
