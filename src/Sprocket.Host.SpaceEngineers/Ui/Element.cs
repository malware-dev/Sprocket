using System;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Styling;
using VRage.Input;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui
{
    /// <summary>Anything that occupies a rectangle and draws in it.</summary>
    public abstract class Element
    {
        /// <summary>Room along an axis that stands for no limit at all, as a scroll gives its content.</summary>
        /// <remarks>Finite, because a stack subtracts from what it was offered as it goes.</remarks>
        public const float Unbounded = 1000000f;

        bool _arrangeDirty = true;
        RectangleF _arrangedTo;
        bool _collapsed;

        bool _measureDirty = true;

        ICommand _command;

        /// <summary>Held so that listening to a command allocates once in this element's life.</summary>
        Action _commandChanged;

        bool _disabled;
        bool _focused;
        bool _hot;

        Vector2 _measuredFor;
        Vector2 _offset;
        Thickness _padding;
        IPlacement _placement;
        IStyle _style;

        /// <summary>Held so that listening to a placement allocates once in this element's life.</summary>
        Action _placementChanged;

        Deed _press;

        /// <summary>Set when this is added to another element's children. Null while it is loose.</summary>
        public Element Parent { get; internal set; }

        /// <summary>The collection this sits in, so being added somewhere else can take it out of where it was.</summary>
        internal ElementCollection Holder { get; set; }

        /// <summary>How whatever holds this is asked to place it, or null for the holder's own default.</summary>
        /// <remarks>Throws, leaving the placement as it was, when the holder cannot place this that way.</remarks>
        public IPlacement Placement
        {
            get { return _placement; }
            set
            {
                if (_placement == value)
                    return;

                Parent?.Admit(this, value);

                if (_placementChanged == null)
                    _placementChanged = PlacementChanged;

                if (_placement != null)
                    _placement.Changed -= _placementChanged;

                _placement = value;

                if (_placement != null)
                    _placement.Changed += _placementChanged;

                PlacementChanged();
            }
        }

        /// <summary>Painted across <see cref="Bounds" /> before anything else.</summary>
        public Color Background { get; set; }

        /// <summary>A hairline drawn around <see cref="Bounds" />, inside it.</summary>
        public Color Edge { get; set; }

        /// <summary>Painted instead of <see cref="Background" /> while the pointer is resting on this.</summary>
        public Color Highlight { get; set; }

        /// <summary>Drawn instead of <see cref="Edge" /> while the pointer is resting on this.</summary>
        public Color HighlightEdge { get; set; }

        /// <summary>Whether the pointer is on this right now.</summary>
        /// <remarks>Set by whatever owns the screen.</remarks>
        public bool IsHot
        {
            get { return _hot; }
            set
            {
                if (_hot == value)
                    return;

                _hot = value;
                Restyle();
            }
        }

        /// <summary>Whether this can be used now: not switched off, and its command, where it has one, can be carried out.</summary>
        public bool IsEnabled
        {
            get { return !_disabled && (_command == null || _command.CanExecute); }
            set
            {
                if (_disabled == !value)
                    return;

                _disabled = !value;
                Restyle();
            }
        }

        /// <summary>What using this does, or null for nothing.</summary>
        /// <remarks>Carried out when a press begun on this is released on it, while it is enabled.</remarks>
        public ICommand Command
        {
            get { return _command; }
            set
            {
                if (_command == value)
                    return;

                if (_commandChanged == null)
                    _commandChanged = Restyle;

                if (_command != null)
                    _command.CanExecuteChanged -= _commandChanged;

                _command = value;

                if (_command != null)
                    _command.CanExecuteChanged += _commandChanged;

                Restyle();
            }
        }

        /// <summary>Whether this can hold the keyboard.</summary>
        public bool Focusable { get; set; }

        /// <summary>Whether this holds the keyboard right now.</summary>
        /// <remarks>Set by the <see cref="InputRoot" /> this sits under.</remarks>
        public bool IsFocused
        {
            get { return _focused; }
            internal set
            {
                if (_focused == value)
                    return;

                _focused = value;
                Restyle();
            }
        }

        /// <summary>How this looks, applied onto its own properties now and again whenever its state changes.</summary>
        /// <remarks>A property set after the style is kept only until a change of state applies the style again.</remarks>
        public IStyle Style
        {
            get { return _style; }
            set
            {
                if (_style == value)
                    return;

                _style = value;
                Restyle();
            }
        }

        /// <summary>What pressing this means.</summary>
        public Deed Press
        {
            get { return _press; }
            set { _press = value; }
        }

        /// <summary>What to say while the pointer rests on this, as plain words.</summary>
        public string Tip { get; set; }

        /// <summary>What to say while the pointer rests on this, with structure; said in place of <see cref="Tip" /> where it is set.</summary>
        public Rich.Document RichTip { get; set; }

        /// <summary>What of this lets presses, the hover and the keyboard pass through to what is beneath it.</summary>
        public InputTransparency InputTransparency { get; set; }

        /// <summary>Whether a press has business here: something to do.</summary>
        /// <remarks>
        ///     Reads the field rather than <see cref="Press" />, which would copy the whole struct, because
        ///     <see cref="Under" /> asks this of every element it visits on every frame. A tip is not a reason:
        ///     what the pointer rests on to read one is found by <see cref="SayingAt" />.
        /// </remarks>
        bool Answers => _press.Means != Means.Nothing || _command != null || Focusable || TakesPointer;

        /// <summary>Whether this has something to say while the pointer rests on it.</summary>
        bool Says => RichTip != null || !string.IsNullOrEmpty(Tip);

        /// <summary>How thick <see cref="Edge" /> is drawn.</summary>
        public float EdgeThickness { get; set; }

        /// <summary>Whether children and content are cut to <see cref="Bounds" />.</summary>
        public bool ClipsContent { get; set; }

        /// <summary>Room held clear inside <see cref="Bounds" />, which content is measured and placed within.</summary>
        public Thickness Padding
        {
            get { return _padding; }
            set
            {
                if (_padding == value)
                    return;

                _padding = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Moved by this much after being placed, without anything having measured it there.</summary>
        public Vector2 Offset
        {
            get { return _offset; }
            set
            {
                if (_offset.X == value.X && _offset.Y == value.Y)
                    return;

                _offset = value;
                InvalidateArrange();
            }
        }

        /// <summary>Whether this is out of the layout entirely, rather than merely invisible.</summary>
        /// <remarks>Containers skip a collapsed child completely, spacing included.</remarks>
        public bool Collapsed
        {
            get { return _collapsed; }
            set
            {
                if (_collapsed == value)
                    return;

                _collapsed = value;
                InvalidateMeasure();
            }
        }

        /// <summary>What the last measure asked for, padding included.</summary>
        public Vector2 DesiredSize { get; private set; }

        /// <summary>What the last arrange handed over.</summary>
        public RectangleF Bounds { get; private set; }

        /// <summary>What is left of <see cref="Bounds" /> once the padding is taken out.</summary>
        public RectangleF ContentBounds { get; private set; }

        /// <summary>Whether anything inside this may be reached at all.</summary>
        internal virtual bool Passes => true;

        /// <summary>The children this holds in a collection, or null for none.</summary>
        internal ElementCollection Contents { get; set; }

        /// <summary>Whether this handles the pointer itself, and so answers it with no command or tip.</summary>
        protected virtual bool TakesPointer => false;

        /// <summary>Whether the pointer and keyboard reach this through an <see cref="InputRoot" />: it has a command, can hold the keyboard, or handles the pointer itself.</summary>
        public bool TakesInput => _command != null || Focusable || TakesPointer;

        /// <summary>Says this needs measuring again, and everything above it with it.</summary>
        /// <remarks>
        ///     A property that changes how big this wants to be calls this; one that only changes where it
        ///     sits calls <see cref="InvalidateArrange" />; a colour calls neither.
        /// </remarks>
        public void InvalidateMeasure()
        {
            _arrangeDirty = true;

            if (_measureDirty)
                return;

            _measureDirty = true;

            Parent?.InvalidateMeasure();
        }

        /// <summary>Says this needs placing again, without its size having changed.</summary>
        public void InvalidateArrange()
        {
            if (_arrangeDirty)
                return;

            _arrangeDirty = true;

            Parent?.InvalidateArrange();
        }

        /// <summary>Asks how much room this would like, given <paramref name="available" />.</summary>
        public void Measure(IGraphics g, Vector2 available)
        {
            // Safe to stop without asking the children: a child that changed has already dirtied every
            // parent between it and the root.
            if (!_measureDirty && _measuredFor.X == available.X && _measuredFor.Y == available.Y)
                return;

            _measureDirty = false;
            _measuredFor = available;

            if (Collapsed)
            {
                DesiredSize = Vector2.Zero;
                return;
            }

            var inner = new Vector2(
                available.X - Padding.Horizontal,
                available.Y - Padding.Vertical);

            if (inner.X < 0f)
                inner.X = 0f;
            if (inner.Y < 0f)
                inner.Y = 0f;

            var content = MeasureContent(g, inner);
            DesiredSize = new Vector2(content.X + Padding.Horizontal, content.Y + Padding.Vertical);
        }

        /// <summary>Tells this what it got, which may be less than it asked for.</summary>
        public void Arrange(RectangleF bounds)
        {
            if (!_arrangeDirty
                && _arrangedTo.X == bounds.X && _arrangedTo.Y == bounds.Y
                && _arrangedTo.Width == bounds.Width && _arrangedTo.Height == bounds.Height)
                return;

            _arrangeDirty = false;
            _arrangedTo = bounds;

            bounds = new RectangleF(bounds.X + Offset.X, bounds.Y + Offset.Y, bounds.Width, bounds.Height);
            Bounds = bounds;

            var width = bounds.Width - Padding.Horizontal;
            var height = bounds.Height - Padding.Vertical;

            ContentBounds = new RectangleF(
                bounds.X + Padding.Left,
                bounds.Y + Padding.Top,
                width > 0f ? width : 0f,
                height > 0f ? height : 0f);

            ArrangeContent(ContentBounds);
        }

        /// <summary>Draws the background, then the content, with the clip in place if one was asked for.</summary>
        /// <remarks>Not virtual, so that no subclass can get between the clip's push and its pop.</remarks>
        public void Draw(IGraphics g)
        {
            if (Collapsed)
                return;

            var fill = IsHot && Highlight.A > 0 ? Highlight : Background;
            if (fill.A > 0)
                g.Fill(Bounds, fill);

            var edge = IsHot && HighlightEdge.A > 0 ? HighlightEdge : Edge;
            if (edge.A > 0)
                Outline(g, Bounds, edge, EdgeThickness > 0f ? EdgeThickness : 1f);

            if (!ClipsContent)
            {
                DrawContent(g);
                return;
            }

            g.PushClip(Bounds);
            DrawContent(g);
            g.PopClip();
        }

        /// <summary>The element a press or a hover at <paramref name="point" /> reaches, or null for none.</summary>
        /// <remarks>
        ///     An unclipped element asks its children even when it does not hold the point, because a child
        ///     moved by <see cref="Offset" /> is drawn outside its parent and can be pressed there.
        /// </remarks>
        public Element Under(Vector2 point)
        {
            if (Collapsed)
                return null;

            if (ClipsContent && !Holds(point))
                return null;

            if ((InputTransparency & InputTransparency.Children) == 0)
            {
                var inner = UnderContent(point);
                if (inner != null)
                    return inner;
            }

            return (InputTransparency & InputTransparency.Self) == 0 && Answers && Hits(point) ? this : null;
        }

        /// <summary>The deepest element at <paramref name="point" /> with something to say, or null.</summary>
        /// <remarks>
        ///     Separate from <see cref="Under" />, because what a press reaches and what the pointer is resting on
        ///     are different questions: a light inside a row says what it means while a press on it opens the row.
        ///     Input transparency does not hide a tip.
        /// </remarks>
        public Element SayingAt(Vector2 point)
        {
            if (Collapsed || !Holds(point))
                return null;

            for (var i = VisualChildCount - 1; i >= 0; i--)
            {
                var found = GetVisualChild(i).SayingAt(point);
                if (found != null)
                    return found;
            }

            return Says ? this : null;
        }

        /// <summary>Whether <paramref name="point" /> is inside <see cref="Bounds" />.</summary>
        public bool Holds(Vector2 point)
        {
            return point.X >= Bounds.X && point.X <= Bounds.X + Bounds.Width
                                       && point.Y >= Bounds.Y && point.Y <= Bounds.Y + Bounds.Height;
        }

        /// <summary>Whether a press or a hover at <paramref name="point" />, where no child took it, reaches this: anywhere it holds, unless it says otherwise.</summary>
        protected virtual bool Hits(Vector2 point)
        {
            return Holds(point);
        }

        /// <summary>
        ///     Whether this is on show at <paramref name="point" />: it holds it, and nothing above it has
        ///     folded it away, cut it off, or not finished bringing it into view.
        /// </summary>
        /// <remarks>
        ///     Blind to a sibling drawn over this and to popups hanging outside the layout; the caller has to
        ///     have settled those already.
        /// </remarks>
        public bool Reaches(Vector2 point)
        {
            if (Collapsed || !Holds(point))
                return false;

            for (var above = Parent; above != null; above = above.Parent)
            {
                if (above.Collapsed || !above.Passes)
                    return false;

                if (above.ClipsContent && !above.Holds(point))
                    return false;
            }

            return true;
        }

        /// <summary>Draws a hairline just inside <paramref name="bounds" />, as four thin rectangles.</summary>
        protected static void Outline(IGraphics g, RectangleF bounds, Color color, float thickness)
        {
            g.Fill(new RectangleF(bounds.X, bounds.Y, bounds.Width, thickness), color);
            g.Fill(new RectangleF(
                bounds.X, bounds.Y + bounds.Height - thickness, bounds.Width, thickness), color);
            g.Fill(new RectangleF(bounds.X, bounds.Y, thickness, bounds.Height), color);
            g.Fill(new RectangleF(
                bounds.X + bounds.Width - thickness, bounds.Y, thickness, bounds.Height), color);
        }

        /// <summary>How much room this wants.</summary>
        /// <remarks>Must not change anything a later pass reads.</remarks>
        protected abstract Vector2 MeasureContent(IGraphics g, Vector2 available);

        /// <summary>Places whatever is inside.</summary>
        protected virtual void ArrangeContent(RectangleF bounds)
        {
        }

        /// <summary>Draws whatever is inside, background already painted and any clip already pushed.</summary>
        protected abstract void DrawContent(IGraphics g);

        /// <summary>Whatever inside this answers a press at <paramref name="point" />, or null.</summary>
        /// <remarks>
        ///     An override asks in the reverse of the order <see cref="DrawContent" /> draws in, so the thing
        ///     on top is the thing that answers.
        /// </remarks>
        protected virtual Element UnderContent(Vector2 point)
        {
            return null;
        }

        /// <summary>The last of <paramref name="children" /> that answers, which is the one drawn over the rest.</summary>
        /// <remarks>Right only for a container that draws its children in the order they were added.</remarks>
        protected static Element OnTop(ElementCollection children, Vector2 point)
        {
            for (var i = children.Count - 1; i >= 0; i--)
            {
                var found = children[i].Under(point);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>Told that a press has begun on this at <paramref name="point" />, the <paramref name="presses" />th in quick succession.</summary>
        /// <remarks>The pointer's moves and its release come to this until it is released, wherever they are.</remarks>
        protected internal virtual void OnPointerDown(Vector2 point, int presses)
        {
        }

        /// <summary>Told that the pointer has moved to <paramref name="point" /> during a press begun on this.</summary>
        protected internal virtual void OnPointerMove(Vector2 point)
        {
        }

        /// <summary>Told that a press begun on this has been released at <paramref name="point" />.</summary>
        protected internal virtual void OnPointerUp(Vector2 point)
        {
        }

        /// <summary>Offered a key while this holds the keyboard, answering whether it took it.</summary>
        /// <remarks>A key not taken goes to the <see cref="InputRoot" />, which moves focus on Tab and lets go of it on Escape.</remarks>
        protected internal virtual bool OnKey(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            return false;
        }

        /// <summary>Given a typed character while this holds the keyboard.</summary>
        protected internal virtual void OnText(char c)
        {
        }

        /// <summary>How many children this has, which <see cref="GetVisualChild" /> hands out in the order they are drawn.</summary>
        protected internal virtual int VisualChildCount => Contents == null ? 0 : Contents.Count;

        /// <summary>The child at <paramref name="index" />, counted in the order the children are drawn.</summary>
        protected internal virtual Element GetVisualChild(int index)
        {
            return Contents[index];
        }

        /// <summary>Checks that this can place <paramref name="child" /> by <paramref name="placement" />, before it joins or changes.</summary>
        /// <remarks>A container that cannot place it throws.</remarks>
        internal virtual void Admit(Element child, IPlacement placement)
        {
        }

        /// <summary>Told that <paramref name="child" /> has joined this element's children at <paramref name="index" />.</summary>
        internal virtual void Joined(Element child, int index)
        {
        }

        /// <summary>Told that <paramref name="child" /> has left this element's children from <paramref name="index" />.</summary>
        internal virtual void Released(Element child, int index)
        {
        }

        /// <summary>Told that the placement of <paramref name="child" />, one of this element's children, has changed.</summary>
        internal virtual void Replaced(Element child)
        {
        }

        /// <summary>Applies this element's style again, for a control whose own state has just changed.</summary>
        protected void Restyle()
        {
            _style?.Apply(this);
        }

        void PlacementChanged()
        {
            InvalidateMeasure();

            if (Parent == null)
                return;

            Parent.Replaced(this);
            Parent.InvalidateMeasure();
        }
    }
}