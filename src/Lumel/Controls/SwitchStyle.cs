using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>How a switch looks, and how it changes while hot and while it cannot be used.</summary>
    /// <remarks>Laid in that order, so a disabled switch looks disabled whatever else it is.</remarks>
    public sealed class SwitchStyle : Style<SwitchState>
    {
        /// <summary>A style with nothing set, which applies as the neutral values.</summary>
        public SwitchStyle()
        {
            Hot = Layer();
            Disabled = Layer();
        }

        /// <summary>A style that is everything <paramref name="basedOn" /> is, with whatever this sets laid over it.</summary>
        public SwitchStyle(SwitchStyle basedOn) : base(basedOn)
        {
            Hot = Layer();
            Disabled = Layer();
        }

        /// <summary>What changes while a press would reach the switch.</summary>
        public SwitchState Hot { get; }

        /// <summary>What changes while the switch cannot be used.</summary>
        public SwitchState Disabled { get; }

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
