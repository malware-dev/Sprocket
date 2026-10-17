using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A safe zone block, read for whether its zone is up, what the zone allows, and its shape, size and look.</summary>
    /// <remarks>The game takes a change to what the zone allows, its shape or its size only while the zone is up.</remarks>
    public sealed class SafeZone : Capability
    {
        const string Unheard = "No event of the block's is known to say when its zone goes up or down.";

        static readonly OwnValue Active = OwnValue.Flag(Reading.Of<IMySafeZoneBlock>(zone => zone.IsSafeZoneEnabled()));

        // The game's own id for each switch, and what Sprocket says it allows.
        static readonly string[] Allows =
        {
            "SafeZoneDamageCb", "damage",
            "SafeZoneShootingCb", "shooting",
            "SafeZoneDrillingCb", "drilling",
            "SafeZoneWeldingCb", "welding",
            "SafeZoneGrindingCb", "grinding",
            "SafeZoneBuildingCb", "building",
            "SafeZoneBuildingProjectionCb", "building from projections",
            "SafeZoneVoxelHandCb", "the voxel hand",
            "SafeZoneLandingGearCb", "landing gear to lock",
            "SafeZoneConvertToStationCb", "conversion to station"
        };

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMySafeZoneBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("active", "active", Active).Status().Looked(Unheard)
                .Renaming("SafeZoneCreate_On", false, "activate")
                .Renaming("SafeZoneCreate_Off", false, "deactivate")
                .Hiding("SafeZoneCreate")
                .Renaming("SafeZoneShapeCombo", true, "shape")
                .Renaming("SafeZoneSlider", true, "radius")
                .Measuring("SafeZoneSlider", " m")
                .Renaming("SafeZoneXSlider", true, "size X")
                .Measuring("SafeZoneXSlider", " m")
                .Renaming("SafeZoneYSlider", true, "size Y")
                .Measuring("SafeZoneYSlider", " m")
                .Renaming("SafeZoneZSlider", true, "size Z")
                .Measuring("SafeZoneZSlider", " m")
                .Renaming("SafeZoneColor", true, "color")
                .Renaming("SafeZoneTextureCombo", true, "texture")
                .Renaming("UseConveyor", true, "automatic push and pull")
                .Renaming("UseConveyor", false, "toggle automatic push and pull");

            for (var i = 0; i < Allows.Length; i += 2)
                kind.Renaming(Allows[i], true, $"allowing {Allows[i + 1]}");
        }
    }
}
