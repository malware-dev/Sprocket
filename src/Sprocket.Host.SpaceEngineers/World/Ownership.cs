using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>Who a construct's automation belongs to, and its pause when the construct changes hands outside sharing.</summary>
    public sealed class Ownership
    {
        readonly GridWorld _world;
        readonly Automation _program;
        readonly AutomationRunner _runner;
        readonly List<string> _returned = new List<string>();
        readonly ConstructSharing _sharing = new ConstructSharing();
        bool _changed = true;

        /// <summary>The hands <paramref name="program" />, on <paramref name="world" /> and run by <paramref name="runner" />, belongs in.</summary>
        public Ownership(GridWorld world, Automation program, AutomationRunner runner)
        {
            _world = world;
            _program = program;
            _runner = runner;
            _world.HandsChanged += Handed;
        }

        /// <summary>Whether the construct's owners are to be looked at, which a load and every change of hands asks for.</summary>
        public bool Due => _changed;

        /// <summary>The blocks the automation could reach again when it paused, which the resume screen names.</summary>
        public List<string> Returned => _returned;

        /// <summary>
        /// Takes the construct's big owners as the automation's, pausing it where it changed hands outside sharing, and reads
        /// who the construct is shared with off <paramref name="major" />, a block one of the big owners owns, null where none
        /// is left.
        /// </summary>
        /// <remarks>Once paused it stays paused until a big owner resumes it, whoever owns it next.</remarks>
        public void Owned(IMyTerminalBlock major)
        {
            _changed = false;
            var now = _world.BigOwners;
            var owners = _program.Owners;
            if (Same(owners, now))
            {
                Reread(major, now);
                return;
            }

            GameLog.Say($"{_world.Name} belongs to {Players(now)}, after {Players(owners)}");
            if (!_program.Paused && !Shared(major, owners, now))
            {
                GameLog.Say($"{_world.Name} changed hands outside sharing, and its automation is paused");
                _program.PausedFrom.AddRange(owners);
                _runner.Live = false;
                _returned.Clear();
                _returned.AddRange(_world.Returned);
            }

            owners.Clear();
            for (var i = 0; i < now.Count; i++)
                owners.Add(now[i]);

            Reread(major, now);
        }

        /// <summary>Reads who the construct is shared with off <paramref name="major" />, where it belongs to one of <paramref name="owners" />, the big owners.</summary>
        /// <remarks>A block belonging to anyone else holds somebody else's sharing, so the big owners' is kept as it was.</remarks>
        void Reread(IMyTerminalBlock major, IReadOnlyList<long> owners)
        {
            if (major != null && Among(owners, major.OwnerId))
                _sharing.Read(major);
        }

        /// <summary>
        /// Whether the construct passing from <paramref name="before" /> to <paramref name="now" /> stays within its sharing:
        /// nobody owned it, nobody owns it, or one of the new big owners could already use it.
        /// </summary>
        /// <remarks>
        /// Judged by the sharing as it was, since the game turns sharing off on every block it gives away; where that was
        /// never read, by whether one of <paramref name="before" /> may still use <paramref name="major" />, a block one of the
        /// new big owners owns.
        /// </remarks>
        bool Shared(IMyTerminalBlock major, List<long> before, IReadOnlyList<long> now)
        {
            if (before.Count == 0 || now.Count == 0)
                return true;

            foreach (var owner in now)
            {
                if (Among(before, owner) || _sharing.IsKnown && _sharing.Lets(owner))
                    return true;
            }

            if (_sharing.IsKnown || major == null)
                return false;

            foreach (var owner in before)
            {
                if (Access.Shared(major, owner))
                    return true;
            }

            return false;
        }

        /// <summary>The identities of <paramref name="players" />, for the log; "nobody" where there are none.</summary>
        static string Players(IReadOnlyList<long> players)
        {
            return players.Count == 0 ? "nobody" : string.Join(", ", players);
        }

        /// <summary>Whether <paramref name="player" /> is among <paramref name="players" />.</summary>
        static bool Among(IReadOnlyList<long> players, long player)
        {
            for (var i = 0; i < players.Count; i++)
            {
                if (players[i] == player)
                    return true;
            }

            return false;
        }

        /// <summary>Whether <paramref name="one" /> and <paramref name="other" /> name the same players, both in order.</summary>
        static bool Same(List<long> one, IReadOnlyList<long> other)
        {
            if (one.Count != other.Count)
                return false;

            for (var i = 0; i < one.Count; i++)
            {
                if (one[i] != other[i])
                    return false;
            }

            return true;
        }

        /// <summary>Stops listening for the construct changing hands.</summary>
        public void Release()
        {
            _world.HandsChanged -= Handed;
        }

        /// <summary>A block on the construct changing hands, or sharing changing, which the next tick looks at.</summary>
        void Handed()
        {
            _changed = true;
        }
    }
}
