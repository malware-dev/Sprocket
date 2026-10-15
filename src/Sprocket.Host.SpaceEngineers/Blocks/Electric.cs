using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.Definitions;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A block that draws electricity from its grid: how much it uses, asks for and can take, and how much of that it gets.</summary>
    /// <remarks>
    /// Not a power producer: a battery takes electricity to charge, and what it takes and gives belongs to it. Read from the
    /// block's resource sink, which every block that draws electricity has.
    /// </remarks>
    public sealed class Electric : Capability
    {
        const string Busy = "What a block uses changes as often as every tick, faster than any when looks.";

        const string Unheard = "The game raises no event as the most a block can draw changes.";

        /// <summary>The game's resource id for electricity, transcribed from its resource distributor.</summary>
        static readonly MyDefinitionId Electricity = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");

        static readonly Being Use = Being.Number<IMyTerminalBlock>(block => block.ResourceSink.CurrentInputByType(Electricity));

        static readonly Being Required = Being.Number<IMyTerminalBlock>(block => block.ResourceSink.RequiredInputByType(Electricity));

        static readonly Being Most = Being.Number<IMyTerminalBlock>(block => block.ResourceSink.MaxRequiredInputByType(Electricity));

        static readonly Being Supplied = Being.Number<IMyTerminalBlock>(block => block.ResourceSink.SuppliedRatioByType(Electricity) * 100d);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return !(block is IMyPowerProducer) && Draws(block.ResourceSink);
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("power-use", "power use", Use).Measured(" MW").In(Category.Power).Looked(Busy)
                .WithValue("power-required", "power required", Required).Measured(" MW").In(Category.Power).Looked(Busy)
                .WithValue("maximum-power-use", "maximum power use", Most).Measured(" MW").In(Category.Power).Looked(Unheard)
                .WithValue("power-supplied", "power supplied", Supplied).Percent().In(Category.Power).Looked(Busy);
        }

        /// <summary>Whether <paramref name="sink" /> takes electricity.</summary>
        static bool Draws(MyResourceSinkComponentBase sink)
        {
            if (sink == null)
                return false;

            var accepted = sink.AcceptedResources;
            for (var i = 0; i < accepted.Count; i++)
            {
                if (accepted[i] == Electricity)
                    return true;
            }

            return false;
        }
    }
}
