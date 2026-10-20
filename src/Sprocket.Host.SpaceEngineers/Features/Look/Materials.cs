using VRage.Utils;

namespace Sprocket.Host.SpaceEngineers.Features.Look
{
    /// <summary>The transparent materials the screens draw with.</summary>
    /// <remarks>Declared in <c>Content/Data/TransparentMaterials.sbc</c>.</remarks>
    public static class Materials
    {
        /// <summary>Flat white, tinted to whatever color is wanted: every fill on the screen.</summary>
        public static readonly MyStringId Fill = MyStringId.GetOrCompute("Sprocket_Fill");

        /// <summary>The soft dark fall-off a floating window casts, drawn as a nine-slice: 64 pixels square, its corners 24.</summary>
        public static readonly MyStringId Shadow = MyStringId.GetOrCompute("Sprocket_Shadow");

        /// <summary>Every hue at full saturation and value, red at the top round to red at the bottom.</summary>
        public static readonly MyStringId Hues = MyStringId.GetOrCompute("Sprocket_Hues");

        /// <summary>White, solid at the left and gone at the right: over a fill of one hue, its saturation across.</summary>
        public static readonly MyStringId Pale = MyStringId.GetOrCompute("Sprocket_Pale");

        /// <summary>Black, gone at the top and solid at the bottom: over the pale fade, value up.</summary>
        public static readonly MyStringId Dark = MyStringId.GetOrCompute("Sprocket_Dark");
    }
}
