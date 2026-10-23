using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Works out the fewest slots a blueprint's steps and triggers can name, from the sets of blocks they name as the installation shown fills them.</summary>
    /// <remarks>
    /// Arranges the slots on request, never on an edit or a tick: it compares every set with every other, which is cheap at the
    /// few dozen sets a system names.
    /// </remarks>
    public sealed class SlotFinder
    {
        readonly AutomationSystem _system;
        readonly SystemInstance _shown;
        readonly Func<string, IList<string>> _lineage;
        readonly Func<int, string> _numbered;
        readonly List<Entry> _entries = new List<Entry>();
        readonly List<Set> _sets = new List<Set>();
        readonly List<string> _taken = new List<string>();
        int _made;

        /// <summary>
        /// Slots for <paramref name="system" /> as <paramref name="shown" /> fills them; a new slot is called <paramref name="numbered" />
        /// of the first number free, counting on from the slots there are, and expects the nearest type the lineages
        /// <paramref name="lineage" /> gives its blocks share.
        /// </summary>
        public SlotFinder(AutomationSystem system, SystemInstance shown, Func<string, IList<string>> lineage, Func<int, string> numbered)
        {
            _system = system;
            _shown = shown;
            _lineage = lineage;
            _numbered = numbered;
        }

        /// <summary>The changes that give the blueprint the fewest slots its lists, as they are now, can name, made by nothing yet.</summary>
        public Reslot Settle()
        {
            Gather();
            var reslot = new Reslot(_system, _shown);
            Sets();
            Wanted();
            Choose(reslot);
            Name(reslot);
            Prune(reslot);
            return reslot;
        }

        void Gather()
        {
            _entries.Clear();
            var lists = new List<IList<string>>();
            _system.Lists(lists);
            foreach (var list in lists)
            {
                var entry = new Entry { Names = list };
                entry.Blocks = BlocksOf(list, out entry.Pinned);
                _entries.Add(entry);
            }
        }

        /// <summary>
        /// The blocks <paramref name="names" /> stand for in the installation shown, each once; pinned where one is a slot it leaves
        /// empty, a signal, or a slot holding a type the host provides.
        /// </summary>
        List<string> BlocksOf(IList<string> names, out bool pinned)
        {
            pinned = false;
            var blocks = new List<string>();
            for (var i = 0; i < names.Count; i++)
            {
                var part = _system.FindPart(names[i]);
                if (part == null)
                {
                    Add(blocks, names[i]);
                    continue;
                }

                var filled = _shown.FilledWith(part.Id);
                if (part.IsSignal || part.Holds != "" || filled == null || filled.Count == 0)
                {
                    pinned = true;
                    return blocks;
                }

                for (var n = 0; n < filled.Count; n++)
                    Add(blocks, filled[n]);
            }

            return blocks;
        }

        /// <summary>Every set the lists name, once, smallest first.</summary>
        void Sets()
        {
            _sets.Clear();
            foreach (var entry in _entries)
            {
                if (entry.Pinned || entry.Blocks.Count == 0)
                    continue;

                var set = Find(entry.Blocks);
                if (set == null)
                {
                    set = new Set { Blocks = entry.Blocks };
                    _sets.Add(set);
                }

                entry.Set = set;
            }

            _sets.Sort((a, b) => a.Blocks.Count.CompareTo(b.Blocks.Count));
        }

        /// <summary>Marks each set that is not exactly the union of smaller sets as one that is a slot.</summary>
        void Wanted()
        {
            var union = new List<string>();
            for (var i = 0; i < _sets.Count; i++)
            {
                union.Clear();
                for (var n = 0; n < _sets.Count; n++)
                {
                    if (n != i && _sets[n].Blocks.Count < _sets[i].Blocks.Count && Within(_sets[n].Blocks, _sets[i].Blocks))
                    {
                        foreach (var block in _sets[n].Blocks)
                            Add(union, block);
                    }
                }

                _sets[i].IsSlot = union.Count != _sets[i].Blocks.Count;
            }
        }

        /// <summary>Gives each set that is a slot its slot: the one already holding exactly those blocks, or a new one.</summary>
        void Choose(Reslot reslot)
        {
            _taken.Clear();
            _made = 0;
            var parts = _system.Parts;
            foreach (var set in _sets)
            {
                if (!set.IsSlot)
                    continue;

                for (var p = 0; p < parts.Count && set.Part == null; p++)
                {
                    if (!parts[p].IsSignal && parts[p].Holds == "" && !_taken.Contains(parts[p].Id) &&
                        ListComparison.SameAnyOrder(_shown.FilledWith(parts[p].Id), set.Blocks))
                        set.Part = parts[p];
                }

                if (set.Part != null)
                {
                    _taken.Add(set.Part.Id);
                    continue;
                }

                var name = Numbered();
                set.Part = new Part(name) { Expects = Nearest(set.Blocks) };

                _taken.Add(name);
                _made++;
                reslot.Make(set.Part, set.Blocks);
            }
        }

        /// <summary>Names each list's set by its slot, or by the fewest slots that make it, and renames every list that changes.</summary>
        void Name(Reslot reslot)
        {
            foreach (var entry in _entries)
            {
                if (entry.Set == null)
                {
                    entry.Now = new List<string>(entry.Names);
                    continue;
                }

                entry.Now = Cover(entry.Set);
                if (!ListComparison.SameAnyOrder(entry.Names, entry.Now))
                    reslot.Rename(entry.Names, entry.Now);
            }
        }

        /// <summary>The slot that is <paramref name="set" />, or the fewest slots within it that make it, larger ones first.</summary>
        List<string> Cover(Set set)
        {
            var names = new List<string>();
            if (set.IsSlot)
            {
                names.Add(set.Part.Id);
                return names;
            }

            var covered = new List<string>();
            for (var i = _sets.Count - 1; i >= 0 && covered.Count < set.Blocks.Count; i--)
            {
                var slot = _sets[i];
                if (!slot.IsSlot || slot == set || !Within(slot.Blocks, set.Blocks) || Within(slot.Blocks, covered))
                    continue;

                names.Add(slot.Part.Id);
                foreach (var block in slot.Blocks)
                    Add(covered, block);
            }

            return names;
        }

        /// <summary>Takes out every slot nothing names once the lists are renamed, whatever an installation holds in it.</summary>
        void Prune(Reslot reslot)
        {
            var used = new List<string>();
            foreach (var entry in _entries)
            {
                var names = entry.Now;
                foreach (var name in names)
                    Add(used, name);
            }

            var parts = _system.Parts;
            for (var p = 0; p < parts.Count; p++)
            {
                if (!used.Contains(parts[p].Id))
                    reslot.Drop(parts[p]);
            }
        }

        /// <summary>The name for a new slot: the first numbered name free in any case, counting on from the slots there are and those made here.</summary>
        string Numbered()
        {
            for (var n = _system.Parts.Count + _made + 1; ; n++)
            {
                var name = _numbered(n);
                if (!_system.TakesInAnyCase(name) && !Taken(name))
                    return name;
            }
        }

        /// <summary>Whether a slot this works out is called <paramref name="name" /> in any case.</summary>
        bool Taken(string name)
        {
            foreach (var taken in _taken)
            {
                if (string.Equals(taken, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>The nearest type every one of <paramref name="blocks" /> is, or empty for any block.</summary>
        string Nearest(IList<string> blocks)
        {
            var lineages = new List<IList<string>>();
            for (var i = 0; i < blocks.Count; i++)
                lineages.Add(_lineage(blocks[i]));

            return Part.Nearest(lineages);
        }

        Set Find(List<string> blocks)
        {
            foreach (var set in _sets)
            {
                if (ListComparison.SameAnyOrder(set.Blocks, blocks))
                    return set;
            }

            return null;
        }

        static void Add(List<string> into, string name)
        {
            if (!into.Contains(name))
                into.Add(name);
        }

        /// <summary>Whether every one of <paramref name="part" /> is in <paramref name="whole" />.</summary>
        static bool Within(IList<string> part, IList<string> whole)
        {
            for (var i = 0; i < part.Count; i++)
            {
                if (!whole.Contains(part[i]))
                    return false;
            }

            return true;
        }

        /// <summary>One list a step or a trigger names, what it stands for in the installation shown, and what it names after.</summary>
        sealed class Entry
        {
            public IList<string> Names;
            public List<string> Blocks;
            public bool Pinned;
            public Set Set;
            public List<string> Now;
        }

        /// <summary>A set of blocks some list names, and the slot it is where it is one.</summary>
        sealed class Set
        {
            public List<string> Blocks;
            public bool IsSlot;
            public Part Part;
        }
    }
}
