using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Screen;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Words;

namespace Sprocket.Host.SpaceEngineers.Picking
{
    /// <summary>
    /// The stages of a pick, as columns, in the manner of a file browser's column view.
    /// </summary>
    /// <remarks>
    /// Columns are this target's decision, not core's. Core says there are stages and that each narrows
    /// the next; with every stage still in view the blocks already picked stay
    /// beside what is happening to them, and changing an earlier answer is a glance to the left rather than
    /// a retreat through screens.
    /// <para>
    /// Built once and thereafter changed in place. Anything torn down and made again loses what somebody
    /// was doing with it - the caret in the box they were typing into above all, since typing is what
    /// changes the list underneath. Only a change in how many stages there are rebuilds anything.
    /// </para>
    /// </remarks>
    public sealed class PickView : ComponentView
    {
        public const float ColumnWidth = Sizes.Column;

        /// <summary>How wide the way back to the page before is, standing where its last column was.</summary>
        const float FoldWidth = 56f;

        /// <summary>
        /// How wide a column is on a page after the first, which makes room for the way back beside
        /// three of them in the width three columns take on the first.
        /// </summary>
        const float PagedWidth = (ColumnWidth * 3f - FoldWidth - Space.Item) / 3f;

        /// <summary>How many ticks a page takes to slide in, which is a third of a second.</summary>
        const int SlideTicks = 20;

        readonly PickComponent _component;
        readonly Stack _columns = Widgets.RowOf(Space.Item);
        readonly Scroll _strip = new Scroll();

        /// <summary>What <see cref="_grabbed" /> says when the strip's own bar is the one being held.</summary>
        const int Strip = -2;

        const int None = -1;

        /// <summary>Which column's bar is being dragged, or <see cref="Strip" />, or nothing.</summary>
        int _grabbed = None;

        /// <summary>Where the pointer was when the strip last moved.</summary>
        float _held;
        readonly List<StageView> _stages = new List<StageView>();

        int _revision = -1;
        int _built = -1;

        /// <summary>The page the columns were last built for, or minus one before they have been.</summary>
        int _paged = -1;

        /// <summary>The pages that have been shown, which is when each one gave its column the caret.</summary>
        readonly List<int> _arrived = new List<int>();

        /// <summary>
        /// The way back to the page before, where the last column of that page stood, drawn narrow
        /// because what it was about is answered.
        /// </summary>
        readonly Sized _fold = new Sized(FoldWidth);
        readonly Dock _foldColumn = new Dock();

        /// <summary>
        /// The way on to the page after, at the right of the last column, where something chosen has a
        /// page after it.
        /// </summary>
        readonly Sized _next = new Sized(FoldWidth);
        readonly Dock _nextColumn = new Dock();

        /// <summary>Whether the way on stands at the right of the strip.</summary>
        bool _onward;

        /// <summary>How far into making room for the way on or taking it away, or minus one where neither is under way.</summary>
        int _squeeze = -1;

        float _squeezeFrom;
        float _squeezeTo;

        /// <summary>
        /// The column coming back out of the way back, drawn only as wide as the way back has grown, so
        /// what is in it is there all the way and nothing is put in its place at the end.
        /// </summary>
        readonly Sized _reveal = new Sized(FoldWidth);
        readonly Scroll _revealWindow = new Scroll();

        /// <summary>What grows or shrinks as a page turns: the way back folding, or a column coming back out of it.</summary>
        Sized _resized;

        /// <summary>The columns of the page being left, sliding out of sight, and nothing otherwise.</summary>
        readonly List<Element> _leaving = new List<Element>();

        /// <summary>
        /// Room past the last column while a page turns, a page wide, so the strip can scroll as far as
        /// the slide goes.
        /// </summary>
        /// <remarks>
        /// A scroller stops where its content ends, and a page turning forward has only its first
        /// column yet - so without this the slide stops short of the page and jumps the rest at the end.
        /// </remarks>
        readonly Sized _room = new Sized(ColumnWidth * 3f + Space.Item * 2f);

        /// <summary>How far into a slide from one page to another, or minus one where none is under way.</summary>
        int _slide = -1;

        float _slideFrom;
        float _slideTo;

        /// <summary>How wide what grows or shrinks as a page turns starts the slide, and ends it.</summary>
        float _resizedFrom;

        float _resizedTo;

