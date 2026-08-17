using System.Collections.Generic;
using Reactor.World;

namespace Reactor.Editing
{
    /// <summary>
    /// Works out what a slot is allowed to contain, given what is already in the
    /// sequence.
    /// </summary>
    /// <remarks>
    /// Nothing invalid is ever offered. An illegal sequence is not rejected with a
    /// message - it is simply unreachable, which is worth more than any error text
    /// because the user never has to recover from anything.
    /// </remarks>
    public static class Vocabulary
    {
        /// <summary>
        /// Events every one of the given devices can raise.
        /// </summary>
        /// <remarks>
        /// The intersection, not the union. "When any of these happens" has to mean
        /// something for every device in the selection, or the sentence stops being
        /// true of the things it names.
        /// </remarks>
        public static List<DeviceEvent> EventsFor(IWorld world, IList<long> deviceIds)
        {
            var result = new List<DeviceEvent>();
            var devices = Resolve(world, deviceIds);
            if (devices.Count == 0)
                return result;

            var candidates = devices[0].Kind.Events;
            for (var i = 0; i < candidates.Count; i++)
            {
                if (AllHaveEvent(devices, candidates[i].Id))
                    result.Add(candidates[i]);
            }
            return result;
        }

        /// <summary>Actions every one of the given devices accepts.</summary>
        public static List<DeviceAction> ActionsFor(IWorld world, IList<long> deviceIds)
        {
            var result = new List<DeviceAction>();
            var devices = Resolve(world, deviceIds);
            if (devices.Count == 0)
                return result;

            var candidates = devices[0].Kind.Actions;
            for (var i = 0; i < candidates.Count; i++)
            {
                if (AllHaveAction(devices, candidates[i].Id))
                    result.Add(candidates[i]);
            }
            return result;
        }

        /// <summary>
        /// Devices that could be added to a selection without emptying its
        /// vocabulary.
        /// </summary>
        /// <remarks>
        /// This is what stops a user from building a selection of a door and a light
        /// and then discovering there is nothing they can do with it. Combinations
        /// that would leave nothing to say are never offered in the first place.
        /// </remarks>
        public static List<IDevice> CompatibleWith(IWorld world, IList<long> deviceIds, bool forEvents)
        {
            var result = new List<IDevice>();
            var devices = world.Devices;
            for (var i = 0; i < devices.Count; i++)
            {
                var device = devices[i];
                if (Contains(deviceIds, device.Id))
                    continue;

                var probe = new List<long>(deviceIds);
                probe.Add(device.Id);

                var count = forEvents ? EventsFor(world, probe).Count : ActionsFor(world, probe).Count;
                if (count > 0)
                    result.Add(device);
            }
            return result;
        }

        static List<IDevice> Resolve(IWorld world, IList<long> deviceIds)
        {
            var result = new List<IDevice>();
            for (var i = 0; i < deviceIds.Count; i++)
            {
                var device = world.FindDevice(deviceIds[i]);
                if (device != null)
                    result.Add(device);
            }
            return result;
        }

        static bool AllHaveEvent(List<IDevice> devices, string eventId)
        {
            for (var i = 1; i < devices.Count; i++)
            {
                if (devices[i].Kind.FindEvent(eventId) == null)
                    return false;
            }
            return true;
        }

        static bool AllHaveAction(List<IDevice> devices, string actionId)
        {
            for (var i = 1; i < devices.Count; i++)
            {
                if (devices[i].Kind.FindAction(actionId) == null)
                    return false;
            }
            return true;
        }

        static bool Contains(IList<long> ids, long id)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                if (ids[i] == id)
                    return true;
            }
            return false;
        }
    }
}
