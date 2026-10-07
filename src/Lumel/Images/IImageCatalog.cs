namespace Lumel.Images
{
    /// <summary>Resolves the locators written for pictures, such as an image's in markdown, to pictures that can be drawn.</summary>
    /// <remarks>
    ///     The host's own, handed to the graphics it draws with. A locator means only what its host makes of it: a screen
    ///     and a text panel can draw different pictures, so the same locator need not resolve on both.
    /// </remarks>
    public interface IImageCatalog
    {
        /// <summary>The picture <paramref name="locator" /> leads to, or null where it leads to none here.</summary>
        IImageSource Find(string locator);
    }
}