        /// <summary>What the strip held before a page turned, kept only while the new page is laid out.</summary>
        readonly List<Element> _old = new List<Element>();

        /// <summary>
        /// The values the chosen thing takes, and the way to finish, once a page of the pick gives any.
        /// </summary>
        /// <remarks>Made when first wanted, at the width of the page that wants it.</remarks>
        Ask _given;

        /// <summary>
        /// The way to finish where there are no values to give, under the rows of the column the pick is
        /// finished from, as the values column carries its own under its fields.
        /// </summary>
        readonly Label _done = Widgets.Label("Done", Palette.Ink, Lettering.Small);

        readonly Sized _givenRoot = new Sized(ColumnWidth);
        readonly Dock _givenColumn = new Dock();
        readonly Stack _givenHead = Widgets.Column(Space.Row);

        /// <summary>The heading on the left, and Done on the right of it while there are no fields.</summary>
        readonly Bar _givenTop = new Bar();
        readonly Label _givenTitle = Widgets.Label("", Palette.Ink, Lettering.Body);

        /// <summary>
        /// The line under the heading, where a stage says what it holds, kept so the first field lines
        /// up with the search boxes beside it.
        /// </summary>
        readonly Label _givenUnder = Widgets.Label("", Palette.Faint, Lettering.Tiny);

        /// <summary>The fields, and the line under them.</summary>
        readonly Stack _givenBody = Widgets.Column(Space.Region);

        /// <summary>Under the fields: why nothing can be finished yet, and Done on the right of it.</summary>
        readonly Bar _givenFoot = new Bar();

        /// <summary>
        /// What was entered for each chosen thing while the pick is open, by what it is for and then by
        /// field.
        /// </summary>
        /// <remarks>
        /// Kept by the view because what is typed is the view's until it is finished, and for as long as
        /// the view lives, which is as long as the pick is open. Each field as its own control spells it,
        /// so going back to something refills its controls the way opening on a step does.
        /// </remarks>
        readonly Dictionary<string, Dictionary<string, string>> _entered =
            new Dictionary<string, Dictionary<string, string>>();

        /// <summary>Which chosen thing the fields are showing, or null before they have shown anything.</summary>
        string _shown;

        int _restated = -1;

        /// <summary>Whether the fields have been filled for the first time, which is when they may take the caret on opening.</summary>
        bool _opened;

        readonly List<string> _filling = new List<string>();

        /// <summary>What core was last told the values hold.</summary>
        string _reported;

        public PickView(PickComponent component)
        {
            _component = component;
            _strip.Horizontal = true;
            Widgets.Barred(_strip);
            _strip.Content = _columns;

            // Pressed anywhere down its length: it is the whole of what that page was, and going back
            // to it is the one thing it does.
            _foldColumn.Background = Palette.Panel;
            _foldColumn.Edge = Palette.Line;
            _foldColumn.Highlight = Palette.PanelHot;
            _foldColumn.Padding = new Thickness(0f, Space.Region);
            _foldColumn.Fill = Widgets.Centred(Symbols.Left, Palette.Muted);
            _foldColumn.Tip = "Back";
            _fold.Content = _foldColumn;

            _nextColumn.Background = Palette.Panel;
            _nextColumn.Edge = Palette.Line;
            _nextColumn.Highlight = Palette.PanelHot;
            _nextColumn.Padding = new Thickness(0f, Space.Region);
            _nextColumn.Fill = Widgets.Centred(Symbols.Right, Palette.Muted);
            _nextColumn.Tip = "Next";
            _next.Content = _nextColumn;

            // A window rather than a scroller: it is never moved, only made wider, and what it shows
            // keeps the width it will have when it stands on its own.
            _revealWindow.Horizontal = true;
            _reveal.Content = _revealWindow;
        }

