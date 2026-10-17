using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A projector: whether a blueprint is showing, how far its build has come, and where and how it shows.</summary>
    /// <remarks>The build's counts raise no event and are looked at; the game keeps the last blueprint's counts after it is taken away.</remarks>
    public sealed class Projector : Capability
    {
        const string Unheard = "A projector raises nothing as its build moves on.";

        static readonly OwnValue Projecting = OwnValue.Flag(Reading.Of<IMyProjector>(projector => projector.IsProjecting));

        static readonly OwnValue Left = OwnValue.Whole<IMyProjector>(projector => projector.RemainingBlocks);

        static readonly OwnValue ArmorLeft = OwnValue.Whole<IMyProjector>(projector => projector.RemainingArmorBlocks);

        static readonly OwnValue Total = OwnValue.Whole<IMyProjector>(projector => projector.TotalBlocks);

        static readonly OwnValue Ready = OwnValue.Whole<IMyProjector>(projector => projector.BuildableBlocksCount);

        // The terminal's own Remove, which every player sees go.
        static readonly OwnAction Removes = OwnAction.Of<IMyProjector>(projector => projector.SetProjectedGrid(null));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyProjector;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("projecting", "projecting", Projecting).Looked(Unheard).In(Category.Production)
                .Renaming("KeepProjection", true, "keep projection")
                .Renaming("KeepProjection", false, "toggle keep projection")
                .WithAction("remove-the-blueprint", "remove the blueprint", Removes).In(Category.Production)
                .WithValue("blocks-left", "blocks left", Left).Looked(Unheard).In(Category.Production)
                .WithChange("finishes-building", "finishes building", "blocks-left", Literal.Of(0L))
                .Reading("has no blocks left", "has blocks left").Looked(Unheard)
                .WithValue("armor-blocks-left", "armor blocks left", ArmorLeft).Looked(Unheard).In(Category.Production)
                .WithValue("total-blocks", "total blocks", Total).Looked(Unheard).In(Category.Production)
                .WithValue("blocks-ready-to-weld", "blocks ready to weld", Ready).Looked(Unheard).In(Category.Production)
                .Renaming("X", true, "horizontal offset")
                .Renaming("Y", true, "vertical offset")
                .Renaming("Z", true, "forward offset")
                .Renaming("RotX", true, "pitch")
                .Measuring("RotX", "°", 90, 90)
                .Renaming("RotY", true, "yaw")
                .Measuring("RotY", "°", 90, 90)
                .Renaming("RotZ", true, "roll")
                .Measuring("RotZ", "°", 90, 90)
                .Renaming("Scale", true, "scale")
                .Renaming("ShowOnlyBuildable", true, "show only buildable")
                .Renaming("MarkMissingBlocks", true, "mark missing blocks")
                .Renaming("MarkUnfinishedBlocks", true, "mark unfinished blocks");
        }
    }
}
