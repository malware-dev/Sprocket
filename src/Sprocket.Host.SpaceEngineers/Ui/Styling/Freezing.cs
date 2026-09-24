namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>Finishing a style as it is declared.</summary>
    public static class Freezing
    {
        /// <summary>Freezes <paramref name="style" /> and hands it back, so a declaration can end with it.</summary>
        public static T Frozen<T>(this T style) where T : IStyle
        {
            style.Freeze();
            return style;
        }
    }
}
