using Lumel.Images;
using VRage.Utils;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Look
{
    /// <summary>
    /// Turns core's name for a kind of thing into the picture that draws it.
    /// </summary>
    public static class Icons
    {
        /// <summary>What every block icon's material name begins with.</summary>
        public const string Prefix = "Sprocket_Block_";

        /// <summary>The picture of the kind called <paramref name="name" />, of a size not known: wherever one is shown, its placement sizes it.</summary>
        public static IImageSource PictureFor(string name)
        {
            return new Sprite(MaterialFor(name), Vector2.Zero);
        }

        static MyStringId MaterialFor(string name)
        {
            // A name with a slash is a block definition id, which the game's screen-space API resolves to that block's picture.
            if (name != null && name.IndexOf('/') >= 0)
                return MyStringId.GetOrCompute(name);

            return MyStringId.GetOrCompute(Prefix + name);
        }
    }
}
