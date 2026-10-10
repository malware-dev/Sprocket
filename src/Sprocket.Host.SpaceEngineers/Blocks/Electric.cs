using System;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.Definitions;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A block that draws electricity from its grid: whether it gets it, and how much it uses, asks for and can take.</summary>
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

        static readonly Being Powered = Being.Flag(Reading.Of<IMyTerminalBlock>(block => block.ResourceSink.IsPoweredByType(Electricity)));

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
            kind.WithValue("powered", "powered", Powered).In(Category.Power).Announced()
                .WithChange("loses-power", "loses power", "powered", Literal.Of(false))
                .Reading("is not powered", "is powered").Announced()
                .WithChange("gets-power", "gets power", "powered", Literal.Of(true))
                .Reading("is powered", "is not powered").Announced()
                .WithValue("power-use", "power use", Use).In(Category.Power).Looked(Busy)
                .WithValue("power-required", "power required", Required).In(Category.Power).Looked(Busy)
                .WithValue("maximum-power-use", "maximum power use", Most).In(Category.Power).Looked(Unheard)
                .WithValue("power-supplied", "power supplied", Supplied).In(Category.Power).Looked(Busy);
        }

        /// <summary>Told as the block gets or loses its power, which the game's own sink announces.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var sink = block.ResourceSink as MyResourceSinkComponent;
            return sink == null ? null : new Listening(sink, told);
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

        /// <summary>A sink being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly MyResourceSinkComponent _sink;
            readonly Action _told;

            public Listening(MyResourceSinkComponent sink, Action told)
            {
                _sink = sink;
                _told = told;
                _sink.IsPoweredChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _sink.IsPoweredChanged -= Changed;
            }

            void Changed()
            {
                _told();
            }
        }
    }
}
