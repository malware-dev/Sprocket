using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Screens;
using Lumel.Text;
using Lumel.Viewing;
using Lumel.Windows;
using VRage.Input;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>A <see cref="SprocketEditor" /> on the screen: the chrome around the page, the page's own view, and whatever hangs over them.</summary>
    /// <remarks>
    /// It decides nothing about what the editor does: the editor states what is on screen in <see cref="EditorScreen" />
    /// and each page draws itself, and this decides how the chrome looks and where the pointer and the keyboard go.
    /// <para>
    /// <b>Nothing already on screen is allowed to move.</b> Controls that come and go keep their place whether or not they
    /// are showing, the heading turns into its own edit box rather than summoning one, and a question about a control
    /// hangs directly beneath it so that everything above the press stays exactly where the eye left it.
    /// </para>
    /// </remarks>
    public sealed partial class EditorView : IScreenView
    {
        /// <summary>How wide one of the game's own screens is, as a share of the safe area.</summary>
        /// <remarks>
        /// Space Engineers sizes a GUI screen in normalized units where 1.0 is the safe GUI rectangle exactly, and its
        /// terminal, which is the screen Sprocket is the closest thing to, is <c>new Vector2(1.0157f, 0.9172f)</c>. The
        /// width here is 1.0 rather than 1.0157: that overhang exists because the game's screen background has soft edges
        /// to hide, and Sprocket's has hard ones.
        /// </remarks>
        public const float ScreenWidth = 1f;

        /// <summary>How tall one of the game's own screens is, as a share of the safe area; see <see cref="ScreenWidth" />.</summary>
        public const float ScreenHeight = 0.9172f;

        /// <summary>Lifts a symbol in the heading so its middle is level with the middle of the heading's capitals.</summary>
        /// <remarks>
        /// A symbol is centered in its room like any image, and the heading's capitals sit above the middle of their line,
        /// where the face leaves room for accents. Measured in the game.
        /// </remarks>
        public static readonly Thickness LevelWithCapitals = new Thickness(0f, -2.5f, 0f, 2.5f);

        readonly SprocketEditor _editor;

        readonly VStack _root;
        readonly VStack _head;
        readonly Glyph _back;
        readonly HeadingRow _title;
        readonly Glyph _undo;
        readonly Glyph _redo;
        readonly Switch _switch;
        readonly Button _done;
        readonly HazardButton _admin;
        readonly Glyph _close;
        readonly Glyph _help;
        readonly Button _library;
        readonly Glyph _run;
        readonly Glyph _delete;
        readonly Label _subtitle;
        readonly Banner _banner = new Banner { Padding = new Thickness(0f, 0f, Sizes.Bar, 0f) };
        readonly Grid _page;
        readonly ScrollView _scroll;
        readonly VStack _body;
        readonly VStack _foot;
        readonly HStack _actions;

        readonly Relay _undoes;
        readonly Relay _redoes;
        readonly Relay _helps;
        readonly Relay _libraries;
        readonly Relay _flips;
        readonly Relay _dones;
        readonly Relay _runs;
        readonly Relay _deletes;

        readonly Arguments _undone = new Arguments();
        readonly Arguments _redone = new Arguments();

        readonly WindowLayer _windows = new WindowLayer();
        readonly MessageBox _dialog;
        readonly ConfirmPopup _confirm = new ConfirmPopup();
        readonly RichText _tip = new RichText { Style = Styles.Tip };
        readonly InputRoot _input;
        readonly List<ChoiceEntry> _actionsShown = new List<ChoiceEntry>();

        EditorPage _shownPage;
        int _seen = -1;
        int _asked;
        bool _hanging;
        bool _wantsClose;

        Element _raisedBy;
        Element _lit;
        Document _saying;
        Arguments _sayingArguments;

        int _renaming = -1;
        HeadingPart _renamed;
        HeadingPart _namedArrival;
        string _checkedText;
        bool _checkedTaken;

        /// <summary>The chrome for <paramref name="editor" />, which it hands its input root and window layer to.</summary>
        public EditorView(SprocketEditor editor)
        {
            _editor = editor;
            _title = new HeadingRow(Rename, new Relay(CommitRename), new Relay(EndRename), Retyped);

            _undoes = new Relay(() => Run(_editor.Screen.Undo));
            _redoes = new Relay(() => Run(_editor.Screen.Redo));
            _helps = new Relay(() => Run(_editor.Screen.Help));
            _libraries = new Relay(() => Run(_editor.Screen.Library));
            _flips = new Relay(() => Run(_editor.Screen.Flip));
            _dones = new Relay(() => Run(_editor.Screen.Primary));
            _runs = new Relay(() => Run(_editor.Screen.Running ? _editor.Screen.Stops : _editor.Screen.Runs));
            _deletes = new Relay(() => Run(_editor.Screen.Deletes));

            _scroll = new ScrollView { Style = Styles.ScrollView, Content = new VStack().As(out _body) };

            _root = new VStack
            {
                Background = Palette.Page,
                Children =
                {
                    new VStack
                    {
                        Background = Palette.Chrome,
                        Padding = new Thickness(16f, 8f),
                        Spacing = 4f,
                        Children =
                        {
                            new HStack
                            {
                                Spacing = 8f,
                                Children =
                                {
                                    new Glyph { Symbol = Symbols.Left, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f), Padding = LevelWithCapitals, Command = new Relay(Back) }.As(out _back),
                                    _title,
                                    // Quietest first, and Done or the close cross last: the row stands against the right edge, so a
                                    // control's place depends only on what stands to its right, and the last cannot be moved by anything.
                                    // Meant to stand out: it changes the whole world for everyone, and only an admin sees it.
                                    new HazardButton { Style = Styles.Button, Command = new Relay(OpenAdmin) }.As(out _admin),
                                    new Glyph { Symbol = "?", Style = Styles.Help, Placement = FixedSize.Size(32f, 32f), Command = _helps }.As(out _help),
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
                    }.As(out _head),
                    new Rule(1f, Palette.Line),
                    new Grid
                    {
                        Padding = new Thickness(16f),
                        Placement = Flex.Grow,
                        Columns = { GridLength.Flex(1f) },
                        Rows = { GridLength.Auto, GridLength.Flex(1f) },
                        Cells = { [0, 0] = _banner }
                    }.As(out _page),
                    // Only while the screen has actions, which only developer mode gives it, so everybody else has the room.
                    new VStack
                    {
                        IsVisible = false,
                        Children =
                        {
                            new Rule(1f, Palette.Line),
                            new HStack { Background = Palette.Chrome, Padding = new Thickness(16f, 8f), Spacing = 8f }.As(out _actions)
                        }
                    }.As(out _foot)
                }
            };

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

            _windows.Covers(_root);
            _input = new InputRoot(_root);
            _input.FocusChanged += Refocused;

            // What a page opens over the screen or gives the keyboard to, it reaches through its editor.
            _editor.Input = _input;
            _editor.Windows = _windows;
        }

        /// <summary>Where the pointer is, in UI units. Somewhere outside the screen means there is not one.</summary>
        /// <remarks>
        /// A property the host sets rather than an argument to <see cref="Draw(IGraphics)" />, because a target without a
        /// pointer - a text panel, a screenshot, a test - should not have to pass one.
        /// </remarks>
        public Vector2 Pointer { get; set; }

        /// <summary>Where the pointer and the keyboard enter the screen.</summary>
        public InputRoot Input => _input;

        /// <summary>Whether a question with answers is waiting, which owns Enter and Escape.</summary>
        public bool IsQuestioning => _windows.IsOpen(_dialog) || _windows.IsOpen(_confirm);

        /// <summary>Whether the caret is drawn this frame. The blink belongs to whoever owns a clock.</summary>
        public bool CaretVisible
        {
            set
            {
                var box = _input.Focused as TextBox;
                if (box != null)
                    box.CaretVisible = value;
            }
        }

        /// <summary>Why the name in hand cannot be given as it stands, or empty when it can or none is.</summary>
        /// <remarks>
        /// About the <b>edit in hand</b> and not about the model: walking away from a box somebody is being told to fill in
        /// would throw away their words without saying it had. Escape is the way out, and is never refused.
        /// </remarks>
        string Blocked
        {
            get
            {
                if (_renaming < 0)
                    return "";

                // Says what to do rather than what is wrong: there is only one cure and it fits in three words.
                var part = _renamed;
                var text = _title.Box(_renaming).Text;
                if (part.NameRequired && string.IsNullOrEmpty(text.Trim()))
                    return _editor.Texts.RenameEmpty;

                return Taken(part, text) ? _editor.Texts.RenameTaken : "";
            }
        }

        /// <summary>Whether somebody has just asked to leave, answered once and then forgotten.</summary>
        /// <remarks>Taken rather than read, so a host that forgets to ask does not close the screen on a press nobody remembers making.</remarks>
        public bool TakeClose()
        {
            var wanted = _wantsClose;
            _wantsClose = false;
            return wanted;
        }

        /// <summary>Brings the chrome into line with the screen, and puts the page's view on it where the page changed.</summary>
        /// <remarks>What the screen says is written onto the controls only when its revision has moved.</remarks>
        public void Update()
        {
            var screen = _editor.Screen;

            Ask(screen);

            // A screen that cannot be renamed while the box is open takes the box away with it.
            if (_renaming >= 0 && !screen.CanRename)
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

        /// <summary>Draws the editor at the size one of the game's screens is, centered where they are centered.</summary>
        public void Draw(IGraphics g)
        {
            var size = g.Size;
            var width = size.X * ScreenWidth;
            var height = size.Y * ScreenHeight;

            Draw(g, new RectangleF((size.X - width) * 0.5f, (size.Y - height) * 0.5f, width, height));
        }

        /// <summary>Draws the editor filling <paramref name="bounds" />, what hangs over it on top, and the note the pointer rests on last.</summary>
        public void Draw(IGraphics g, RectangleF bounds)
        {
            _root.Measure(g, new Vector2(bounds.Width, bounds.Height));
            _root.Arrange(bounds);

            Light();

            _root.Draw(g);
            _windows.Draw(g, bounds);
            DrawTip(g, bounds);
        }

        /// <summary>A press beginning at <paramref name="point" />, the <paramref name="presses" />th in quick succession, with shift or ctrl held.</summary>
        /// <remarks>
        /// A press that closes a list hanging open is that and nothing more, a press outside a dialog does nothing, and
        /// while the name in hand cannot be given only the box it is in answers.
        /// </remarks>
        public void PointerDown(Vector2 point, int presses, bool shift, bool ctrl)
        {
            if (_windows.Dismiss(point))
                return;

            if (Blocked.Length > 0 && !_title.Plate(_renaming).Reaches(point))
                return;

            var target = Under(point);
            if (target == null && _windows.Modal != null)
                return;

            // What a question this press raises hangs under.
            _raisedBy = target;
            _input.PointerDown(target, point, presses, shift, ctrl);
        }

        /// <summary>The pointer moving to <paramref name="point" /> while pressed.</summary>
        public void PointerMove(Vector2 point)
        {
            _input.PointerMove(point);
        }

        /// <summary>The press ending at <paramref name="point" />.</summary>
        public void PointerUp(Vector2 point)
        {
            _input.PointerUp(point);
        }

        /// <summary>Scrolls whatever is under <paramref name="point" /> by <paramref name="amount" />, if anything there can scroll.</summary>
        /// <remarks>
        /// While something hangs open, only what is open moves, for the same reason nothing behind it is pressed: a wheel that
        /// reached through took the rows out from under a pointer that was on the list hanging over them. A floating window
        /// hangs over nothing: the wheel scrolls what is in it where the pointer is on it, and the screen anywhere else.
        /// </remarks>
        public void ScrollBy(Vector2 point, float amount)
        {
            if (_renaming >= 0)
                return;

            if (_windows.Count > 0)
            {
                bool stopped;
                var over = _windows.WheelAt(point, out stopped);
                if (stopped || !_windows.Floats)
                {
                    if (over != null)
                        over.ScrollBy(amount);

                    return;
                }
            }

            if (_shownPage == null || !_shownPage.ScrollsItself)
            {
                _scroll.ScrollBy(amount);
                return;
            }

            var under = _root.WheelAt(point);
            if (under != null)
                under.ScrollBy(amount);
        }

        /// <summary>A key going down, answering whether anything took it.</summary>
        /// <remarks>A question with answers takes Enter as the answer its page named, and nothing else is offered the key while one is up.</remarks>
        public bool Key(MyKeys key, bool shift, bool ctrl)
        {
            if (IsQuestioning)
                return key == MyKeys.Enter && TakeDefault();

            return _input.Key(key, shift, ctrl);
        }

        /// <summary>A character typed, which goes to whatever holds the keyboard.</summary>
        public void Text(char c)
        {
            _input.Text(c);
        }

        /// <summary>Escape, answering whether the host should close the editor.</summary>
        /// <remarks>
        /// Escape is the key everybody already reads as "back", so it closes what is open over the page, a standing question
        /// or else the window on top, and otherwise goes back; only the first page with nothing open closes. Holding the
        /// keyboard is not being open: a box keeps what was typed in it, as pressing elsewhere does, and a name being typed
        /// into the heading is given, since it is only the name shown so it can be typed into.
        /// </remarks>
        public bool Escape()
        {
            if (IsQuestioning)
            {
                Dismiss();
                return false;
            }

            var top = _windows.Top;
            if (top != null)
            {
                _windows.Close(top);
                return false;
            }

            if (_renaming >= 0)
            {
                CommitRename();
                EndRename();
            }

            _input.Focus(null);

            if (!_editor.Screen.CanGoBack)
                return true;

            Run(_editor.Screen.Back);
            return false;
        }

        /// <summary>Takes the answer the page said Enter takes, and says whether there was one.</summary>
        /// <remarks>Nothing where the page named none: a key that guesses an answer is worse than a key that does nothing.</remarks>
        public bool TakeDefault()
        {
            if (_windows.IsOpen(_dialog))
                return _dialog.TakeAccept();

            var taken = _editor.Screen.Default;
            if (!_windows.IsOpen(_confirm) || taken == null)
                return false;

            Run(taken.Command);
            return true;
        }

        /// <summary>Puts the standing question away without answering it.</summary>
        /// <remarks>
        /// A third outcome rather than one of the answers: a page that asks has not moved yet, and its answers are what
        /// move it, so dismissing already means "I did not mean to leave".
        /// </remarks>
        public void Dismiss()
        {
            if (_windows.IsOpen(_dialog))
            {
                _dialog.TakeCancel();
                return;
            }

            _windows.Close(_confirm);
            _editor.Unask();
        }

        /// <summary>What a press or a hover at <paramref name="point" /> reaches, or null for nothing at all.</summary>
        /// <remarks>
        /// The windows first, top down, since each one owns what it covers and a modal one owns everything; then, while the
        /// name in hand cannot be given, only the box it is in; then the screen.
        /// </remarks>
        public Element Under(Vector2 point)
        {
            bool stopped;
            var windowed = _windows.Under(point, out stopped);
            if (stopped)
                return windowed;

            if (Blocked.Length > 0)
                return _title.Plate(_renaming).Under(point);

            return _root.Under(point);
        }

        /// <summary>What the pointer at <paramref name="point" /> rests on that has something to say, or null.</summary>
        Element SayingAt(Vector2 point)
        {
            bool stopped;
            var windowed = _windows.SayingAt(point, out stopped);
            if (stopped)
                return windowed;

            if (Blocked.Length > 0)
                return _title.Plate(_renaming).SayingAt(point);

            return _root.SayingAt(point);
        }

        /// <summary>Lights the one thing the pointer is resting on, puts out whatever was lit before, and takes up what it says.</summary>
        /// <remarks>Found the same way a press finds what it acts on, so the thing that would be pressed is the thing that lights.</remarks>
        void Light()
        {
            var wanted = Under(Pointer);

            var speaker = SayingAt(Pointer);
            var tip = speaker?.TipAt(speaker.FromScreen(Pointer));
            _saying = tip?.Document;
            _sayingArguments = tip?.Arguments;

            if (wanted == _lit)
                return;

            if (_lit != null)
                _lit.IsHot = false;

            _lit = wanted;

            if (_lit != null)
                _lit.IsHot = true;
        }

        /// <summary>Puts a note beside the pointer, if what it is resting on had anything to say.</summary>
        /// <remarks>
        /// Drawn last, over everything including a list somebody opened, whose rows it may well be describing. Below and to
        /// the right, where the arrow is not covering what was just read, and flipped only where there is no room.
        /// </remarks>
        void DrawTip(IGraphics g, RectangleF bounds)
        {
            if (_saying == null)
                return;

            _tip.Document = _saying;
            _tip.Arguments = _sayingArguments;
            _tip.Measure(g, new Vector2(Sizes.Popup, bounds.Height));

            var size = _tip.DesiredSize;
            var x = Pointer.X + Spacing.Item;
            var y = Pointer.Y + Spacing.Region;

            if (x + size.X > bounds.X + bounds.Width)
                x = Pointer.X - size.X - Spacing.Item;

            if (y + size.Y > bounds.Y + bounds.Height)
                y = Pointer.Y - size.Y - Spacing.Row;

            if (x < bounds.X)
                x = bounds.X;

            if (y < bounds.Y)
                y = bounds.Y;

            _tip.Arrange(new RectangleF(x, y, size.X, size.Y));
            _tip.Draw(g);
        }

        /// <summary>Opens the question the screen raised, or puts away the one it no longer asks.</summary>
        /// <remarks>
        /// Opened again where a different question has been raised, and not only where none was up: one question answered
        /// by raising another would otherwise keep the first one's words. A question hanging under a control that a press
        /// elsewhere has put away is dismissed, which is what that press meant.
        /// </remarks>
        void Ask(EditorScreen screen)
        {
            if (!screen.HasQuestion)
            {
                _windows.Close(_dialog);
                _windows.Close(_confirm);
                _hanging = false;
                return;
            }

            if (_asked == screen.Asked)
            {
                if (_hanging && !_windows.IsOpen(_confirm))
                {
                    _hanging = false;
                    _editor.Unask();
                }

                return;
            }

            _asked = screen.Asked;
            _windows.Close(_dialog);
            _windows.Close(_confirm);

            _hanging = screen.QuestionIsHere && _raisedBy != null;
            if (_hanging)
            {
                _confirm.Ask(screen.Question, screen.QuestionArguments, screen.Answers, screen.Default, _raisedBy, _windows);
                return;
            }

            // The first answer as Yes and the second as No; Escape puts it away without an answer, which has no button of its own.
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

            _windows.Open(_dialog);
        }

        /// <summary>Puts the page on top on the screen, in the screen's scroller unless it scrolls itself.</summary>
        /// <remarks>
        /// Whether the heading has a line under it is settled here, once for the life of the page, because changing it
        /// later would move the page under whoever is reading it.
        /// </remarks>
        void Show(EditorScreen screen)
        {
            EndRename();
            _windows.Close(_confirm);

            _shownPage = _editor.Current;
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
        }

        /// <summary>Writes what the screen says onto the chrome: the heading, the words and states beside it, what each control does, and the way on.</summary>
        void Dress(EditorScreen screen)
        {
            _title.Update(screen.Heading, _renaming);
            NameArrival(screen);

            _subtitle.Text = screen.Subtitle;

            // While the name in hand is one that cannot be given, the box says so, in its look and in its note.
            if (_renaming >= 0)
            {
                var wrong = Blocked;
                _title.Refuse(_renaming, wrong.Length > 0 ? wrong : null);
            }

            var texts = _editor.Texts;

            // The first screen has nowhere to go back to, so it closes, with the cross at the far right where a close button stands.
            var closes = !screen.CanGoBack;
            _back.IsVisible = screen.CanGoBack;
            _back.Tip = Tip.For(_back.Tip, _back.IsVisible ? texts.ChromeBack : null);
            _close.IsVisible = closes;
            _close.Tip = Tip.For(_close.Tip, !closes ? null : texts.ChromeClose);

            // On the home screen alone, which is where the whole world's automation starts from. Asked as the screen is
            // dressed, which it is as the editor opens: creative tools are switched on and off with the editor put away.
            _admin.IsVisible = !screen.CanGoBack && _editor.MayAdminister;
            _admin.Text = texts.AdminButton;
            _help.IsVisible = screen.Help != null;
            _helps.CanExecute = screen.Help != null;
            _help.Tip = Tip.For(_help.Tip, screen.Help == null ? null : _editor.CodexOpen ? texts.CodexClose : texts.CodexHelp);

            _undone.Set("action", screen.UndoText);
            _redone.Set("action", screen.RedoText);

            _undo.IsVisible = screen.HasHistory;
            _redo.IsVisible = screen.HasHistory;
            _undoes.CanExecute = screen.Undo != null;
            _undo.Tip = Tip.For(_undo.Tip, screen.Undo != null ? texts.ChromeUndo : null, _undone);
            _redoes.CanExecute = screen.Redo != null;
            _redo.Tip = Tip.For(_redo.Tip, screen.Redo != null ? texts.ChromeRedo : null, _redone);

            _library.IsVisible = screen.Library != null;
            _library.Text = texts.SystemsLoad;
            _libraries.CanExecute = screen.Library != null;
            _library.Tip = Tip.For(_library.Tip, screen.Library != null ? texts.SystemsLoadAbout : null);

            // One mark, two acts, and never both at once: a subject that is going is one to stop and one that is not is
            // one to start.
            var doing = screen.Running ? screen.Stops : screen.Runs;
            _run.IsVisible = doing != null;
            _run.Symbol = screen.Running ? Symbols.Stop : Symbols.Play;
            _runs.CanExecute = doing != null;
            _run.Tip = Tip.For(_run.Tip, doing == null ? null : screen.Running ? SequenceText.Stops(texts) : SequenceText.Starts(texts));

            // A bin held back keeps its place, so nothing beside it moves when a switch is flipped.
            _delete.IsVisible = screen.Deletes != null || screen.DeleteLocked != null;
            _deletes.CanExecute = screen.Deletes != null;
            _delete.Tip = Tip.For(_delete.Tip, screen.Deletes != null ? screen.DeleteAbout : screen.DeleteLocked);

            // The words say the act and its reach, not what it is on - the heading directly above says that.
            _switch.IsVisible = screen.HasSwitch;
            _switch.IsOn = screen.SwitchIsOn;

            // Switched off is a warning, as it is on a row, and the bar holding the switch says it across the whole page.
            _head.Background = screen.HasSwitch && !screen.SwitchIsOn ? Palette.NoticeWash : Palette.Chrome;
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
        bool Taken(HeadingPart part, string text)
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
                _wantsClose = true;
        }

        /// <summary>Begins renaming the part of the heading at <paramref name="index" />.</summary>
        void Rename(int index)
        {
            var parts = _editor.Screen.Heading;
            if (index >= parts.Count || !parts[index].CanRename)
                return;

            _renaming = index;
            _renamed = parts[index];
            _checkedText = null;
            _title.Begin(index, parts[index].Name);
            _input.Focus(_title.Box(index));
            _seen = -1;
        }

        /// <summary>Begins renaming the part of the heading that arrives to be named, once for that part.</summary>
        void NameArrival(EditorScreen screen)
        {
            if (_renaming >= 0)
                return;

            for (var i = 0; i < screen.Heading.Count && i < _title.Count; i++)
            {
                var part = screen.Heading[i];
                if (!part.NamedOnArrival || !part.CanRename || part == _namedArrival)
                    continue;

                _namedArrival = part;
                Rename(i);
                return;
            }
        }

        /// <summary>Gives the name in hand, where it can be given, and puts the box away.</summary>
        /// <remarks>
        /// A name that cannot be given leaves the box open rather than closing it and doing nothing: somebody who has just
        /// emptied a name they are required to give is mid-thought.
        /// </remarks>
        void CommitRename()
        {
            if (_renaming < 0 || Blocked.Length > 0)
                return;

            // The part the name was begun on, which a press that changed the screen has taken out of the heading.
            var part = _renamed;
            var name = _title.Box(_renaming).Text;

            EndRename();

            var renames = part.Renames;
            if (renames != null)
                renames(name);
        }

        /// <summary>Puts the box away, giving nothing.</summary>
        void EndRename()
        {
            if (_renaming < 0)
                return;

            var renamed = _renaming;
            _renaming = -1;
            _renamed = null;
            _title.End(renamed);
            _seen = -1;

            if (_input.Focused == _title.Box(renamed))
                _input.Focus(null);
        }

        /// <summary>Says the name in hand changed, which decides whether it can be given.</summary>
        void Retyped()
        {
            _seen = -1;
        }

        /// <summary>Takes the keyboard leaving a name being typed as the press away it is, which gives the name.</summary>
        /// <remarks>A name that cannot be given keeps the keyboard, because the box is already saying why.</remarks>
        void Refocused()
        {
            if (_renaming < 0)
                return;

            var box = _title.Box(_renaming);
            if (_input.Focused == box)
                return;

            CommitRename();

            if (_renaming >= 0)
                _input.Focus(box);
        }

        /// <summary>Opens the world's settings over the screen, for an admin.</summary>
        void OpenAdmin()
        {
            if (_editor.MayAdminister)
                _windows.Open(new Admin.AdminDialog(_editor));
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
        /// <remarks>
        /// Without a body in anything packed for the game, where the call compiles to nothing. <c>EditorView.debug.cs</c>
        /// counts the writes, so a test can hold an idle screen to none.
        /// </remarks>
        partial void Wrote();
    }
}
