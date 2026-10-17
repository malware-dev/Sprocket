using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.ModAPI;

// The item type lives in the Ingame namespace, which cannot be imported whole: IMyInventory is declared in both.
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A reactor: read for its uranium and its output, and heard as it runs out of uranium.</summary>
    public sealed class Reactor : Capability
    {
        static readonly OwnValue Output = OwnValue.Number<IMyReactor>(reactor => reactor.CurrentOutput);

        static readonly OwnValue MostOut = OwnValue.Number<IMyReactor>(reactor => reactor.MaxOutput);

        // Made as the first reactor is described: the uranium ingot's type can be named only once the game has registered
        // its types.
        OwnValue _fuel;

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyReactor;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            if (_fuel == null)
            {
                var uranium = new MyItemType("MyObjectBuilder_Ingot", "Uranium");
                _fuel = OwnValue.Number<IMyReactor>(reactor => Held(reactor, uranium));
            }

            kind.WithValue("uranium", "uranium", _fuel).Measured(" kg").Announced().In(Category.Fuel)
                .WithChange("runs-out-of-uranium", "runs out of uranium", "uranium", Literal.Of(0.0)).Announced()
                .Reading("has no uranium", "has uranium")
                .WithValue("output", "output", Output).Measured(" MW").Announced().In(Category.Power)
                .WithValue("maximum-output", "maximum output", MostOut).Measured(" MW").Announced().In(Category.Power)
                .Renaming("UseConveyor", true, "automatic push and pull")
                .Renaming("UseConveyor", false, "toggle automatic push and pull");
        }

        /// <summary>Told as the reactor's inventory changes and as its output changes.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var reactor = block as IMyReactor;
            var inventory = reactor?.GetInventory(0);
            return inventory == null ? null : new Listening(inventory, reactor, told);
        }

        /// <summary>How much of <paramref name="uranium" /> <paramref name="reactor" /> holds, in kilograms; none where it has no inventory.</summary>
        static double Held(IMyReactor reactor, MyItemType uranium)
        {
            var inventory = reactor.GetInventory(0);
            return inventory == null ? 0.0 : (double)inventory.GetItemAmount(uranium);
        }

        /// <summary>A reactor's inventory and output being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyInventory _inventory;
            readonly Action _told;
            readonly OutputListener _output;

            public Listening(IMyInventory inventory, IMyPowerProducer reactor, Action told)
            {
                _inventory = inventory;
                _told = told;
                _inventory.OnVolumeChanged += Changed;
                _output = new OutputListener(reactor, told);
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _inventory.OnVolumeChanged -= Changed;
                _output.Dispose();
            }

            void Changed(IMyInventory inventory, float before, float after)
            {
                _told();
            }
        }
    }
}