        /// <summary>Makes the values column, as wide as the columns beside it.</summary>
        void Giving(float width)
        {
            _given = Ask.Column(width - Space.Region * 2f);
            _givenRoot.Width = width;

            _givenTitle.Wraps = false;
            _givenTitle.Trims = true;
            _givenTitle.Text = Widgets.Caps(SequenceText.ValuesTitle());
            _givenHead.Children.Add(_givenTitle);
            _givenHead.Children.Add(_givenUnder);
            _givenTop.Spacing = Space.Item;
            _givenTop.Fill = _givenHead;

            // The gap a stage keeps between its heading and its search box, so the first field lines
            // up with the search boxes beside it.
            _givenTop.Padding = new Thickness(0f, 0f, 0f, Space.Row);

            // The reason beside Done, wrapping downwards, so Done stays where it is whatever it says.
            _givenFoot.Spacing = Space.Item;
            _givenFoot.Tops = true;
            _givenFoot.Fill = _given.Why;

            _givenBody.Children.Add(_given.Form);
            _givenBody.Children.Add(_givenFoot);

            _givenColumn.Background = Palette.Panel;
            _givenColumn.Edge = Palette.Line;
            _givenColumn.Padding = new Thickness(Space.Region);
            _givenColumn.Top = _givenTop;
            _givenColumn.Fill = _givenBody;
            _givenRoot.Content = _givenColumn;
        }

        /// <summary>
        /// Tells core what the values hold, where that differs from what it was last told.
        /// </summary>
        /// <remarks>
        /// Asked after something that can change them - a key, a press or a drag in the column, or the
        /// column being filled again - and never on a tick.
        /// </remarks>
        public override void Entered()
        {
            if (_given == null || _component.EnteredCommand == 0 || Host == null)
                return;

            var answer = _given.Answer;
            if (answer == _reported)
                return;

            _reported = answer;
            Host.SetText(_component.EnteredCommand, answer);
        }

        /// <inheritdoc />
        public override bool Gives
        {
            get { return _component.Finishes; }
        }

        /// <summary>The way to finish under the column the page is finished from, where the page has one.</summary>
        public override Label Done
        {
            get
            {
                var stages = _component.Stages;
                for (int i = _component.Page; i < stages.Count; i++)
                {
                    if (stages[i].Finishes)
                        return _done;
                }

                return null;
            }
        }

        /// <summary>The values column, while it is on the screen.</summary>
        public override Ask Given
        {
            get { return _given != null && _givenRoot.Parent != null ? _given : null; }
        }

        /// <summary>
        /// Answered with the revision this view already keeps. The picker's stages are rebuilt whole
        /// rather than changed in place - a search narrows them, a choice re-derives them - so one
        /// number says everything a screenful of reading would have.
        /// </summary>
        protected override bool Stale
        {
            get
            {
                return _revision != _component.Revision || _built != _component.Stages.Count
                    || _paged != _component.Page;
            }
        }

        public override Element Root
        {
            get { return _strip; }
        }

        /// <summary>The strip scrolls sideways and every column scrolls its own rows.</summary>
        public override bool ScrollsItself
        {
            get { return true; }
        }

        protected override void Refresh()
        {
            // Core says when it has changed something and says nothing the rest of the time, so an
            // ordinary tick costs one comparison.
            if (!Stale)
                return;

            _revision = _component.Revision;

            if (_given == null && _component.Finishes)
                Giving(Width);

            var onward = _component.Onward != 0;
            _nextColumn.Press = onward ? Deed.Sends(_component.Onward) : Deed.Nothing;

            if (_built != _component.Stages.Count || _paged != _component.Page)
            {
                _built = _component.Stages.Count;
                Rebuild();
            }
            else if (onward != _onward)
                Squeeze(onward);

            var page = _component.Page;
            for (int i = 0; i < _stages.Count && page + i < _component.Stages.Count; i++)
                _stages[i].Update(_component.Stages[page + i], Host);

            Arrive();

            if (_given != null && _restated != _component.Restated)
            {
                _restated = _component.Restated;
                Refill();
            }

            Show();
        }

        /// <summary>
        /// Puts the values column on after the stages once something to do is chosen, and takes it off
        /// when nothing is.
        /// </summary>
        void Show()
        {
            if (_given == null)
                return;

            var wanted = _component.Finishes && _component.Stages.Count - _component.Page > 1
                && !string.IsNullOrEmpty(_component.Given);
            var shown = _givenRoot.Parent != null;
            if (wanted == shown)
                return;

            if (wanted)
                _columns.Children.Add(_givenRoot);
            else
                _columns.Children.Remove(_givenRoot);
        }

