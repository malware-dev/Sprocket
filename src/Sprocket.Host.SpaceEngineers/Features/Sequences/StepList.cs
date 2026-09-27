using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    /// <summary>A sequence's steps, one row each, any of which can be carried by its grip to another place in the list.</summary>
    /// <remarks>
    /// While a row is carried, the rows between where it came from and where it would land step aside by its height, so
    /// the place it would take is visibly empty rather than imagined from where the pointer is. They slide rather than
    /// jump, because a row appearing somewhere new reads as the list glitching rather than as the list answering. None
    /// of it moves the layout: carrying only moves how the rows are drawn.
    /// </remarks>
    public class StepList : VStack
    {
        readonly List<float> _wanted = new List<float>();
        readonly Dictionary<StepRow, Action<Vector2>> _takes = new Dictionary<StepRow, Action<Vector2>>();
        readonly Dictionary<Element, float> _landed = new Dictionary<Element, float>();

        int _carrying = -1;
        float _grabbedAt;
        float _moved;

        /// <summary>An empty list.</summary>
        public StepList()
        {
            Spacing = 8f;
        }

        /// <summary>Told, when a row is dropped somewhere new, where it came from and where it landed, both as rows.</summary>
        public Action<int, int> Moves { get; set; }

        /// <summary>Moves every row a little closer to where it should be drawn; the row being carried follows the pointer exactly.</summary>
        /// <remarks>
        /// For whoever holds the list to call on every frame, since sliding is done a frame at a time. Short and eased
        /// out, because it is feedback rather than decoration; a list at rest moves nothing.
        /// </remarks>
        public void Slide()
        {
            for (var i = 0; i < Children.Count; i++)
            {
                var row = (StepRow)Children[i];
                var shift = row.RenderTransform.Translate.Y;
                var target = i == _carrying ? _moved : _wanted[i];

                if (i == _carrying)
                    shift = target;
                else
                    shift += (target - shift) * 0.35f;

                if (shift < 0.5f && shift > -0.5f && target == 0f)
                    shift = 0f;

                row.RenderTransform = RenderTransform.Moved(new Vector2(0f, shift));
            }
        }

        /// <summary>Lays the rows out, and keeps each one drawn where a drop left it, to slide home from there.</summary>
        /// <remarks>
        /// A drop reorders the rows at once, but where the new order puts each one is only known once it has been laid
        /// out, so a row's place on the screen is carried across that and its offset measured again from its new place.
        /// </remarks>
        protected override void ArrangeContent(RectangleF bounds)
        {
            base.ArrangeContent(bounds);

            if (_landed.Count == 0)
                return;

            for (var i = 0; i < Children.Count; i++)
            {
                float seen;
                if (_landed.TryGetValue(Children[i], out seen))
                    Children[i].RenderTransform = RenderTransform.Moved(new Vector2(0f, seen - Children[i].Bounds.Y));
            }

            _landed.Clear();
        }

        /// <inheritdoc />
        protected override void Joined(Element child, int index)
        {
            base.Joined(child, index);

            _wanted.Insert(index, 0f);

            var row = (StepRow)child;
            Action<Vector2> take = delegate(Vector2 point) { Take(row, point); };
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

            var row = (StepRow)child;
            Action<Vector2> take;
            if (_takes.TryGetValue(row, out take))
                row.Grip.Taken -= take;

            _takes.Remove(row);
            row.Grip.Carried -= Carry;
            row.Grip.Dropped -= Drop;
        }

        void Take(StepRow row, Vector2 point)
        {
            _landed.Clear();
            _carrying = Children.IndexOf(row);
            _grabbedAt = point.Y;
            _moved = 0f;
            row.IsCarried = true;
            _wanted[_carrying] = 0f;
        }

        void Carry(Vector2 point)
        {
            if (_carrying < 0)
                return;

            _moved = point.Y - _grabbedAt;
            MakeRoom(LandingFor(_moved));
        }

        void Drop(Vector2 point)
        {
            if (_carrying < 0)
                return;

            var from = _carrying;
            var to = LandingFor(_moved);
            var carried = (StepRow)Children[from];

            _carrying = -1;
            _moved = 0f;
            carried.IsCarried = false;

            for (var i = 0; i < _wanted.Count; i++)
                _wanted[i] = 0f;

            if (to == from)
                return;

            // Everything stays where the drag put it and the carried row drops into the gap held for it, so the
            // reorder that follows changes nothing anybody can see.
            for (var i = 0; i < Children.Count; i++)
            {
                var row = Children[i];
                _landed[row] = i == from ? Landing(from, to) : row.Bounds.Y + row.RenderTransform.Translate.Y;
            }

            carried.RenderTransform = RenderTransform.Moved(new Vector2(0f, Landing(from, to) - carried.Bounds.Y));

            var moves = Moves;
            if (moves != null)
                moves(from, to);
        }

        void MakeRoom(int to)
        {
            var height = Children[_carrying].DesiredSize.Y + Spacing;

            for (var i = 0; i < Children.Count; i++)
            {
                if (i == _carrying)
                    continue;

                var shift = 0f;
                if (_carrying < to && i > _carrying && i <= to)
                    shift = -height;
                else if (_carrying > to && i >= to && i < _carrying)
                    shift = height;

                _wanted[i] = shift;
            }
        }

        int LandingFor(float moved)
        {
            // By the row, not by the pointer: somebody grabs a row wherever the grip is under their finger and then
            // aims with the row, because that is what they can see moving. Counting the rows the carried one has passed
            // gives the answer directly, and it is measured against where the rows were laid out rather than where they
            // have slid to, since the sliding is a picture of this answer.
            var carried = Children[_carrying].Bounds;
            var middle = carried.Y + carried.Height * 0.5f + moved;

            var index = 0;
            for (var i = 0; i < Children.Count; i++)
            {
                if (i == _carrying)
                    continue;

                var bounds = Children[i].Bounds;
                if (middle > bounds.Y + bounds.Height * 0.5f)
                    index++;
            }

            return index;
        }

        float Landing(int from, int to)
        {
            // Added up from the rows that will be above it, because the rows are not all one height: a wait is a shorter
            // line than an action.
            var top = Children[0].Bounds.Y;
            var placed = 0;

            for (var i = 0; i < Children.Count && placed < to; i++)
            {
                if (i == from)
                    continue;

                top += Children[i].DesiredSize.Y + Spacing;
                placed++;
            }

            return top;
        }
    }
}
