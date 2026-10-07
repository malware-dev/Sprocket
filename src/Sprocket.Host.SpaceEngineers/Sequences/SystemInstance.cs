using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// One run of a system pointed at real blocks: an instance of a system blueprint, or a system's own run.
    /// </summary>
    /// <remarks>
    /// A system is what a system is; this is one somebody has. The front airlock and the port
    /// airlock are two of these over one system, and fixing the system fixes both.
    ///
    /// The player never meets the word. It is called <c>Airlock 1</c> until they rename it,
    /// the way the game itself calls a block <c>Interior Light 4</c> - provisional enough to
    /// invite a better name without a dialog asking for one, and it never collides with the
    /// name of the system it belongs to.
    ///
    /// What it is for is the blocks: each instance of a system blueprint answers the blueprint's slots with its own
    /// doors and its own vent, which is why one blueprint can run two airlocks at opposite ends
    /// of a ship. See <see cref="Fill(string, IList{string})" /> and <see cref="Resolve" /> - the runtime goes through them
    /// whenever it works out which blocks a step or a watch is about.
    /// </remarks>
    public sealed class SystemInstance
    {
        readonly AutomationSystem _system;
        bool _enabled;

        /// <summary>Use <paramref name="number" /> of <paramref name="system" />.</summary>
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
        /// <remarks>
        /// Empty is the ordinary state. The name shown is worked out from the system and this
        /// number - Airlock 1 - so a system that only learns its own name later takes its
        /// children with it rather than leaving them called something stale.
        /// </remarks>
        public string Name { get; set; }

        /// <summary>Whether somebody has named it.</summary>
        public bool HasName => !string.IsNullOrEmpty(Name);

        /// <summary>What somebody wrote above it, or empty.</summary>
        public string Annotation { get; set; } = "";

        /// <summary>
        /// Whether this one may run, independently of the others.
        /// </summary>
        /// <remarks>
        /// Welding on the port airlock should stop the port airlock, not every airlock made from the
        /// same blueprint.
        ///
        /// Separate from the system's switch, and each has to survive the other. Stopping the whole
        /// system to work on it and starting it again must leave a deliberately stopped one stopped.
        /// </remarks>
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value)
                    Halt = null;

                _enabled = value;
            }
        }

        /// <summary>Why Sprocket switched this one off because it could not work, or null where it did not.</summary>
        public Halt Halt { get; set; }

        readonly Dictionary<string, List<string>> _fills = new Dictionary<string, List<string>>();

        readonly Dictionary<string, List<ToolbarSlot>> _positions = new Dictionary<string, List<ToolbarSlot>>();

        readonly Dictionary<string, string> _notes = new Dictionary<string, string>();

        /// <summary>The slots it fills.</summary>
        public IEnumerable<string> Filled => _fills.Keys;

        /// <summary>Whether no slot has been filled.</summary>
        public bool IsEmpty => _fills.Count == 0;

        /// <summary>Slots this one's own blocks into one of the system's block slots.</summary>
        public void Fill(string slot, IList<string> entities)
        {
            _positions.Remove(slot);
            Blocks(slot, entities);
        }

        /// <summary>Answers a signal slot with toolbar slots: the slot <paramref name="at" /> on each block of <paramref name="entities" />, in the same order.</summary>
        public void Fill(string slot, IList<string> entities, IList<ToolbarSlot> at)
        {
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
            _notes.Remove(slot);
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

                for (var n = 0; n < chosen.Count; n++)
                    into.Add(chosen[n]);
            }
        }
    }
}
