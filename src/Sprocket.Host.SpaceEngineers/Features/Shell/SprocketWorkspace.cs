using Lumel;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Screens;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>A workspace on the game's screen whose pictures include Sprocket's own marks.</summary>
    public class SprocketWorkspace : ScreenSpaceWorkspace
    {
        /// <summary>A workspace showing nothing until it is given children.</summary>
        public SprocketWorkspace()
        {
        }

        /// <summary>The workspace showing <paramref name="root" />, which fills its room unless its alignment says otherwise.</summary>
        public SprocketWorkspace(Element root) : base(root)
        {
        }

        /// <inheritdoc />
        protected override IImageCatalog Pictures(IScreenSpace screen)
        {
            return new MarkImages(new ScreenSpaceImages(screen));
        }
    }
}
