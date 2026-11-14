using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System;
using Sandbox.ModAPI;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>Whether a suspension's wheel is still moving along its travel, asked of where the wheel stands.</summary>
    /// <remarks>
    /// The game says nothing of a wheel's travel, so it is measured: how far the wheel stands from the suspension along the
    /// suspension's forward, which is the axis its height moves the wheel on. A ship carried by the wheels settles with them.
    /// </remarks>
    public static class WheelMotion
    {
        // A loaded ship moves at a centimeter a second or less in the last of its settling (WheelHeightGameTest).
        const double Stir = 0.005;

        const int Beat = 30;

        const int Stale = 600;

        /// <summary>Starts the watch afresh, for a suspension that has just been told to move its wheel.</summary>
        public static void Began(IMyMotorSuspension suspension, ReaderMemory seen)
        {
            var frame = seen.Now;
            if (suspension == null || frame < 0)
                return;

            seen.Number = Travel(suspension);
            seen.Frame = frame;
        }

        /// <summary>Whether the wheel has moved along its travel within the beat, or has no wheel to measure.</summary>
        public static bool Moving(IMyMotorSuspension suspension, ReaderMemory seen)
        {
            if (suspension?.Top == null)
                return false;

            var frame = seen.Now;
            if (frame < 0)
                return true;

            var travel = Travel(suspension);
            if (!seen.Seen
                || Math.Abs(travel - seen.Number) > Stir
                || frame < seen.Frame
                || frame - seen.Frame > Stale)
            {
                seen.Number = travel;
                seen.Frame = frame;
            }

            return frame - seen.Frame < Beat;
        }

        /// <summary>How far the wheel of <paramref name="suspension" /> stands along the suspension's forward, in meters.</summary>
        static double Travel(IMyMotorSuspension suspension)
        {
            var top = suspension.Top;
            return top == null ? 0 : Vector3D.Dot(top.GetPosition() - suspension.GetPosition(), suspension.WorldMatrix.Forward);
        }
    }
}
