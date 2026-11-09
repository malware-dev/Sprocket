using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// One run of a system pointed at real blocks: an instance of a system blueprint, or a system's own run.
    /// </summary>
    public sealed class SystemInstance
    {
        readonly AutomationSystem _system;
        bool _enabled;

        /// <summary>Run <paramref name="number" /> of <paramref name="system" />.</summary>
        public SystemInstance(int number, AutomationSystem system)
        {
            Number = number;
            Name = "";
            Enabled = true;
            _system = system;
        }

        /// <summary>Which one of its system this is.</summary>
        public int Number { get; private set; }

        /// <summary>
        /// What the player called it, or empty if they have not said.
        /// </summary>
        public string Name { get; set; }

        /// <summary>Whether somebody has named it.</summary>
        public bool HasName => !string.IsNullOrEmpty(Name);

        /// <summary>What somebody wrote above it, or empty.</summary>
        public string Annotation { get; set; } = "";

        /// <summary>The attributes written above it that Sprocket does not use, kept as written, or null where there are none.</summary>
        public KeptAttributes Attributes { get; set; }

        /// <summary>
        /// Whether this one may run, independently of the others.
        /// </summary>
        /// <remarks>Switching it on while its system is on starts it afresh, as switching the system on does.</remarks>
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value)
                    Halt = null;

                var started = value && !_enabled;
                var switched = value != _enabled;
                _enabled = value;

                if (started && _system != null && _system.Enabled && _system.Started != null)
                    _system.Started();

                if (switched && _system != null && _system.Switched != null)
                    _system.Switched();
            }
        }

        /// <summary>Why Sprocket switched this one off because it could not work, or null where it did not.</summary>
        public Halt Halt { get; set; }

        readonly Dictionary<string, List<string>> _fills = new Dictionary<string, List<string>>();

        readonly Dictionary<string, List<ToolbarSlot>> _positions = new Dictionary<string, List<ToolbarSlot>>();

        readonly Dictionary<string, List<int>> _facets = new Dictionary<string, List<int>>();

        readonly Dictionary<string, string> _notes = new Dictionary<string, string>();
        readonly Dictionary<string, KeptAttributes> _kept = new Dictionary<string, KeptAttributes>();

        /// <summary>The slots it fills.</summary>
        public IEnumerable<string> Filled => _fills.Keys;

        /// <summary>Whether no slot has been filled.</summary>
        public bool IsEmpty => _fills.Count == 0;

        /// <summary>Slots this one's own blocks into one of the system's block slots.</summary>
        public void Fill(string slot, IList<string> entities)
        {
            _positions.Remove(slot);
            _facets.Remove(slot);
            Blocks(slot, entities);
        }

        /// <summary>Answers a slot of a type the host provides: the facet <paramref name="facets" /> numbers, from 1, of each block of <paramref name="entities" />, in the same order.</summary>
        public void Fill(string slot, IList<string> entities, IList<int> facets)
        {
            _positions.Remove(slot);
            Blocks(slot, entities);

            List<int> held;
            if (!_facets.TryGetValue(slot, out held))
            {
                held = new List<int>();
                _facets[slot] = held;
            }

            held.Clear();
            for (var i = 0; i < facets.Count; i++)
                held.Add(facets[i]);
        }

        /// <summary>Which facet of each of the blocks filling <paramref name="slot" /> it holds, by number from 1, in the blocks' order, or null where it holds no host type.</summary>
        public IList<int> Facets(string slot)
        {
            List<int> held;
            return _facets.TryGetValue(slot, out held) ? held : null;
        }

        /// <summary>Answers a signal slot with toolbar slots: the slot <paramref name="at" /> on each block of <paramref name="entities" />, in the same order.</summary>
        public void Fill(string slot, IList<string> entities, IList<ToolbarSlot> at)
        {
            _facets.Remove(slot);
            Blocks(slot, entities);

            List<ToolbarSlot> positions;
            if (!_positions.TryGetValue(slot, out positions))
            {
                positions = new List<ToolbarSlot>();
                _positions[slot] = positions;
            }

            positions.Clear();
            for (var i = 0; i < at.Count; i++)
                positions.Add(at[i]);
        }

        /// <summary>Where on each of the blocks filling <paramref name="slot" /> a trigger is answered, in the blocks' order, or null where it is not a trigger's.</summary>
        public IList<ToolbarSlot> Positions(string slot)
        {
            List<ToolbarSlot> positions;
            return _positions.TryGetValue(slot, out positions) ? positions : null;
        }

        void Blocks(string slot, IList<string> entities)
        {
            List<string> chosen;
            if (!_fills.TryGetValue(slot, out chosen))
            {
                chosen = new List<string>();
                _fills[slot] = chosen;
            }

            chosen.Clear();
            for (var i = 0; i < entities.Count; i++)
                chosen.Add(entities[i]);
        }

        /// <summary>Leaves <paramref name="slot" /> unfilled, forgetting what filled it and what was written above it.</summary>
        public void Clear(string slot)
        {
            _fills.Remove(slot);
            _positions.Remove(slot);
            _facets.Remove(slot);
            _notes.Remove(slot);
            _kept.Remove(slot);
        }

        /// <summary>What somebody wrote above how <paramref name="slot" /> is answered, or empty.</summary>
        public string NoteOn(string slot)
        {
            string note;
            return _notes.TryGetValue(slot, out note) ? note : "";
        }

        /// <summary>Keeps <paramref name="annotation" /> as what is written above how <paramref name="slot" /> is answered.</summary>
        public void Note(string slot, string annotation)
        {
            if (string.IsNullOrEmpty(annotation))
                _notes.Remove(slot);
            else
                _notes[slot] = annotation;
        }

        /// <summary>The attributes written above how <paramref name="slot" /> is answered that Sprocket does not use, or null.</summary>
        public KeptAttributes KeptOn(string slot)
        {
            KeptAttributes kept;
            return _kept.TryGetValue(slot, out kept) ? kept : null;
        }

        /// <summary>Keeps <paramref name="kept" /> as the attributes written above how <paramref name="slot" /> is answered.</summary>
        public void Keep(string slot, KeptAttributes kept)
        {
            if (kept == null)
                _kept.Remove(slot);
            else
                _kept[slot] = kept;
        }

        /// <summary>Moves what fills slot <paramref name="was" /> to the slot's new name, <paramref name="now" />.</summary>
        public void RenameSlot(string was, string now)
        {
            List<string> chosen;
            if (!_fills.TryGetValue(was, out chosen))
                return;

            _fills.Remove(was);
            _fills[now] = chosen;

            string note;
            if (_notes.TryGetValue(was, out note))
            {
                _notes.Remove(was);
                _notes[now] = note;
            }

            KeptAttributes kept;
            if (_kept.TryGetValue(was, out kept))
            {
                _kept.Remove(was);
                _kept[now] = kept;
            }

            List<int> facets;
            if (_facets.TryGetValue(was, out facets))
            {
                _facets.Remove(was);
                _facets[now] = facets;
            }

            List<ToolbarSlot> positions;
            if (!_positions.TryGetValue(was, out positions))
                return;

            _positions.Remove(was);
            _positions[now] = positions;
        }

        /// <summary>What fills <paramref name="slot" />, or null where nothing does.</summary>
        public IList<string> FilledWith(string slot)
        {
            List<string> chosen;
            return _fills.TryGetValue(slot, out chosen) ? chosen : null;
        }

        /// <summary>
        /// What <paramref name="names" /> come to as <paramref name="instance" /> fills them, or the names
        /// as they are where there is no instance.
        /// </summary>
        public static List<string> Through(SystemInstance instance, IList<string> names)
        {
            var seen = new List<string>();
            if (instance != null)
            {
                instance.Resolve(names, seen);
                return seen;
            }

            for (var i = 0; i < names.Count; i++)
                seen.Add(names[i]);

            return seen;
        }

        /// <summary>
        /// Adds what <paramref name="slots" /> come to here to <paramref name="into" />: what answers each slot, nothing for a
        /// slot left unanswered, and any other name as it is.
        /// </summary>
        public void Resolve(IList<string> slots, List<string> into)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                List<string> chosen;
                if (!_fills.TryGetValue(slots[i], out chosen))
                {
                    if (!_system.IsBlueprint || _system.FindPart(slots[i]) == null)
                        into.Add(slots[i]);

                    continue;
                }

                foreach (var choice in chosen)
                    into.Add(choice);
            }
        }

        /// <summary>Puts into <paramref name="into" /> the number of every facet of <paramref name="block" /> the slots <paramref name="slots" /> hold, each once; none where they hold the block itself.</summary>
        public void FacetsOf(IList<string> slots, string block, List<int> into)
        {
            into.Clear();
            for (var i = 0; i < slots.Count; i++)
            {
                List<int> held;
                List<string> chosen;
                if (!_facets.TryGetValue(slots[i], out held) || !_fills.TryGetValue(slots[i], out chosen))
                    continue;

                for (var at = 0; at < chosen.Count && at < held.Count; at++)
                {
                    if (chosen[at] == block && !into.Contains(held[at]))
                        into.Add(held[at]);
                }
            }
        }

        /// <summary>
        /// The number of the facet of <paramref name="block" /> the first slot among <paramref name="slots" /> that holds a
        /// host type and holds the block holds, from 1; 0 where none of them does.
        /// </summary>
        /// <remarks>Asked as a step is worked out, never on a tick.</remarks>
        public int FacetOf(IList<string> slots, string block)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                List<int> held;
                List<string> chosen;
                if (!_facets.TryGetValue(slots[i], out held) || !_fills.TryGetValue(slots[i], out chosen))
                    continue;

                var at = chosen.IndexOf(block);
                if (at >= 0 && at < held.Count)
                    return held[at];
            }

            return 0;
        }
    }
}
