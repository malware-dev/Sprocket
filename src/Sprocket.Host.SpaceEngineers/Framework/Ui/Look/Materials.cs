using VRage.Utils;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Look
{
    /// <summary>The transparent materials the screens draw with.</summary>
    /// <remarks>Declared in <c>Content/Data/TransparentMaterials.sbc</c>.</remarks>
    public static class Materials
    {
        /// <summary>The soft dark fall-off a floating window casts, drawn as a nine-slice: 64 pixels square, its corners 24.</summary>
        public static readonly MyStringId Shadow = MyStringId.GetOrCompute("Sprocket_Shadow");

        /// <summary>White hazard stripes at forty-five degrees on nothing, 64 pixels square, meeting the next tile with no seam: laid across a warning or an error.</summary>
        public static readonly MyStringId Stripes = MyStringId.GetOrCompute("Sprocket_Stripes");
    }
}
