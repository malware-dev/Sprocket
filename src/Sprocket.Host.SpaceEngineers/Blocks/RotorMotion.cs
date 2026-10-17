using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
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
    /// The same shape as <see cref="VentFlow" />, and for the same reason: a reading, the frame it last
    /// changed on, and a beat before anything is called stopped. What it remembers is the
    /// <see cref="ReaderMemory" /> of the step turning the rotor: the angle in its number and the frame in its frame.
    /// </para>
    /// </remarks>
    public static class RotorMotion
    {
        const float Stir = 0.0005f;

        const int Beat = 60;

        const int Stale = 600;

        /// <summary>
        /// Starts the watch afresh, for a rotor that has just been told to turn.
        /// </summary>
        /// <remarks>
        /// Told rather than worked out: a rotor waited on twice stands at the same angle at the start
        /// of the second turn as it did at the end of the first, and a watch carried over from then
        /// would call it held before it had been given a chance to move.
        /// </remarks>
        public static void Began(IMyMotorStator rotor, ReaderMemory seen)
        {
            int frame = seen.Now;
            if (rotor == null || frame < 0)
                return;

            seen.Number = rotor.Angle;
            seen.Frame = frame;
        }

        /// <summary>Whether the rotor has moved within the beat, and so is turning rather than held.</summary>
        public static bool Turning(IMyMotorStator rotor, ReaderMemory seen)
        {
            if (rotor == null)
                return false;

            int frame = seen.Now;
            if (frame < 0)
                return true;

            if (!seen.Seen
                || Math.Abs(rotor.Angle - (float)seen.Number) > Stir
                || frame < seen.Frame
                || frame - seen.Frame > Stale)
            {
                seen.Number = rotor.Angle;
                seen.Frame = frame;
            }

            return frame - seen.Frame < Beat;
        }
    }
}
