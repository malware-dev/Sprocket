using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>Whether a rotor is still going round, asked of the angle it is at.</summary>
    /// <remarks>The game reports a held rotor's speed and torque as it does a free one's, so only its angle tells them apart.</remarks>
    public static class RotorMotion
    {
        const float Stir = 0.0005f;

        const int Beat = 60;

        const int Stale = 600;

        /// <summary>Starts the watch afresh, for a rotor that has just been told to turn.</summary>
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
