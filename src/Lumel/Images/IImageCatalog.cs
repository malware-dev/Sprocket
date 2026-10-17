namespace Lumel.Images
{
    /// <summary>Resolves the locators written for pictures, such as an image's in markdown, to pictures that can be drawn.</summary>
    /// <remarks>A locator means only what its host makes of it, so the same locator need not resolve on every host.</remarks>
    public interface IImageCatalog
    {
        /// <summary>The picture <paramref name="locator" /> leads to, or null where it leads to none here.</summary>
        IImageSource Find(string locator);
    }
}
