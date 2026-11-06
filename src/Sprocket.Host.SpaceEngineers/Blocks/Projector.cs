using Sandbox.Definitions;
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

        // A scaling projector turns its blueprint a degree at a time and any other a quarter turn at a time, from the game's
        // projector code.
        static double Turn(IMyTerminalBlock block)
        {
            var definition = block.SlimBlock?.BlockDefinition as MyProjectorDefinition;
            return definition != null && definition.AllowScaling ? 1 : 90;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            var turn = Turn(block);
            kind.WithValue("projecting", "projecting", Projecting).Looked(Unheard).In(Category.Production)
                .Renaming("KeepProjection", true, "keep projection")
                .Renaming("KeepProjection", false, "toggle keep projection")
                .WithAction("remove_the_blueprint", "remove the blueprint", Removes).In(Category.Production)
                .WithValue("blocks_left", "blocks left", Left).Looked(Unheard).In(Category.Production)
                .WithChange("finishes_building", "finishes building", "blocks_left", Literal.Of(0L))
                .Reading("has no blocks left", "has blocks left").Looked(Unheard)
                .WithValue("armor_blocks_left", "armor blocks left", ArmorLeft).Looked(Unheard).In(Category.Production)
                .WithValue("total_blocks", "total blocks", Total).Looked(Unheard).In(Category.Production)
                .WithValue("blocks_ready_to_weld", "blocks ready to weld", Ready).Looked(Unheard).In(Category.Production)
                .Renaming("X", true, "horizontal offset")
                .Renaming("Y", true, "vertical offset")
                .Renaming("Z", true, "forward offset")
                .Renaming("RotX", true, "pitch")
                .Measuring("RotX", "°", turn, turn)
                .Renaming("RotY", true, "yaw")
                .Measuring("RotY", "°", turn, turn)
                .Renaming("RotZ", true, "roll")
                .Measuring("RotZ", "°", turn, turn)
                .Renaming("Scale", true, "scale")
                .Renaming("ShowOnlyBuildable", true, "show only buildable")
                .Renaming("MarkMissingBlocks", true, "mark missing blocks")
                .Renaming("MarkUnfinishedBlocks", true, "mark unfinished blocks");
        }
    }
}
