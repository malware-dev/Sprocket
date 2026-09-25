using Sprocket.Host.SpaceEngineers.Ui.Layout;

namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>How a scroll looks, and how it changes while its thumb is held.</summary>
    public sealed class ScrollStyle : Style<ScrollState>
    {
        /// <summary>A style with nothing set, which applies as the neutral values.</summary>
        public ScrollStyle()
        {
            Held = Layer();
        }

        /// <summary>A style starting from a copy of everything <paramref name="basedOn" /> sets.</summary>
        public ScrollStyle(ScrollStyle basedOn) : base(basedOn)
        {
            Held = Layer();
        }

        /// <summary>What changes while the thumb is held.</summary>
        public ScrollState Held { get; }

        /// <inheritdoc />
        protected override void LayStates(Element element)
        {
            var scroll = element as Scroll;
            if (scroll != null && scroll.IsHeld)
                Lay(Held, element);
        }
    }
}
