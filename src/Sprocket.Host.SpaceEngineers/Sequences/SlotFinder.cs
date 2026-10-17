using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Works out a blueprint's slots from the sets of blocks its steps and triggers name, as the installation shown fills them.</summary>
    /// <remarks>
    /// Called once per edit in the editor, never on a tick: it compares every set with every other, which is cheap at the few
    /// dozen sets a system names.
    /// </remarks>
    public sealed class SlotFinder
    {
        readonly AutomationSystem _system;
        readonly SystemInstance _shown;
        readonly Func<string, string> _called;
        readonly Func<string, IList<string>> _lineage;
        readonly Func<int, string> _numbered;
        readonly List<Entry> _entries = new List<Entry>();
        readonly List<Set> _sets = new List<Set>();
        readonly List<string> _taken = new List<string>();
        int _made;

        /// <summary>
        /// Slots for <paramref name="system" /> as <paramref name="shown" /> fills them; a new slot of one block is called what
        /// <paramref name="called" /> says of it, one of several is called <paramref name="numbered" /> of the first number free,
        /// and either expects the nearest type the lineages <paramref name="lineage" /> gives its blocks share.
        /// </summary>
        public SlotFinder(AutomationSystem system, SystemInstance shown, Func<string, string> called, Func<string, IList<string>> lineage,
            Func<int, string> numbered)
        {
            _system = system;
            _shown = shown;
            _called = called;
            _lineage = lineage;
            _numbered = numbered;
        }

        /// <summary>
        /// The slots a list picking <paramref name="blocks" /> names, in place of <paramref name="replacing" /> where it replaces
        /// one, with every other change to the slots that brings about in <paramref name="reslot" />; <paramref name="held" /> are
        /// lists picked before it whose edits have not been made yet, which count as named and are left as they are.
        /// </summary>
        public List<string> Pick(IList<string> blocks, IList<string> replacing, IList<List<string>> held, out Reslot reslot)
        {
            Gather(replacing);
            for (var i = 0; i < held.Count; i++)
            {
                var entry = new Entry { Names = held[i], Held = true };
                entry.Blocks = BlocksOf(held[i], out entry.Pinned);
                _entries.Add(entry);
            }

            var picked = new Entry { Names = null, Blocks = Distinct(blocks) };
            _entries.Add(picked);
            reslot = Settle(picked, replacing);
            return picked.Now;
        }

        /// <summary>
        /// The slot of the type <paramref name="type" /> a step acting on <paramref name="blocks" /> names, holding the facet
        /// <paramref name="facets" /> numbers of each, in place of <paramref name="replacing" /> where it replaces a list, with
        /// every other change to the slots that brings about in <paramref name="reslot" />; <paramref name="held" /> are as
        /// <see cref="Pick" /> has them.
        /// </summary>
        public List<string> Hold(string type, IList<string> blocks, IList<int> facets, IList<string> replacing, IList<List<string>> held,
            out Reslot reslot)
        {
            Gather(replacing);
            for (var i = 0; i < held.Count; i++)
            {
                var entry = new Entry { Names = held[i], Held = true };
                entry.Blocks = BlocksOf(held[i], out entry.Pinned);
                _entries.Add(entry);
            }

            reslot = new Reslot(_system, _shown);
            Sets();
            Wanted();
            Choose(reslot, null, null);
            Name(reslot, null);

            var part = Holding(type, replacing);
            if (part != null)
                reslot.Refill(part, blocks, part.Expects, facets);
            else
            {
                part = new Part(blocks.Count == 1 ? Free(_called(blocks[0])) : Numbered()) { Holds = type };
                reslot.Make(part, blocks, facets);
            }

            _taken.Add(part.Id);
            var now = new List<string> { part.Id };
            _entries.Add(new Entry { Names = now, Pinned = true, Now = now });
            Prune(reslot);
            return now;
        }

        /// <summary>The changes to the slots the lists as they are now bring about, after an edit that picked nothing.</summary>
        public Reslot Settle()
        {
            Gather(null);
            return Settle(null, null);
        }

        void Gather(IList<string> replacing)
        {
            _entries.Clear();
            var lists = new List<IList<string>>();
            _system.Lists(lists);
            foreach (var list in lists)
            {
                if (ReferenceEquals(list, replacing))
                    continue;

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

        Reslot Settle(Entry picked, IList<string> replacing)
        {
            var reslot = new Reslot(_system, _shown);
            Sets();
            Wanted();
            Choose(reslot, picked, replacing);
            Name(reslot, picked);
            Prune(reslot);
            return reslot;
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

        /// <summary>Gives each set that is a slot its slot: the one already holding exactly those blocks, the one the pick replaced, or a new one.</summary>
        void Choose(Reslot reslot, Entry picked, IList<string> replacing)
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
                        Same(_shown.FilledWith(parts[p].Id), set.Blocks))
                        set.Part = parts[p];
                }

                if (set.Part != null)
                {
                    _taken.Add(set.Part.Id);
                    continue;
                }

                var kept = picked != null && picked.Set == set ? Kept(replacing, set.Blocks) : null;
                if (kept != null)
                {
                    set.Part = kept;
                    _taken.Add(kept.Id);
                    reslot.Refill(kept, set.Blocks, kept.Expects);
                    continue;
                }

                var name = set.Blocks.Count == 1 ? Free(_called(set.Blocks[0])) : Numbered();
                set.Part = new Part(name) { Expects = Nearest(set.Blocks) };

                _taken.Add(name);
                _made++;
                reslot.Make(set.Part, set.Blocks);
            }
        }

        /// <summary>
        /// The one block slot <paramref name="replacing" /> names, where it names exactly one, nothing else names it, and every
        /// one of <paramref name="blocks" /> is of a kind it expects; null otherwise.
        /// </summary>
        Part Kept(IList<string> replacing, IList<string> blocks)
        {
            if (replacing == null)
                return null;

            Part only = null;
            for (var i = 0; i < replacing.Count; i++)
            {
                var part = _system.FindPart(replacing[i]);
                if (part == null || part.IsSignal || part.Holds != "" || (only != null && only != part))
                    return null;

                only = part;
            }

            if (only == null || _taken.Contains(only.Id))
                return null;

            for (var i = 0; i < blocks.Count; i++)
            {
                if (!only.Fits(_lineage(blocks[i])))
                    return null;
            }

            return _system.Mentions(only.Id) == AutomationSystem.Count(replacing, only.Id) ? only : null;
        }

        /// <summary>
        /// The one slot of the type <paramref name="type" /> that <paramref name="replacing" /> names, where it names exactly one
        /// and nothing else names it; null otherwise.
        /// </summary>
        Part Holding(string type, IList<string> replacing)
        {
            if (replacing == null || replacing.Count == 0)
                return null;

            var part = _system.FindPart(replacing[0]);
            if (part == null || part.Holds != type || _taken.Contains(part.Id))
                return null;

            for (var i = 1; i < replacing.Count; i++)
            {
                if (replacing[i] != part.Id)
                    return null;
            }

            return _system.Mentions(part.Id) == replacing.Count ? part : null;
        }

        /// <summary>Names each list's set by its slot, or by the fewest slots that make it, and renames every list that changes.</summary>
        void Name(Reslot reslot, Entry picked)
        {
            foreach (var entry in _entries)
            {
                if (entry.Set == null)
                {
                    entry.Now = entry.Names == null ? new List<string>() : new List<string>(entry.Names);
                    continue;
                }

                entry.Now = entry.Held ? new List<string>(entry.Names) : Cover(entry.Set);
                if (entry != picked && !entry.Held && !Same(entry.Names, entry.Now))
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

        /// <summary>Takes out every slot but a signal slot that nothing names and no installation but the one shown holds blocks in.</summary>
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
            var instances = _system.Instances;
            for (var p = 0; p < parts.Count; p++)
            {
                var part = parts[p];
                if (part.IsSignal || used.Contains(part.Id))
                    continue;

                var held = false;
                for (var i = 0; i < instances.Count && !held; i++)
                {
                    var filled = instances[i].FilledWith(part.Id);
                    held = instances[i] != _shown && filled != null && filled.Count > 0;
                }

                if (!held)
                    reslot.Drop(part);
            }
        }

        string Free(string called)
        {
            var name = called;
            for (var n = 2; _system.FindPart(name) != null || _taken.Contains(name); n++)
                name = $"{called} {n}";

            return name;
        }

        /// <summary>The name for a new slot of several blocks, counting on from the block slots there are.</summary>
        string Numbered()
        {
            var slots = _made;
            var parts = _system.Parts;
            for (var i = 0; i < parts.Count; i++)
            {
                if (!parts[i].IsSignal)
                    slots++;
            }

            for (var n = slots + 1; ; n++)
            {
                var name = _numbered(n);
                if (_system.FindPart(name) == null && !_taken.Contains(name))
                    return name;
            }
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
                if (Same(set.Blocks, blocks))
                    return set;
            }

            return null;
        }

        static List<string> Distinct(IList<string> blocks)
        {
            var distinct = new List<string>();
            for (var i = 0; i < blocks.Count; i++)
                Add(distinct, blocks[i]);

            return distinct;
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

        /// <summary>Whether two lists hold the same names, in any order; null holds none.</summary>
        static bool Same(IList<string> left, IList<string> right)
        {
            var leftCount = left?.Count ?? 0;
            var rightCount = right?.Count ?? 0;
            if (leftCount != rightCount)
                return false;

            return leftCount == 0 || Within(left, right) && Within(right, left);
        }

        /// <summary>One list a step or a trigger names, what it stands for in the installation shown, and what it names after.</summary>
        sealed class Entry
        {
            public IList<string> Names;
            public bool Held;
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
