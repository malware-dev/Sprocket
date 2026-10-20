using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>How a tab's header looks, and how it changes while hot, while its page is the one shown, and while it cannot be used.</summary>
    /// <remarks>Laid in that order, so the header of the page shown looks it whether or not a press would reach it.</remarks>
    public sealed class TabStyle : Style<TextState>
    {
        /// <summary>A style with nothing set, which applies as the neutral values.</summary>
        public TabStyle()
        {
            Hot = Layer();
            Current = Layer();
            Disabled = Layer();
        }

        /// <summary>A style that is everything <paramref name="basedOn" /> is, with whatever this sets laid over it.</summary>
        public TabStyle(TabStyle basedOn) : base(basedOn)
        {
            Hot = Layer();
            Current = Layer();
            Disabled = Layer();
        }

        /// <summary>What changes while a press would reach the header.</summary>
        public TextState Hot { get; }

        /// <summary>What changes while the tab's page is the one shown.</summary>
        public TextState Current { get; }

        /// <summary>What changes while the tab cannot be used.</summary>
        public TextState Disabled { get; }

        /// <inheritdoc />
        protected override void LayStates(Element element)
        {
            if (element.IsHot && element.IsEnabled)
                Lay(Hot, element);

            var tab = element as Tab;
            if (tab != null && tab.IsCurrent)
                Lay(Current, element);

            if (!element.IsEnabled)
                Lay(Disabled, element);
        }
    }
}
