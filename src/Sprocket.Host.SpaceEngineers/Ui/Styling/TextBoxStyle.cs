namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>How a text box looks, and how it changes while hot, while it holds the keyboard, and while it cannot be used.</summary>
    /// <remarks>Laid in that order, so a disabled box looks disabled whatever else it is.</remarks>
    public sealed class TextBoxStyle : Style<TextBoxState>
    {
        /// <summary>A style with nothing set, which applies as the neutral values.</summary>
        public TextBoxStyle()
        {
            Hot = Layer();
            Focused = Layer();
            Disabled = Layer();
        }

        /// <summary>A style starting from a copy of everything <paramref name="basedOn" /> sets.</summary>
        public TextBoxStyle(TextBoxStyle basedOn) : base(basedOn)
        {
            Hot = Layer();
            Focused = Layer();
            Disabled = Layer();
        }

        /// <summary>What changes while a press would reach the box.</summary>
        public TextBoxState Hot { get; }

        /// <summary>What changes while the box holds the keyboard.</summary>
        public TextBoxState Focused { get; }

        /// <summary>What changes while the box cannot be used.</summary>
        public TextBoxState Disabled { get; }

        /// <inheritdoc />
        protected override void LayStates(Element element)
        {
            if (element.IsHot)
                Lay(Hot, element);

            if (element.IsFocused)
                Lay(Focused, element);

            if (!element.IsEnabled)
                Lay(Disabled, element);
        }
    }
}
