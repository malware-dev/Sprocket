using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.Components;
using VRage.Game.ModAPI.Interfaces;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A launcher with a capacitor, which is a railgun: how charged it is, how long until it is, and when it is.</summary>
    public sealed partial class Railgun : Capability
    {
        const string Uncounted = "A capacitor says when it is charged and when it is not, and nothing as its charge moves.";

        readonly OwnValue _charge;
        readonly OwnValue _remaining;
        readonly OwnValue _charged;

        readonly OwnValue _charging;

        // Learned from the first capacitor found by walking the components, which allocates; every later lookup by this
        // type does not. The component's own type is one a mod may not name.
        Type _capacitor;

        /// <summary>A railgun's values, each reading the block's capacitor.</summary>
        public Railgun()
        {
            _charge = OwnValue.Number<IMyTerminalBlock>(block =>
            {
                var capacitor = Capacitor(block);
                return capacitor == null || capacitor.Capacity <= 0f ? 0d : 100d * capacitor.StoredPower / capacitor.Capacity;
            });

            _remaining = OwnValue.Number<IMyTerminalBlock>(block =>
            {
                var capacitor = Capacitor(block);
                return capacitor?.TimeRemaining ?? 0d;
            });

            _charged = OwnValue.Flag(Reading.Of<IMyTerminalBlock>(block =>
            {
                var capacitor = Capacitor(block);
                return capacitor != null && capacitor.IsCharged;
            }));

            _charging = OwnValue.Flag(Reading.Of<IMyTerminalBlock>(block =>
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
            kind.WithValue("charge", "charge", _charge).Percent().Looked(Uncounted).In(Category.Weapons)
                .WithValue("time-until-charged", "time until charged", _remaining).Measured(" s").Looked(Uncounted)
                .In(Category.Weapons)
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
    }
}
