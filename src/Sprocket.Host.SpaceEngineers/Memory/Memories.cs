using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Memory
{
    /// <summary>What the world remembers of Sprocket while it runs: Sprocket's clock, each player's settings, and each construct's state until it is claimed.</summary>
    /// <remarks>
    /// Every tick kept is on Sprocket's clock, the game's frame carried on from where the world was saved, since the game's
    /// frame counter starts from nought each session. Read whole as the world loads and written whole as it saves.
    /// </remarks>
    public sealed class Memories
    {
        /// <summary>The name of the world's file the memory is kept in.</summary>
        public const string File = "state";

        readonly IBinary _binary;
        readonly Dictionary<long, ConstructMemory> _unclaimed = new Dictionary<long, ConstructMemory>();

        readonly Dictionary<long, PlayerMemory> _players = new Dictionary<long, PlayerMemory>();


        int _offset;

        /// <summary>The codec the world's memory is packed with, which a construct's block and a copy's memory are packed with too.</summary>
        public IBinary Binary => _binary;

        /// <summary>Memories turned to and from text by <paramref name="binary" />.</summary>
        public Memories(IBinary binary)
        {
            _binary = binary;
        }

        /// <summary>Why what the world held would not read, or empty where it read or there was nothing.</summary>
        public string Trouble { get; private set; } = "";

        /// <summary>Sprocket's clock on the game's <paramref name="frame" />.</summary>
        public int Now(int frame)
        {
            return frame + _offset;
        }

        /// <summary>The blueprints shared in this world.</summary>
        public List<SharedBlueprint> Shares { get; } = new List<SharedBlueprint>();

        /// <summary>The identities of the players an admin has stopped sharing.</summary>
        public List<long> Barred { get; } = new List<long>();

        /// <summary>Takes up what the world kept, <paramref name="packed" />, on the game's <paramref name="frame" />; nothing kept is a world new to Sprocket.</summary>
        public void Read(string packed, int frame)
        {
            _unclaimed.Clear();
            _players.Clear();
            Shares.Clear();
            Barred.Clear();
            _offset = 0;
            Trouble = "";
            if (string.IsNullOrEmpty(packed))
                return;

            WorldMemory memory;
            try
            {
                memory = _binary.Unpack<WorldMemory>(packed);
            }
            catch (Exception e)
            {
                Trouble = $"What the world remembers of Sprocket would not read, and starts afresh: {e.Message}";
                return;
            }

            _offset = memory.Clock - frame;
            foreach (var construct in memory.Constructs)
                _unclaimed[construct.Keeper] = construct;

            foreach (var player in memory.Players)
                _players[player.Player] = player;

            Shares.AddRange(memory.Shares);
            Barred.AddRange(memory.Barred);
        }

        /// <summary>The settings of the player with <paramref name="identity" />, made the first time they are asked for.</summary>
        public PlayerMemory Player(long identity)
        {
            PlayerMemory player;
            if (!_players.TryGetValue(identity, out player))
            {
                player = new PlayerMemory { Player = identity };
                _players.Add(identity, player);
            }

            return player;
        }

        /// <summary>The state filed under <paramref name="keeper" />, given up to whoever claims it; a fresh one where nothing is.</summary>
        public ConstructMemory Claim(long keeper)
        {
            ConstructMemory memory;
            if (!_unclaimed.TryGetValue(keeper, out memory))
                return new ConstructMemory { Keeper = keeper };

            _unclaimed.Remove(keeper);
            return memory;
        }

        /// <summary>
        /// What the world keeps now, on the game's <paramref name="frame" />: every live construct's state, and what nothing has
        /// claimed yet.
        /// </summary>
        /// <remarks>Called once a save.</remarks>
        public string Write(int frame, IList<ConstructMemory> constructs)
        {
            var memory = new WorldMemory { Clock = Now(frame) };
            memory.Constructs.AddRange(constructs);
            foreach (var unclaimed in _unclaimed.Values)
                memory.Constructs.Add(unclaimed);

            memory.Players.AddRange(_players.Values);
            memory.Shares.AddRange(Shares);
            memory.Barred.AddRange(Barred);

            return _binary.Pack(memory);
        }
    }
}
