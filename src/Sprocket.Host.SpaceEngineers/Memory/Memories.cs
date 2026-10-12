using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Memory
{
    /// <summary>What the world remembers of Sprocket while it runs: Sprocket's clock, each player's settings, and each construct's state and door's coming close until it is claimed.</summary>
    /// <remarks>
    /// <b>Sprocket keeps a clock of its own.</b> The game's frame counter starts again from nought with every session, so a
    /// tick remembered across a save would mean another moment after the load. Sprocket's clock is the game's frame carried
    /// on from where the world was saved, so every tick it remembers still means the same moment, and a reload passes
    /// unnoticed.
    /// <para>
    /// Read whole as the world loads and written whole as it saves. A memory that will not read is said out loud and ignored:
    /// the world carries on from a fresh one, which the next save writes over it.
    /// </para>
    /// </remarks>
    public sealed class Memories
    {
        /// <summary>The name of the world's file the memory is kept in.</summary>
        public const string File = "state";

        readonly IBinary _binary;
        readonly Dictionary<long, ConstructMemory> _unclaimed = new Dictionary<long, ConstructMemory>();

        readonly Dictionary<long, PlayerMemory> _players = new Dictionary<long, PlayerMemory>();

        readonly Dictionary<long, int> _closing = new Dictionary<long, int>();

        int _offset;

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

        /// <summary>Takes up what the world kept, <paramref name="packed" />, on the game's <paramref name="frame" />; nothing kept is a world new to Sprocket.</summary>
        public void Read(string packed, int frame)
        {
            _unclaimed.Clear();
            _players.Clear();
            _closing.Clear();
            _offset = 0;
            Trouble = "";
            if (string.IsNullOrEmpty(packed))
                return;

            WorldMemory memory;
            try
            {
                memory = _binary.Unpack(packed);
            }
            catch (Exception e)
            {
                Trouble = "What the world remembers of Sprocket would not read, and starts afresh: " + e.Message;
                return;
            }

            _offset = memory.Clock - frame;
            for (var i = 0; i < memory.Constructs.Count; i++)
                _unclaimed[memory.Constructs[i].Keeper] = memory.Constructs[i];

            for (var i = 0; i < memory.Players.Count; i++)
                _players[memory.Players[i].Player] = memory.Players[i];

            for (var i = 0; i < memory.Doors.Count; i++)
                _closing[memory.Doors[i].Door] = memory.Doors[i].Due;
        }

        /// <summary>The tick <paramref name="door" /> was to close on when the world was saved, given up to whoever claims it; -1 where none was coming.</summary>
        public int Closing(long door)
        {
            int due;
            if (!_closing.TryGetValue(door, out due))
                return -1;

            _closing.Remove(door);
            return due;
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
        /// What the world keeps now, on the game's <paramref name="frame" />: every live construct's state, every door's coming
        /// close, and what nothing has claimed yet.
        /// </summary>
        /// <remarks>Once a save. What nothing claimed is kept too, so a construct whose automation is not built yet loses nothing.</remarks>
        public string Write(int frame, IList<ConstructMemory> constructs, IList<DoorMemory> doors)
        {
            var memory = new WorldMemory { Clock = Now(frame) };
            memory.Constructs.AddRange(constructs);
            foreach (var unclaimed in _unclaimed.Values)
                memory.Constructs.Add(unclaimed);

            memory.Players.AddRange(_players.Values);

            memory.Doors.AddRange(doors);
            foreach (var closing in _closing)
                memory.Doors.Add(new DoorMemory { Door = closing.Key, Due = closing.Value });

            return _binary.Pack(memory);
        }
    }
}
