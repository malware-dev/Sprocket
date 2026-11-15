using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sandbox.ModAPI;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A farm plot's grow light, which a sequence colors and dims as it does a light.</summary>
    /// <remarks>
    /// The game's own light settings come from the farm plot's component, and the Mod API lists and finds only the values of
    /// a block's type, never those of its components: a server never has them. Each is Sprocket's own, through the component,
    /// in a light's words. The bounds are the farm plot light's, read from the game's Content/Data/EntityComponents.sbc
    /// (LightingComponent FarmPlot: LightRadius, LightFalloff, LightIntensity).
    /// </remarks>
    public sealed class FarmPlot : Capability
    {
        const string Unlit = "A farm plot raises no event that says its light was changed.";

        static readonly OwnValue Colored = OwnValue.Color<IMyFunctionalBlock>(block => Lamp(block)?.Color ?? Color.White)
            .Writing<IMyFunctionalBlock>((block, value) => Change(block, lamp => lamp.Color = new Color(value.Red, value.Green, value.Blue, value.Alpha)));

        static readonly OwnValue Intensity = OwnValue.Number<IMyFunctionalBlock>(block => Lamp(block)?.Intensity ?? 0f)
            .Writing<IMyFunctionalBlock>((block, value) => Change(block, lamp => lamp.Intensity = (float)value.Number));

        static readonly OwnValue Radius = OwnValue.Number<IMyFunctionalBlock>(block => Lamp(block)?.Radius ?? 0f)
            .Writing<IMyFunctionalBlock>((block, value) => Change(block, lamp => lamp.Radius = (float)value.Number));

        static readonly OwnValue Falloff = OwnValue.Number<IMyFunctionalBlock>(block => Lamp(block)?.Falloff ?? 0f)
            .Writing<IMyFunctionalBlock>((block, value) => Change(block, lamp => lamp.Falloff = (float)value.Number));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            IMyLightingComponent lamp;
            return block is IMyFunctionalBlock && block.Components.TryGet(out lamp);
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("color", "color", Colored).Looked(Unlit).In(Category.Appearance)
                .WithValue("intensity", "intensity", Intensity).Between("0.5", "5").Looked(Unlit).In(Category.Appearance)
                .WithValue("radius", "radius", Radius).Measured(" m").Between("2", "20").Looked(Unlit).In(Category.Appearance)
                .WithValue("falloff", "falloff", Falloff).Between("1", "3").Looked(Unlit).In(Category.Appearance);
        }

        static IMyLightingComponent Lamp(IMyFunctionalBlock block)
        {
            IMyLightingComponent lamp;
            return block.Components.TryGet(out lamp) ? lamp : null;
        }

        static void Change(IMyFunctionalBlock block, System.Action<IMyLightingComponent> change)
        {
            var lamp = Lamp(block);
            if (lamp != null)
                change(lamp);
        }
    }
}
