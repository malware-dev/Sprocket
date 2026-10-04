using System.Collections.Generic;
using Lumel.Drawing;
using VRage.Utils;

namespace Lumel.Images
{
    /// <summary>The pictures the game's screen space can draw, found by material name and kept once found.</summary>
    /// <remarks>
    ///     A picture never changes while the game runs, so each name is asked about once and its answer kept,
    ///     unknown names included. Whoever makes the catalog owns what it keeps: one made for a world is
    ///     dropped with it.
    /// </remarks>
    public class ScreenSpaceImages : IImageCatalog
    {
        readonly IScreenSpace _screen;
        readonly Dictionary<string, IImageSource> _found = new Dictionary<string, IImageSource>();

        /// <summary>The pictures <paramref name="screen" /> can draw.</summary>
        public ScreenSpaceImages(IScreenSpace screen)
        {
            _screen = screen;
        }

        /// <inheritdoc />
        public IImageSource Find(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            IImageSource image;
            if (_found.TryGetValue(name, out image))
                return image;

            var material = MyStringId.GetOrCompute(name);
            var size = _screen.TextureSize(material);
            image = size.X > 0f && size.Y > 0f ? new Sprite(material, size) : null;
            _found.Add(name, image);
            return image;
        }
    }
}
