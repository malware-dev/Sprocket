
namespace Lumel
{
    /// <summary>An element that holds others, which may draw outside it, with room held clear between its edge and what is inside.</summary>
    public abstract class Container : Element, IPadded
    {
        Thickness _padding;

        /// <summary>Room held clear inside the bounds, which the children are measured and placed within.</summary>
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

        /// <inheritdoc />
        protected override Thickness Inset => _padding;

        /// <summary>Where this places a child whose <see cref="Element.Align" /> leaves an axis to it, on every axis it does not stack along.</summary>
        /// <remarks>Declared by every container, so a child that says nothing is placed by a rule this container chose.</remarks>
        protected abstract Align Aligns { get; }

        /// <summary>Where <paramref name="child" /> goes on one axis of the room from <paramref name="start" />, <paramref name="room" /> long.</summary>
        /// <remarks>
        /// <paramref name="given" /> is a length the child's placement fixes, or NaN; <paramref name="desired" /> is the length it
        /// asked for. A fixed length is kept; otherwise a stretched child takes the room and any other takes what it asked for,
        /// and a length of nothing, or more than the room, takes the room.
        /// </remarks>
        protected void Span(Element child, bool across, float start, float room, float given, float desired, out float at,
            out float length)
        {
            var edges = Edges(child.Align, across);
            if (edges == Align.Default)
                edges = Edges(Aligns, across);

            var low = (edges & (Align.Left | Align.Top)) != 0;
            var high = (edges & (Align.Right | Align.Bottom)) != 0;
            var stretched = low == high && edges != Align.Center;

            length = !float.IsNaN(given) ? given : stretched ? room : desired;
            if (length <= 0f || length > room)
                length = room;

            at = low || stretched ? start : high ? start + room - length : start + (room - length) * 0.5f;
        }

        /// <summary>What <paramref name="align" /> says about one axis: its edges there, else <see cref="Lumel.Align.Center" /> where set, else nothing.</summary>
        static Align Edges(Align align, bool across)
        {
            var edges = align & (across ? Align.Wide : Align.Tall);
            return edges != Align.Default ? edges : align & Align.Center;
        }

        /// <summary>The collection this holds its children in, which asks it whether it can place each child and tells it of every change.</summary>
        protected Collection Hold()
        {
            return new Collection(this, Admit, Joined, Released, Replaced);
        }

        /// <summary>
        ///     Checks that this can place <paramref name="child" /> by <paramref name="placement" />, before it joins or
        ///     changes.
        /// </summary>
        /// <remarks>A container that cannot place it throws.</remarks>
        protected virtual void Admit(Element child, IPlacement placement)
        {
        }

        /// <summary>Told that <paramref name="child" /> has joined this container's children at <paramref name="index" />.</summary>
        protected virtual void Joined(Element child, int index)
        {
        }

        /// <summary>Told that <paramref name="child" /> has left this container's children from <paramref name="index" />.</summary>
        protected virtual void Released(Element child, int index)
        {
        }

        /// <summary>Told that the placement of <paramref name="child" />, one of this container's children, has changed.</summary>
        protected virtual void Replaced(Element child)
        {
        }
    }
}
