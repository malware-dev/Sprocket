using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Values;
using Lumel;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Windows;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>The stages of a pick as columns side by side, in the manner of a file browser's column view, and the values the last of them takes.</summary>
    /// <remarks>
    /// With every stage of a page in view, the blocks already picked stay beside what is happening to them, and changing an
    /// earlier answer is a glance to the left. A pick asking more than a screen holds goes on in pages, one after another in
    /// the same strip: the stages before the page showing were answered on an earlier one, and the fold at the left stands
    /// for them and turns back to them; the one at the right turns on where something chosen has a page after it. A page
    /// turning slides in from the side it comes from, and the way on is made room for a step at a time, so nothing appears
    /// anywhere without having come from somewhere.
    /// </remarks>
    public class PickStrip : HStack
    {
        /// <summary>How wide a column is on the first page of a pick.</summary>
        public const float ColumnWidth = Sizes.Column;

        const float Gap = 8f;
        const float FoldWidth = 56f;
        const float PagedWidth = (ColumnWidth * 3f - FoldWidth - Gap * 2f) / 3f;
        const int SlideTicks = 20;

        readonly List<StageColumn> _stages = new List<StageColumn>();
        readonly List<int> _arrived = new List<int>();
        readonly Dictionary<string, Dictionary<string, string>> _entered = new Dictionary<string, Dictionary<string, string>>();
        readonly VStack _fold;
        readonly VStack _next;
        readonly Texts _texts;
        readonly Func<WindowLayer> _windows;

        readonly List<Element> _old = new List<Element>();
        readonly List<Element> _leaving = new List<Element>();
        readonly Cut _reveal = new Cut();

        ValuesColumn _values;
        string _given;
        int _shownPage = -1;
        bool _onward;
        int _slide = -1;
        float _slideFrom;
        float _slideTo;
        Element _resized;
        float _resizedFrom;
        float _resizedTo;
        int _squeeze = -1;
        float _squeezeFrom;
        float _squeezeTo;

        /// <summary>An empty strip saying its fixed words in <paramref name="texts" />, whose lists hang open in <paramref name="windows" />.</summary>
        public PickStrip(Texts texts, Func<WindowLayer> windows)
        {
            _texts = texts;
            _windows = windows;
            Spacing = Gap;

            // Pressed anywhere down its length: it is the whole of what that page was, and going back to it is the one thing it does.
            _fold = Folded(Symbols.Left, texts.PickBack);
            _next = Folded(Symbols.Right, texts.PickNext);
        }

        /// <summary>Every stage of the pick in order, across every page, each narrowing the next.</summary>
        public IReadOnlyList<StageColumn> Stages => _stages;

        /// <summary>The first stage asked on the page showing now.</summary>
        public int Page { get; set; }

        /// <summary>Where going back to the page before goes, or null on the first page.</summary>
        public ICommand Returns
        {
            get { return _fold.Command; }
            set { _fold.Command = value; }
        }

        /// <summary>Where going on to the page after goes, or null where nothing chosen has a page after it.</summary>
        public ICommand Onward
        {
            get { return _next.Command; }
            set { _next.Command = value; }
        }

        /// <summary>The values what is chosen takes, made when the pick first finishes with values.</summary>
        public ValuesColumn Values => _values;

        /// <summary>Whether the values column stands after the stages of the page showing.</summary>
        public bool ShowsValues { get; set; }

        /// <summary>How wide a column is on the page showing now.</summary>
        public float Width => Page > 0 ? PagedWidth : ColumnWidth;

        /// <inheritdoc />
        protected override bool Clips => true;

        /// <summary>Adds a stage after the last, for the page that asks it.</summary>
        public void Add(StageColumn stage)
        {
            _stages.Add(stage);
        }

        /// <summary>Drops the last stage, when there is no longer anything for it to be about.</summary>
        public void RemoveLast()
        {
            if (_stages.Count > 0)
                _stages.RemoveAt(_stages.Count - 1);
        }

        /// <summary>Comes back to the page starting at <paramref name="page" />, whose stages end before <paramref name="count" />; what later pages asked goes.</summary>
        public void Return(int page, int count)
        {
            Page = page;
            while (_stages.Count > count)
                RemoveLast();
        }

        /// <summary>
        /// Asks for the values what is chosen takes, known as <paramref name="given" /> and headed by its name
        /// <paramref name="named" />: <paramref name="fields" />, holding what was given for it earlier in this pick where anything
        /// was and <paramref name="filled" /> otherwise.
        /// </summary>
        /// <remarks>What was given for what was chosen before is kept, so choosing it again gives it back.</remarks>
        /// <returns>The column the values are asked for in.</returns>
        public ValuesColumn Restate(string given, string named, IList<Field> fields, IList<string> filled, IList<Hsv> colors)
        {
            if (_values == null)
            {
                _values = new ValuesColumn(Width, named);
                _values.Form.Windows = _windows;
            }

            _values.Title = named;

            if (!string.IsNullOrEmpty(_given))
            {
                var was = Kept(_given);
                was.Clear();
                _values.Form.Spell(was);
            }

            _given = given ?? "";

            Dictionary<string, string> kept = null;
            if (_given.Length > 0)
                _entered.TryGetValue(_given, out kept);

            var filling = new List<string>();
            for (var i = 0; i < fields.Count; i++)
            {
                string said;
                if (kept != null)
                    filling.Add(kept.TryGetValue(FieldSet.Key(fields[i], i), out said) ? said : "");
                else
                    filling.Add(i < filled.Count ? filled[i] : "");
            }

            _values.Form.Show(fields, filling, colors, _texts);
            _values.Why = "";
            return _values;
        }

        /// <summary>Lays out the page showing: the way back where there is a page before it, its columns, the values, and the way on where there is a page after.</summary>
        /// <remarks>
        /// A page other than the one showing before is turned to. Going on, the page being left slides out to the left and the
        /// column the new page follows on from folds into the way back as it goes; going back, the page coming back slides in
        /// from the left and that column comes out of the way back, shown only as wide as it has grown.
        /// </remarks>
        public void Show()
        {
            var was = _shownPage;
            var turned = was >= 0 && was != Page;

            _old.Clear();
            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (child != _next && child != _values)
                    _old.Add(child == _reveal ? _reveal.Content : child);
            }

            Settle();

            var onward = Onward != null;
            if (!turned && was >= 0 && onward != _onward)
                Squeeze(onward);
            else if (_squeeze < 0)
                _onward = onward;

            Children.Clear();

            Element folding = was == 0 && Page > 0 && Page - 1 < _stages.Count ? _stages[Page - 1] : null;
            Element unfolding = Page == 0 && was > 0 && was - 1 < _stages.Count ? _stages[was - 1] : null;

            if (folding != null)
                Fold(folding);
            else if (unfolding != null)
                Unfold(unfolding);
            else
                Lay();

            _old.Clear();
            _shownPage = Page;
        }

        /// <summary>Moves a page turning, or room being made for the way on, on by a tick.</summary>
        /// <remarks>
        /// Every tick only while either is under way, which is a third of a second. A page turning moves how the strip is drawn
        /// and resizes one panel; room being made resizes the last column and the way on.
        /// </remarks>
        public void Tick()
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
            Slid(_slideFrom + (_slideTo - _slideFrom) * eased);
            _resized.Placement = Fixed.Wide(_resizedFrom + (_resizedTo - _resizedFrom) * eased);
        }

        /// <summary>Gives the keyboard to the box of the column at <paramref name="opens" />, the first time its page is shown.</summary>
        /// <remarks>Once a page, so a page gone back to is where it was left rather than where it opened.</remarks>
        public void Arrive(int opens, InputRoot input)
        {
            if (_arrived.Contains(Page))
                return;

            _arrived.Add(Page);
            if (input == null || opens < Page || opens >= _stages.Count || !_stages[opens].CanNarrow)
                return;

            input.Focus(_stages[opens].Narrow);
        }

        /// <summary>The page as it stands: the way back where there is a page before it, its columns, the values, and the way on.</summary>
        void Lay()
        {
            if (Page > 0)
            {
                _fold.Placement = Fixed.Wide(FoldWidth);
                Children.Add(_fold);
            }

            Columns(_stages.Count);
            After();
        }

        /// <summary>The page's own columns up to the one at <paramref name="end" />, each as wide as the page has them.</summary>
        void Columns(int end)
        {
            var width = Width;
            var last = _stages.Count - 1;
            for (var i = Page; i < end && i <= last; i++)
            {
                _stages[i].Width = _onward && i == last && _squeeze < 0 ? width - FoldWidth - Gap : width;
                Children.Add(_stages[i]);
            }
        }

        /// <summary>The values after the columns where they are showing, and the way on where there is one.</summary>
        void After()
        {
            if (ShowsValues && _values != null)
            {
                _values.Width = Width;
                Children.Add(_values);
            }

            if (_onward)
                Children.Add(_next);
        }

        /// <summary>Turns forward: the page being left slides out to the left, and <paramref name="folding" /> folds into the way back.</summary>
        /// <remarks>
        /// The column folds as an empty panel. What was in it is answered, and a column's rows squeezed narrower on every tick
        /// would be words wrapping and measured again all the way down.
        /// </remarks>
        void Fold(Element folding)
        {
            var across = 0f;
            var before = true;
            var from = FoldWidth;
            for (var i = 0; i < _old.Count; i++)
            {
                if (_old[i] == folding)
                {
                    from = Across(folding);
                    _fold.Placement = Fixed.Wide(from);
                    Children.Add(_fold);
                    before = false;
                    continue;
                }

                Children.Add(_old[i]);
                _leaving.Add(_old[i]);
                if (before)
                    across += Across(_old[i]) + Gap;
            }

            Columns(_stages.Count);
            After();
            Slide(0f, -across, _fold, from, FoldWidth);
        }

        /// <summary>Turns back: the page coming back slides in from the left, and <paramref name="unfolding" /> comes out of the way back.</summary>
        void Unfold(Element unfolding)
        {
            var width = Width;
            var across = 0f;
            var before = true;
            var last = _stages.Count - 1;
            for (var i = Page; i <= last; i++)
            {
                var column = _stages[i];
                column.Width = _onward && i == last && _squeeze < 0 ? width - FoldWidth - Gap : width;

                if (column == unfolding)
                {
                    _reveal.Content = column;
                    _reveal.Placement = Fixed.Wide(FoldWidth);
                    Children.Add(_reveal);
                    before = false;
                    continue;
                }

                Children.Add(column);
                if (before)
                    across += column.Width + Gap;
            }

            After();

            for (var i = 0; i < _old.Count; i++)
            {
                if (_old[i] == _fold || _stages.Contains(_old[i] as StageColumn))
                    continue;

                Children.Add(_old[i]);
                _leaving.Add(_old[i]);
            }

            Slide(-across, 0f, _reveal, FoldWidth, Across(unfolding));
        }

        /// <summary>Starts the strip sliding from <paramref name="from" /> to <paramref name="to" />, and <paramref name="resized" /> growing or shrinking with it.</summary>
        void Slide(float from, float to, Element resized, float resizedFrom, float resizedTo)
        {
            _slide = 0;
            _slideFrom = from;
            _slideTo = to;
            _resized = resized;
            _resizedFrom = resizedFrom;
            _resizedTo = resizedTo;
            Slid(from);
        }

        /// <summary>Ends a page turning where it is going, taking away what it left behind.</summary>
        void Settle()
        {
            if (_slide < 0)
                return;

            _slide = -1;
            for (var i = 0; i < _leaving.Count; i++)
                Children.Remove(_leaving[i]);

            _leaving.Clear();
            _resized.Placement = Fixed.Wide(_resizedTo);
            Slid(0f);

            // The column that came back stands on its own where its window stood, looking the same.
            if (_resized == _reveal)
            {
                var at = Children.IndexOf(_reveal);
                var column = _reveal.Content;
                _reveal.Content = null;
                Children.Remove(_reveal);
                if (at >= 0 && column != null)
                    Children.Insert(at, column);
            }

            _resized = null;
        }

        /// <summary>How wide a column of the strip is, from what it was made to be rather than from a measure.</summary>
        static float Across(Element column)
        {
            var stage = column as StageColumn;
            if (stage != null)
                return stage.Width;

            var values = column as ValuesColumn;
            return values != null ? values.Width : column.DesiredSize.X;
        }

        /// <summary>Quick to leave and slow to arrive, so the eye has where it is going before it stops.</summary>
        static float Eased(int step)
        {
            var t = (float)step / SlideTicks;
            return 1f - (1f - t) * (1f - t) * (1f - t);
        }

        void Slid(float by)
        {
            var moved = by == 0f ? RenderTransform.Identity : RenderTransform.Moved(new Vector2(by, 0f));
            for (var i = 0; i < Children.Count; i++)
                Children[i].RenderTransform = moved;
        }

        /// <summary>Starts making room for the way on at the right of the last column, or taking it away.</summary>
        void Squeeze(bool opening)
        {
            _onward = opening;
            _squeeze = 0;
            _squeezeFrom = opening ? 0f : FoldWidth;
            _squeezeTo = opening ? FoldWidth : 0f;
            Squeezed(_squeezeFrom);
        }

        /// <summary>One tick of making room for the way on, or of taking it away.</summary>
        void Squeezing()
        {
            _squeeze++;
            var done = _squeeze >= SlideTicks;
            Squeezed(done ? _squeezeTo : _squeezeFrom + (_squeezeTo - _squeezeFrom) * Eased(_squeeze));

            if (!done)
                return;

            _squeeze = -1;
            if (!_onward)
                Children.Remove(_next);
        }

        void Squeezed(float room)
        {
            if (_stages.Count <= Page)
                return;

            if (_next.Parent == null)
                Children.Add(_next);

            _next.Placement = Fixed.Wide(room);
            _stages[_stages.Count - 1].Width = Width - (room > 0f ? room + Gap : 0f);
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

        /// <summary>A column the whole height of the strip, with nothing on it but the way it turns.</summary>
        static VStack Folded(string symbol, string tip)
        {
            return new VStack
            {
                Style = Styles.Fold,
                Placement = Fixed.Wide(FoldWidth),
                Tip = Tip.For(null, tip),
                Children = { new Glyph { Symbol = symbol, Style = Styles.Waits, Placement = Flex.Grow } }
            };
        }

        /// <summary>A window a column is shown through as it comes out of the way back, only as wide as it has grown.</summary>
        /// <remarks>The column keeps the width it will have on its own, so nothing in it is measured again as the window widens.</remarks>
        sealed class Cut : ContentControl
        {
            /// <inheritdoc />
            protected override bool Clips => true;

            /// <inheritdoc />
            protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
            {
                if (Content == null)
                    return Vector2.Zero;

                Content.Measure(g, new Vector2(float.PositiveInfinity, available.Y));
                return new Vector2(0f, Content.DesiredSize.Y);
            }

            /// <inheritdoc />
            protected override void ArrangeContent(RectangleF bounds)
            {
                if (Content != null)
                    Content.Arrange(new RectangleF(bounds.X, bounds.Y, Content.DesiredSize.X, bounds.Height));
            }
        }
    }
}
