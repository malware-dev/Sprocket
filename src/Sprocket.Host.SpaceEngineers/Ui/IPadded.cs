namespace Sprocket.Host.SpaceEngineers.Ui
{
    /// <summary>An element with room held clear between its edge and what it draws inside, which a style can set.</summary>
    public interface IPadded
    {
        /// <summary>Room held clear inside the bounds, which content is measured and placed within.</summary>
        Thickness Padding { get; set; }
    }
}
