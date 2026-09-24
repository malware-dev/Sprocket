namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>How any element looks: its background, its edge and its padding.</summary>
    public sealed class ElementStyle : Style<ElementState>
    {
        /// <summary>A style with nothing set, which applies as the neutral values.</summary>
        public ElementStyle()
        {
        }

        /// <summary>A style starting from a copy of everything <paramref name="basedOn" /> sets.</summary>
        public ElementStyle(ElementStyle basedOn) : base(basedOn)
        {
        }
    }
}
