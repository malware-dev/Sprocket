using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A projector: whether a blueprint is showing, how far its build has come, and where and how it shows.</summary>
    /// <remarks>
    /// The projector's own switch runs it, so the words every switch has show and hide the blueprint. None of the build's
    /// counts raises an event; the projector works them out again on its own pace, and they are looked at.
    /// <para>
    /// The game keeps the counts of the last blueprint once it is taken away, so blocks left comes to nothing only as a
    /// build is finished, whether or not the projection is kept.
    /// </para>
    /// </remarks>
    public sealed class Projector : Capability
    {
        const string Unheard = "A projector raises nothing as its build moves on.";

        static readonly Being Projecting = Being.Flag(Reading.Of<IMyProjector>(projector => projector.IsProjecting));

        static readonly Being Left = Being.Whole<IMyProjector>(projector => projector.RemainingBlocks);

        static readonly Being ArmorLeft = Being.Whole<IMyProjector>(projector => projector.RemainingArmorBlocks);

        static readonly Being Total = Being.Whole<IMyProjector>(projector => projector.TotalBlocks);

        static readonly Being Ready = Being.Whole<IMyProjector>(projector => projector.BuildableBlocksCount);

        // The terminal's own Remove, which every player sees go.
        static readonly Doing Removes = Doing.Of<IMyProjector>(projector => projector.SetProjectedGrid(null));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyProjector;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("projecting", "projecting", Projecting).Looked(Unheard).In(Category.Making)
                .Renaming("KeepProjection", true, "keep projection")
                .Renaming("KeepProjection", false, "toggle keep projection")
                .WithAction("remove-the-blueprint", "remove the blueprint", Removes).In(Category.Making)
                .WithValue("blocks-left", "blocks left", Left).Looked(Unheard).In(Category.Making)
                .WithChange("finishes-building", "finishes building", "blocks-left", Literal.Of(0L))
                .Reading("has no blocks left", "has blocks left").Looked(Unheard)
                .WithValue("armor-blocks-left", "armor blocks left", ArmorLeft).Looked(Unheard).In(Category.Making)
                .WithValue("total-blocks", "total blocks", Total).Looked(Unheard).In(Category.Making)
                .WithValue("blocks-ready-to-weld", "blocks ready to weld", Ready).Looked(Unheard).In(Category.Making)
                .Renaming("X", true, "horizontal offset")
                .Renaming("Y", true, "vertical offset")
                .Renaming("Z", true, "forward offset")
                .Renaming("RotX", true, "pitch")
                .Renaming("RotY", true, "yaw")
                .Renaming("RotZ", true, "roll")
                .Renaming("Scale", true, "scale")
                .Renaming("ShowOnlyBuildable", true, "show only buildable")
                .Renaming("MarkMissingBlocks", true, "mark missing blocks")
                .Renaming("MarkUnfinishedBlocks", true, "mark unfinished blocks");
        }
    }
}
