using System;
using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Layout;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>Rows one under the next, any of which can be carried by its grip to another place in the list, with the rows it carries.</summary>
    /// <remarks>Every row is an <see cref="ICarried" />; the rows not carried slide aside to make room where the carried ones would land.</remarks>
    // The rows are the host's own elements, so Lumel inspected alone holds no type that is both an element and carried.
    // ReSharper disable SuspiciousTypeConversion.Global
    public class CarryList : VStack
    {
        readonly List<float> _wanted = new List<float>();
        readonly Dictionary<ICarried, Action<Vector2>> _takes = new Dictionary<ICarried, Action<Vector2>>();
        readonly Dictionary<Element, float> _landed = new Dictionary<Element, float>();

        int _carrying = -1;
        int _span;
        IList<int> _allowed;
        float _grabbedAt;
        float _moved;

        /// <summary>Asked when a row is taken: the places it may land, each as how many rows not carried would be above it, or null for anywhere.</summary>
        public Func<int, IList<int>> Landings { get; set; }

        /// <summary>Whether a row is being carried.</summary>
        public bool IsCarrying => _carrying >= 0;

        /// <summary>The row being carried, by its place in the list, or -1 while none is.</summary>
        protected int CarryingAt => _carrying;

        /// <summary>Told, when a row is dropped somewhere new, where it came from as a row, and how many of the rows it was not carrying are above it now.</summary>
        public Action<int, int> Moves { get; set; }

        /// <summary>Moves every row a little closer to where it should be drawn; the row being carried follows the pointer exactly.</summary>
        /// <remarks>Called every frame by whoever holds the list.</remarks>
        public void Slide()
        {
            for (var i = 0; i < Children.Count; i++)
            {
                var row = Children[i];
                var shift = row.RenderTransform.Translate.Y;
                var carried = Carried(i);
                var target = carried ? _moved : _wanted[i];

                if (carried)
                    shift = target;
                else
                    shift += (target - shift) * 0.35f;

                if (shift < 0.5f && shift > -0.5f && target == 0f)
                    shift = 0f;

                row.RenderTransform = RenderTransform.Moved(new Vector2(0f, shift));
            }
        }

        /// <summary>Lays the rows out, and keeps each one drawn where a drop left it, to slide home from there.</summary>
        protected override void ArrangeContent(RectangleF bounds)
        {
            base.ArrangeContent(bounds);

            if (_landed.Count == 0)
                return;

            foreach (var child in Children)
            {
                float seen;
                if (_landed.TryGetValue(child, out seen))
                    child.RenderTransform = RenderTransform.Moved(new Vector2(0f, seen - child.Bounds.Y));
            }

            _landed.Clear();
        }

        /// <summary>Draws the rows standing in the list, then any carried rows over them, with what <see cref="DrawBeside" /> adds to each.</summary>
        protected override void DrawContent(IGraphics g)
        {
            for (var i = 0; i < Children.Count; i++)
            {
                if (!Carried(i))
                    Children[i].Draw(g);
            }

            DrawBeside(g, false);
            if (_carrying < 0)
                return;

            for (var i = _carrying; i <= _carrying + _span; i++)
                Children[i].Draw(g);

            DrawBeside(g, true);
        }

        /// <summary>Draws what goes with the rows standing in the list, or with the rows being carried, as <paramref name="carried" /> says; nothing here.</summary>
        protected virtual void DrawBeside(IGraphics g, bool carried)
        {
        }

        /// <summary>Whether the row at <paramref name="index" /> is being carried: the one taken, or one it carries.</summary>
        protected bool Carried(int index)
        {
            return _carrying >= 0 && index >= _carrying && index <= _carrying + _span;
        }

        /// <inheritdoc />
        protected override void Joined(Element child, int index)
        {
            base.Joined(child, index);

            _wanted.Insert(index, 0f);

            var row = (ICarried)child;
            Action<Vector2> take = point => Take(row, point);
            _takes[row] = take;
            row.Grip.Taken += take;
            row.Grip.Carried += Carry;
            row.Grip.Dropped += Drop;
        }

        /// <inheritdoc />
        protected override void Released(Element child, int index)
        {
            base.Released(child, index);
            _wanted.RemoveAt(index);

            var row = (ICarried)child;
            Action<Vector2> take;
            if (_takes.TryGetValue(row, out take))
                row.Grip.Taken -= take;

            _takes.Remove(row);
            row.Grip.Carried -= Carry;
            row.Grip.Dropped -= Drop;
        }

        void Take(ICarried row, Vector2 point)
        {
            _landed.Clear();
            _carrying = Children.IndexOf((Element)row);
            _span = Math.Max(0, Math.Min(row.Carries, Children.Count - 1 - _carrying));
            var landings = Landings;
            _allowed = landings?.Invoke(_carrying);
            _grabbedAt = FromScreen(point).Y;
            _moved = 0f;
            row.IsCarried = true;
            for (var i = _carrying; i <= _carrying + _span; i++)
                _wanted[i] = 0f;
        }

        void Carry(Vector2 point)
        {
            if (_carrying < 0)
                return;

            _moved = FromScreen(point).Y - _grabbedAt;
            MakeRoom(LandingFor(_moved));
        }

        void Drop(Vector2 point)
        {
            if (_carrying < 0)
                return;

            var from = _carrying;
            var to = LandingFor(_moved);
            var carried = Children[from];

            _carrying = -1;
            _moved = 0f;
            ((ICarried)carried).IsCarried = false;

            for (var i = 0; i < _wanted.Count; i++)
                _wanted[i] = 0f;

            if (to == from)
                return;

            var landing = Landing(from, to);
            for (var i = 0; i < Children.Count; i++)
            {
                var row = Children[i];
                var inSpan = i >= from && i <= from + _span;
                _landed[row] = inSpan
                    ? landing + row.Bounds.Y - carried.Bounds.Y
                    : row.Bounds.Y + row.RenderTransform.Translate.Y;

                if (inSpan)
                    row.RenderTransform = RenderTransform.Moved(new Vector2(0f, _landed[row] - row.Bounds.Y));
            }

            var moves = Moves;
            if (moves != null)
                moves(from, to);
        }

        void MakeRoom(int to)
        {
            var height = 0f;
            for (var i = _carrying; i <= _carrying + _span; i++)
                height += Children[i].DesiredSize.Y + Spacing;

            for (var i = 0; i < Children.Count; i++)
            {
                if (Carried(i))
                    continue;

                // Counted among the rows not carried, as landings are.
                var among = i < _carrying ? i : i - _span - 1;
                var shift = 0f;
                if (_carrying < to && among >= _carrying && among < to)
                    shift = -height;
                else if (_carrying > to && among >= to && among < _carrying)
                    shift = height;

                _wanted[i] = shift;
            }
        }

        int LandingFor(float moved)
        {
            var carried = Children[_carrying].Bounds;
            var middle = carried.Y + carried.Height * 0.5f + moved;

            var index = 0;
            for (var i = 0; i < Children.Count; i++)
            {
                if (Carried(i))
                    continue;

                var bounds = Children[i].Bounds;
                if (middle > bounds.Y + bounds.Height * 0.5f)
                    index++;
            }

            return Allowed(index);
        }

        /// <summary>The place nearest <paramref name="index" /> the carried row may land, or <paramref name="index" /> where it may land anywhere.</summary>
        int Allowed(int index)
        {
            if (_allowed == null || _allowed.Count == 0)
                return index;

            var best = _allowed[0];
            for (var i = 1; i < _allowed.Count; i++)
            {
                if (Math.Abs(_allowed[i] - index) < Math.Abs(best - index))
                    best = _allowed[i];
            }

            return best;
        }

        float Landing(int from, int to)
        {
            var top = Children[0].Bounds.Y;
            var placed = 0;

            for (var i = 0; i < Children.Count && placed < to; i++)
            {
                if (i >= from && i <= from + _span)
                    continue;

                top += Children[i].DesiredSize.Y + Spacing;
                placed++;
            }

            return top;
        }
    }
}
