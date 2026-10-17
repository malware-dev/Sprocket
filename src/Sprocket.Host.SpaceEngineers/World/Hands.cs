using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>Who a construct's automation belongs to, and its pause when the construct changes hands outside sharing.</summary>
    public sealed class Hands
    {
        readonly GridWorld _world;
        readonly Automation _program;
        readonly AutomationRunner _runner;
        readonly List<string> _returned = new List<string>();
        bool _changed = true;

        /// <summary>The hands <paramref name="program" />, on <paramref name="world" /> and run by <paramref name="runner" />, belongs in.</summary>
        public Hands(GridWorld world, Automation program, AutomationRunner runner)
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
        /// Takes the construct's big owners as the automation's, pausing it where it changed hands outside sharing; whether they
        /// changed, so the automation is written again.
        /// </summary>
        /// <remarks>
        /// Sharing works both ways: an owner the automation had who may still use its block now is exactly the case where the new
        /// owner could use it before, such as a faction mate handed a ship shared with the faction. A capture, or a hand-over to
        /// somebody it was not shared with, pauses it until a big owner resumes it. Once paused it stays paused, whoever owns it
        /// next, and remembers who it was taken from.
        /// </remarks>
        public bool Owned(IMyTerminalBlock hero)
        {
            _changed = false;
            var now = _world.BigOwners;
            var owners = _program.Owners;
            if (Same(owners, now))
                return false;

            if (!_program.Paused && !Shared(hero, owners, now))
            {
                Told.Say($"{_world.Name} changed hands outside sharing, and its automation is paused");
                _program.PausedFrom.AddRange(owners);
                _runner.Live = false;
                _returned.Clear();
                _returned.AddRange(_world.Returned);
            }

            owners.Clear();
            for (var i = 0; i < now.Count; i++)
                owners.Add(now[i]);

            return true;
        }

        /// <summary>Whether <paramref name="before" />, the owners the automation had, holds one who is among <paramref name="now" /> or may still use <paramref name="hero" />.</summary>
        static bool Shared(IMyTerminalBlock hero, List<long> before, IReadOnlyList<long> now)
        {
            if (before.Count == 0)
                return true;

            foreach (var owner in before)
            {
                if (Among(now, owner) || hero.HasPlayerAccess(owner))
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
