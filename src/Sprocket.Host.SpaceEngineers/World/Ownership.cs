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
        /// Takes the construct's big owners as the automation's, pausing it where it changed hands outside sharing.
        /// </summary>
        /// <remarks>Once paused it stays paused until a big owner resumes it, whoever owns it next.</remarks>
        public void Owned(IMyTerminalBlock hero)
        {
            _changed = false;
            var now = _world.BigOwners;
            var owners = _program.Owners;
            if (Same(owners, now))
                return;

            GameLog.Say($"{_world.Name} belongs to {Players(now)}, after {Players(owners)}");
            if (!_program.Paused && !Shared(hero, owners, now))
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
        }

        /// <summary>Whether <paramref name="before" />, the owners the automation had, holds one who is among <paramref name="now" /> or may still use <paramref name="hero" />.</summary>
        static bool Shared(IMyTerminalBlock hero, List<long> before, IReadOnlyList<long> now)
        {
            if (before.Count == 0)
                return true;

            foreach (var owner in before)
            {
                if (Among(now, owner) || Access.Shared(hero, owner))
                    return true;
            }

            return false;
        }

        /// <summary>Whether <paramref name="player" /> may open the automation: one of the construct's big owners, or anybody where nobody owns any of it.</summary>
        public bool Owns(long player)
        {
            var owners = _world.BigOwners;
            return owners.Count == 0 || Among(owners, player);
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
