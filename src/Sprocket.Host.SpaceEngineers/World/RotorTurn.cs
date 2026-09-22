using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// Whether a rotor is still going round, asked of the angle it is at.
    /// </summary>
    /// <remarks>
    /// <b>Nothing on a rotor says it is held.</b> Its speed is what it was told to run at, not what it
    /// is doing, and a rotor leaning on a wall reports the same speed and the same torque as one
    /// turning freely. The one thing that tells them apart is the angle: a rotor that is moving changes
    /// it, and one that is held does not.
    /// <para>
    /// The same shape as <see cref="VentGas" />, and for the same reason: a reading, the frame it last
    /// changed on, and a beat before anything is called stopped.
    /// </para>
    /// </remarks>
    static class RotorTurn
    {
        /// <summary>
        /// How far a rotor has to move to count as moving, in radians.
        /// </summary>
        /// <remarks>
        /// A rotor held against a limit is not perfectly still, and the slowest turn the game allows -
        /// a tenth of a revolution a minute - covers a hundred times this between two looks a second
        /// apart. Anything smaller is the block settling rather than travelling.
        /// </remarks>
        const float Stir = 0.0005f;

        /// <summary>
        /// How long a rotor has to have moved nothing before it counts as held, in frames.
        /// </summary>
        /// <remarks>
        /// A second, which is longer than the moment between a turn being asked for and the physics
        /// taking it up. Being held is reported rather than acted on, so late and right beats early
        /// and wrong.
        /// </remarks>
        const int Beat = 60;

        /// <summary>
        /// How long ago a look has to have been for it to say nothing about now, in frames.
        /// </summary>
        /// <remarks>
        /// A rotor is watched only while a sequence waits on it, so the look before this one may be
        /// from the last time it was waited on - minutes ago, at the same angle it happens to be at
        /// again. Read as a look just taken, that would call a rotor held before it had moved.
        /// </remarks>
        const int Stale = 600;

        /// <summary>Where each rotor was when it was last seen moving, and when that was.</summary>
        /// <remarks>
        /// One entry per rotor ever asked about, which is the rotors a sequence waits on. A rotor never
        /// seen moving is treated as having moved just now, so it is given its beat to get going rather
        /// than being called held before it has begun.
        /// </remarks>
        static readonly Dictionary<long, Went> _went = new Dictionary<long, Went>();

        /// <summary>
        /// Starts the watch afresh, for a rotor that has just been told to turn.
        /// </summary>
        /// <remarks>
        /// Told rather than worked out: a rotor waited on twice stands at the same angle at the start
        /// of the second turn as it did at the end of the first, and a watch carried over from then
        /// would call it held before it had been given a chance to move.
        /// </remarks>
        public static void Began(IMyMotorStator rotor)
        {
            int frame = Frame();
            if (rotor == null || frame < 0)
                return;

            _went[rotor.EntityId] = new Went(rotor.Angle, frame);
        }

        /// <summary>Whether the rotor has moved within the beat, and so is turning rather than held.</summary>
        public static bool Turning(IMyMotorStator rotor)
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

        static int Frame()
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
