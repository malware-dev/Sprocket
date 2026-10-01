using Lumel.Styling;


namespace Lumel.Viewing
{
    /// <summary>How a scroll looks, and how it changes while its thumb is held.</summary>
    public sealed class ScrollViewStyle : Style<ScrollViewState>
    {
        /// <summary>A style with nothing set, which applies as the neutral values.</summary>
        public ScrollViewStyle()
        {
            Held = Layer();
        }

        /// <summary>A style that is everything <paramref name="basedOn" /> is, with whatever this sets laid over it.</summary>
        public ScrollViewStyle(ScrollViewStyle basedOn) : base(basedOn)
        {
            Held = Layer();
        }

        /// <summary>What changes while the thumb is held.</summary>
        public ScrollViewState Held { get; }

        /// <inheritdoc />
        protected override void LayStates(Element element)
        {
            var scroll = element as ScrollView;
            if (scroll != null && scroll.IsHeld)
                Lay(Held, element);
        }
    }
}
