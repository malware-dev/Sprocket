using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Tips;
using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;

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

        // Whether the value is a condition tested, and whether it is a flag set rather than tested, which takes one value alone.
        readonly bool _tests;
        readonly bool _single;

        // The places on the board and in the tray a drag can end on, in the order they were made: a box before what is in it.
        readonly List<Element> _targets = new List<Element>();
        readonly List<DraftPiece> _targetPieces = new List<DraftPiece>();

        Dragged _carried;
        Literal _value;
        bool _reads;

        /// <summary>
        /// A panel building a value of <paramref name="system" /> of the type the language names <paramref name="wanted" />,
        /// or of any type where it is null, starting from <paramref name="start" />, or from one empty place where it is null;
        /// a condition an if or a when tests where <paramref name="tests" />, and otherwise, where it is on or off, one value
        /// alone, as on and off are set and never worked out.
        /// </summary>
        public ExpressionPanel(SprocketEditor editor, AutomationSystem system, string wanted, Literal? start, bool tests = false)
        {
            _editor = editor;
            _system = system;
            _wanted = wanted;
            _tests = tests;
            _single = wanted == "flag" && !tests;
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
        public static bool IsLit(Element element) => element.Is(DragDrop.Accepting);

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
            var why = texts.ExpressionUnfinished;
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
            var wanted = DraftKinds.Wanted(_wanted);
            _tray.Offer(_system, _single ? wanted : ExpressionOffers.Usable(wanted, types), types, _tests, _single);
        }

        // Shows the draft on the board again, as it now is, and says again what it comes to.
        void Lay()
        {
            _targets.Clear();
            _targetPieces.Clear();
            Target(_tray.Bin, null, true);
            _board.Children.Clear();
            _board.Children.Add(Shown(_draft.Root));
            Mark(_draft.Root, Types());
            _optimized.Text = "";
            Reckon();
        }

        // Marks what Optimize would change in piece and in each part of it: a box that always comes to one fixed value, or a part
        // that changes nothing beside the one that takes the box's place alone.
        void Mark(DraftPiece piece, ITypeScope types)
        {
            var join = piece as DraftJoin;
            if (join == null)
                return;

            var texts = _editor.Texts;
            int stays;
            var becomes = ExpressionDraft.Folds(join, types, out stays);
            var value = becomes as DraftValue;
            if (value != null && Folding.IsFixed(value.Atom))
            {
                Warn(join, Template.Fill(texts.Codex.ExpressionAlwaysAbout, "value", ((Operand)value.Atom).Value.Said));
                return;
            }

            for (var i = 0; i < join.Parts.Count; i++)
            {
                if (becomes != null && stays >= 0 && i != stays)
                    Warn(join.Parts[i], texts.Codex.ExpressionNoDifferenceAbout);
                else
                    Mark(join.Parts[i], types);
            }
        }

        // Shows piece warned, with said as its tip, which stays while tips are off: it says what the expression shows nowhere else.
        void Warn(DraftPiece piece, string said)
        {
            var shown = ShownFor(piece);
            shown.Set(LookStates.Warned, true);
            shown.Tip = KeptTip.For(null, said);
        }

        Element Shown(DraftPiece piece)
        {
            var texts = _editor.Texts;
            if (piece is DraftHole)
            {
                var hole = new HStack
                {
                    Style = Styles.ExpressionHole,
                    Placement = FixedSize.Height(NodeHigh),
                    Tip = CodexTip.For(null, texts.Codex.ExpressionEmptyAbout),
                    Children = { new Label { Style = Styles.Detail, Text = texts.ExpressionEmpty } }
                };
                Target(hole, piece);
                return hole;
            }

            var shown = piece is DraftJoin
                ? new HStack { Style = Styles.ExpressionBox }
                : new HStack { Style = Styles.ExpressionChip, Placement = FixedSize.Height(NodeHigh) };
            var grip = new NodeGrip { Placement = FixedSize.Size(16f, 24f), CanDrag = true, Align = Align.Center };
            // While something is carried only the places it can go are lit, so no grip shows what it would take.
            grip.Pointed += pointed => Grabbable(piece, pointed && _carried == null);
            shown.Children.Add(grip);
            Grab(grip, shown, new Dragged { Piece = piece });
            Target(shown, piece);

            var value = piece as DraftValue;
            if (value != null && TypedValue.Takes(value.Atom))
            {
                var typed = new TypedValue(texts, ((Operand)value.Atom).Value);
                typed.Typed += atom => _draft.Retype(value, atom);
                shown.Children.Add(new Swatch { Style = Styles.ExpressionEdge, Color = Palette.Muted, Placement = FixedSize.Width(4f), Align = Align.FillHeight });
                shown.Children.Add(typed);
                return shown;
            }

            if (value != null)
            {
                string kind;
                var name = ExpressionNames.Of(_editor, _system, value.Atom, out kind);
                shown.Children.Add(new Swatch { Style = Styles.ExpressionEdge, Color = Palette.Accent, Placement = FixedSize.Width(4f), Align = Align.FillHeight });
                shown.Children.Add(Combining(value, shown, kind));
                shown.Children.Add(new Button
                {
                    Style = Styles.ExpressionName,
                    Text = name,
                    Align = Align.Center,
                    Tip = CodexTip.For(null, texts.Codex.ExpressionChangeAbout),
                    Command = new RelayCommand(() => Choose(SourceOf(value.Atom), value, shown))
                });
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
                case JoinKind.Is:
                case JoinKind.IsNot:
                case JoinKind.Above:
                case JoinKind.AtLeast:
                case JoinKind.Below:
                case JoinKind.AtMost:
                case JoinKind.And:
                case JoinKind.Or:
                {
                    // Brackets only where the order the language reads it in needs them, as a formula is written.
                    var bracketed = JoinKinds.Bracketed(join);
                    if (bracketed)
                        shown.Children.Add(Word("("));

                    shown.Children.Add(Shown(join.Parts[0]));
                    shown.Children.Add(Sign(join, shown, symbol));
                    shown.Children.Add(Shown(join.Parts[1]));
                    if (bracketed)
                        shown.Children.Add(Word(")"));

                    break;
                }
                case JoinKind.Negate:
                    shown.Children.Add(Sign(join, shown, "-"));
                    shown.Children.Add(Shown(join.Parts[0]));
                    break;
                case JoinKind.Not:
                    shown.Children.Add(Sign(join, shown, symbol));
                    shown.Children.Add(Shown(join.Parts[0]));
                    break;
                default:
                    shown.Children.Add(Sign(join, shown, symbol + "("));
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

        // Every value and empty place stands as high as a typed value does: its box, small text with 8 above and below (36 measured
        // in the simulator), in a chip keeping 3 clear inside its edge so a lit edge shows.
        const float NodeHigh = 42f;

        // Shows piece, and every calculation and value inside it, as what the grip under the pointer would take, where pointed;
        // plainly otherwise. A value inside is taken along, quieter than the calculation in view; an empty place has no look of
        // its own for it.
        void Grabbable(DraftPiece piece, bool pointed, bool along = false)
        {
            if (piece is DraftHole)
                return;

            var join = piece as DraftJoin;
            ShownFor(piece)?.Set(along && join == null ? LookStates.TakenAlong : LookStates.Grabbable, pointed);
            if (join == null)
                return;

            foreach (var part in join.Parts)
                Grabbable(part, pointed, true);
        }

        static Label Word(string words) => new Label { Style = Styles.ExpressionWord, Text = words, Align = Align.Center };

        // The sign of join, shown, in words: pressed to pick another calculation in its place where another fits there.
        Element Sign(DraftJoin join, Element shown, string words)
        {
            var others = Others(join);
            if (others.Count == 0)
                return Word(words);

            return new Button
            {
                Style = Styles.ExpressionName,
                Text = words,
                Align = Align.Center,
                Tip = CodexTip.For(null, _editor.Texts.Codex.ExpressionChangeAbout),
                Command = new RelayCommand(() => Rekind(join, others, shown))
            };
        }

        // The calculations other than join's own that take as many values and leave the whole able to come to the kind being built
        // in its place, holding what it holds.
        List<JoinKind> Others(DraftJoin join)
        {
            var others = new List<JoinKind>();
            if (_single)
                return others;

            var types = Types();
            var wanted = DraftKinds.Wanted(_wanted);
            // JoinKind runs from Add to Or, each of the builder's calculations once.
            for (var kind = JoinKind.Add; kind <= JoinKind.Or; kind++)
            {
                if (kind == join.Kind || DraftJoin.Places(kind) != join.Parts.Count || !_tests && (JoinKinds.Compares(kind) || JoinKinds.JoinsFlags(kind)))
                    continue;

                if (DraftKinds.Suits(_draft.TriedReplace(join, Rekinded(join, kind)), wanted, types))
                    others.Add(kind);
            }

            return others;
        }

        // Asks under shown, where join is on the board, which of others takes its place, and puts it there holding what join holds.
        void Rekind(DraftJoin join, List<JoinKind> others, Element shown)
        {
            var texts = _editor.Texts;
            _editor.AskUnder("", shown);
            foreach (var other in others)
            {
                var kind = other;
                _editor.Answer(ExpressionWords.Symbol(texts, kind), () => _draft.Replace(join, Rekinded(join, kind)));
            }
        }

        // A box of kind holding copies of what join holds.
        static DraftJoin Rekinded(DraftJoin join, JoinKind kind)
        {
            var parts = new List<DraftPiece>();
            foreach (var part in join.Parts)
                parts.Add(part.Copied());

            return new DraftJoin(kind, parts);
        }

        // The small word saying what kind of value value is, kind: pressed to pick another way of combining its blocks where it
        // is a reading that combines them, into one number or into one check.
        Element Combining(DraftValue value, Element shown, string kind)
        {
            var reading = value.Atom as BlockReading;
            if (reading == null || Ways(reading).Length == 0)
                return new Label { Style = Styles.ExpressionKindWord, Text = kind, Align = Align.Center };

            return new Button
            {
                Style = Styles.ExpressionKind,
                Text = kind,
                Align = Align.Center,
                Tip = CodexTip.For(null, _editor.Texts.Codex.ExpressionChangeAbout),
                Command = new RelayCommand(() => Recombine(value, reading, shown))
            };
        }

        static readonly Combination[] Totals = { Combination.Sum, Combination.Average, Combination.Min, Combination.Max };
        static readonly Combination[] Checks = { Combination.Any, Combination.All };
        static readonly Combination[] Alone = { };

        // The ways reading may combine its blocks: into one number, as it does, or into one check; none for one block read alone.
        static Combination[] Ways(BlockReading reading)
        {
            if (reading.Checks)
                return Checks;

            return Array.IndexOf(Totals, reading.Combine) >= 0 ? Totals : Alone;
        }

        // Asks under shown, where value is on the board, which other way of combining its blocks reading takes, and puts the
        // reading combined that way in its place.
        void Recombine(DraftValue value, BlockReading reading, Element shown)
        {
            var texts = _editor.Texts;
            _editor.AskUnder("", shown);
            foreach (var way in Ways(reading))
            {
                if (way == reading.Combine)
                    continue;

                var combine = way;
                var combined = reading.Checks
                    ? new BlockReading(reading.Key, combine, reading.Arguments, reading.Targets, reading.Relation, reading.Compared.Copied())
                    : new BlockReading(reading.Key, combine, reading.Arguments, reading.Targets);
                _editor.Answer(ExpressionNames.Combined(texts, combine), () => _draft.Replace(value, new DraftValue(combined)));
            }
        }

        // The kind of value atom, a value on the board that is not typed in, is chosen from.
        static ValueSource SourceOf(Expression atom)
        {
            if (atom is BlockReading)
                return ValueSource.Reading;

            return atom is StateOperand ? ValueSource.State : ValueSource.Setting;
        }

        // Makes element, showing piece, or the bin where bin, a place what is dragged can be dropped on.
        void Target(Element element, DraftPiece piece, bool bin = false)
        {
            _targets.Add(element);
            _targetPieces.Add(piece);
            element.DropTarget = new Place(this, piece, element, bin);
        }

        void Grab(DragHandle grip, Element shown, Dragged carried)
        {
            grip.DragData = carried;
            grip.DragGhost = shown;
            grip.DragStarted += _ => Begin(carried);
            // The piece carried is drawn where the pointer has taken it, so the screen is drawn again as the pointer moves.
            grip.DragDelta += _ => _editor.Invalidate();
            grip.DragCompleted += _ => End();
            grip.DragCanceled += End;
        }

        // Takes hold of what is dragged, once the places it could go are lit, and dims every place it cannot.
        void Begin(Dragged carried)
        {
            _carried = carried;
            foreach (var target in _targets)
            {
                target.Set(LookStates.Grabbable, false);
                target.Set(LookStates.TakenAlong, false);
            }

            // The bin and what is carried keep their own look: the carried piece is drawn again where the pointer takes it.
            for (var i = 1; i < _targets.Count; i++)
            {
                if (carried.Piece == null || !ExpressionDraft.Holds(carried.Piece, _targetPieces[i]))
                    Dim(_targets[i], !_targets[i].Is(DragDrop.Accepting));
            }

            var drag = Windows?.DragDrop;
            _hint.Text = drag != null && drag.FitsAnywhere ? _editor.Texts.ExpressionDragging : _editor.Texts.ExpressionNowhere;
            _editor.Invalidate();
        }

        // What carried does dropped on piece, shown by shown, or in the bin where bin.
        void Dropped(Dragged carried, DraftPiece piece, Element shown, bool bin)
        {
            if (bin)
            {
                _draft.Remove(carried.Piece);
                return;
            }

            var hole = piece as DraftHole;
            if (carried.Piece != null)
                _draft.Move(carried.Piece, hole, moved => Compared(Condition(hole), moved));
            else if (carried.Kind != null && hole != null)
                _draft.Fill(hole, new DraftJoin(carried.Kind.Value));
            else if (carried.Kind != null)
                _draft.Wrap(piece, carried.Kind.Value, wrapped => Compared(JoinKinds.JoinsFlags(carried.Kind.Value), wrapped));
            else if (carried.Source == ValueSource.Typed)
                _draft.Fill(hole, Fixed(carried.Typed));
            else
                Choose(carried.Source, hole, shown);
        }

        void End()
        {
            _carried = null;
            foreach (var target in _targets)
                Dim(target, false);

            _hint.Text = "";
            _editor.Invalidate();
        }

        // Shows shown, a place on the board, as one what is dragged cannot go, where off, or as it was: the place, its own words,
        // its edge and the box or switch its value is typed in, not the places inside it.
        static void Dim(Element shown, bool off)
        {
            shown.Set(LookStates.Off, off);
            var stack = shown as Stack;
            if (stack == null)
                return;

            foreach (var child in stack.Children)
            {
                if (child is Label || child is Button || child is Swatch)
                    child.Set(LookStates.Off, off);

                var typed = child as TypedValue;
                if (typed == null)
                    continue;

                foreach (var part in typed.Children)
                    part.Set(LookStates.Off, off);
            }
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

            // A flag set takes one value alone; a comparison or what joins flags only where a condition is built.
            if (carried.Kind != null && (_single || !_tests && (JoinKinds.Compares(carried.Kind.Value) || JoinKinds.JoinsFlags(carried.Kind.Value))))
                return false;

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

        // Of the values named, those that leave the whole able to come to the kind being built when they are put in place.
        List<string> Fitting(List<string> named, DraftPiece place, Func<string, DraftValue> value)
        {
            var types = Types();
            var wanted = DraftKinds.Wanted(_wanted);
            var fitting = new List<string>();
            foreach (var name in named)
            {
                if (DraftKinds.Suits(_draft.TriedReplace(place, value(name)), wanted, types))
                    fitting.Add(name);
            }

            return fitting;
        }

        // Whether place stands where a condition does: the whole of what is tested, a side of and or or, or what not turns over.
        bool Condition(DraftPiece place)
        {
            var join = place.Parent;
            return join == null ? _tests : JoinKinds.JoinsFlags(join.Kind);
        }

        // A block value, setting or state that is on or off, put where a condition stands, compared with on: every "is" on the
        // screen is a box, so a flag reads as a condition only by a comparison that is really there, which can be changed.
        // A check of any or all of some blocks already compares, and stands as it is.
        DraftPiece Compared(DraftPiece place, DraftValue value) => Compared(Condition(place), value);

        // The piece placed, compared with on where it is a value that is on or off placed where a condition stands.
        DraftPiece Compared(bool condition, DraftPiece piece)
        {
            var value = piece as DraftValue;
            var reading = value?.Atom as BlockReading;
            if (!condition || value == null || reading != null && reading.Checks || DraftKinds.Of(value, Types()) != KindClass.Flag)
                return piece;

            return new DraftJoin(JoinKind.Is, new DraftPiece[] { value, new DraftValue(new Operand(Literal.Of(true))) });
        }

        static DraftValue Setting(string name) => new DraftValue(new Operand(Literal.Setting(name)));

        static DraftValue State(string name) => new DraftValue(new StateOperand(name));

        static DraftValue Fixed(KindClass kind) => new DraftValue(new Operand(TypedValue.Neutral(kind)));

        // Asks under shown, where place is on the board, which value of source goes in place, an empty place or a value there, of
        // those that fit there, and puts it there.
        void Choose(ValueSource source, DraftPiece place, Element shown)
        {
            if (source == ValueSource.Reading)
            {
                PickReading(shown, reading => _draft.Replace(place, Compared(place, new DraftValue(reading))));
                return;
            }

            var setting = source == ValueSource.Setting;
            var types = Types();
            var named = setting ? ExpressionOffers.Settings(_system, KindClass.Any, types) : ExpressionOffers.States(_system, KindClass.Any, types);
            Func<string, DraftValue> value = setting ? (Func<string, DraftValue>)Setting : State;
            _editor.AskUnder("", shown);
            foreach (var name in Fitting(named, place, value))
            {
                var chosen = name;
                _editor.Answer(chosen, () => _draft.Replace(place, Compared(place, value(chosen))));
            }
        }

        // Opens the selector on what blocks to read and which of their values, asking under shown which blocks it selects from,
        // and gives done the reading picked.
        void PickReading(Element shown, Action<BlockReading> done)
        {
            var origin = _editor.Depth;
            var source = new SourceSwitch(_editor, slots =>
            {
                var page = new PickPage(_editor, _editor.Texts.PickHeadingReading, false, new List<long>(), default(MemberKey), ReachKind.Value,
                    answer => _editor.Picks.Name(_system, answer, false, null, named =>
                    {
                        _editor.UnwindTo(origin);
                        done(Reading(answer, named));
                    }),
                    true, forReadings: true, checksBlocks: _tests);
                page.Slotted = _system;
                page.ListsSlots = slots;
                return page;
            }, null);
            source.Ask(null, shown);
        }

        // The reading answer picked, of the blocks named.
        BlockReading Reading(PickAnswer answer, List<string> named)
        {
            var combine = Combination.None;
            if (answer.Targets.Count > 1 && _tests && (answer.Quantifier == Quantifier.Any || answer.Quantifier == Quantifier.All))
                return Checked(answer, named);

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

        // The check of any or all of the blocks answer picked, named: the options the member is read with, then how each block is
        // compared and with what, as a test of blocks is given them.
        BlockReading Checked(PickAnswer answer, List<string> named)
        {
            var value = EntityAction.Value(Vocabulary.ActionsOf(_editor.World, answer.Targets), answer.Key);
            var given = answer.Arguments ?? new List<Literal>();
            var taken = ComparisonQuestion.Taken(value);
            var options = new List<Literal>();
            for (var i = 0; i < taken && i < given.Count; i++)
                options.Add(given[i]);

            var relation = given.Count > taken ? ComparisonQuestion.Read(given[taken]) : Relation.Is;
            var compared = given.Count > taken + 1 ? given[taken + 1] : Literal.Of(true);
            var combine = answer.Quantifier == Quantifier.All ? Combination.All : Combination.Any;
            return new BlockReading(answer.Key, combine, options, named, relation, Operand.Of(compared));
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
