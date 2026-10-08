using System;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Styling;
using Lumel.Viewing;
using Lumel.Windows;
using VRage.Input;
using VRageMath;

namespace Lumel
{
    /// <summary>Anything that occupies a rectangle and draws in it.</summary>
    public abstract partial class Element
    {
        /// <summary>Room along an axis that stands for no limit at all, as a scroll gives its content.</summary>
        /// <remarks>Finite, because a stack subtracts from what it was offered as it goes.</remarks>
        public const float Unbounded = 1000000f;

        Action<Element, IPlacement> _admit;
        Align _align;
        bool _arrangeDirty = true;
        RectangleF _arrangedTo;
        ICommand _command;
        Action _commandChanged;
        Collection _contents;
        bool _disabled;
        bool _focused;
        bool _hidden;
        bool _hot;
        Action<Element, int> _joined;
        bool _measureDirty = true;
        Vector2 _measuredFor;
        Element _parent;
        WindowLayer _layer;
        IPlacement _placement;
        Action _placementChanged;
        Action<Element, int> _released;
        Action<Element> _replaced;
        IStyle _style;
        RenderTransform _transform;

        /// <summary>Set when this is added to another element's children. Null while it is loose.</summary>
        public Element Parent => _parent;

        /// <summary>Where this opens windows: the layer of the window it is in, or the layer over the screen it is on.</summary>
        /// <remarks>
        /// Found by climbing to the top of the tree this is in, which is an open window or the screen a layer covers, so
        /// nothing that opens a window has to be handed the layer. Null while this is loose, or in a window that is closed.
        /// </remarks>
        public WindowLayer Windows
        {
            get
            {
                for (var at = this; at != null; at = at._parent)
                {
                    if (at._layer != null)
                        return at._layer;
                }

                return null;
            }
        }

        /// <summary>The layer this is the top of, as a screen it covers or a window open in it; null for anything else.</summary>
        /// <remarks>Set by the layer, through <see cref="WindowLayer.Covers" /> and <see cref="WindowLayer.Open" />.</remarks>
        public WindowLayer Layer
        {
            get { return _layer; }
            set { _layer = value; }
        }

        /// <summary>Where this sits in the room its holder gives it; <see cref="Lumel.Align.Default" /> leaves that to the holder.</summary>
        public Align Align
        {
            get { return _align; }
            set
            {
                if (_align == value)
                    return;

                _align = value;
                InvalidateArrange();
            }
        }

        /// <summary>How whatever holds this is asked to place it, or null for the holder's own default.</summary>
        /// <remarks>Throws, leaving the placement as it was, when the holder cannot place this that way.</remarks>
        public IPlacement Placement
        {
            get { return _placement; }
            set
            {
                if (_placement == value)
                    return;

                if (_parent != null && _parent._admit != null)
                    _parent._admit(this, value);

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
                StateChanged();
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
                StateChanged();
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
                    _commandChanged = StateChanged;

                if (_command != null)
                    _command.CanExecuteChanged -= _commandChanged;

                _command = value;

                if (_command != null)
                    _command.CanExecuteChanged += _commandChanged;

                StateChanged();
            }
        }

        /// <summary>Whether this can hold the keyboard.</summary>
        public bool Focusable { get; set; }

        /// <summary>Whether this holds the keyboard right now.</summary>
        /// <remarks>Kept by <see cref="OnFocus" /> and <see cref="OnBlur" />, which the <see cref="InputRoot" /> this sits under calls.</remarks>
        public bool IsFocused => _focused;

        /// <summary>How this looks, applied onto its own properties now and again whenever its state changes.</summary>
        /// <remarks>
        ///     A style writes only what it sets. A property it sets and a place sets as well is kept only until a change
        ///     of state applies the style again; one it leaves alone stays as the place set it. Replacing the style
        ///     takes back what the old one set first.
        /// </remarks>
        public IStyle Style
        {
            get { return _style; }
            set
            {
                if (_style == value)
                    return;

                if (_style != null)
                    _style.Withdraw(this);

                _style = value;
                StateChanged();
            }
        }

        /// <summary>What this says while the pointer rests on it, or null for nothing.</summary>
        public Tip Tip { get; set; }

        /// <summary>What of this lets presses, the hover and the keyboard pass through to what is beneath it.</summary>
        public InputTransparency InputTransparency { get; set; }

        /// <summary>How thick <see cref="Edge" /> is drawn.</summary>
        public float EdgeThickness { get; set; }

        /// <summary>How this is drawn and pressed away from where layout put it, which layout never sees.</summary>
        public RenderTransform RenderTransform
        {
            get { return _transform; }
            set { _transform = value; }
        }

