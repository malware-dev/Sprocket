using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// Whether a rotor is still going round, asked of the angle it is at.
    /// </summary>
    /// <remarks>
    /// One per set of extensions, which is one per world: what it remembers is about the rotors of one
    /// construct, and it goes when they do.
    /// <para>
    /// <b>Nothing on a rotor says it is held.</b> Its speed is what it was told to run at, not what it
    /// is doing, and a rotor leaning on a wall reports the same speed and the same torque as one
    /// turning freely. The one thing that tells them apart is the angle: a rotor that is moving changes
    /// it, and one that is held does not.
    /// </para>
    /// <para>
    /// The same shape as <see cref="VentGas" />, and for the same reason: a reading, the frame it last
    /// changed on, and a beat before anything is called stopped.
    /// </para>
    /// </remarks>
    public sealed class RotorTurn
    {
        const float Stir = 0.0005f;

        const int Beat = 60;

        const int Stale = 600;

        readonly Dictionary<long, Went> _went = new Dictionary<long, Went>();

        /// <summary>
        /// Starts the watch afresh, for a rotor that has just been told to turn.
        /// </summary>
        /// <remarks>
        /// Told rather than worked out: a rotor waited on twice stands at the same angle at the start
        /// of the second turn as it did at the end of the first, and a watch carried over from then
        /// would call it held before it had been given a chance to move.
        /// </remarks>
        public void Began(IMyMotorStator rotor)
        {
            int frame = Frame();
            if (rotor == null || frame < 0)
                return;

            _went[rotor.EntityId] = new Went(rotor.Angle, frame);
        }

        /// <summary>Whether the rotor has moved within the beat, and so is turning rather than held.</summary>
        public bool Turning(IMyMotorStator rotor)
        {
            if (rotor == null)
                return false;

            int frame = Frame();
            if (frame < 0)
                return true;

            Went was;
            if (!_went.TryGetValue(rotor.EntityId, out was)
                || Math.Abs(rotor.Angle - was.Angle) > Stir
                || frame < was.At
                || frame - was.At > Stale)
            {
                was = new Went(rotor.Angle, frame);
            }

            _went[rotor.EntityId] = was;
            return frame - was.At < Beat;
        }

        int Frame()
        {
            return MyAPIGateway.Session == null ? -1 : MyAPIGateway.Session.GameplayFrameCounter;
        }

        /// <summary>Where a rotor was when it was last seen moving, and the frame that was.</summary>
        struct Went
        {
            public readonly float Angle;
            public readonly int At;

            public Went(float angle, int at)
            {
                Angle = angle;
                At = at;
            }
        }
    }
}
