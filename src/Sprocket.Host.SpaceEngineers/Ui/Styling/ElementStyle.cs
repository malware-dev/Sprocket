namespace Sprocket.Host.SpaceEngineers.Ui.Styling
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

        /// <summary>A style starting from a copy of everything <paramref name="basedOn" /> sets.</summary>
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
            if (element.IsHot)
                Lay(Hot, element);

            if (!element.IsEnabled)
                Lay(Disabled, element);
        }
    }
}
