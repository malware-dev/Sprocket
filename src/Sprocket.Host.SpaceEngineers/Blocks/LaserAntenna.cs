using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

// The status lives in the Ingame namespace, which cannot be imported whole: IMyLaserAntenna is declared in both, and the
// mod-side one is the one with the event on it.
using MyLaserAntennaStatus = Sandbox.ModAPI.Ingame.MyLaserAntennaStatus;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// A laser antenna: how its link stands, the three edges of the link as signals, dropping it, and the members of its
    /// terminal said in Sprocket's words.
    /// </summary>
    /// <remarks>
    /// The game raises the block's properties-changed event each time the link changes state, but not as the target falls
    /// outside the antenna's turning limits, which reads as out of range. That can only happen while the antenna is doing
    /// something, so the link is looked for on its cadence only while it is not idle.
    /// </remarks>
    public sealed class LaserAntenna : Capability
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
        /// <remarks>Starting to connect is the link leaving idle, and disconnecting is its leaving connected.</remarks>
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

        /// <summary>Told as the link changes state, which the antenna raises as its properties changing.</summary>
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

        /// <summary>A laser antenna being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyLaserAntenna _antenna;
            readonly Action _told;

            public Listening(IMyLaserAntenna antenna, Action told)
            {
                _antenna = antenna;
                _told = told;
                _antenna.PropertiesChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _antenna.PropertiesChanged -= Changed;
            }

            void Changed(IMyTerminalBlock block)
            {
                _told();
            }
        }
    }
}
