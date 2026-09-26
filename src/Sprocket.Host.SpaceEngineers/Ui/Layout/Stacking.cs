using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Layout
{
    /// <summary>Children in a line, each its own size along it, with the room left shared among those that grow.</summary>
    /// <remarks>Inside a scroll along the same axis there is nothing to share, and a child that grows is its natural size.</remarks>
    public abstract class Stacking : Panel
    {
        readonly List<Entry> _entries = new List<Entry>();
        readonly bool _runsAcross;

        bool _sharing;

        float _spacing;

        /// <summary>An empty line, running across when <paramref name="runsAcross" /> is set and down otherwise.</summary>
        protected Stacking(bool runsAcross)
        {
            _runsAcross = runsAcross;
        }

        /// <summary>Blank room left between one child and the next.</summary>
        public float Spacing
        {
            get { return _spacing; }
            set
            {
                if (_spacing == value)
                    return;

                _spacing = value;
                InvalidateMeasure();
            }
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var room = Along(available);
            var roomAcross = Across(available);
            var bounded = room < Unbounded;

            var used = 0f;
            var across = 0f;
            var weight = 0f;
            var placed = false;

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                var entry = _entries[i];
                var grows = bounded && entry.Grow > 0f;

                if (!grows)
                {
                    var left = room - used;
                    child.Measure(g, Make(Given(entry.Along, left > 0f ? left : 0f), Given(entry.Across, roomAcross)));
                }

                if (!child.IsVisible)
                    continue;

                if (placed)
                    used += Spacing;

                placed = true;

                if (grows)
                {
                    weight += entry.Grow;
                    continue;
                }

                used += Given(entry.Along, Along(child.DesiredSize));
                across = Wider(across, Given(entry.Across, Across(child.DesiredSize)));
            }

            _sharing = weight > 0f;

            if (!_sharing)
                return Make(used, across);

            var spare = room - used;
            if (spare < 0f)
                spare = 0f;

            for (var i = 0; i < Children.Count; i++)
            {
                var entry = _entries[i];
                if (!(entry.Grow > 0f))
                    continue;

                var child = Children[i];
                child.Measure(g, Make(spare * entry.Grow / weight, Given(entry.Across, roomAcross)));

                if (child.IsVisible)
                    across = Wider(across, Given(entry.Across, Across(child.DesiredSize)));
            }

            return Make(room, across);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            var room = _runsAcross ? bounds.Width : bounds.Height;

            var used = 0f;
            var weight = 0f;
            var placed = false;

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (!child.IsVisible)
                    continue;

                if (placed)
                    used += Spacing;

                placed = true;

                var entry = _entries[i];
                if (_sharing && entry.Grow > 0f)
                    weight += entry.Grow;
                else
                    used += Given(entry.Along, Along(child.DesiredSize));
            }

            var spare = room - used;
            if (spare < 0f)
                spare = 0f;

            var cursor = _runsAcross ? bounds.X : bounds.Y;
            placed = false;

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (!child.IsVisible)
                    continue;

                if (placed)
                    cursor += Spacing;

                placed = true;

                var entry = _entries[i];
                var size = _sharing && entry.Grow > 0f
                    ? spare * entry.Grow / weight
                    : Given(entry.Along, Along(child.DesiredSize));

                Place(child, entry, cursor, size, bounds);
                cursor += size;
            }
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            for (var i = 0; i < Children.Count; i++)
                Children[i].Draw(g);
        }

        /// <inheritdoc />
        protected override Element UnderContent(Vector2 point)
        {
            return OnTop(Children, point);
        }

        /// <summary>Refuses a child whose placement is not <see cref="ISized" />.</summary>
        protected override void Admit(Element child, IPlacement placement)
        {
            if (placement != null && !(placement is ISized))
                throw new InvalidOperationException(
                    GetType().Name + " cannot place a child by " + placement.GetType().Name + ".");
        }

        /// <inheritdoc />
        protected override void Joined(Element child, int index)
        {
            _entries.Insert(index, Resolve(child.Placement));
        }

        /// <inheritdoc />
        protected override void Released(Element child, int index)
        {
            _entries.RemoveAt(index);
        }

        /// <inheritdoc />
        protected override void Replaced(Element child)
        {
            var index = Children.IndexOf(child);
            if (index >= 0)
                _entries[index] = Resolve(child.Placement);
        }

        /// <summary>Puts a child at <paramref name="at" /> along the line, <paramref name="size" /> long.</summary>
        void Place(Element child, Entry entry, float at, float size, RectangleF bounds)
        {
            if (!_runsAcross)
            {
                child.Arrange(new RectangleF(bounds.X, at, Given(entry.Across, bounds.Width), size));
                return;
            }

            var height = Given(entry.Across, child.DesiredSize.Y);
            var y = height > 0f && height < bounds.Height
                ? bounds.Y + (bounds.Height - height) * 0.5f
                : bounds.Y;

            child.Arrange(new RectangleF(at, y, size, height > 0f ? height : bounds.Height));
        }

        Entry Resolve(IPlacement placement)
        {
            var entry = new Entry { Along = float.NaN, Across = float.NaN };

            var sized = placement as ISized;
            if (sized != null)
            {
                entry.Along = _runsAcross ? sized.Width : sized.Height;
                entry.Across = _runsAcross ? sized.Height : sized.Width;
            }

            var stacked = placement as IStacked;
            if (stacked != null)
                entry.Grow = stacked.Grow;

            return entry;
        }

        float Along(Vector2 size)
        {
            return _runsAcross ? size.X : size.Y;
        }

        float Across(Vector2 size)
        {
            return _runsAcross ? size.Y : size.X;
        }

        Vector2 Make(float along, float across)
        {
            return _runsAcross ? new Vector2(along, across) : new Vector2(across, along);
        }

        /// <summary><paramref name="size" /> where the placement gives one, and <paramref name="otherwise" /> where it is <c>float.NaN</c>.</summary>
        static float Given(float size, float otherwise)
        {
            return float.IsNaN(size) ? otherwise : size;
        }

        static float Wider(float a, float b)
        {
            return a > b ? a : b;
        }

        /// <summary>What a child's placement says, read once when it joins or changes.</summary>
        struct Entry
        {
            public float Along;
            public float Across;
            public float Grow;
        }
    }
}
