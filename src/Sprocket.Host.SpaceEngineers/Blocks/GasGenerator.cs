using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A gas generator, which turns ice into oxygen and hydrogen, and the irrigation system, which turns it into water.</summary>
    public sealed class GasGenerator : Capability
    {
        static readonly MyItemType Ice = MyItemType.MakeOre("Ice");

        // Ice is read off the generator's inventory, in kilograms.
        static readonly OwnValue Held = OwnValue.Number<IMyGasGenerator>(generator =>
        {
            var inventory = generator.GetInventory(0);
            return inventory == null ? 0.0 : (double)inventory.GetItemAmount(Ice);
        });

        static readonly OwnValue Producing = OwnValue.Flag(Reading.Of<IMyGasGenerator>(generator => generator.IsProducing));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyGasGenerator;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("ice", "ice", Held).Measured(" kg")
                .Looked("A generator announces what it holds every time it burns ice, which saves no look while it works.")
                .In(Category.Gas)
                .WithChange("runs-out-of-ice", "runs out of ice", "ice", Literal.Of(0.0)).Reading("has no ice", "has ice").Announced()
                .WithValue("producing", "producing", Producing)
                .Looked("A generator makes gas only while something draws on it, and announces neither.").In(Category.Gas)
                .Renaming("Refill", false, "refill bottles")
                .Renaming("Auto-Refill", true, "refill bottles automatically")
                .Renaming("Auto-Refill", false, "toggle refill bottles automatically")
                .Renaming("AutomaticallyPullBottles", true, "automatically pull bottles")
                .Renaming("AutomaticallyPullBottles", false, "toggle automatically pull bottles")
                .Renaming("UseConveyor", true, "automatic push and pull")
                .Renaming("UseConveyor", false, "toggle automatic push and pull");
        }

        /// <summary>Told as the generator runs out of ice and as it gets some back.</summary>
        /// <remarks>Only that: its own properties-changed event comes every time it burns ice.</remarks>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            return ResourceListener.Heard(block, told);
        }
    }
}
