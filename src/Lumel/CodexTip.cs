using System;
using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;
using Lumel.Windows;
using VRageMath;

namespace Lumel
{
    /// <summary>
    /// A tip standing edge to edge with the element it explains, which the pointer can move onto: pages of rich text, gone
    /// between through the links in them and back and forward again.
    /// </summary>
    /// <remarks>
    /// The host draws it over everything else with <see cref="Draw(IGraphics, RectangleF)" />, tells it on every frame what
    /// the pointer rests on with <see cref="Hover" />, and hands it every press first through <see cref="Dismiss" />. A page
    /// opening with a heading of the first level shows that heading as its title.
    /// </remarks>
    public partial class CodexTip : VStack
    {
        readonly List<Tip> _pages = new List<Tip>();

        int _at = -1;
        Element _anchor;
        Tip _said;
        TipSide _side;
        bool _sided;
        int _left = -1;
        Element _resting;
        int _restingSince;
        Element _spent;
        WindowLayer _watched;
        bool _keptByKey;

        /// <summary>A closed tip with no pages, which opens after no delay and closes the moment the pointer is off both it and its element.</summary>
        public CodexTip()
        {
            CreateView();
            IsVisible = false;
        }

        /// <summary>Finds the page a link inside the tip leads to, given the link's target, or null where there is none.</summary>
        public Func<string, Tip> Pages { get; set; }

        /// <summary>How long the pointer rests on an element before its tip opens, in the units of the time <see cref="Hover" /> is given.</summary>
        public int Delay { get; set; }

        /// <summary>How long the tip stays open while the pointer is on neither it nor its element, in the units of the time <see cref="Hover" /> is given.</summary>
        public int Grace { get; set; }

        /// <summary>
        /// Whether the tip is kept only while the host's key is held: without the key it is an ordinary tooltip, closing as the
        /// pointer leaves its element and letting the pointer through; with it, the pointer can move onto it.
        /// </summary>
        public bool KeptByKey
        {
            get { return _keptByKey; }
            set
            {
                _keptByKey = value;
                _keepHint.IsVisible = value && _keepHint.Document != null;
            }
        }

        /// <summary>The line at the bottom of every page that says how to keep the tip, shown while <see cref="KeptByKey" /> is set, or null for none.</summary>
        public Document KeepHint
        {
            get { return _keepHint.Document; }
            set
            {
                _keepHint.Document = value;
                _keepHint.IsVisible = _keptByKey && value != null;
            }
        }

        /// <summary>Asks whether the host's keep key is held, or null for a host with none.</summary>
        /// <remarks>Asked only while the tip is open and <see cref="KeptByKey" /> is set.</remarks>
        public Func<bool> KeepKey { get; set; }

        /// <summary>Whether the tip holds the pointer: open, and either always kept or kept now by the host's keep key.</summary>
        public bool IsKept => _anchor != null && (!_keptByKey || KeepKey != null && KeepKey());

        /// <summary>How tall the tip grows at most, in UI units; a page taller than this is cut off at it.</summary>
        public float MaxHeight { get; set; } = float.PositiveInfinity;

        /// <summary>How a page's title looks.</summary>
        public IStyle TitleStyle
        {
            get { return _title.Style; }
            set { _title.Style = value; }
        }

        /// <summary>How a page's words look, the line that says how to keep the tip included.</summary>
        public IStyle BodyStyle
        {
            get { return _body.Style; }
            set
            {
                _body.Style = value;
                _keepHint.Style = value;
            }
        }

        /// <summary>How the steps back and forward look.</summary>
        public IStyle StepStyle
        {
            get { return _back.Style; }
            set
            {
                _back.Style = value;
                _forward.Style = value;
            }
        }

        /// <summary>The symbol of the step back to the page before.</summary>
        public string BackSymbol
        {
            get { return _back.Symbol; }
            set { _back.Symbol = value; }
        }

        /// <summary>The symbol of the step forward to the page stepped back from.</summary>
        public string ForwardSymbol
        {
            get { return _forward.Symbol; }
            set { _forward.Symbol = value; }
        }

