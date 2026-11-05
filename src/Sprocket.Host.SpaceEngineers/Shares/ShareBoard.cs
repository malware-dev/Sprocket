using System;
using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Network;

namespace Sprocket.Host.SpaceEngineers.Shares
{
    /// <summary>What this player knows of shared blueprints, as the server last told it, and the way to ask for more.</summary>
    public sealed class ShareBoard
    {
        static readonly List<ShareLine> NoLines = new List<ShareLine>();
        static readonly List<KnownPlayer> NoPlayers = new List<KnownPlayer>();

        readonly Action<ShareOrder> _post;

        /// <summary>A board that sends what is asked through <paramref name="post" />.</summary>
        public ShareBoard(Action<ShareOrder> post)
        {
            _post = post;
        }

        /// <summary>Raised when the server has told something new.</summary>
        public event Action Changed;

        /// <summary>The shares that reach this player, each with its blueprint.</summary>
        public IReadOnlyList<ShareLine> Reaching { get; private set; } = NoLines;

        /// <summary>This player's own shares.</summary>
        public IReadOnlyList<ShareLine> Mine { get; private set; } = NoLines;

        /// <summary>Every share in the world, where this player is an admin.</summary>
        public IReadOnlyList<ShareLine> All { get; private set; } = NoLines;

        /// <summary>The other players of this world.</summary>
        public IReadOnlyList<KnownPlayer> Known { get; private set; } = NoPlayers;

        /// <summary>How many blueprints each player may share.</summary>
        public int Limit { get; private set; }

        /// <summary>Whether this player is stopped from sharing.</summary>
        public bool IsBarred { get; private set; }

        /// <summary>What the server last said to a blueprint offered, or null where it has said nothing since.</summary>
        public ShareAnswer? Answer { get; private set; }

        /// <summary>This player's share of the blueprint <paramref name="identity" />, or null.</summary>
        public ShareLine MineOf(Guid identity)
        {
            var said = identity.ToString("D");
            foreach (var line in Mine)
            {
                if (line.Identity == said)
                    return line;
            }

            return null;
        }

        /// <summary>Whether anything reaching this player has changed since they last opened the library.</summary>
        public bool HasNew
        {
            get
            {
                foreach (var line in Reaching)
                {
                    if (line.IsNew)
                        return true;
                }

                return false;
            }
        }

        /// <summary>Tells the server this player has opened the library, and asks how things stand.</summary>
        public void Look()
        {
            _post(new ShareOrder { Act = ShareAct.Look });
        }

        /// <summary>Asks the server how things stand.</summary>
        public void Ask()
        {
            _post(new ShareOrder { Act = ShareAct.Ask });
        }

        /// <summary>Shares the blueprint <paramref name="text" /> with those <paramref name="reach" /> and <paramref name="players" /> say.</summary>
        public void Share(string text, ShareReach reach, IEnumerable<long> players)
        {
            var said = new List<string>();
            if (players != null)
            {
                foreach (var player in players)
                    said.Add(player.ToString(CultureInfo.InvariantCulture));
            }

            _post(new ShareOrder { Act = ShareAct.Share, Reach = reach, Blueprint = text, Players = string.Join(" ", said) });
        }

        /// <summary>Puts <paramref name="text" /> in place of what this player shares of the blueprint <paramref name="identity" />, for the same people; does nothing where they share none of it.</summary>
        public void Renew(Guid identity, string text)
        {
            var mine = MineOf(identity);
            if (mine != null)
                _post(new ShareOrder { Act = ShareAct.Share, Reach = mine.Reach, Blueprint = text, Players = mine.Players });
        }

        /// <summary>Takes back the share of the blueprint <paramref name="identity" /> from <paramref name="sharer" />.</summary>
        public void Withdraw(long sharer, Guid identity)
        {
            _post(new ShareOrder { Act = ShareAct.Withdraw, Target = sharer, Identity = identity.ToString("D") });
        }

        /// <summary>Hides what <paramref name="sharer" /> shares from this player.</summary>
        public void Block(long sharer)
        {
            _post(new ShareOrder { Act = ShareAct.Block, Target = sharer });
        }

        /// <summary>Shows this player what <paramref name="sharer" /> shares again.</summary>
        public void Unblock(long sharer)
        {
            _post(new ShareOrder { Act = ShareAct.Unblock, Target = sharer });
        }

        /// <summary>Stops <paramref name="player" /> sharing, and takes back what they share.</summary>
        public void Bar(long player)
        {
            _post(new ShareOrder { Act = ShareAct.Bar, Target = player });
        }

        /// <summary>Lets <paramref name="player" /> share again.</summary>
        public void Unbar(long player)
        {
            _post(new ShareOrder { Act = ShareAct.Unbar, Target = player });
        }

        /// <summary>Takes in what the server told.</summary>
        public void Heard(ShareNews news)
        {
            Reaching = news.Reaching;
            Mine = news.Mine;
            All = news.All;
            Known = news.Known;
            Limit = news.Limit;
            IsBarred = news.IsBarred;
            Answer = news.Answer;
            Changed?.Invoke();
        }
    }
}
