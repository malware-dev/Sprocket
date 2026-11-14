using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>Whether a vent is still working on a room, asked of the gas moving through it.</summary>
    /// <remarks>Measured in game: 300 in while filling, 0 within one sample of finishing, 300 out while emptying.</remarks>
    public static class VentFlow
    {
        const float Trickle = 1f;

        const int Beat = 10;

        static IGasMeter _meter = new GameGasMeter();

        /// <summary>Puts something else behind every reading of a vent's gas.</summary>
        /// <remarks>For the test harness.</remarks>
        public static void Use(IGasMeter meter)
        {
            _meter = meter ?? new GameGasMeter();
        }

        /// <summary>Whether the vent is moving gas the way <paramref name="emptying" /> asks, right now.</summary>
        /// <remarks>Keeps no clock on the gas.</remarks>
        public static bool Moving(IMyAirVent vent, bool emptying)
        {
            if (vent == null)
                return false;

            float flow = emptying ? Out(vent) : In(vent);
            return flow > Trickle;
        }

        /// <summary>Whether the vent is moving gas the way <paramref name="emptying" /> asks, by the last frame <paramref name="seen" /> saw it moving.</summary>
        /// <remarks>Read on the tick path while a step is settling, and only once the level is already inside the band.</remarks>
        public static bool Working(IMyAirVent vent, bool emptying, ReaderMemory seen)
        {
            if (vent == null)
                return false;

            float flow = emptying ? Out(vent) : In(vent);

            // No components to ask is no answer, and no answer must not read as "still working" or
            // a sequence waits for a thing that will never be reported.
            if (flow < 0f)
                return false;

            int frame = seen.Now;
            if (frame < 0)
                return false;

            int last = seen.Frame;
            if (flow > Trickle || (seen.Seen && frame < last))
                last = frame;
            else if (!seen.Seen)
                // Idle the first time it is looked at: nothing says it moved gas a moment ago, so it has stopped.
                last = frame - Beat;

            seen.Frame = last;
            return frame - last < Beat;
        }

        /// <summary>What it is being given, which is how a vent fills a room.</summary>
        public static float In(IMyAirVent vent)
        {
            return _meter.In(vent);
        }

        /// <summary>What it is passing on, which is how a vent empties one.</summary>
        public static float Out(IMyAirVent vent)
        {
            return _meter.Out(vent);
        }
    }
}