        /// <summary>Whether the tip is open beside an element.</summary>
        public bool IsOpen => _anchor != null;

        /// <summary>The element the tip explains, or null while it is closed.</summary>
        public Element Anchor => _anchor;

        /// <summary>The page shown, or null while the tip is closed.</summary>
        public Tip Page => _at >= 0 ? _pages[_at] : null;

        /// <summary>The side of its element the tip stands on, chosen when it is first drawn after opening and kept while its pages change.</summary>
        public TipSide Side => _side;

        /// <summary>Whether there is a page before the one shown to step back to.</summary>
        public bool CanGoBack => _at > 0;

        /// <summary>Whether there is a page stepped back from to step forward to.</summary>
        public bool CanGoForward => _at >= 0 && _at < _pages.Count - 1;

        /// <inheritdoc />
        protected override bool Clips => true;

        /// <summary>Closes the tip whenever a window opens on <paramref name="layer" />, so nothing that opens is covered by it.</summary>
        public void GiveWayTo(WindowLayer layer)
        {
            if (_watched != null)
                _watched.Opened -= GiveWay;

            _watched = layer;

            if (layer != null)
                layer.Opened += GiveWay;
        }

        /// <summary>Opens the tip beside <paramref name="anchor" /> at <paramref name="page" />, with no pages behind it or ahead of it.</summary>
        public void Open(Element anchor, Tip page)
        {
            if (anchor == null)
                throw new ArgumentNullException(nameof(anchor));

            if (page == null)
                throw new ArgumentNullException(nameof(page));

            _anchor = anchor;
            _said = page;
            _sided = false;
            _left = -1;
            _pages.Clear();
            _pages.Add(page);
            _at = 0;
            IsVisible = true;
            Show();
        }

        /// <summary>Closes the tip, if it is open; its element shows it again only once the pointer has rested on something else.</summary>
        public void Close()
        {
            if (_anchor == null)
                return;

            _spent = _anchor;
            _anchor = null;
            _said = null;
            _pages.Clear();
            _at = -1;
            IsVisible = false;
        }

        /// <summary>Shows <paramref name="page" />, dropping every page stepped back from; nothing happens while the tip is closed.</summary>
        public void Go(Tip page)
        {
            if (_anchor == null || page == null)
                return;

            _pages.RemoveRange(_at + 1, _pages.Count - _at - 1);
            _pages.Add(page);
            _at++;
            Show();
        }

        /// <summary>Shows the page the link target <paramref name="id" /> leads to, and answers whether <see cref="Pages" /> found one.</summary>
        public bool Follow(string id)
        {
            var page = Pages?.Invoke(id);
            if (page == null || _anchor == null)
                return false;

            Go(page);
            return true;
        }

        /// <summary>Shows the page before the one shown, where there is one.</summary>
        public void GoBack()
        {
            if (!CanGoBack)
                return;

            _at--;
            Show();
        }

        /// <summary>Shows the page stepped back from, where there is one.</summary>
        public void GoForward()
        {
            if (!CanGoForward)
                return;

            _at++;
            Show();
        }

        /// <summary>
        /// Opens, keeps or closes the tip by where the pointer is: <paramref name="speaker" /> is what it rests on that has
        /// something to say, <paramref name="said" /> what that says, and <paramref name="now" /> the time.
        /// </summary>
        /// <remarks>Called on every frame the host is drawn, with <paramref name="pointer" /> in screen coordinates.</remarks>
        public void Hover(Element speaker, Tip said, Vector2 pointer, int now)
        {
            // Kept while a tip is open too, so an element the pointer reached during the grace has rested from the moment it was reached.
            if (speaker != _resting)
            {
                _resting = speaker;
                _restingSince = now;
            }

            if (_anchor != null)
            {
                Keep(speaker, said, pointer, now);
                if (_anchor != null)
                    return;
            }

            if (speaker != _spent)
                _spent = null;

            if (speaker == null || said == null || speaker == _spent || now - _restingSince < Delay)
                return;

            Open(speaker, said);
        }

