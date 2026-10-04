namespace Lumel.Images
{
    /// <summary>Finds pictures by the names written for them, such as an image's name in markdown.</summary>
    /// <remarks>What a name means is up to the surface drawing it: a screen and a text panel know different pictures.</remarks>
    public interface IImageCatalog
    {
        /// <summary>The picture called <paramref name="name" />, or null for a name that is no picture here.</summary>
        IImageSource Find(string name);
    }
}