        /// <summary>
        /// Fills the fields for what is chosen now, keeping what was entered for what was chosen before.
        /// </summary>
        /// <remarks>
        /// What something opens holding is what was entered for it earlier in this pick, where anything
        /// was, and what the pick says otherwise: a step's own values, for what the step does.
        /// </remarks>
        void Refill()
        {
            if (!string.IsNullOrEmpty(_shown))
            {
                var was = Kept(_shown);
                was.Clear();
                _given.Spell(was);
            }

            var given = _component.Given ?? "";
            if (given != _shown)
                _given.Unjudge();

            _shown = given;

            Dictionary<string, string> kept = null;
            if (given.Length > 0)
                _entered.TryGetValue(given, out kept);

            List<Field> fields = _component.Fields;
            _filling.Clear();
            for (var i = 0; i < fields.Count; i++)
            {
                string said;
                if (kept != null)
                    _filling.Add(kept.TryGetValue(Ask.Key(fields[i], i), out said) ? said : "");
                else
                    _filling.Add(i < _component.Filled.Count ? _component.Filled[i] : "");
            }

            // The fields say what is wanted by being there, and nothing is said over them.
            _given.Values("", 0, fields, _filling);

            Place(_given.Count > 0);

            // Nowhere to type is nowhere to keep the keyboard. Somebody who came for the values gets
            // the caret in them, once, when they first arrive.
            if (_given.Count == 0)
                _given.Release();
            else if (_component.OpensOnValues && !_opened)
                _given.Take();

            _opened = true;
            Entered();
        }

        /// <summary>
        /// Puts Done under the fields where there are any, and beside the heading where there are none.
        /// </summary>
        /// <remarks>Only when what is chosen changes, never while it is being filled in.</remarks>
        void Place(bool underFields)
        {
            if (underFields)
            {
                _givenTop.Right = null;
                _givenFoot.Right = _given.Done;
            }
            else
            {
                _givenFoot.Right = null;
                _givenTop.Right = _given.Done;
            }
        }

        Dictionary<string, string> Kept(string given)
        {
            Dictionary<string, string> kept;
            if (!_entered.TryGetValue(given, out kept))
            {
                kept = new Dictionary<string, string>();
                _entered[given] = kept;
            }

            return kept;
        }

        /// <summary>
        /// Moves the column being pointed at, or the whole strip when that column has nowhere to go.
        /// </summary>
        /// <remarks>
        /// The fallback is not a flourish. With several stages open the strip runs off the side, and a
        /// wheel that did nothing because this particular column happened to fit would read as the
        /// screen being broken rather than as the column being full.
        /// </remarks>
        public override bool ScrollBy(Vector2 point, float amount)
        {
            if (!_strip.Reaches(point))
                return false;

            for (int i = 0; i < _stages.Count; i++)
            {
                if (_stages[i].ScrollBy(point, amount))
                    return true;
            }

            if (!_strip.CanScroll)
                return false;

            _strip.ScrollBy(amount);
            return true;
        }

        /// <summary>
        /// Takes hold of a scrollbar - a column's, or the strip's own.
        /// </summary>
        /// <remarks>
        /// Through the drag path rather than the press path, because a bar is the one control here that
        /// wants every move of the pointer rather than the fact of a click. It costs nothing: this view
        /// had no other use for grabbing, and the screen already offers a grab before it offers a press.
        /// </remarks>
        public override bool TryGrab(Vector2 point)
        {
            for (int i = 0; i < _stages.Count; i++)
            {
                if (!_stages[i].TryGrabBar(point))
                    continue;

                _grabbed = i;
                return true;
            }

            if (!_strip.OnBar(point))
                return false;

            if (!_strip.OnThumb(point))
                _strip.ThumbTo(point.X);

            _strip.BarHeld = true;
            _held = point.X;
            _grabbed = Strip;
            return true;
        }

        public override void Carry(Vector2 point)
        {
            if (_grabbed == Strip)
            {
                _strip.DragBy(point.X - _held);
                _held = point.X;
                return;
            }

            if (_grabbed >= 0 && _grabbed < _stages.Count)
                _stages[_grabbed].CarryBar(point);
        }

        public override void Drop()
        {
            _strip.BarHeld = false;

            for (int i = 0; i < _stages.Count; i++)
                _stages[i].DropBar();

            _grabbed = None;
        }

        /// <summary>
        /// Gives the caret to the box of the column the pick opens on, the first time its page is shown.
        /// </summary>
        /// <remarks>
        /// Once per page, so a page gone back to is where it was left rather than where it opened.
        /// </remarks>
        void Arrive()
        {
            var page = _component.Page;
            if (Host == null || _arrived.Contains(page))
                return;

            _arrived.Add(page);

            var at = _component.Opens - page;
            if (at >= 0 && at < _stages.Count)
                _stages[at].Take(Host);
        }

