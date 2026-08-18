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

        int _nextSequenceId = 1;

        public IReadOnlyList<Sequence> Sequences { get { return _sequences; } }

        public Sequence AddSequence(Trigger trigger)
        {
            var sequence = new Sequence(_nextSequenceId++, trigger);
            _sequences.Add(sequence);
            return sequence;
        }

        public void RemoveSequence(Sequence sequence)
        {
            _sequences.Remove(sequence);
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
        /// How many sequences touch the given device, without allocating.
        /// </summary>
        /// <remarks>
        /// Asked once per listed block on every tick, so it must not produce garbage
        /// - core runs inside the game's update loop.
        /// </remarks>
        public int CountReferencing(long deviceId)
        {
            var count = 0;
            for (var i = 0; i < _sequences.Count; i++)
            {
                if (References(_sequences[i], deviceId))
                    count++;
            }
            return count;
        }

        public static bool References(Sequence sequence, long deviceId)
        {
            var when = sequence.Trigger as WhenTrigger;
            if (when != null && when.Sources.Contains(deviceId))
                return true;

            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                var step = sequence.Steps[i];
                if (step.Targets.Contains(deviceId))
                    return true;

                var gate = step.Gate as OnceGate;
                if (gate != null && gate.Condition.Devices.Contains(deviceId))
                    return true;
            }
            return false;
        }
    }
}
