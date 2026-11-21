using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Running
{
    /// <summary>The value each installation holds in each of its system's states, where a step has set one, kept in what the world saves.</summary>
    /// <remarks>
    /// Each value is found in the construct's saved record by its state's name the first time it is read, and kept by the
    /// state itself from then on, so a renamed state keeps its value. A value its state can no longer hold, its type having
    /// changed, is dropped and reads as the starting value. Nothing here is looked at on a tick.
    /// </remarks>
    public sealed class StateValues
    {
        readonly Automation _automation;
        readonly Dictionary<SystemInstance, Dictionary<State, StateMemory>> _held =
            new Dictionary<SystemInstance, Dictionary<State, StateMemory>>();

        /// <summary>The values of <paramref name="automation" />'s states, kept in its saved record.</summary>
        public StateValues(Automation automation)
        {
            _automation = automation;
        }

        /// <summary>What <paramref name="instance" /> of <paramref name="system" /> holds in <paramref name="state" />: what a step last set it to, or its starting value.</summary>
        public Literal Read(AutomationSystem system, SystemInstance instance, State state)
        {
            var kept = Kept(system, instance, state, false);
            if (kept == null)
                return Starting(instance, state);

            var value = kept.Choice.Length > 0 ? Literal.Name(kept.Choice) : Literal.Of(kept.Flag);
            if (Fits(system, state, value))
                return value;

            GameLog.Say($"State {state.Name} of {system.Name} held a value it can no longer hold, and starts over at its starting value.");
            Drop(instance, state, kept);
            return Starting(instance, state);
        }

        /// <summary>The value <paramref name="state" /> starts at in <paramref name="instance" />: its starting value, or the value the installation gives the setting it starts at.</summary>
        /// <remarks>A setting with no value there leaves the setting standing; the sequence's check says so before anything runs.</remarks>
        static Literal Starting(SystemInstance instance, State state)
        {
            Literal given;
            return state.Start.IsSetting && instance != null && instance.Gives(state.Start.Text, out given) ? given : state.Start;
        }

        /// <summary>Gives <paramref name="state" /> of <paramref name="instance" /> the value <paramref name="value" />, answering whether that changed what it holds.</summary>
        public bool Write(AutomationSystem system, SystemInstance instance, State state, Literal value)
        {
            if (instance == null || StateMentions.Same(Read(system, instance, state), value))
                return false;

            var kept = Kept(system, instance, state, true);
            kept.State = state.Name;
            kept.Choice = value.Kind == ValueKind.Name ? value.Text : "";
            kept.Flag = value.Kind == ValueKind.Flag && value.Flag;
            return true;
        }

        /// <summary>Whether <paramref name="test" /> holds on what <paramref name="instance" /> holds in its states and gives its settings.</summary>
        /// <remarks>False where a state it names is gone, or a setting it names has no value in <paramref name="instance" />.</remarks>
        public bool Holds(AutomationSystem system, SystemInstance instance, StateTest test)
        {
            Literal tested;
            if (test.Setting.Length > 0)
            {
                if (instance == null || !instance.Gives(test.Setting, out tested))
                    return false;
            }
            else
            {
                var state = system.FindState(test.State);
                if (state == null)
                    return false;

                tested = Read(system, instance, state);
            }

            Literal other;
            if (test.Other.Length == 0)
            {
                other = test.Value;
                if (other.IsSetting && (instance == null || !instance.Gives(other.Text, out other)))
                    return false;
            }
            else
            {
                var compared = system.FindState(test.Other);
                if (compared == null)
                    return false;

                other = Read(system, instance, compared);
            }

            var same = StateMentions.Same(tested, other);
            return test.Relation == Relation.IsNot ? !same : same;
        }

        /// <summary>Brings the saved record up to the program as the world saves: each value under its state's name now, and none for a state that is gone.</summary>
        public void Settle()
        {
            foreach (var system in _automation.Systems)
            {
                foreach (var instance in system.Uses)
                {
                    Dictionary<State, StateMemory> values;
                    if (!_held.TryGetValue(instance, out values))
                        continue;

                    foreach (var held in new List<KeyValuePair<State, StateMemory>>(values))
                    {
                        if (system.States.Contains(held.Key))
                            held.Value.State = held.Key.Name;
                        else
                            Drop(instance, held.Key, held.Value);
                    }
                }
            }
        }

        /// <summary>Gives <paramref name="to" /> what <paramref name="from" /> holds in each of <paramref name="system" />'s states, and leaves <paramref name="from" /> holding none.</summary>
        /// <remarks>For a logic system made into a blueprint, whose first installation goes on from where it was.</remarks>
        public void Move(AutomationSystem system, SystemInstance from, SystemInstance to)
        {
            foreach (var state in system.States)
            {
                var kept = Kept(system, from, state, false);
                if (kept == null)
                    continue;

                Drop(from, state, kept);
                kept.Instance = to.Number;
                _automation.Memory.States.Add(kept);
                Dictionary<State, StateMemory> values;
                if (!_held.TryGetValue(to, out values))
                {
                    values = new Dictionary<State, StateMemory>();
                    _held.Add(to, values);
                }

                values[state] = kept;
            }
        }

        /// <summary>
        /// What the saved record holds for <paramref name="state" /> of <paramref name="instance" />, found by name the first
        /// time and kept by the state after; made where it holds nothing and <paramref name="make" /> says so, or null.
        /// </summary>
        StateMemory Kept(AutomationSystem system, SystemInstance instance, State state, bool make)
        {
            if (instance == null)
                return null;

            Dictionary<State, StateMemory> values;
            if (!_held.TryGetValue(instance, out values))
            {
                values = new Dictionary<State, StateMemory>();
                _held.Add(instance, values);
            }

            StateMemory kept;
            if (values.TryGetValue(state, out kept))
                return kept;

            var saved = _automation.Memory.States;
            foreach (var one in saved)
            {
                if (one.System == system.Identity && one.Instance == instance.Number && one.State == state.Name)
                {
                    values.Add(state, one);
                    return one;
                }
            }

            if (!make)
                return null;

            kept = new StateMemory { System = system.Identity, Instance = instance.Number, State = state.Name };
            saved.Add(kept);
            values.Add(state, kept);
            return kept;
        }

        /// <summary>Lets go of every record it found, so each is found again in the construct's memory as it stands.</summary>
        /// <remarks>For a copy whose memory was put back in place from the one that lent it.</remarks>
        public void Forget()
        {
            _held.Clear();
        }

        /// <summary>Forgets what <paramref name="instance" /> held in <paramref name="state" />, in the saved record as well.</summary>
        void Drop(SystemInstance instance, State state, StateMemory kept)
        {
            _automation.Memory.States.Remove(kept);
            Dictionary<State, StateMemory> values;
            if (_held.TryGetValue(instance, out values))
                values.Remove(state);
        }

        /// <summary>Whether <paramref name="state" /> can hold <paramref name="value" /> as it is declared now.</summary>
        public static bool Fits(AutomationSystem system, State state, Literal value)
        {
            if (state.IsFlag)
                return value.Kind == ValueKind.Flag;

            var type = system.FindType(state.Type);
            return value.Kind == ValueKind.Name && type != null && type.Has(value.Text);
        }
    }
}