        /// <summary>How wide a column is on the page showing now.</summary>
        float Width => _component.Page > 0 ? PagedWidth : ColumnWidth;

        /// <summary>
        /// Makes the columns of the page showing now, turning to it where the page is a different one.
        /// </summary>
        /// <remarks>
        /// Only the columns of this page are made. Those before it were answered on an earlier page and
        /// are nothing to press now, and the way back to them is the fold.
        /// </remarks>
        void Rebuild()
        {
            var page = _component.Page;
            var was = _paged;
            _paged = page;

            Settle();
            _squeeze = -1;

            // Going on from the first page, the column the next page follows is the one that folds.
            Element folding = null;
            if (was == 0 && page > 0 && page - 1 < _stages.Count)
                folding = _stages[page - 1].Root;

            _old.Clear();
            foreach (var column in _columns.Children)
            {
                if (column != _givenRoot && column != _next)
                    _old.Add(column);
            }

            for (int i = 0; i < _stages.Count; i++)
                _stages[i].Finishing(null);

            _stages.Clear();
            _columns.Children.Clear();

            // The last column gives the way on its room, where there is a way on.
            _onward = _component.Onward != 0;
            var width = Width;
            var last = _component.Stages.Count - 1;
            for (int i = page; i <= last; i++)
            {
                var across = _onward && i == last ? width - FoldWidth - Space.Item : width;
                var stage = new StageView(_component, i, across);
                if (_component.Stages[i].Finishes)
                    stage.Finishing(_done);

                _stages.Add(stage);
            }

            _foldColumn.Press = _component.Returns != 0 ? Deed.Sends(_component.Returns) : Deed.Nothing;

            if (folding != null)
                Fold(folding);
            else if (page == 0 && was > 0 && was - 1 < _stages.Count)
                Unfold(_stages[was - 1].Root);
            else
                Lay();

            _old.Clear();
            Show();
        }

        /// <summary>The page as it stands: the way back where there is a page before it, its columns, and the way on where there is a page after.</summary>
        void Lay()
        {
            if (_component.Page > 0)
            {
                _fold.Width = FoldWidth;
                _columns.Children.Add(_fold);
            }

            for (int i = 0; i < _stages.Count; i++)
                _columns.Children.Add(_stages[i].Root);

            Onward();
        }

        /// <summary>Puts the way on at the right of the columns, where there is a page after this one.</summary>
        void Onward()
        {
            if (!_onward)
                return;

            _next.Width = FoldWidth;
            _columns.Children.Add(_next);
        }

        /// <summary>
        /// Turns forward: the page being left slides out to the left, and the column this page follows
        /// on from folds into the way back as it goes.
        /// </summary>
        /// <remarks>
        /// The column folds as an empty panel. What was in it is answered, and a column's rows squeezed
        /// narrower on every tick would be words wrapping and measured again all the way down.
        /// </remarks>
        void Fold(Element folding)
        {
            var across = 0f;
            var before = true;
            for (int i = 0; i < _old.Count; i++)
            {
                if (_old[i] == folding)
                {
                    _fold.Width = Across(folding);
                    _columns.Children.Add(_fold);
                    before = false;
                    continue;
                }

                _columns.Children.Add(_old[i]);
                _leaving.Add(_old[i]);
                if (before)
                    across += Across(_old[i]) + Space.Item;
            }

            for (int i = 0; i < _stages.Count; i++)
                _columns.Children.Add(_stages[i].Root);

            Onward();
            Slide(0f, across, _fold, _fold.Width, FoldWidth);
        }

        /// <summary>
        /// Turns back: the page coming back slides in from the left, and the column the way back stood
        /// for comes out of it, shown only as wide as it has grown.
        /// </summary>
        void Unfold(Element unfolding)
        {
            var across = 0f;
            var before = true;
            for (int i = 0; i < _stages.Count; i++)
            {
                var root = _stages[i].Root;
                if (root == unfolding)
                {
                    _revealWindow.Content = unfolding;
                    _reveal.Width = FoldWidth;
                    _columns.Children.Add(_reveal);
                    before = false;
                    continue;
                }

                _columns.Children.Add(root);
                if (before)
                    across += Across(root) + Space.Item;
            }

            Onward();

            for (int i = 0; i < _old.Count; i++)
            {
                if (_old[i] == _fold)
                    continue;

                _columns.Children.Add(_old[i]);
                _leaving.Add(_old[i]);
            }

            Slide(across, 0f, _reveal, FoldWidth, Across(unfolding));
        }

