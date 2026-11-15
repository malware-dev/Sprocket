using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A micro emitter: a pulse to fire now or after a countdown, once it has charged.</summary>
    /// <remarks>
    /// The game's own discharge, countdown and their buttons come from the emitter's component, and the Mod API lists and finds
    /// only the actions and values of a block's type, never those of its components: a server never has them. Each is
    /// Sprocket's own, through the component, as a warhead's are.
    /// </remarks>
    public sealed class MicroEmitter : Capability
    {
        const string Uncounted = "A micro emitter raises no event that says its countdown has started or stopped.";

        const string Unset = "A micro emitter raises no event that says its countdown was changed.";

        static readonly OwnAction Discharges = OwnAction.Of<IMyFunctionalBlock>(block => Pulse(block)?.Discharge());

        static readonly OwnAction Starts = OwnAction.Of<IMyFunctionalBlock>(block => Pulse(block)?.StartCountdown());

        static readonly OwnAction Stops = OwnAction.Of<IMyFunctionalBlock>(block => Pulse(block)?.StopCountdown());

        static readonly OwnValue Counting = OwnValue.Flag(Reading.Of<IMyFunctionalBlock>(block => Pulse(block)?.IsCountingDown == true));

        static readonly OwnValue Countdown = OwnValue.Number<IMyFunctionalBlock>(block => Pulse(block)?.Countdown ?? 0f)
            .Writing<IMyFunctionalBlock>(Set);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            IMyEmpEntityComponent pulse;
            return block is IMyFunctionalBlock && block.Components.TryGet(out pulse);
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction("discharge", "discharge", Discharges, "Discharge").In(Category.Weapons)
                .WithAction("start_countdown", "start countdown", Starts, "StartCountdown").In(Category.Weapons)
                .WithAction("stop_countdown", "stop countdown", Stops, "StopCountdown").In(Category.Weapons)
                .WithValue("counting_down", "counting down", Counting).Looked(Uncounted).In(Category.Weapons)
                .WithValue("countdown", "countdown", Countdown).Measured(" s").Looked(Unset).In(Category.Weapons)
                .Hiding("Countdown", "IncreaseCountdown", "DecreaseCountdown");
        }

        static IMyEmpEntityComponent Pulse(IMyFunctionalBlock block)
        {
            IMyEmpEntityComponent pulse;
            return block.Components.TryGet(out pulse) ? pulse : null;
        }

        static void Set(IMyFunctionalBlock block, Model.Literal seconds)
        {
            var pulse = Pulse(block);
            if (pulse != null)
                pulse.Countdown = (float)seconds.Number;
        }
    }
}
