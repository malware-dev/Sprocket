namespace Sprocket.Host.SpaceEngineers.Ui.Styling
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

        /// <summary>A style starting from a copy of everything <paramref name="basedOn" /> sets.</summary>
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
            if (element.IsHot)
                Lay(Hot, element);

            if (!element.IsEnabled)
                Lay(Disabled, element);
        }
    }
}
