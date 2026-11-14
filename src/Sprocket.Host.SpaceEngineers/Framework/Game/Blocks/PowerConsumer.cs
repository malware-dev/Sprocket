using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A block that draws electricity from its grid: how much it uses, asks for and can take, and how much of that it gets.</summary>
    public sealed class PowerConsumer : Capability
    {
        const string Busy = "What a block uses changes as often as every tick, faster than any when looks.";

        const string Unheard = "The game raises no event as the most a block can draw changes.";


        static readonly OwnValue Use = OwnValue.Number<IMyTerminalBlock>(block => block.ResourceSink.CurrentInputByType(GameResources.Electricity));

        static readonly OwnValue Required = OwnValue.Number<IMyTerminalBlock>(block => block.ResourceSink.RequiredInputByType(GameResources.Electricity));

        static readonly OwnValue Most = OwnValue.Number<IMyTerminalBlock>(block => block.ResourceSink.MaxRequiredInputByType(GameResources.Electricity));

        static readonly OwnValue Supplied = OwnValue.Number<IMyTerminalBlock>(block => block.ResourceSink.SuppliedRatioByType(GameResources.Electricity) * 100d);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return !(block is IMyPowerProducer) && Draws(block.ResourceSink);
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("power_use", "power use", Use).Measured(" MW").In(Category.Power).Looked(Busy)
                .WithValue("power_required", "power required", Required).Measured(" MW").In(Category.Power).Looked(Busy)
                .WithValue("maximum_power_use", "maximum power use", Most).Measured(" MW").In(Category.Power).Looked(Unheard)
                .WithValue("power_supplied", "power supplied", Supplied).Percent().In(Category.Power).Looked(Busy);
        }

        /// <summary>Whether <paramref name="sink" /> takes electricity.</summary>
        static bool Draws(MyResourceSinkComponentBase sink)
        {
            if (sink == null)
                return false;

            var accepted = sink.AcceptedResources;
            for (var i = 0; i < accepted.Count; i++)
            {
                if (accepted[i] == GameResources.Electricity)
                    return true;
            }

            return false;
        }
    }
}
