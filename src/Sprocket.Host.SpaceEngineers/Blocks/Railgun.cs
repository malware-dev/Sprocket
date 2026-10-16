using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.Components;
using VRage.Game.ModAPI.Interfaces;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A launcher with a capacitor, which is a railgun: how charged it is, how long until it is, and when it is.</summary>
    /// <remarks>
    /// Only the railguns of the reloading launchers carry a capacitor, so the block is asked for one rather than claimed by
    /// its interface. Charged is a flag the capacitor says turns each way; it also says so as a world loads and as a full
    /// railgun is switched back on, which a look finds unchanged.
    /// </remarks>
    public sealed class Railgun : Capability
    {
        const string Uncounted = "A capacitor says when it is charged and when it is not, and nothing as its charge moves.";

        readonly Being _charge;
        readonly Being _remaining;
        readonly Being _charged;

        readonly Being _charging;

        /// <summary>
        /// The type the game keeps a capacitor under among a block's components, learned from the first one found; null until
        /// then.
        /// </summary>
        /// <remarks>
        /// The component's own type is not one a mod may name, so the first capacitor is found by walking the components,
        /// which allocates; every one after is found by its type, which does not. The game keeps it under its own type.
        /// </remarks>
        Type _capacitor;

        /// <summary>A railgun's values, each reading the block's capacitor.</summary>
        public Railgun()
        {
            _charge = Being.Number<IMyTerminalBlock>(block =>
            {
                var capacitor = Capacitor(block);
                return capacitor == null || capacitor.Capacity <= 0f ? 0d : 100d * capacitor.StoredPower / capacitor.Capacity;
            });

            _remaining = Being.Number<IMyTerminalBlock>(block =>
            {
                var capacitor = Capacitor(block);
                return capacitor?.TimeRemaining ?? 0d;
            });

            _charged = Being.Flag(Reading.Of<IMyTerminalBlock>(block =>
            {
                var capacitor = Capacitor(block);
                return capacitor != null && capacitor.IsCharged;
            }));

            _charging = Being.Flag(Reading.Of<IMyTerminalBlock>(block =>
            {
                var capacitor = Capacitor(block);
                return capacitor != null && !capacitor.IsCharged && capacitor.TimeRemaining > 0f;
            }));
        }

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMySmallMissileLauncherReload && Capacitor(block) != null;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("charge", "charge", _charge).Percent().Looked(Uncounted)
                .WithValue("time-until-charged", "time until charged", _remaining).Measured(" s").Looked(Uncounted)
                .WithValue("charging", "charging", _charging).Status().Looked(Uncounted)
                .WithValue("charged", "charged", _charged).Status().Announced()
                .WithChange("finishes-charging", "finishes charging", "charged", Literal.Of(true)).Announced()
                .Reading("is charged", "is not charged");
        }

        /// <summary>Told as the capacitor comes to be charged or stops being so.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var capacitor = Capacitor(block);
            return capacitor == null ? null : new Listening(capacitor, told);
        }

        /// <summary>The block's capacitor, or null where it has none.</summary>
        IMyEntityCapacitorComponent Capacitor(IMyTerminalBlock block)
        {
            MyComponentBase kept;
            if (_capacitor != null)
                return block.Components.TryGet(_capacitor, out kept) ? kept as IMyEntityCapacitorComponent : null;

            foreach (var component in block.Components)
            {
                var capacitor = component as IMyEntityCapacitorComponent;
                if (capacitor == null)
                    continue;

                _capacitor = component.GetType();
                return capacitor;
            }

            return null;
        }

        /// <summary>A capacitor being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyEntityCapacitorComponent _capacitor;
            readonly Action _told;

            public Listening(IMyEntityCapacitorComponent capacitor, Action told)
            {
                _capacitor = capacitor;
                _told = told;
                _capacitor.OnCharged += Changed;
                _capacitor.OnNotCharged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _capacitor.OnCharged -= Changed;
                _capacitor.OnNotCharged -= Changed;
            }

            void Changed(IMyEntityCapacitorComponent capacitor)
            {
                _told();
            }
        }
    }
}
