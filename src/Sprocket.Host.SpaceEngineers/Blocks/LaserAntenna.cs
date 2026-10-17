using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

// The status lives in the Ingame namespace, which cannot be imported whole: IMyLaserAntenna is declared in both, and the
// mod-side one is the one with the event on it.
using MyLaserAntennaStatus = Sandbox.ModAPI.Ingame.MyLaserAntennaStatus;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A laser antenna, read for how its link stands, with the link's three edges as signals and dropping it.</summary>
    /// <remarks>
    /// The game raises no event as the target falls outside the antenna's turning limits, so the link is looked at on its
    /// cadence while it is not idle.
    /// </remarks>
    public sealed partial class LaserAntenna : Capability
    {
        static readonly string[] Links = { "idle", "turning", "searching", "connecting", "connected", "out-of-range" };

        static readonly Reading Idle = Reading.Of<IMyLaserAntenna>(antenna => antenna.Status == MyLaserAntennaStatus.Idle);

        static readonly Reading Connected =
            Reading.Of<IMyLaserAntenna>(antenna => antenna.Status == MyLaserAntennaStatus.Connected);

        static readonly OwnValue Link = OwnValue.OneOf<IMyLaserAntenna>(Links, antenna => Position(antenna.Status));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyLaserAntenna;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("link", "link", Link).Status().RuledOutBy(Idle)
                .WithEvent("starts-connecting", Idle, SignalEdge.Falling, "starts connecting").RuledOutBy(Idle)
                .WithChange("connects", "connects", "link", Literal.Name("connected")).RuledOutBy(Idle)
                .Reading("is connected", "is not connected")
                .WithEvent("disconnects", Connected, SignalEdge.Falling, "disconnects").RuledOutBy(Idle)
                .Renaming("Idle", false, "disconnect")
                .Renaming("Range", true, "range")
                .Measuring("Range", " m")
                .Renaming("isPerm", true, "permanent connection")
                .Renaming("isPerm", false, "toggle permanent connection")
                .Hiding("PasteGpsCoords");
        }

        /// <summary>Told as the link changes state.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var antenna = block as IMyLaserAntenna;
            return antenna == null ? null : new Listening(antenna, told);
        }

        /// <summary>Where <paramref name="status" /> stands among the link's names.</summary>
        static int Position(MyLaserAntennaStatus status)
        {
            switch (status)
            {
                case MyLaserAntennaStatus.Idle:
                    return 0;
                case MyLaserAntennaStatus.RotatingToTarget:
                    return 1;
                case MyLaserAntennaStatus.SearchingTargetForAntenna:
                    return 2;
                case MyLaserAntennaStatus.Connecting:
                    return 3;
                case MyLaserAntennaStatus.Connected:
                    return 4;
            }

            return 5;
        }
    }
}
