using System;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>What an element looks like in one state: only what that state sets, the rest left to the layers beneath.</summary>
    public class ElementState
    {
        Color? _background;
        Color? _edge;
        Thickness? _padding;

        /// <summary>The style this belongs to, which says whether it may still be changed.</summary>
        internal IStyle Owner { get; set; }

        /// <summary>What is painted behind the element, or null to leave it to the layers beneath.</summary>
        public Color? Background
        {
            get { return _background; }
            set
            {
                Check();
                _background = value;
            }
        }

        /// <summary>The hairline round the element, or null to leave it to the layers beneath.</summary>
        public Color? Edge
        {
            get { return _edge; }
            set
            {
                Check();
                _edge = value;
            }
        }

        /// <summary>The room held clear inside the element, or null to leave it to the layers beneath.</summary>
        public Thickness? Padding
        {
            get { return _padding; }
            set
            {
                Check();
                _padding = value;
            }
        }

        /// <summary>Writes the values every style starts from onto <paramref name="element" />.</summary>
        internal virtual void Neutral(Element element)
        {
            element.Background = Color.Transparent;
            element.Edge = Color.Transparent;
            element.Padding = new Thickness(0f);
        }

        /// <summary>Writes what this state sets onto <paramref name="element" />, and nothing else.</summary>
        internal virtual void Lay(Element element)
        {
            if (_background.HasValue)
                element.Background = _background.Value;

            if (_edge.HasValue)
                element.Edge = _edge.Value;

            if (_padding.HasValue)
                element.Padding = _padding.Value;
        }

        /// <summary>Takes every value <paramref name="other" /> sets.</summary>
        internal virtual void Copy(ElementState other)
        {
            _background = other._background;
            _edge = other._edge;
            _padding = other._padding;
        }

        /// <summary>Refuses a change once the style is frozen.</summary>
        protected void Check()
        {
            if (Owner != null && Owner.IsFrozen)
                throw new InvalidOperationException("A frozen style cannot be changed.");
        }
    }
}
