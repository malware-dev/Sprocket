using Reactor.Sequences;
using Reactor.World;

namespace Reactor.Runtime
{
    /// <summary>Reads whether a situation currently holds.</summary>
    public static class Conditions
    {
        /// <summary>
        /// True when every named device is in the named state.
        /// </summary>
        /// <remarks>
        /// An unfinished condition - no devices picked yet - is false rather than
        /// true. A half-written gate that let everything through would be a sequence
        /// that appears to work while the user is still building it, and then behaves
        /// differently once they finish.
        /// </remarks>
        public static bool IsTrue(IWorld world, Condition condition)
        {
            if (condition == null || condition.Devices.Count == 0)
                return false;

            for (var i = 0; i < condition.Devices.Count; i++)
            {
                if (!Holds(world, condition.Devices[i], condition.StateId))
                    return false;
            }
            return true;
        }

        /// <summary>As <see cref="IsTrue"/>, but ignoring one device.</summary>
        public static bool IsTrueExcept(IWorld world, Condition condition, long ignoreDeviceId)
        {
            for (var i = 0; i < condition.Devices.Count; i++)
            {
                if (condition.Devices[i] == ignoreDeviceId)
                    continue;
                if (!Holds(world, condition.Devices[i], condition.StateId))
                    return false;
            }
            return true;
        }

        static bool Holds(IWorld world, long deviceId, string stateId)
        {
            var device = world.FindDevice(deviceId);
            if (device == null)
                return false;

            var state = device.Kind.FindState(stateId);
            if (state == null)
                return false;

            return device.GetSignal(state.SignalId) == state.WhenSignal;
        }
    }
}
