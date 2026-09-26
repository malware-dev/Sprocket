namespace Sprocket.Host.SpaceEngineers.Ui
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
