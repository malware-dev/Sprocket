using System.Collections.Generic;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// Whether a vent is still working on a room, asked of the gas moving through it.
    /// </summary>
    /// <remarks>
    /// One per set of extensions, which is one per world: what it remembers is about the vents of one
    /// construct, and it goes when they do.
    /// <para>
    /// <b>The level cannot answer this and the status is worse.</b> A room that is as full as it
    /// will ever get settles below one and the game goes on calling it Pressurizing for as long as
    /// it exists, so waiting for either is waiting forever. What does answer it is the gas: a vent
    /// filling a room draws its full rate the whole time and drops to nothing the moment it is
    /// finished, and a vent emptying one pushes out the same way.
    /// </para>
    /// <para>
    /// Measured in game: 300 in while filling, 0 within one sample of finishing, 300 out while
    /// emptying. What reads it is an <see cref="IGasMeter" />.
    /// </para>
    /// </remarks>
    public sealed class VentGas
    {
        const float Trickle = 1f;

        const int Beat = 10;

        /// <summary>What reads the gas: the game's components, or what the harness put in their place.</summary>
        /// <remarks>
        /// <b>Static because the block vocabulary is,</b> as <see cref="Toolbars" /> is: the extensions are made by a
        /// registry with no instance to hand anything to. It holds a reference to an ordinary object rather than being one.
        /// </remarks>
        static IGasMeter _meter = new GameGasMeter();

        readonly Dictionary<long, int> _stopped = new Dictionary<long, int>();

        /// <summary>Puts something else behind every reading of a vent's gas.</summary>
        /// <remarks>For the harness, which answers it over the vents it makes up.</remarks>
        public static void Use(IGasMeter meter)
        {
            _meter = meter ?? new GameGasMeter();
        }

        /// <summary>
        /// Whether the vent is moving gas the way <paramref name="emptying" /> asks, right now.
        /// </summary>
        /// <remarks>
        /// The plain reading, with no clock on it. What a screen shows is about this instant and
        /// can be wrong for a frame without costing anybody anything, where a step finishing early
        /// opens a door - so only the settling path pays for the beat.
        /// </remarks>
        public bool Moving(IMyAirVent vent, bool emptying)
        {
            if (vent == null)
                return false;

            float flow = emptying ? Out(vent) : In(vent);
            return flow > Trickle;
        }

        /// <summary>Whether the vent is moving gas the way <paramref name="emptying" /> asks.</summary>
        /// <remarks>
        /// Read on the tick path while a step is settling, and it earns that: it is the difference
        /// between an airlock that opens the moment the room is ready and one that opens a second
        /// late, which is the one delay in a cycle a person feels. It is also reached only once the
        /// level is already inside the band - the reading before it short-circuits - so the cost
        /// falls in the last moments of a fill rather than for as long as the room is filling.
        /// </remarks>
        public bool Working(IMyAirVent vent, bool emptying)
        {
            if (vent == null)
                return false;

            float flow = emptying ? Out(vent) : In(vent);

            // No components to ask is no answer, and no answer must not read as "still working" or
            // a sequence waits for a thing that will never be reported.
            if (flow < 0f)
                return false;

            int frame = Frame();
            if (frame < 0)
                return false;

            int last;
            var seen = _stopped.TryGetValue(vent.EntityId, out last);
            if (flow > Trickle || (seen && frame < last))
                last = frame;
            else if (!seen)
                // Idle the first time it is looked at: nothing says it moved gas a moment ago, so it has stopped.
                last = frame - Beat;

            _stopped[vent.EntityId] = last;
            return frame - last < Beat;
        }

        int Frame()
        {
            return MyAPIGateway.Session == null ? -1 : MyAPIGateway.Session.GameplayFrameCounter;
        }

        /// <summary>What it is being given, which is how a vent fills a room.</summary>
        public float In(IMyAirVent vent)
        {
            return _meter.In(vent);
        }

        /// <summary>What it is passing on, which is how a vent empties one.</summary>
        public float Out(IMyAirVent vent)
        {
            return _meter.Out(vent);
        }
    }
}