        /// <summary>Whether this is shown, which is also whether it takes part in layout at all.</summary>
        /// <remarks>Containers skip an element that is not visible completely, spacing included.</remarks>
        public bool IsVisible
        {
            get { return !_hidden; }
            set
            {
                if (_hidden == !value)
                    return;

                _hidden = !value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        ///     Room held clear inside <see cref="Bounds" />, which content is measured and placed within: the padding of an
        ///     element that has one.
        /// </summary>
        protected virtual Thickness Inset => default(Thickness);

        /// <summary>Whether what this draws inside it is cut to its bounds.</summary>
        /// <remarks>An element that must clip to work answers true itself, so nothing outside it can turn that off.</remarks>
        protected virtual bool Clips => false;

        /// <summary>What the last measure asked for, padding included.</summary>
        public Vector2 DesiredSize { get; private set; }

        /// <summary>What the last arrange handed over.</summary>
        public RectangleF Bounds { get; private set; }

        /// <summary>What is left of <see cref="Bounds" /> once the padding is taken out.</summary>
        public RectangleF ContentBounds { get; private set; }

        /// <summary>Whether anything inside this may be reached at all.</summary>
        protected virtual bool Passes => true;

        /// <summary>Whether this handles the pointer itself, and so answers it with no command or tip.</summary>
        protected virtual bool TakesPointer => false;

        /// <summary>
        ///     Whether the pointer and keyboard reach this through an <see cref="InputRoot" />: it has a command, can hold
        ///     the keyboard, or handles the pointer itself.
        /// </summary>
        public bool TakesInput => _command != null || Focusable || TakesPointer;

        /// <summary>How many children this has, which <see cref="GetVisualChild" /> hands out in the order they are drawn.</summary>
        public virtual int VisualChildCount => _contents == null ? 0 : _contents.Count;

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

            if (_hidden)
            {
                DesiredSize = Vector2.Zero;
                return;
            }

            var inset = Inset;
            var inner = new Vector2(
                available.X - inset.Horizontal,
                available.Y - inset.Vertical);

            if (inner.X < 0f)
                inner.X = 0f;
            if (inner.Y < 0f)
                inner.Y = 0f;

            var content = MeasureContent(g, inner);
            DesiredSize = new Vector2(content.X + inset.Horizontal, content.Y + inset.Vertical);
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
            Bounds = bounds;

            var inset = Inset;
            var width = bounds.Width - inset.Horizontal;
            var height = bounds.Height - inset.Vertical;

            ContentBounds = new RectangleF(
                bounds.X + inset.Left,
                bounds.Y + inset.Top,
                width > 0f ? width : 0f,
                height > 0f ? height : 0f);

            ArrangeContent(ContentBounds);
        }

        /// <summary>
        ///     Draws the background, then the content, with the clip in place if one was asked for, all under the render
        ///     transform.
        /// </summary>
        /// <remarks>Not virtual, so that no subclass can get between a push and its pop.</remarks>
        public void Draw(IGraphics g)
        {
            if (_hidden)
                return;

            if (_transform.IsIdentity)
            {
                DrawBox(g);
                return;
            }

            g.PushTransform(_transform, new Vector2(Bounds.X, Bounds.Y));
            DrawBox(g);
            g.PopTransform();
        }

        /// <summary>Where <paramref name="point" />, as drawn, lies in the layout this was placed by.</summary>
        /// <remarks>The reverse of <see cref="RenderTransform" />, so a press lands on what is drawn under it.</remarks>
        public Vector2 Local(Vector2 point)
        {
            return _transform.IsIdentity ? point : _transform.Undo(point, new Vector2(Bounds.X, Bounds.Y));
        }

        void DrawBox(IGraphics g)
        {
            if (Background.A > 0)
                g.Fill(Bounds, Background);

            var edge = IsHot && HighlightEdge.A > 0 ? HighlightEdge : Edge;
            if (edge.A > 0)
                Outline(g, Bounds, edge, EdgeThickness > 0f ? EdgeThickness : 1f);

            if (!Clips)
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
        ///     moved by its <see cref="RenderTransform" /> is drawn outside its parent and can be pressed there.
        /// </remarks>
        public Element Under(Vector2 point)
        {
            if (_hidden)
                return null;

            point = Local(point);

            if (Clips && !Holds(point))
                return null;

            if ((InputTransparency & InputTransparency.Children) == 0)
            {
                var inner = UnderContent(point);
                if (inner != null)
                    return inner;
            }

            return (InputTransparency & InputTransparency.Self) == 0 && TakesInput && Hits(point) ? this : null;
        }

        /// <summary>The deepest element at <paramref name="point" /> with something to say, or null.</summary>
        /// <remarks>
        ///     Separate from <see cref="Under" />, because what a press reaches and what the pointer is resting on
        ///     are different questions: a light inside a row says what it means while a press on it opens the row.
        ///     Input transparency does not hide a tip.
        /// </remarks>
        public Element SayingAt(Vector2 point)
        {
            if (_hidden)
                return null;

            point = Local(point);
            if (!Holds(point))
                return null;

            for (var i = VisualChildCount - 1; i >= 0; i--)
            {
                var found = GetVisualChild(i).SayingAt(point);
                if (found != null)
                    return found;
            }

            return TipAt(point) != null ? this : null;
        }

        /// <summary>What a wheel turned at <paramref name="point" /> moves: the innermost scroll that holds the point and can move, or null.</summary>
        /// <remarks>
        ///     Separate from <see cref="Under" />, because a wheel moves a list wherever the pointer is on it, where a press
        ///     reaches only what takes one: the words of a page take no press, and the page is still what the wheel turns.
        /// </remarks>
        public IScrolls WheelAt(Vector2 point)
        {
            if (_hidden)
                return null;

            point = Local(point);
            if (!Holds(point))
                return null;

            for (var i = VisualChildCount - 1; i >= 0; i--)
            {
                var found = GetVisualChild(i).WheelAt(point);
                if (found != null)
                    return found;
            }

            var scrolls = this as IScrolls;
            return scrolls != null && scrolls.CanScroll ? scrolls : null;
        }

        /// <summary>What this says while the pointer rests on it at <paramref name="point" />: its <see cref="Tip" />, unless it says something different in different places.</summary>
        /// <remarks>Asked while the pointer rests on this, so whatever an override hands back is made once and kept, as a tip always is.</remarks>
        public virtual Tip TipAt(Vector2 point)
        {
            return Tip;
        }

        /// <summary>Whether <paramref name="point" /> is inside <see cref="Bounds" />.</summary>
        public bool Holds(Vector2 point)
        {
            return point.X >= Bounds.X && point.X <= Bounds.X + Bounds.Width
                                       && point.Y >= Bounds.Y && point.Y <= Bounds.Y + Bounds.Height;
        }

        /// <summary>
        ///     Whether a press or a hover at <paramref name="point" />, where no child took it, reaches this: anywhere it
        ///     holds, unless it says otherwise.
        /// </summary>
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
            if (_hidden)
                return false;

            // The point as each element above this sees it, from the outermost in, since each one's render
            // transform moves everything inside it.
            var depth = 0;
            for (var above = Parent; above != null; above = above.Parent)
                depth++;

            for (var level = depth; level > 0; level--)
            {
                var above = Parent;
                for (var i = 1; i < level; i++)
                    above = above.Parent;

                if (!above.IsVisible || !above.Passes)
                    return false;

                point = above.Local(point);
                if (above.Clips && !above.Holds(point))
                    return false;
            }

            return Holds(Local(point));
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
        protected static Element OnTop(Collection children, Vector2 point)
        {
            for (var i = children.Count - 1; i >= 0; i--)
            {
                var found = children[i].Under(point);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>
        ///     Told that a press has begun on this at <paramref name="point" />, the <paramref name="presses" />th in quick
        ///     succession, with <paramref name="shift" /> and <paramref name="ctrl" /> saying whether those keys were held.
        /// </summary>
        /// <remarks>The pointer's moves and its release come to this until it is released, wherever they are.</remarks>
        public virtual void OnPointerDown(Vector2 point, int presses, bool shift, bool ctrl)
        {
        }

        /// <summary>Told that the pointer has moved to <paramref name="point" /> during a press begun on this.</summary>
        public virtual void OnPointerMove(Vector2 point)
        {
        }

        /// <summary>Told that a press begun on this has been released at <paramref name="point" />.</summary>
        public virtual void OnPointerUp(Vector2 point)
        {
        }

        /// <summary>Offered a key while this holds the keyboard, answering whether it took it.</summary>
        /// <remarks>A key not taken goes to the <see cref="InputRoot" />, which moves focus on Tab and lets go of it on Escape.</remarks>
        public virtual bool OnKey(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            return false;
        }

        /// <summary>Given a typed character while this holds the keyboard.</summary>
        public virtual void OnText(char c)
        {
        }

        /// <summary>Told that this now holds the keyboard.</summary>
        public void OnFocus()
        {
            if (_focused)
                return;

            _focused = true;
            StateChanged();
        }

        /// <summary>Told that this no longer holds the keyboard.</summary>
        public void OnBlur()
        {
            if (!_focused)
                return;

            _focused = false;
            StateChanged();
        }

        /// <summary>The child at <paramref name="index" />, counted in the order the children are drawn.</summary>
        public virtual Element GetVisualChild(int index)
        {
            return _contents[index];
        }

        /// <summary>Applies the style of this and of everything inside it again, so that a change to a style shows.</summary>
        public void Restyle()
        {
            StateChanged();

            for (var i = 0; i < VisualChildCount; i++)
                GetVisualChild(i).Restyle();
        }

        /// <summary>Applies this element's style again, for a control whose own state has just changed.</summary>
        protected void StateChanged()
        {
            _style?.Apply(this);
        }

        void PlacementChanged()
        {
            InvalidateMeasure();

            if (_parent == null)
                return;

            if (_parent._replaced != null)
                _parent._replaced(this);

            _parent.InvalidateMeasure();
        }
    }
}