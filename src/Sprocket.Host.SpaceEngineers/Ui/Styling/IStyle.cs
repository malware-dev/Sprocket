namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>How an element looks, applied onto the element's own properties.</summary>
    public interface IStyle
    {
        /// <summary>Writes this style onto <paramref name="element" /> as it looks in the states it is in now.</summary>
        void Apply(Element element);
    }
}
