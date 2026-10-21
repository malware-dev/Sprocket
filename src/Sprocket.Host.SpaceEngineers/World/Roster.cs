using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>Every Sprocket in the world, each filed under the programmable block that keeps it.</summary>
    public sealed class Roster : IDisposable
    {
        readonly Dictionary<long, SprocketUnit> _units = new Dictionary<long, SprocketUnit>();
        readonly Dictionary<long, long> _moved = new Dictionary<long, long>();
        readonly List<SprocketUnit> _saving = new List<SprocketUnit>();

        // The Sprockets whose program or switches changed and are not written into their hero yet.
        readonly HashSet<SprocketUnit> _unwritten = new HashSet<SprocketUnit>();
        readonly Func<Settings> _settings;
        readonly Rota _rota;
        readonly BlockKinds _kinds;
        readonly Texts _texts;
        readonly Memories _memories;
        readonly Backups _backups;
        readonly Func<int> _frame;
        readonly Action<Action> _later;

        /// <summary>An empty roster; <paramref name="later" /> runs what must wait for the game to finish its current event.</summary>
        public Roster(Func<Settings> settings, Rota rota, BlockKinds kinds, Texts texts, Memories memories, Backups backups, Func<int> frame,
            Action<Action> later)
        {
            _settings = settings;
            _rota = rota;
            _kinds = kinds;
            _texts = texts;
            _memories = memories;
            _backups = backups;
            _frame = frame;
            _later = later;
        }

        /// <summary>Tells the clients what a Sprocket now holds.</summary>
        public Action<SprocketUnit> Spreading { get; set; }

        /// <summary>Tells the clients a block holds no Sprocket any more.</summary>
        public Action<long> Emptied { get; set; }

        /// <summary>Told of each Sprocket let go of, so a screen open on it closes.</summary>
        public Action<SprocketUnit> Dropped { get; set; }

        /// <summary>The library a lost Sprocket's backup is shelved beside.</summary>
        public LibraryShelf Shelf { get; set; }

        /// <summary>How many there are.</summary>
        public int Count => _units.Count;

        /// <summary>Each of them.</summary>
        public IEnumerable<SprocketUnit> All => _units.Values;

        /// <summary>The Sprocket <paramref name="block" /> keeps, or null.</summary>
        public SprocketUnit Kept(IMyTerminalBlock block)
        {
            SprocketUnit unit;
            return block != null && _units.TryGetValue(block.EntityId, out unit) ? unit : null;
        }

        /// <summary>The Sprocket kept under <paramref name="id" />, or where it moved to, or null.</summary>
        public SprocketUnit Kept(long id)
        {
            SprocketUnit unit;
            return _units.TryGetValue(Key(id), out unit) ? unit : null;
        }

        /// <summary>The id the automation once kept under <paramref name="id" /> is kept under now.</summary>
        public long Key(long id)
        {
            long next;
            while (_moved.TryGetValue(id, out next))
                id = next;

            return id;
        }

        /// <summary>The Sprocket of the construct <paramref name="block" /> is on, or null.</summary>
        /// <remarks>One lookup per Sprocket in the world; never asked on a tick.</remarks>
        public SprocketUnit On(IMyTerminalBlock block)
        {
            SprocketUnit unit;
            if (_units.TryGetValue(block.EntityId, out unit))
                return unit;

            foreach (KeyValuePair<long, SprocketUnit> each in _units)
            {
                if (each.Value.World.Holds(block.EntityId))
                    return each.Value;
            }

            return null;
        }

        /// <summary>The Sprocket of the construct <paramref name="block" /> is on, made where there is none, and joined where the block brings another.</summary>
        public SprocketUnit Adopt(IMyTerminalBlock block)
        {
            SprocketUnit unit = On(block);
            if (unit != null && (unit.Block == block || !BlockStorage.HasProgram(block)))
                return unit;

            SprocketUnit arrived = new SprocketUnit(block, _settings(), _rota, _kinds, _texts, _memories.Claim(block.EntityId), _backups);
            arrived.Begin(_frame());
            if (unit != null)
                return Join(unit, arrived) ?? unit;

            Listen(arrived);
            _units.Add(block.EntityId, arrived);
            return arrived;
        }

        /// <summary>Builds <paramref name="was" /> again from <paramref name="program" /> and <paramref name="switches" />, in its place; answers it, or null where the text will not read.</summary>
        public SprocketUnit Replace(SprocketUnit was, string program, string switches)
        {
            var id = was.Block.EntityId;
            SprocketUnit rebuilt = was.Rebuilt(program, switches, _settings(), _rota);
            if (!rebuilt.Program.Intact)
            {
                GameLog.Say($"a change to {was.Block.CustomName} would not read, and was not taken");
                rebuilt.Dispose();
                return null;
            }

            _units[id] = rebuilt;
            Listen(rebuilt);

            // A pairing missing either version was settled.
            rebuilt.Memory.Conflicts.RemoveAll(conflict =>
                rebuilt.Program.FindSystem(conflict.System) == null || rebuilt.Program.FindSystem(conflict.Waiting) == null);
            Drop(was);
            rebuilt.Persist();
            File(id, rebuilt);
            Spread(rebuilt);
            return rebuilt;
        }

        /// <summary>Ticks each of them, runs the world's rota, then brings each open screen up to date.</summary>
        public void Tick(int frame)
        {
            foreach (KeyValuePair<long, SprocketUnit> unit in _units)
                unit.Value.Tick(frame);

            _rota.Run(frame);

            foreach (KeyValuePair<long, SprocketUnit> unit in _units)
                unit.Value.Show(frame);
        }

        /// <summary>Writes into its hero, now, every one whose change is still waiting to be written, as the world saves.</summary>
        /// <remarks>Everything else is in its hero already, since each is written as it changes.</remarks>
        public void Flush()
        {
            _saving.Clear();
            _saving.AddRange(_unwritten);
            foreach (var unit in _saving)
                Write(unit);

            _saving.Clear();
        }

        /// <summary>A Sprocket's program or switches changed: it is written into its hero once the change is done.</summary>
        void Changed(SprocketUnit unit)
        {
            if (_unwritten.Add(unit))
                _later(() => Write(unit));
        }

        /// <summary>Writes <paramref name="unit" /> into its hero and tells the clients, where it is still waiting to be.</summary>
        void Write(SprocketUnit unit)
        {
            if (!_unwritten.Remove(unit))
                return;

            var was = unit.Block.EntityId;
            unit.Persist();
            File(Key(was), unit);
            if (!unit.Lost)
                Spread(unit);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (KeyValuePair<long, SprocketUnit> unit in _units)
                unit.Value.Dispose();

            _units.Clear();
        }

        void Listen(SprocketUnit unit)
        {
            unit.HeroRemoved += Removed;
            unit.Gathered += Gathered;
            unit.Changed += Changed;
        }

        void Removed(SprocketUnit unit)
        {
            _later(() => Rehome(unit));
        }

        void Rehome(SprocketUnit unit)
        {
            long was = unit.Block.EntityId;
            SprocketUnit held;
            if (!_units.TryGetValue(was, out held) || held != unit)
                return;

            unit.Rehome();
            File(was, unit);
        }

        void Gathered(SprocketUnit unit)
        {
            _later(() => Meet(unit));
        }

        /// <summary>Gives each programmable block that parted from <paramref name="unit" />'s construct a copy, and joins any automation it now meets.</summary>
        void Meet(SprocketUnit unit)
        {
            SprocketUnit held;
            if (!_units.TryGetValue(unit.Block.EntityId, out held) || held != unit)
                return;

            var hero = unit.Block;
            foreach (IMyTerminalBlock block in new List<IMyTerminalBlock>(unit.Parted))
            {
                if (On(block) == null)
                    Part(On(hero), block);
            }

            unit = On(hero);
            foreach (IMyTerminalBlock holder in new List<IMyTerminalBlock>(unit.Holders))
            {
                SprocketUnit other;
                if (holder != hero && _units.TryGetValue(holder.EntityId, out other) && other != unit)
                    unit = Join(unit, other) ?? unit;
            }
        }

        void Part(SprocketUnit unit, IMyTerminalBlock block)
        {
            string program;
            string state;
            unit.Written(out program, out state);
            BlockStorage.Store(block, program, StateText.Save(unit.Program, null, false));

            SprocketUnit half = Adopt(block);
            if (half == null)
                return;

            GameLog.Say($"{block.CustomName} parted from the construct of {unit.Block.CustomName}, with a copy of its automation");
            string switches;
            var away = half.World;
            program = AutomationMerge.Part(half, unit.World, out switches);
            var parted = Replace(half, program, switches);
            if (parted != null)
                parted.TakeOver(unit.Block.EntityId);

            program = AutomationMerge.Part(unit, away, out switches);
            Replace(unit, program, switches);
        }

        /// <summary>Joins <paramref name="other" /> into <paramref name="kept" />, emptying <paramref name="other" />'s block; answers the rebuilt Sprocket, or null.</summary>
        SprocketUnit Join(SprocketUnit kept, SprocketUnit other)
        {
            var conflicts = new List<ConflictMemory>();
            string switches;
            var program = AutomationMerge.Join(kept, other, conflicts, out switches);

            // Both stay as they are where the joined program will not read.
            var trouble = ProgramReader.Load(program, new Automation(), kept.World.Values);
            if (trouble.Length > 0)
            {
                GameLog.Say($"the automation of {other.Block.CustomName} could not join that of {kept.Block.CustomName}: {trouble}");
                if (!_units.ContainsKey(other.Block.EntityId))
                    other.Dispose();

                return null;
            }

            var id = other.Block.EntityId;
            BlockStorage.Store(other.Block, "", "");
            if (Emptied != null)
                Emptied(id);

            _units.Remove(id);
            Drop(other);
            kept.Memory.Conflicts.AddRange(conflicts);
            GameLog.Say($"the automation of {other.Block.CustomName} joined that of {kept.Block.CustomName}, {conflicts.Count} logic system(s) waiting for a choice");
            var joined = Replace(kept, program, switches);
            if (joined != null)
                joined.TakeOver(id);

            return joined;
        }

        /// <summary>Files a Sprocket kept under <paramref name="was" /> under the block that holds it now, or lets it go where it is lost.</summary>
        void File(long was, SprocketUnit unit)
        {
            if (unit.Lost)
            {
                if (unit.Destroyed)
                    unit.BackUp(_backups, DateTime.UtcNow, Shelf);
                _units.Remove(was);
                Drop(unit);
                return;
            }

            long now = unit.Block.EntityId;
            if (now == was)
                return;

            _units.Remove(was);
            _units[now] = unit;
            _moved[was] = now;
            Spread(unit);
        }

        void Drop(SprocketUnit unit)
        {
            if (Dropped != null)
                Dropped(unit);

            _unwritten.Remove(unit);
            unit.Dispose();
        }

        void Spread(SprocketUnit unit)
        {
            if (Spreading != null && unit.Program.Intact)
                Spreading(unit);
        }
    }
}
