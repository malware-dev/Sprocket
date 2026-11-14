using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Wiring
{
    /// <summary>Which sequences nothing on a construct can start, on which one of their system, and why where that is known.</summary>
    public sealed class UnstartableSequences
    {
        readonly HashSet<long> _unstartable = new HashSet<long>();
        readonly Dictionary<long, UnansweredButton> _why = new Dictionary<long, UnansweredButton>();

        /// <summary>How many times this has been told again, so what reads it knows to read it again.</summary>
        public int Told { get; private set; }

        /// <summary>One sequence on one of its system, as one number.</summary>
        public static long Key(int sequenceId, SystemInstance instance)
        {
            return ((long)sequenceId << 32) | (uint)(instance?.Number ?? 0);
        }

        /// <summary>Takes <paramref name="starts" />, the sequences nothing can start each as <see cref="Key" /> gives it, and <paramref name="why" /> for those where that is known.</summary>
        public void Tell(List<long> starts, Dictionary<long, UnansweredButton> why)
        {
            Told++;
            _unstartable.Clear();
            foreach (var start in starts)
                _unstartable.Add(start);

            _why.Clear();
            foreach (var pair in why)
                _why[pair.Key] = pair.Value;
        }

        /// <summary>Why nothing can start <paramref name="sequence" /> on <paramref name="instance" />, or null where that is not known.</summary>
        public UnansweredButton Why(Sequence sequence, SystemInstance instance)
        {
            UnansweredButton why;
            return _why.TryGetValue(Key(sequence.Id, instance), out why) ? why : null;
        }

        /// <summary>Whether anything on the construct can start <paramref name="sequence" /> on <paramref name="instance" />.</summary>
        public bool CanStart(Sequence sequence, SystemInstance instance)
        {
            return _unstartable.Count == 0 || !_unstartable.Contains(Key(sequence.Id, instance));
        }
    }
}
