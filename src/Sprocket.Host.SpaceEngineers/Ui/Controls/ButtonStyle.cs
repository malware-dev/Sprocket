using Sprocket.Host.SpaceEngineers.Ui.Styling;

namespace Sprocket.Host.SpaceEngineers.Ui.Controls
{
    /// <summary>How a button looks, and how it changes while hot, while pressed and while it cannot be used.</summary>
    /// <remarks>Laid in that order, so a disabled button looks disabled whatever else it is.</remarks>
    public sealed class ButtonStyle : Style<TextState>
    {
        /// <summary>A style with nothing set, which applies as the neutral values.</summary>
        public ButtonStyle()
        {
            Hot = Layer();
            Pressed = Layer();
            Disabled = Layer();
        }

        /// <summary>A style that is everything <paramref name="basedOn" /> is, with whatever this sets laid over it.</summary>
        public ButtonStyle(ButtonStyle basedOn) : base(basedOn)
        {
            Hot = Layer();
            Pressed = Layer();
            Disabled = Layer();
        }

        /// <summary>What changes while a press would reach the button.</summary>
        public TextState Hot { get; }

        /// <summary>What changes while the pointer is pressed down on the button.</summary>
        public TextState Pressed { get; }

        /// <summary>What changes while the button cannot be used.</summary>
        public TextState Disabled { get; }

        /// <inheritdoc />
        protected override void LayStates(Element element)
        {
            if (element.IsHot && element.IsEnabled)
                Lay(Hot, element);

            var button = element as Button;
            if (button != null && button.IsPressed)
                Lay(Pressed, element);

            if (!element.IsEnabled)
                Lay(Disabled, element);
        }
    }
}
