using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>Whether a block that should be moving still is, asked of where it stands: a rotor's angle, a piston's position.</summary>
    /// <remarks>
    /// The game reports a held rotor's speed and torque as it does a free one's, and a held piston's speed and status as it
    /// does a free one's, so only where they stand tells them apart.
    /// </remarks>
    public static class MotionWatch
    {
        const float Stir = 0.0005f;

        const int Beat = 60;

        const int Stale = 600;

        /// <summary>Starts the watch afresh at <paramref name="at" />, for a block that has just been told to move.</summary>
        public static void Began(float at, ReaderMemory seen)
        {
            int frame = seen.Now;
            if (frame < 0)
                return;

            seen.Number = at;
            seen.Frame = frame;
        }

        /// <summary>Whether the block, standing at <paramref name="at" />, has moved within the beat, and so is moving rather than held.</summary>
        public static bool Moving(float at, ReaderMemory seen)
        {
            int frame = seen.Now;
            if (frame < 0)
                return true;

            if (!seen.Seen
                || Math.Abs(at - (float)seen.Number) > Stir
                || frame < seen.Frame
                || frame - seen.Frame > Stale)
            {
                seen.Number = at;
                seen.Frame = frame;
            }

            return frame - seen.Frame < Beat;
        }
    }
}
