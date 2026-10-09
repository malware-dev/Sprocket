using System.Collections.Generic;
using Lumel.Images;
using Sandbox.ModAPI;
using VRage.Utils;

namespace RichTextPanels
{
    /// <summary>The pictures a text panel can draw, found by sprite name and kept once found.</summary>
    /// <remarks>Each name is asked about once, unknown ones included; one panel's app owns its catalog, which goes with it.</remarks>
    public class PanelPictures : IImageCatalog
    {
        readonly Dictionary<string, IImageSource> _found = new Dictionary<string, IImageSource>();

        /// <inheritdoc />
        public IImageSource Find(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            IImageSource image;
            if (_found.TryGetValue(name, out image))
                return image;

            var material = MyStringId.GetOrCompute(name);
            var size = MyScreenSpaceSprite.GetTextureSize(material);
            image = size.X > 0f && size.Y > 0f ? new Sprite(material, size) : null;
            _found.Add(name, image);
            return image;
        }
    }
}
