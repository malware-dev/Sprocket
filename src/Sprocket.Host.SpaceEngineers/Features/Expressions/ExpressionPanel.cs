using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    /// <summary>
    /// Where an expression is built by dragging: calculations and kinds of value from the tray into empty places and onto
    /// values, parts moved between places and into the bin, with what it comes to now on the installation being edited, how
    /// it runs once its fixed parts are worked out, and what is wrong with it.
    /// </summary>
    public partial class ExpressionPanel : VStack
    {
        readonly SprocketEditor _editor;
        readonly AutomationSystem _system;
        readonly string _wanted;
        readonly ExpressionDraft _draft;

        // The places on the board and in the tray a drag can end on, in the order they were made: a box before what is in it.
        readonly List<Element> _targets = new List<Element>();
        readonly List<DraftPiece> _targetPieces = new List<DraftPiece>();

        Dragged _carried;
        Element _grabbed;
        Vector2 _from;
        Vector2 _at;
        Element _aimed;
        Literal _value;
        bool _reads;

        /// <summary>
        /// A panel building a value of <paramref name="system" /> of the type the language names <paramref name="wanted" />,
        /// or of any type where it is null, starting from <paramref name="start" />, or from one empty place where it is null.
        /// </summary>
        public ExpressionPanel(SprocketEditor editor, AutomationSystem system, string wanted, Literal? start)
        {
            _editor = editor;
            _system = system;
            _wanted = wanted;
            _draft = new ExpressionDraft(Start(start));
            CreateView();
            Offer();
            Wire();
            Show();
            Lay();
        }

        /// <summary>Whether the expression is whole and suits the value being built.</summary>
        public bool Reads => _reads;

        /// <summary>The value built, with its fixed parts worked out, as it is stored; only while it <see cref="Reads" />.</summary>
        public Literal Value => _value;

        /// <summary>The expression as it is being built.</summary>
        public ExpressionDraft Draft => _draft;

        /// <summary>What the expression comes to now, or why it has no value.</summary>
        public string Now => _now.Text;

        /// <summary>What is wrong with the expression, or empty.</summary>
        public string Why => _why.IsVisible ? _why.Text : "";

        /// <summary>Whether something is being dragged.</summary>
        public bool IsDragging => _carried != null;

        /// <summary>The element on the board showing <paramref name="piece" />, or null where it is not shown.</summary>
        public Element ShownFor(DraftPiece piece)
        {
            var at = _targetPieces.IndexOf(piece);
            return at < 0 ? null : _targets[at];
        }

        /// <summary>The box or switch the fixed value <paramref name="piece" /> is set by, or null where it shows none.</summary>
        public TypedValue TypedFor(DraftValue piece)
        {
            var shown = ShownFor(piece) as HStack;
            return shown == null ? null : shown.Children[shown.Children.Count - 1] as TypedValue;
        }

        /// <summary>The shelves pieces are dragged from, and the bin.</summary>
        public ExpressionTray Tray => _tray;

        /// <summary>Whether the place showing <paramref name="element" /> is lit, so what is dragged can be dropped there.</summary>
        public static bool IsLit(Element element) => element.Is(LookStates.Lit);

        /// <summary>Brings the words to the language shown.</summary>
        public void Show()
        {
            var texts = _editor.Texts;
            _optimize.Text = texts.ExpressionOptimize;
            _optimize.Tip = CodexTip.For(null, texts.Codex.ExpressionOptimizeAbout);
            _tray.Show(texts);
            Reckon();
        }

        /// <summary>Says again what the expression comes to now on the installation being edited, or what is wrong with it.</summary>
        /// <remarks>Asked as the expression changes, and once a second for what the construct comes to; never on a tick.</remarks>
        public void Reckon()
        {
            var texts = _editor.Texts;
            var built = _draft.Built();
            var why = "";
            if (built != null)
            {
                var value = Stored(built);
                why = Misfit(value);
                _value = why.Length == 0 ? value : default(Literal);
            }

            _reads = built != null && why.Length == 0;
            _why.Text = why;
            _why.IsVisible = why.Length > 0;
            _now.Text = Template.Fill(texts.ExpressionNow, "value", _reads ? Worked(_value) : texts.ExpressionNoValue);
            _editor.Invalidate();
        }

        /// <summary>Works out what can be worked out of the expression, as one change, or says there is nothing to.</summary>
        public void Optimize()
        {
            if (!_draft.Optimize(Types()))
                _optimized.Text = _editor.Texts.ExpressionNothingToOptimize;
        }

        /// <summary>What was said after Optimize was pressed, or empty.</summary>
        public string Optimized => _optimized.Text;

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            base.DrawContent(g);
            if (_carried == null || _grabbed == null)
                return;

            // The piece being dragged is drawn again over everything, where the pointer has taken it.
            g.PushTransform(RenderTransform.Moved(_at - _from), Vector2.Zero);
            _grabbed.Draw(g);
            g.PopTransform();
        }

        static Expression Start(Literal? given)
        {
            if (given == null)
                return null;

            var start = given.Value;
            if (start.IsExpression)
                return start.Tree;

            if (start.IsSetting || (start.Said.Length > 0 && start.Kind != ValueKind.Slots))
                return new Operand(start);

            return null;
        }

        void Wire()
        {
            _draft.Changed += Lay;
            _draft.Retyped += Reckon;
            _draft.Retyped += () => _optimized.Text = "";
            _optimize.Command = new RelayCommand(Optimize);
        }

        // Shows in the tray only what can go somewhere in a value of the kind being built.
        void Offer()
        {
            var types = Types();
            _tray.Offer(_system, ExpressionOffers.Usable(DraftKinds.Wanted(_wanted), types), types);
        }

        // Shows the draft on the board again, as it now is, and says again what it comes to.
        void Lay()
        {
            _targets.Clear();
            _targetPieces.Clear();
            _targets.Add(_tray.Bin);
            _targetPieces.Add(null);
            _board.Children.Clear();
            _board.Children.Add(Shown(_draft.Root));
            _optimized.Text = "";
            Reckon();
        }

        Element Shown(DraftPiece piece)
        {
            var texts = _editor.Texts;
            if (piece is DraftHole)
            {
                var hole = new HStack
                {
                    Style = Styles.ExpressionHole,
                    Tip = CodexTip.For(null, texts.Codex.ExpressionEmptyAbout),
                    Children = { new Label { Style = Styles.Detail, Text = texts.ExpressionEmpty } }
                };
                Target(hole, piece);
                return hole;
            }

            var shown = new HStack { Style = piece is DraftJoin ? Styles.ExpressionBox : Styles.ExpressionChip };
            var grip = new DragHandle { Placement = FixedSize.Size(16f, 24f), CanDrag = true, Align = Align.Center };
            shown.Children.Add(grip);
            Grab(grip, shown, new Dragged { Piece = piece });
            Target(shown, piece);

            var value = piece as DraftValue;
            if (value != null && TypedValue.Takes(value.Atom))
            {
                var typed = new TypedValue(texts, ((Operand)value.Atom).Value);
                typed.Typed += atom => _draft.Retype(value, atom);
                shown.Children.Add(new Swatch { Color = Palette.Muted, Placement = FixedSize.Width(4f), Align = Align.FillHeight });
                shown.Children.Add(typed);
                return shown;
            }

            if (value != null)
            {
                string kind;
                var name = ExpressionNames.Of(_editor, _system, value.Atom, out kind);
                shown.Children.Add(new Swatch { Color = Palette.Accent, Placement = FixedSize.Width(4f), Align = Align.FillHeight });
                shown.Children.Add(new Label { Style = Styles.Detail, Text = kind, Align = Align.Center });
                shown.Children.Add(new Label { Base = { Color = Palette.Ink }, Text = name, Align = Align.Center });
                return shown;
            }

            var join = (DraftJoin)piece;
            var symbol = ExpressionWords.Symbol(texts, join.Kind);
            switch (join.Kind)
            {
                case JoinKind.Add:
                case JoinKind.Subtract:
                case JoinKind.Multiply:
                case JoinKind.Divide:
                    shown.Children.Add(Shown(join.Parts[0]));
                    shown.Children.Add(Word(symbol));
                    shown.Children.Add(Shown(join.Parts[1]));
                    break;
                case JoinKind.Negate:
                    shown.Children.Add(Word("-"));
                    shown.Children.Add(Shown(join.Parts[0]));
                    break;
                default:
                    shown.Children.Add(Word(symbol + "("));
                    for (var i = 0; i < join.Parts.Count; i++)
                    {
                        if (i > 0)
                            shown.Children.Add(Word(","));

                        shown.Children.Add(Shown(join.Parts[i]));
                    }

                    shown.Children.Add(Word(")"));
                    break;
            }

            return shown;
        }

        static Label Word(string words) => new Label { Base = { Color = Palette.Ink }, Text = words, Align = Align.Center };

        void Target(Element element, DraftPiece piece)
        {
            _targets.Add(element);
            _targetPieces.Add(piece);
        }

        void Grab(DragHandle grip, Element shown, Dragged carried)
        {
            grip.DragStarted += point => Begin(carried, shown, point);
            grip.DragDelta += Over;
            grip.DragCompleted += Drop;
            grip.DragCanceled += End;
        }

        // Takes hold of what is dragged and lights every place it could go.
        void Begin(Dragged carried, Element shown, Vector2 point)
        {
            _carried = carried;
            _grabbed = shown;
            _from = point;
            _at = point;
            var any = false;
            for (var i = 0; i < _targets.Count; i++)
            {
                var fits = Fits(carried, _targetPieces[i], i == 0);
                _targets[i].Set(LookStates.Lit, fits);
                any |= fits;
            }

            _hint.Text = any ? _editor.Texts.ExpressionDragging : _editor.Texts.ExpressionNowhere;
            _editor.Invalidate();
        }

        // Follows the pointer, marking the lit place it is over.
        void Over(Vector2 point)
        {
            if (_carried == null)
                return;

            _at = point;
            var aimed = Under(point);
            if (aimed != _aimed)
            {
                _aimed?.Set(LookStates.Aimed, false);
                aimed?.Set(LookStates.Aimed, true);
                _aimed = aimed;
            }

            _editor.Invalidate();
        }

        // Lets go: what is carried goes to the lit place under the pointer, or back where it was.
        void Drop(Vector2 point)
        {
            if (_carried == null)
                return;

            var carried = _carried;
            var at = _targets.IndexOf(Under(point));
            End();
            if (at < 0)
                return;

            var piece = _targetPieces[at];
            if (at == 0)
            {
                _draft.Remove(carried.Piece);
                return;
            }

            var hole = piece as DraftHole;
            if (carried.Piece != null)
                _draft.Move(carried.Piece, hole);
            else if (carried.Kind != null && hole != null)
                _draft.Fill(hole, new DraftJoin(carried.Kind.Value));
            else if (carried.Kind != null)
                _draft.Wrap(piece, carried.Kind.Value);
            else if (carried.Source == ValueSource.Typed)
                _draft.Fill(hole, Fixed(carried.Typed));
            else
                Choose(carried.Source, hole);
        }

        void End()
        {
            _carried = null;
            _grabbed = null;
            _aimed = null;
            foreach (var target in _targets)
            {
                target.Set(LookStates.Lit, false);
                target.Set(LookStates.Aimed, false);
            }

            _hint.Text = "";
            _editor.Invalidate();
        }

        // The innermost lit place under the pointer, or null where there is none; a place on the board only where the board
        // shows it, not scrolled out of view.
        Element Under(Vector2 point)
        {
            var onBoard = Inside(_scroll, point);
            for (var i = _targets.Count - 1; i >= 0; i--)
            {
                var target = _targets[i];
                if (target.Is(LookStates.Lit) && (i == 0 || onBoard) && Inside(target, point))
                    return target;
            }

            return null;
        }

        static bool Inside(Element element, Vector2 point)
        {
            var local = element.FromScreen(point);
            var bounds = element.Bounds;
            return local.X >= bounds.X && local.X < bounds.X + bounds.Width && local.Y >= bounds.Y && local.Y < bounds.Y + bounds.Height;
        }

        // Whether what is carried can go on the place showing piece, or in the bin where bin says it is the bin.
        bool Fits(Dragged carried, DraftPiece piece, bool bin)
        {
            if (bin)
                return carried.Piece != null;

            var types = Types();
            var wanted = DraftKinds.Wanted(_wanted);
            var hole = piece as DraftHole;
            if (carried.Piece != null)
            {
                if (hole == null)
                    return false;

                var moved = _draft.TriedMove(carried.Piece, hole);
                return moved != null && DraftKinds.Suits(moved, wanted, types);
            }

            if (carried.Kind != null)
            {
                var tried = hole != null ? _draft.TriedFill(hole, new DraftJoin(carried.Kind.Value)) : _draft.TriedWrap(piece, carried.Kind.Value);
                return DraftKinds.Suits(tried, wanted, types);
            }

            if (hole == null)
                return false;

            switch (carried.Source)
            {
                case ValueSource.Setting: return Fitting(ExpressionOffers.Settings(_system, KindClass.Any, types), hole, Setting).Count > 0;
                case ValueSource.State: return Fitting(ExpressionOffers.States(_system, KindClass.Any, types), hole, State).Count > 0;
                case ValueSource.Typed: return DraftKinds.Suits(_draft.TriedFill(hole, Fixed(carried.Typed)), wanted, types);
                default: return true;
            }
        }

        // Of the values named, those that leave the whole able to come to the kind being built when they are put in hole.
        List<string> Fitting(List<string> named, DraftHole hole, Func<string, DraftValue> value)
        {
            var types = Types();
            var wanted = DraftKinds.Wanted(_wanted);
            var fitting = new List<string>();
            foreach (var name in named)
            {
                if (DraftKinds.Suits(_draft.TriedFill(hole, value(name)), wanted, types))
                    fitting.Add(name);
            }

            return fitting;
        }

        static DraftValue Setting(string name) => new DraftValue(new Operand(Literal.Setting(name)));

        static DraftValue State(string name) => new DraftValue(new StateOperand(name));

        static DraftValue Fixed(KindClass kind) => new DraftValue(new Operand(TypedValue.Neutral(kind)));

        // Asks which value of source goes in hole, of those that fit there, and puts it there.
        void Choose(ValueSource source, DraftHole hole)
        {
            if (source == ValueSource.Reading)
            {
                PickReading(reading => _draft.Fill(hole, new DraftValue(reading)));
                return;
            }

            var setting = source == ValueSource.Setting;
            var types = Types();
            var named = setting ? ExpressionOffers.Settings(_system, KindClass.Any, types) : ExpressionOffers.States(_system, KindClass.Any, types);
            Func<string, DraftValue> value = setting ? (Func<string, DraftValue>)Setting : State;
            _editor.AskHere("");
            foreach (var name in Fitting(named, hole, value))
            {
                var chosen = name;
                _editor.Answer(chosen, () => _draft.Fill(hole, value(chosen)));
            }
        }

        // Opens the selector on what blocks to read and which of their values, and gives done the reading picked.
        void PickReading(Action<BlockReading> done)
        {
            var origin = _editor.Depth;
            var source = new SourceSwitch(_editor, (choice, slots) =>
            {
                var page = new PickPage(_editor, _editor.Texts.PickHeadingReading, false, new List<long>(), default(MemberKey), ReachKind.Value,
                    answer => _editor.Picks.Name(_system, answer, false, null, named =>
                    {
                        _editor.UnwindTo(origin);
                        done(Reading(answer, named));
                    }),
                    true, forReadings: true);
                page.Source = choice;
                page.Slotted = _system;
                page.ListsSlots = slots;
                return page;
            }, null);
            _editor.Push(source.Open(source.Usual));
        }

        // The reading answer picked, of the blocks named.
        static BlockReading Reading(PickAnswer answer, List<string> named)
        {
            var combine = Combination.None;
            if (answer.Targets.Count > 1)
            {
                switch (answer.Quantifier)
                {
                    case Quantifier.Average: combine = Combination.Average; break;
                    case Quantifier.Min: combine = Combination.Min; break;
                    case Quantifier.Max: combine = Combination.Max; break;
                    default: combine = Combination.Sum; break;
                }
            }

            return new BlockReading(answer.Key, combine, answer.Arguments ?? new List<Literal>(), named);
        }

        // The value built is stored as: a fixed value or setting alone as itself, anything else as the expression.
        static Literal Stored(Expression built)
        {
            var operand = built as Operand;
            return operand != null ? operand.Value : Literal.Worked(built);
        }

        // Why value does not suit the value being built, or empty where it does.
        string Misfit(Literal value)
        {
            if (_wanted == null)
                return "";

            ValueKind? kind;
            if (value.IsExpression)
            {
                if (!value.Tree.TypeOf(Types(), out kind) || kind == null)
                    return "";
            }
            else
            {
                kind = value.IsSetting ? Types().Setting(value.Text) : value.Kind;
                if (kind == null)
                    return "";
            }

            var type = ExpressionWords.TypeWord(kind.Value);
            if (type == null || SettingTypes.Fits(_wanted, type))
                return "";

            var texts = _editor.Texts;
            return Template.Fill(Template.Fill(texts.ExpressionWrongKind, "kind", ExpressionWords.KindSaid(texts, type)), "wanted", ExpressionWords.KindSaid(texts, _wanted));
        }

        // What the settings, states and readings of the system hold, on the installation being edited.
        ExpressionTypes Types()
        {
            return new ExpressionTypes(_system, _editor.StandIn(_system), _editor.Runner.Bindings);
        }

        // What value comes to now on the installation being edited, as a script writes a value.
        string Worked(Literal value)
        {
            if (!value.IsExpression)
                return value.IsSetting ? Worked(Literal.Worked(new Operand(value))) : value.Said;

            var scope = new WatchScope(_system, _editor.StandIn(_system), _editor.Runner, () => { });
            var trees = new List<Expression> { value.Tree };
            scope.Prepare(trees, _editor.Runner.Bindings, false);
            Literal worked;
            var failure = scope.Evaluate(value.Tree, out worked);
            scope.Forget();
            return failure == ExpressionFailure.None ? worked.Said : _editor.Texts.ExpressionNoValue;
        }
    }
}
