using System.Collections.Generic;

namespace Reactor.Sequences
{
    /// <summary>
    /// Everything one Reactor unit is running.
    /// </summary>
    /// <remarks>
    /// Sequences are the only thing in here, and that is a deliberate ceiling.
    /// Reactor is an automation system, not a collection of finished automations, so
    /// anything expressible as a trigger plus steps does not get to become a
    /// primitive of its own.
    ///
    /// A worked example: "the doors must never both be open" looks like it wants a
    /// dedicated concept, and it does not. Two sequences say it - lock the outer door
    /// when the inner one opens, unlock it when the inner one shuts - and they say it
    /// better, because locking is a real action on the real world and therefore holds
    /// against a player operating the door by hand. A built-in rule could only ever
    /// have refused Reactor's own actions.
    /// </remarks>
    public sealed class Automation
    {
        readonly List<Sequence> _sequences = new List<Sequence>();
        readonly List<AutomationSystem> _systems = new List<AutomationSystem>();

        int _nextSequenceId = 1;
        int _nextSystemId = 1;

        /// <summary>
        /// Every sequence, flat, in the order they were made.
        /// </summary>
        /// <remarks>
        /// What the runtime iterates. Sequences also belong to a system, and the two views
        /// are kept in step here rather than by flattening on demand - the runtime asks for
        /// this on every tick and must not be given a freshly built list each time.
        /// </remarks>
        public IReadOnlyList<Sequence> Sequences { get { return _sequences; } }

        /// <summary>The systems, which is what the editor opens onto.</summary>
        public IReadOnlyList<AutomationSystem> Systems { get { return _systems; } }

        /// <summary>
        /// A new system, with one of it already on the ship.
        /// </summary>
        /// <remarks>
        /// Making a system and then being asked to place one would be two steps for one
        /// thought. Somebody who builds a thing has one of that thing.
        /// </remarks>
        public AutomationSystem AddSystem()
        {
            var system = new AutomationSystem(_nextSystemId++);
            system.AddInstance();
            _systems.Add(system);
            return system;
        }

        /// <summary>
        /// Puts back a system that was saved, keeping the id it had.
        /// </summary>
        /// <remarks>
        /// Ids have to survive a reload unchanged, because saved sequences refer to each other
        /// by them. Reallocating on load would silently repoint every reference - the classic
        /// way an event-sourced or serialised model goes wrong, and invisible until something
        /// runs the wrong sequence.
        ///
        /// No instance is added, unlike <see cref="AddSystem"/>: whatever was saved is what
        /// comes back, and inventing an extra one would multiply systems on every reload.
        /// </remarks>
        public AutomationSystem Restore(int id)
        {
            var system = new AutomationSystem(id);
            _systems.Add(system);

            if (id >= _nextSystemId)
                _nextSystemId = id + 1;

            return system;
        }

        /// <summary>Puts back a sequence that was saved, keeping the id it had.</summary>
        public Sequence Restore(AutomationSystem system, int id, Trigger trigger)
        {
            var sequence = new Sequence(id, trigger);
            _sequences.Add(sequence);
            system.Add(sequence);

            if (id >= _nextSequenceId)
                _nextSequenceId = id + 1;

            return sequence;
        }

        public AutomationSystem FindSystem(int id)
        {
            for (var i = 0; i < _systems.Count; i++)
            {
                if (_systems[i].Id == id)
                    return _systems[i];
            }
            return null;
        }

        /// <summary>The system a sequence belongs to, or null if it somehow belongs to none.</summary>
        public AutomationSystem SystemOf(Sequence sequence)
        {
            for (var i = 0; i < _systems.Count; i++)
            {
                var sequences = _systems[i].Sequences;
                for (var s = 0; s < sequences.Count; s++)
                {
                    if (ReferenceEquals(sequences[s], sequence))
                        return _systems[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Adds a sequence to a system, making one if none was given.
        /// </summary>
        /// <remarks>
        /// Nothing is declared before it is used, so somebody who starts building gets a
        /// system without having asked for one and without being made to name it first.
        /// </remarks>
        public Sequence AddSequence(Trigger trigger)
        {
            return AddSequence(null, trigger);
        }

        public Sequence AddSequence(AutomationSystem system, Trigger trigger)
        {
            if (system == null)
                system = _systems.Count > 0 ? _systems[_systems.Count - 1] : AddSystem();

            var sequence = new Sequence(_nextSequenceId++, trigger);
            _sequences.Add(sequence);
            system.Add(sequence);
            return sequence;
        }

        public void RemoveSequence(Sequence sequence)
        {
            _sequences.Remove(sequence);
            for (var i = 0; i < _systems.Count; i++)
                _systems[i].Remove(sequence);
        }

        public Sequence FindSequence(int id)
        {
            for (var i = 0; i < _sequences.Count; i++)
            {
                if (_sequences[i].Id == id)
                    return _sequences[i];
            }
            return null;
        }

        /// <summary>Named sequences, which are the ones another trigger can start.</summary>
        public void NamedSequences(List<Sequence> result)
        {
            for (var i = 0; i < _sequences.Count; i++)
            {
                if (_sequences[i].HasName)
                    result.Add(_sequences[i]);
            }
        }

        /// <summary>
        /// How many sequences touch the given entity, without allocating.
        /// </summary>
        /// <remarks>
        /// Asked once per listed block on every tick, so it must not produce garbage
        /// - core runs inside the game's update loop.
        /// </remarks>
        public int CountReferencing(long entityId)
        {
            var count = 0;
            for (var i = 0; i < _sequences.Count; i++)
            {
                if (References(_sequences[i], entityId))
                    count++;
            }
            return count;
        }

        public static bool References(Sequence sequence, long entityId)
        {
            var when = sequence.Trigger as WhenTrigger;
            if (when != null && when.Sources.Contains(entityId))
                return true;

            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                var step = sequence.Steps[i];
                if (step.Targets.Contains(entityId))
                    return true;

                var gate = step.Gate as OnceGate;
                if (gate != null && gate.Condition.Entities.Contains(entityId))
                    return true;
            }
            return false;
        }
    }
}
