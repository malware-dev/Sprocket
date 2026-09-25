using VRage.Utils;

namespace Sprocket.Host.SpaceEngineers.Look
{
    /// <summary>The transparent materials the screens draw with.</summary>
    /// <remarks>Declared in <c>Content/Data/TransparentMaterials.sbc</c>.</remarks>
    public static class Materials
    {
        /// <summary>Flat white, tinted to whatever color is wanted: every fill on the screen.</summary>
        public static readonly MyStringId Fill = MyStringId.GetOrCompute("Sprocket_Fill");
    }
}
