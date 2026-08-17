using System.Collections.Generic;

namespace Reactor.World
{
    /// <summary>
    /// A class of device and the whole vocabulary it brings with it - what can happen
    /// to it, what situations it can be in, and what it can be told to do.
    /// </summary>
    /// <remarks>
    /// The host owns this catalogue. Space Engineers builds kinds from real block
    /// types; the prototype host writes a handful by hand. Core never hardcodes a
    /// door or a light, which is what keeps the editor driven by whatever machine it
    /// happens to be pointed at.
    /// </remarks>
    public sealed class DeviceKind
    {
        readonly List<DeviceEvent> _events = new List<DeviceEvent>();
        readonly List<DeviceState> _states = new List<DeviceState>();
        readonly List<DeviceAction> _actions = new List<DeviceAction>();

        public DeviceKind(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        /// <summary>Stable identifier, referenced by saved sequences.</summary>
        public string Id { get; private set; }

        /// <summary>What this kind is called in the UI, e.g. "Door".</summary>
        public string DisplayName { get; private set; }

        /// <summary>Moments. These start sequences, and nothing else.</summary>
        public IList<DeviceEvent> Events { get { return _events; } }

        /// <summary>Situations. These gate steps and guard interlocks, and nothing else.</summary>
        public IList<DeviceState> States { get { return _states; } }

        public IList<DeviceAction> Actions { get { return _actions; } }

        public DeviceKind WithEvent(string id, string signalId, SignalEdge edge, string text)
        {
            _events.Add(new DeviceEvent(id, signalId, edge, text));
            return this;
        }

        /// <summary>
        /// Adds a situation. <paramref name="whenSignal"/> false describes the other
        /// side of the same signal, so one reading yields both "is open" and "is shut".
        /// </summary>
        public DeviceKind WithState(string id, string signalId, bool whenSignal, string text)
        {
            _states.Add(new DeviceState(id, signalId, whenSignal, text));
            return this;
        }

        /// <summary>
        /// Adds an action. <paramref name="leadsToStateId"/> names the state the
        /// action is trying to reach, which is what allows an interlock to stop it.
        /// </summary>
        public DeviceKind WithAction(string id, string text, string leadsToStateId = null)
        {
            _actions.Add(new DeviceAction(id, text, leadsToStateId));
            return this;
        }

        public DeviceEvent FindEvent(string eventId)
        {
            for (var i = 0; i < _events.Count; i++)
            {
                if (_events[i].Id == eventId)
                    return _events[i];
            }
            return null;
        }

        public DeviceState FindState(string stateId)
        {
            for (var i = 0; i < _states.Count; i++)
            {
                if (_states[i].Id == stateId)
                    return _states[i];
            }
            return null;
        }

        public DeviceAction FindAction(string actionId)
        {
            for (var i = 0; i < _actions.Count; i++)
            {
                if (_actions[i].Id == actionId)
                    return _actions[i];
            }
            return null;
        }
    }
}