        /// <summary>Starts the strip sliding, and <paramref name="resized" /> growing or shrinking with it.</summary>
        void Slide(float from, float to, Sized resized, float resizedFrom, float resizedTo)
        {
            // The strip's own thumb would sweep across under it, saying the strip scrolls when it is
            // only the page turning.
            _strip.BarColor = Palette.None;
            _columns.Children.Add(_room);

            _slide = 0;
            _slideFrom = from;
            _slideTo = to;
            _resized = resized;
            _resizedFrom = resizedFrom;
            _resizedTo = resizedTo;
            _strip.Scrolled = from;
        }

        /// <summary>How wide a column of the strip is, from what it was made to be rather than from a measure.</summary>
        static float Across(Element column)
        {
            var sized = column as Sized;
            return sized != null ? sized.Width : column.DesiredSize.X;
        }

        /// <summary>Ends a slide where it is going, taking away what it left behind.</summary>
        void Settle()
        {
            if (_slide < 0)
                return;

            _slide = -1;
            for (int i = 0; i < _leaving.Count; i++)
                _columns.Children.Remove(_leaving[i]);

            _leaving.Clear();
            _columns.Children.Remove(_room);
            _resized.Width = _resizedTo;

            // The column that came back stands on its own where its window stood, looking the same.
            if (_resized == _reveal)
            {
                var unfolded = _revealWindow.Content;
                _revealWindow.Content = null;

                _old.Clear();
                foreach (var column in _columns.Children)
                    _old.Add(column == _reveal ? unfolded : column);

                _columns.Children.Clear();
                for (int i = 0; i < _old.Count; i++)
                    _columns.Children.Add(_old[i]);

                _old.Clear();
            }

            _resized = null;
            _strip.BarColor = Palette.Thumb;
            _strip.Scrolled = 0f;
        }

        /// <summary>
        /// Makes room for the way on at the right of the last column, or takes it away, a step at a
        /// tick, as something chosen comes to have a page after it or stops having one.
        /// </summary>
        void Squeeze(bool opening)
        {
            _onward = opening;
            if (_stages.Count == 0)
                return;

            if (opening && _next.Parent == null)
            {
                _next.Width = 0f;
                _columns.Children.Add(_next);
            }

            _squeeze = 0;
            _squeezeFrom = _next.Width;
            _squeezeTo = opening ? FoldWidth : 0f;
        }

        /// <summary>Quick to leave and slow to arrive, so the eye has where it is going before it stops.</summary>
        static float Eased(int step)
        {
            var t = (float)step / SlideTicks;
            return 1f - (1f - t) * (1f - t) * (1f - t);
        }

        /// <summary>
        /// Moves a page turning, or the room for the way on being made, on by a tick.
        /// </summary>
        /// <remarks>
        /// Every tick only while either is under way, which is a third of a second. A page turning moves
        /// the strip and resizes one panel; room being made resizes the last column and the way on, and
        /// measures that column again.
        /// </remarks>
        protected override void Moved(int tick)
        {
            if (_squeeze >= 0)
                Squeezing();

            if (_slide < 0)
                return;

            _slide++;
            if (_slide >= SlideTicks)
            {
                Settle();
                return;
            }

            var eased = Eased(_slide);
            _strip.Scrolled = _slideFrom + (_slideTo - _slideFrom) * eased;
            _resized.Width = _resizedFrom + (_resizedTo - _resizedFrom) * eased;
        }

        /// <summary>One tick of making room for the way on, or of taking it away.</summary>
        void Squeezing()
        {
            _squeeze++;
            var done = _squeeze >= SlideTicks;
            var next = done ? _squeezeTo : _squeezeFrom + (_squeezeTo - _squeezeFrom) * Eased(_squeeze);

            var last = _stages[_stages.Count - 1];
            _next.Width = next;
            last.Width = Width - Space.Item - next;
            if (!done)
                return;

            _squeeze = -1;
            if (_squeezeTo > 0f)
                return;

            _columns.Children.Remove(_next);
            last.Width = Width;
        }
    }
}
