using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Sharing
{
    /// <summary>The blueprints shared in this world and who may share, as the server stores them.</summary>
    /// <remarks>Runs on the server alone. One sharer and one blueprint identity make one share.</remarks>
    public sealed class BlueprintShares
    {
        /// <summary>The longest blueprint text a share may hold, in characters.</summary>
        /// <remarks>Every share travels to each player it reaches, so the size of one is a server's to bear.</remarks>
        public const int Longest = 256 * 1024;

        readonly List<SharedBlueprint> _shares;
        readonly List<long> _barred;
        readonly Func<long, PlayerMemory> _player;
        readonly Func<long, long, bool> _sameFaction;
        readonly Values _values;
        readonly Func<int> _now;

        /// <summary>
        /// The shares in <paramref name="shares" /> and the barred players in <paramref name="barred" />, changed in place,
        /// with each player's memory from <paramref name="player" />, faction membership from
        /// <paramref name="sameFaction" />, blueprints read with <paramref name="values" />, and the time from <paramref name="now" />.
        /// </summary>
        /// <remarks>A share whose blueprint no longer reads is dropped, as nothing could show or use it.</remarks>
        public BlueprintShares(List<SharedBlueprint> shares, List<long> barred, Func<long, PlayerMemory> player,
            Func<long, long, bool> sameFaction, Values values, Func<int> now)
        {
            _shares = shares;
            _barred = barred;
            _player = player;
            _sameFaction = sameFaction;
            _values = values;
            _now = now;

            for (var i = _shares.Count - 1; i >= 0; i--)
            {
                if (!Read(_shares[i]))
                    _shares.RemoveAt(i);
            }
        }

        /// <summary>Every share in the world.</summary>
        public IReadOnlyList<SharedBlueprint> All => _shares;

        /// <summary>The players who may not share.</summary>
        public IReadOnlyList<long> Barred => _barred;

        /// <summary>
        /// Shares <paramref name="blueprint" /> from <paramref name="sharer" /> with those <paramref name="reach" /> and
        /// <paramref name="players" /> say, in place of any share of it they have, allowing each player
        /// <paramref name="limit" /> shares.
        /// </summary>
        public ShareAnswer Share(long sharer, string blueprint, ShareReach reach, IList<long> players, int limit)
        {
            if (limit < 1)
                return ShareAnswer.Closed;

            if (_barred.Contains(sharer))
                return ShareAnswer.Barred;

            var share = new SharedBlueprint { Sharer = sharer, Blueprint = blueprint ?? "", Reach = reach };
            if (share.Blueprint.Length > Longest || !Read(share))
                return ShareAnswer.Unreadable;

            var had = -1;
            var counted = 0;
            for (var i = 0; i < _shares.Count; i++)
            {
                if (_shares[i].Sharer != sharer)
                    continue;

                counted++;
                if (_shares[i].Identity == share.Identity)
                    had = i;
            }

            if (had < 0 && counted >= limit)
                return ShareAnswer.AtLimit;

            if (reach == ShareReach.Players && players != null)
                share.Players.AddRange(players);

            share.Changed = had >= 0 && _shares[had].Blueprint == share.Blueprint ? _shares[had].Changed : _now();
            if (had < 0)
                _shares.Add(share);
            else
                _shares[had] = share;

            return ShareAnswer.Shared;
        }

        /// <summary>Takes back the share of the blueprint <paramref name="identity" /> from <paramref name="sharer" />.</summary>
        public void Withdraw(long sharer, Guid identity)
        {
            for (var i = _shares.Count - 1; i >= 0; i--)
            {
                if (_shares[i].Sharer == sharer && _shares[i].Identity == identity)
                    _shares.RemoveAt(i);
            }
        }

        /// <summary>The shares that reach <paramref name="player" />: for them, and from nobody they have blocked.</summary>
        public List<SharedBlueprint> Reaching(long player)
        {
            var blocked = _player(player).Blocked;
            var reaching = new List<SharedBlueprint>();
            foreach (var share in _shares)
            {
                if (share.Sharer != player && !blocked.Contains(share.Sharer) && Reaches(share, player))
                    reaching.Add(share);
            }

            return reaching;
        }

        /// <summary>The shares <paramref name="sharer" /> has made.</summary>
        public List<SharedBlueprint> By(long sharer)
        {
            var made = new List<SharedBlueprint>();
            foreach (var share in _shares)
            {
                if (share.Sharer == sharer)
                    made.Add(share);
            }

            return made;
        }

        /// <summary>Whether <paramref name="share" /> has changed since <paramref name="player" /> last opened the library.</summary>
        public bool IsNewTo(long player, SharedBlueprint share)
        {
            return share.Changed > _player(player).SawShares;
        }

        /// <summary>Counts everything shared so far as seen by <paramref name="player" />.</summary>
        public void Saw(long player)
        {
            _player(player).SawShares = _now();
        }

        /// <summary>Hides what <paramref name="sharer" /> shares from <paramref name="player" />.</summary>
        public void Block(long player, long sharer)
        {
            var blocked = _player(player).Blocked;
            if (player != sharer && !blocked.Contains(sharer))
                blocked.Add(sharer);
        }

        /// <summary>Shows <paramref name="player" /> what <paramref name="sharer" /> shares again.</summary>
        public void Unblock(long player, long sharer)
        {
            _player(player).Blocked.Remove(sharer);
        }

        /// <summary>Stops <paramref name="player" /> sharing, and takes back everything they share.</summary>
        public void Bar(long player)
        {
            if (!_barred.Contains(player))
                _barred.Add(player);

            for (var i = _shares.Count - 1; i >= 0; i--)
            {
                if (_shares[i].Sharer == player)
                    _shares.RemoveAt(i);
            }
        }

        /// <summary>Lets <paramref name="player" /> share again.</summary>
        public void Unbar(long player)
        {
            _barred.Remove(player);
        }

        bool Reaches(SharedBlueprint share, long player)
        {
            switch (share.Reach)
            {
                case ShareReach.Everyone:
                    return true;
                case ShareReach.Faction:
                    return _sameFaction(share.Sharer, player);
                case ShareReach.Players:
                    return share.Players.Contains(player);
                default:
                    return false;
            }
        }

        /// <summary>Fills in what <paramref name="share" />'s text says of it, answering whether it read.</summary>
        bool Read(SharedBlueprint share)
        {
            AutomationSystem read;
            if (ProgramReader.Lone(share.Blueprint, _values, out read).Length > 0)
                return false;

            share.Identity = read.Identity;
            share.Name = read.Name;
            return true;
        }
    }
}
