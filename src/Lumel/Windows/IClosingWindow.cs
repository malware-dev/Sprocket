namespace Lumel.Windows
{
    /// <summary>A window a layer can take away, which then raises its <see cref="Window.Closed" />.</summary>
    /// <remarks>Implemented explicitly by <see cref="Window" />, so only the layer that takes it away raises it.</remarks>
    public interface IClosingWindow
    {
        /// <summary>The layer has taken the window away.</summary>
        void TakenAway();
    }
}
