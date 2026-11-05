using System;
using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Network;

namespace Sprocket.Host.SpaceEngineers.Shares
{
    /// <summary>The server's answer to what players ask about shared blueprints: it carries out each order, and says how things stand.</summary>
    /// <remarks>Runs on the server alone. Every order is checked here, since a client can send anything.</remarks>
    public sealed class ShareDesk
    {
        static readonly char[] Spaces = { ' ' };

        readonly BlueprintShares _shares;
        readonly Func<int> _limit;
        readonly Func<long, string> _name;
        readonly Func<List<long>> _players;
        readonly Func<long, PlayerMemory> _player;
        readonly Func<List<long>> _online;
        readonly Action<long, ShareTiding> _tell;

        /// <summary>
        /// The desk over <paramref name="shares" />, allowing what <paramref name="limit" /> says, naming players with
        /// <paramref name="name" />, listing the world's players with <paramref name="players" /> and finding their memory with
        /// <paramref name="player" />, and telling the players <paramref name="online" /> lists through <paramref name="tell" /> of what newly reaches them.
        /// </summary>
        public ShareDesk(BlueprintShares shares, Func<int> limit, Func<long, string> name, Func<List<long>> players,
            Func<long, PlayerMemory> player, Func<List<long>> online, Action<long, ShareTiding> tell)
        {
            _shares = shares;
            _limit = limit;
            _name = name;
            _players = players;
            _player = player;
            _online = online;
            _tell = tell;
        }

        /// <summary>Carries out <paramref name="order" /> from <paramref name="player" />, an admin where <paramref name="admin" /> says, and answers what they are to be told.</summary>
        public ShareNews Hear(ShareOrder order, long player, bool admin)
        {
            ShareAnswer? answer = null;
            switch (order.Act)
            {
                case ShareAct.Share:
                    var before = Held(player);
                    answer = _shares.Share(player, order.Blueprint, order.Reach, Identities(order.Players), _limit());
                    if (answer == ShareAnswer.Shared)
                        Announce(player, order.Blueprint, before);
                    break;
                case ShareAct.Withdraw:
                    Guid identity;
                    if ((order.Target == player || admin) && Guid.TryParse(order.Identity, out identity))
                        _shares.Withdraw(order.Target, identity);
                    break;
                case ShareAct.Block:
                    _shares.Block(player, order.Target);
                    break;
                case ShareAct.Unblock:
                    _shares.Unblock(player, order.Target);
                    break;
                case ShareAct.Bar:
                    if (admin)
                        _shares.Bar(order.Target);
                    break;
                case ShareAct.Unbar:
                    if (admin)
                        _shares.Unbar(order.Target);
                    break;
            }

            var news = News(answer, player, admin);
            if (order.Act == ShareAct.Look)
                _shares.Saw(player);

            return news;
        }

        /// <summary>What each player online holds now of the shares by <paramref name="sharer" />: the blueprint's text by its identity.</summary>
        Dictionary<long, Dictionary<Guid, string>> Held(long sharer)
        {
            var held = new Dictionary<long, Dictionary<Guid, string>>();
            foreach (var player in _online())
            {
                var theirs = new Dictionary<Guid, string>();
                foreach (var share in _shares.Reaching(player))
                {
                    if (share.Sharer == sharer)
                        theirs[share.Identity] = share.Blueprint;
                }

                held[player] = theirs;
            }

            return held;
        }

        /// <summary>Tells each player online what the share of <paramref name="blueprint" /> by <paramref name="sharer" /> changed for them, against what they held <paramref name="before" />.</summary>
        void Announce(long sharer, string blueprint, Dictionary<long, Dictionary<Guid, string>> before)
        {
            SharedBlueprint shared = null;
            foreach (var share in _shares.By(sharer))
            {
                if (share.Blueprint == blueprint)
                    shared = share;
            }

            if (shared == null)
                return;

            foreach (var player in before)
            {
                if (!_shares.Reaching(player.Key).Contains(shared))
                    continue;

                string had;
                var known = player.Value.TryGetValue(shared.Identity, out had);
                if (!known || had != blueprint)
                    _tell(player.Key, new ShareTiding { IsUpdate = known, Name = shared.Name, Sharer = _name(sharer) });
            }
        }

        ShareNews News(ShareAnswer? answer, long player, bool admin)
        {
            var news = new ShareNews
            {
                Answer = answer,
                Limit = _limit(),
                IsBarred = Contains(_shares.Barred, player),
                Reaching = new List<ShareLine>(),
                Mine = new List<ShareLine>(),
                All = new List<ShareLine>(),
                Known = new List<KnownPlayer>()
            };

            foreach (var share in _shares.Reaching(player))
            {
                var line = Line(share, true);
                line.IsNew = _shares.IsNewTo(player, share);
                news.Reaching.Add(line);
            }

            foreach (var share in _shares.By(player))
                news.Mine.Add(Line(share, false));

            if (admin)
            {
                foreach (var share in _shares.All)
                    news.All.Add(Line(share, false));
            }

            var blocked = _player(player).Blocked;
            foreach (var known in _players())
            {
                if (known == player)
                    continue;

                news.Known.Add(new KnownPlayer
                {
                    Identity = known,
                    Name = _name(known),
                    IsBlocked = blocked.Contains(known),
                    IsBarred = Contains(_shares.Barred, known)
                });
            }

            return news;
        }

        ShareLine Line(SharedBlueprint share, bool withBlueprint)
        {
            var players = new string[share.Players.Count];
            for (var i = 0; i < players.Length; i++)
                players[i] = share.Players[i].ToString(CultureInfo.InvariantCulture);

            return new ShareLine
            {
                Sharer = share.Sharer,
                SharerName = _name(share.Sharer),
                Identity = share.Identity.ToString("D"),
                Name = share.Name,
                Reach = share.Reach,
                Players = string.Join(" ", players),
                Blueprint = withBlueprint ? share.Blueprint : ""
            };
        }

        static bool Contains(IReadOnlyList<long> list, long player)
        {
            foreach (var one in list)
            {
                if (one == player)
                    return true;
            }

            return false;
        }

        static List<long> Identities(string said)
        {
            var identities = new List<long>();
            foreach (var word in (said ?? "").Split(Spaces, StringSplitOptions.RemoveEmptyEntries))
            {
                long identity;
                if (long.TryParse(word, NumberStyles.Integer, CultureInfo.InvariantCulture, out identity))
                    identities.Add(identity);
            }

            return identities;
        }
    }
}
