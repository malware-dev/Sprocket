using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Players;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Grids
{
    /// <summary>Every Sprocket in the world, each filed under the programmable block that stores it.</summary>
    public sealed class Roster : IDisposable
    {
        readonly Dictionary<long, SprocketUnit> _units = new Dictionary<long, SprocketUnit>();
        readonly Dictionary<long, long> _moved = new Dictionary<long, long>();
        readonly List<SprocketUnit> _saving = new List<SprocketUnit>();

        // The Sprockets with a screen open on them, which alone are looked at on every tick.
        readonly List<SprocketUnit> _showing = new List<SprocketUnit>();

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

        /// <summary>Told of each Sprocket let go of, or changed in place by its construct parting or joining another, so a screen open on it closes.</summary>
        public Action<SprocketUnit> Dropped { get; set; }

        /// <summary>The library a lost Sprocket's backup is shelved beside.</summary>
        public LibraryShelf Shelf { get; set; }

        /// <summary>How many there are.</summary>
        public int Count => _units.Count;

        /// <summary>Each of them.</summary>
        public IEnumerable<SprocketUnit> All => _units.Values;

        /// <summary>The Sprocket <paramref name="block" /> stores, or null.</summary>
        public SprocketUnit Stored(IMyTerminalBlock block)
        {
            SprocketUnit unit;
            return block != null && _units.TryGetValue(block.EntityId, out unit) ? unit : null;
        }

        /// <summary>The Sprocket stored under <paramref name="id" />, or where it moved to, or null.</summary>
        public SprocketUnit Stored(long id)
        {
            SprocketUnit unit;
            return _units.TryGetValue(Key(id), out unit) ? unit : null;
        }

        /// <summary>The id the automation once stored under <paramref name="id" /> is stored under now.</summary>
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

            SprocketUnit arrived = new SprocketUnit(block, _settings(), _rota, _kinds, _texts, _memories.Binary, _memories.Claim(block.EntityId), _backups);
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

        /// <summary>Runs the world's rota, which brings each Sprocket up to date where it changed, then brings each open screen up to date.</summary>
        /// <remarks>Every tick: the rota is the one thing on it, and visits only what is due.</remarks>
        public void Tick(int frame)
        {
            _rota.Run(frame);

            for (var i = 0; i < _showing.Count; i++)
                _showing[i].Show(frame);
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
            _showing.Clear();
        }

        void Listen(SprocketUnit unit)
        {
            unit.HeroRemoved += Removed;
            unit.Gathered += Gathered;
            unit.Changed += Changed;
            unit.ShowingChanged += Shown;
            Shown(unit);
        }

        /// <summary>Keeps <paramref name="unit" /> among those looked at every tick for as long as a screen is open on it.</summary>
        void Shown(SprocketUnit unit)
        {
            var at = _showing.IndexOf(unit);
            if (unit.Showing && at < 0)
                _showing.Add(unit);
            else if (!unit.Showing && at >= 0)
                _showing.RemoveAt(at);
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

        /// <summary>
        /// Gives <paramref name="block" />, a programmable block that parted from the construct of <paramref name="unit" />, a
        /// Sprocket of its own with a copy of the automation, and has each half keep what is on it.
        /// </summary>
        /// <remarks>
        ///     Both are changed in memory, and each is written into its block once the change is done. A part holding none of
        ///     the automation's blocks is given nothing.
        /// </remarks>
        void Part(SprocketUnit unit, IMyTerminalBlock block)
        {
            if (!Reaches(unit, block))
            {
                GameLog.Say($"{block.CustomName} parted from the construct of {unit.Block.CustomName} with none of its automation's blocks, and keeps none of it");
                return;
            }

            var half = SprocketUnit.PartedFrom(block, unit, _settings(), _rota, _memories.Claim(block.EntityId));
            half.Begin(_frame());
            Listen(half);
            _units.Add(block.EntityId, half);

            GameLog.Say($"{block.CustomName} parted from the construct of {unit.Block.CustomName}, with a copy of its automation");
            AutomationMerge.Part(half.Program, half.World, unit.World);
            Reworked(half);
            half.TakeOver(unit.Block.EntityId);

            AutomationMerge.Part(unit.Program, unit.World, half.World);
            Reworked(unit);
        }

        /// <summary>Whether the construct <paramref name="block" /> is on holds any block of <paramref name="unit" />'s installations.</summary>
        bool Reaches(SprocketUnit unit, IMyTerminalBlock block)
        {
            var world = new GridWorld(block, _kinds);
            try
            {
                return AutomationMerge.Reaches(unit.Program, world);
            }
            finally
            {
                world.Dispose();
            }
        }

        /// <summary>Has <paramref name="unit" /> take in the change made to its program in place, closing any screen open on it.</summary>
        void Reworked(SprocketUnit unit)
        {
            // A pairing missing either version was settled.
            unit.Memory.Conflicts.RemoveAll(conflict =>
                unit.Program.FindSystem(conflict.System) == null || unit.Program.FindSystem(conflict.Waiting) == null);

            if (Dropped != null)
                Dropped(unit);

            unit.Reworked();
        }

        /// <summary>Joins <paramref name="other" /> into <paramref name="kept" />, emptying <paramref name="other" />'s block; answers <paramref name="kept" />, or null where the two cannot be joined.</summary>
        /// <remarks>Changes <paramref name="kept" /> in memory, and it is written into its block once the change is done.</remarks>
        SprocketUnit Join(SprocketUnit kept, SprocketUnit other)
        {
            var conflicts = new List<ConflictMemory>();
            var moves = new List<InstanceMove>();
            var values = kept.World.Values;
            var program = AutomationMerge.Join(kept, other, conflicts, moves);

            // Written out and read back as its block will be: both stay as they are where the joined program will not read.
            var trouble = ProgramReader.Load(ProgramWriter.Save(program, values), new Automation(), values);
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
            AutomationMerge.Carry(other.Memory, kept.Memory, moves);
            Drop(other);
            kept.Memory.Conflicts.AddRange(conflicts);
            GameLog.Say($"the automation of {other.Block.CustomName} joined that of {kept.Block.CustomName}, {conflicts.Count} logic system(s) waiting for a choice");
            kept.Overwrite(program);
            Reworked(kept);
            kept.TakeOver(id);
            return kept;
        }

        /// <summary>Files a Sprocket stored under <paramref name="was" /> under the block that holds it now, or lets it go where it is lost.</summary>
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
            _showing.Remove(unit);
            unit.ShowingChanged -= Shown;
            unit.Dispose();
        }

        void Spread(SprocketUnit unit)
        {
            if (Spreading != null && unit.Program.Intact)
                Spreading(unit);
        }
    }
}
