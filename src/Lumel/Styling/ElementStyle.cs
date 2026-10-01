
namespace Lumel.Styling
{
    /// <summary>How any element looks, and how it changes while hot and while it cannot be used.</summary>
    /// <remarks>Laid in that order, so a disabled element looks disabled whatever else it is.</remarks>
    public sealed class ElementStyle : Style<ElementState>
    {
        /// <summary>A style with nothing set, which applies as the neutral values.</summary>
        public ElementStyle()
        {
            Hot = Layer();
            Disabled = Layer();
        }

        /// <summary>A style that is everything <paramref name="basedOn" /> is, with whatever this sets laid over it.</summary>
        public ElementStyle(ElementStyle basedOn) : base(basedOn)
        {
            Hot = Layer();
            Disabled = Layer();
        }

        /// <summary>What changes while a press would reach the element.</summary>
        public ElementState Hot { get; }

        /// <summary>What changes while the element cannot be used.</summary>
        public ElementState Disabled { get; }

        /// <inheritdoc />
        protected override void LayStates(Element element)
        {
            if (element.IsHot && element.IsEnabled)
                Lay(Hot, element);

            if (!element.IsEnabled)
                Lay(Disabled, element);
        }
    }
}
