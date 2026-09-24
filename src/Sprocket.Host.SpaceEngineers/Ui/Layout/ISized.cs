namespace Sprocket.Host.SpaceEngineers.Ui.Layout
{
    /// <summary>A placement giving an element a size of its own, across and down.</summary>
    /// <remarks><c>float.NaN</c> on either leaves the element its natural size that way.</remarks>
    public interface ISized : IPlacement
    {
        /// <summary>How wide the element is, or <c>float.NaN</c> for as wide as it asks.</summary>
        float Width { get; }

        /// <summary>How tall the element is, or <c>float.NaN</c> for as tall as it asks.</summary>
        float Height { get; }
    }
}