        /// <summary>Closes the tip where a press at <paramref name="point" /> does not land on it while it is kept; the press goes on to whatever it reaches either way.</summary>
        public void Dismiss(Vector2 point)
        {
            if (_anchor != null && !Covers(point))
                Close();
        }

        /// <summary>Whether <paramref name="point" /> is on the tip while it holds the pointer, so the pointer reaches the tip there and nothing beneath it.</summary>
        public bool Covers(Vector2 point)
        {
            return IsKept && Holds(point);
        }

        /// <summary>Lays the tip out on the side of its element it stands on, inside <paramref name="area" />, and draws it; nothing while it is closed.</summary>
        public void Draw(IGraphics g, RectangleF area)
        {
            if (_anchor == null)
                return;

            var sized = Placement as ISized;
            var wide = sized != null && !float.IsNaN(sized.Width) ? sized.Width : area.Width;
            var high = Math.Min(MaxHeight, area.Height);

            Measure(g, new Vector2(wide, high));
            var size = new Vector2(wide, Math.Min(DesiredSize.Y, high));

            Arrange(Place(area, size));
            Draw(g);
        }

        /// <summary>Keeps the open tip while the pointer is on it or on its element, and closes it once the pointer has been off both for <see cref="Grace" />.</summary>
        void Keep(Element speaker, Tip said, Vector2 pointer, int now)
        {
            if (_anchor.Windows == null)
            {
                Close();
                return;
            }

            if (Covers(pointer))
            {
                _left = -1;
                return;
            }

            if (_anchor.Reaches(pointer))
            {
                _left = -1;
                if (speaker == _anchor && said != null && said != _said)
                    Open(_anchor, said);

                return;
            }

            if (!IsKept)
            {
                Close();
                return;
            }

            if (_left < 0)
                _left = now;

            if (now - _left >= Grace)
                Close();
        }

        /// <summary>Where the tip stands at <paramref name="size" />: flush against its element on the side chosen, and inside <paramref name="area" />.</summary>
        RectangleF Place(RectangleF area, Vector2 size)
        {
            var bounds = _anchor.Bounds;
            var from = _anchor.ToScreen(new Vector2(bounds.X, bounds.Y));
            var to = _anchor.ToScreen(new Vector2(bounds.X + bounds.Width, bounds.Y + bounds.Height));

            if (!_sided)
            {
                _side = to.X + size.X <= area.X + area.Width ? TipSide.Right
                    : from.X - size.X >= area.X ? TipSide.Left
                    : from.Y - size.Y >= area.Y ? TipSide.Above
                    : TipSide.Below;
                _sided = true;
            }

            float x;
            float y;
            switch (_side)
            {
                case TipSide.Right:
                    x = to.X;
                    y = from.Y;
                    break;
                case TipSide.Left:
                    x = from.X - size.X;
                    y = from.Y;
                    break;
                case TipSide.Above:
                    x = from.X;
                    y = from.Y - size.Y;
                    break;
                default:
                    x = from.X;
                    y = to.Y;
                    break;
            }

            x = Math.Max(area.X, Math.Min(x, area.X + area.Width - size.X));
            y = Math.Max(area.Y, Math.Min(y, area.Y + area.Height - size.Y));
            return new RectangleF(x, y, size.X, size.Y);
        }

        /// <summary>Puts the page shown into the title row and the body, and shows the steps there are somewhere to go with.</summary>
        void Show()
        {
            var page = _pages[_at];
            Document title;
            var body = page.Document.Untitled(out title);
            var titled = title != null;

            _title.Document = title;
            _title.Arguments = page.Arguments;
            _body.Document = body;
            _body.Arguments = page.Arguments;
            _back.IsVisible = CanGoBack;
            _forward.IsVisible = CanGoForward;
            _heading.IsVisible = titled || CanGoBack || CanGoForward;
        }

        void GiveWay(Window opened)
        {
            Close();
        }
    }
}
