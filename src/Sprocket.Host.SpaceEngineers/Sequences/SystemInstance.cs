using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// One instance of a system, pointed at real blocks.
    /// </summary>
    /// <remarks>
    /// A system is what a system is; this is one somebody has. The front airlock and the port
    /// airlock are two of these over one system, and fixing the system fixes both.
    ///
    /// The player never meets the word. It is called <c>Airlock 1</c> until they rename it,
    /// the way the game itself calls a block <c>Interior Light 4</c> - provisional enough to
    /// invite a better name without a dialog asking for one, and it never collides with the
    /// name of the system it belongs to.
    ///
    /// What it is for is the blocks: each of these slots its own doors and its own vent into
    /// the system's block slots, which is why one system can run two airlocks at opposite ends
    /// of a ship. See <see cref="Fill" /> and <see cref="Resolve" /> - the runtime goes through them
    /// on every step of every running sequence.
    /// </remarks>
    public sealed class SystemInstance
    {
        bool _enabled;

        public SystemInstance(int number)
        {
            Number = number;
            Name = "";
            Enabled = true;
        }

        /// <summary>Which one of its system this is.</summary>
        public int Number { get; private set; }

        /// <summary>
        /// What the player called it, or empty if they have not said.
        /// </summary>
        /// <remarks>
        /// Empty is the ordinary state. The name shown is worked out from the system and this
        /// number - Airlock 1 - so a system that only learns its own name later takes its
        /// children with it rather than leaving them called something stale.
        /// </remarks>
        public string Name { get; set; }

        public bool HasName => !string.IsNullOrEmpty(Name);

        /// <summary>
        /// Whether this one may run, independently of the others.
        /// </summary>
        /// <remarks>
        /// Welding on the port airlock should stop the port airlock, not every airlock made from the
        /// same blueprint.
        ///
        /// Separate from the system's switch, and each has to survive the other. Stopping the whole
        /// system to work on it and starting it again must leave a deliberately stopped one stopped.
        /// </remarks>
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value)
                    Halt = null;

                _enabled = value;
            }
        }

        /// <summary>Why Sprocket switched this one off because it could not work, or null where it did not.</summary>
        public Halt Halt { get; set; }

        readonly Dictionary<string, List<string>> _fills = new Dictionary<string, List<string>>();

        public IEnumerable<string> Filled => _fills.Keys;

        /// <summary>Whether no slot has been filled.</summary>
        public bool IsEmpty => _fills.Count == 0;

        /// <summary>Slots this one's own blocks into one of the system's block slots.</summary>
        public void Fill(string slot, IList<string> entities)
        {
            List<string> chosen;
            if (!_fills.TryGetValue(slot, out chosen))
            {
                chosen = new List<string>();
                _fills[slot] = chosen;
            }

            chosen.Clear();
            for (var i = 0; i < entities.Count; i++)
                chosen.Add(entities[i]);
        }

        public void Clear(string slot)
        {
            _fills.Remove(slot);
        }

        public IList<string> FilledWith(string slot)
        {
            List<string> chosen;
            return _fills.TryGetValue(slot, out chosen) ? chosen : null;
        }

        /// <summary>
        /// Turns what the system says into what this one actually acts on.
        /// </summary>
        /// <remarks>
        /// Appends rather than returning, and takes the list to append to, because this runs
        /// for every step of every running sequence on every tick. Handing back a fresh list
        /// each time would be a steady drip of garbage into the game's update loop.
        ///
        /// A slot nobody filled resolves to what the system was built with. For the first
        /// one of a system that is exactly right, and for a later one it is the honest failure:
        /// it goes on pointing at somebody else's door until it is told better, which is
        /// visible, rather than silently doing nothing.
        /// </remarks>
        /// <summary>
        /// What <paramref name="names" /> come to as <paramref name="instance" /> fills them, or the names
        /// as they are where there is no instance.
        /// </summary>
        public static List<string> Through(SystemInstance instance, IList<string> names)
        {
            var seen = new List<string>();
            if (instance != null)
            {
                instance.Resolve(names, seen);
                return seen;
            }

            for (var i = 0; i < names.Count; i++)
                seen.Add(names[i]);

            return seen;
        }

        public void Resolve(IList<string> slots, List<string> into)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                List<string> chosen;
                if (!_fills.TryGetValue(slots[i], out chosen))
                {
                    into.Add(slots[i]);
                    continue;
                }

                for (var n = 0; n < chosen.Count; n++)
                    into.Add(chosen[n]);
            }
        }
    }
}
