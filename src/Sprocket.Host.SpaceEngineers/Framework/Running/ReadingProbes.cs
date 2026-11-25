using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Running
{
    /// <summary>The blocks one reading of an expression reads, made ready for the options it was made with.</summary>
    public sealed class ReadingProbes
    {
        readonly Literal[] _made;

        /// <summary>The blocks read, made ready for <paramref name="arguments" />.</summary>
        public ReadingProbes(IList<Literal> arguments)
        {
            _made = new Literal[arguments.Count];
            for (var i = 0; i < _made.Length; i++)
                _made[i] = arguments[i];
        }

        /// <summary>What reads each block.</summary>
        public List<Probe> Probes { get; } = new List<Probe>();

        /// <summary>What the reading that rules out a change in each block remembers between looks, in the order of <see cref="Probes" />; null where it remembers nothing.</summary>
        public List<ReaderMemory> Rulings { get; } = new List<ReaderMemory>();

        /// <summary>What listens to the blocks that announce a change.</summary>
        public List<IDisposable> Listening { get; } = new List<IDisposable>();

        /// <summary>Whether these are made for the options <paramref name="arguments" />, which are the ones worked out now.</summary>
        /// <remarks>Allocates nothing.</remarks>
        public bool Takes(IList<Literal> arguments)
        {
            if (arguments.Count != _made.Length)
                return false;

            for (var i = 0; i < _made.Length; i++)
            {
                if (!StateMentions.Same(_made[i], arguments[i]))
                    return false;
            }

            return true;
        }

        /// <summary>Stops listening to every block.</summary>
        public void Stop()
        {
            foreach (var listening in Listening)
                listening.Dispose();

            Listening.Clear();
        }
    }
}
