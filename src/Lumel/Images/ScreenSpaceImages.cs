using System.Collections.Generic;
using Lumel.Drawing;
using VRage.Utils;

namespace Lumel.Images
{
    /// <summary>A resolver a host may use for the game's screen space, which reads every locator as a material name and keeps what it finds.</summary>
    /// <remarks>Each locator's answer is kept for the life of the resolver, unknown ones included.</remarks>
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
        public IImageSource Find(string locator)
        {
            if (string.IsNullOrEmpty(locator))
                return null;

            IImageSource image;
            if (_found.TryGetValue(locator, out image))
                return image;

            var material = MyStringId.GetOrCompute(locator);
            var size = _screen.TextureSize(material);
            image = size.X > 0f && size.Y > 0f ? new Sprite(material, size) : null;
            _found.Add(locator, image);
            return image;
        }
    }
}
