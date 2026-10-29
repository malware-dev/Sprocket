using System;
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
        /// <summary>A workspace whose screen is called <paramref name="name" />, timed by <paramref name="clock" /> or the game's frame count where it is null, showing nothing until it is given children.</summary>
        public SprocketWorkspace(string name, Func<int> clock = null) : base(name, ScreenArea.Terminal, clock)
        {
        }

        /// <summary>The workspace whose screen is called <paramref name="name" />, showing <paramref name="root" />, which fills its room unless its alignment says otherwise.</summary>
        public SprocketWorkspace(string name, Element root) : base(name, root)
        {
        }

        /// <inheritdoc />
        protected override IImageCatalog CreateImageCatalog(IScreenSpace screen)
        {
            return new MarkImages(new MaterialImages(screen.TextureSize));
        }
    }
}
