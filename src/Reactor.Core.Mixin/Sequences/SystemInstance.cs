using System.Collections.Generic;

namespace Reactor.Sequences
{
    /// <summary>
    /// One of a system, actually fitted to the ship.
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
    /// It holds nothing but a name so far. What it is for is the blocks: each of these will
    /// point the system's roles at its own doors and its own vent, which is why one system can
    /// run two airlocks at opposite ends of a ship.
    /// </remarks>
    public sealed class SystemInstance
    {
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
        /// The third switch, and the one somebody actually reaches for: welding on the port
        /// airlock should stop the port airlock, not both of them and not every rule in the
        /// system one at a time.
        ///
        /// Separate from the system's switch and from each sequence's, and each has to survive
        /// the others. Stopping the whole system to work on it and starting it again must
        /// leave a deliberately-parked one still parked.
        /// </remarks>
        public bool Enabled { get; set; }

        /// <summary>
        /// Which of this one's own things stand in for the parts the system was built with.
        /// </summary>
        /// <remarks>
        /// The whole of what makes one of a system different from another. The system's
        /// sequences all point at the blocks its author used; this says "where they used that
        /// door, I use this one". The first one of a system fills in nothing at all, because
        /// the blocks the author built with are its blocks - which is why every system built
        /// before roles existed keeps working untouched.
        ///
        /// A part may be filled with several things, and needs no new rule to read correctly:
        /// several blocks on a trigger already means <em>any of them</em> and several in a
        /// condition already means <em>all of them</em>, so a wide airlock with two inner doors
        /// says the right thing in both places without anybody deciding it should.
        /// </remarks>
        readonly Dictionary<long, List<long>> _fills = new Dictionary<long, List<long>>();

        public IEnumerable<long> Filled => _fills.Keys;

        /// <summary>Points a part of the system at the things this one uses for it.</summary>
        public void Fill(long role, IList<long> entities)
        {
            List<long> chosen;
            if (!_fills.TryGetValue(role, out chosen))
            {
                chosen = new List<long>();
                _fills[role] = chosen;
            }

            chosen.Clear();
            for (var i = 0; i < entities.Count; i++)
                chosen.Add(entities[i]);
        }

        public void Clear(long role)
        {
            _fills.Remove(role);
        }

        public IList<long> FilledWith(long role)
        {
            List<long> chosen;
            return _fills.TryGetValue(role, out chosen) ? chosen : null;
        }

        /// <summary>
        /// Turns what the system says into what this one actually acts on.
        /// </summary>
        /// <remarks>
        /// Appends rather than returning, and takes the list to append to, because this runs
        /// for every step of every running sequence on every tick. Handing back a fresh list
        /// each time would be a steady drip of garbage into the game's update loop.
        ///
        /// A part nobody filled in resolves to what the system was built with. For the first
        /// one of a system that is exactly right, and for a later one it is the honest failure:
        /// it goes on pointing at somebody else's door until it is told better, which is
        /// visible, rather than silently doing nothing.
        /// </remarks>
        public void Resolve(IList<long> roles, List<long> into)
        {
            for (var i = 0; i < roles.Count; i++)
            {
                List<long> chosen;
                if (!_fills.TryGetValue(roles[i], out chosen))
                {
                    into.Add(roles[i]);
                    continue;
                }

                for (var n = 0; n < chosen.Count; n++)
                    into.Add(chosen[n]);
            }
        }
    }
}
