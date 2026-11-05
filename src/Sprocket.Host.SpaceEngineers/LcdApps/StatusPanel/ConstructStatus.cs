using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel
{
    /// <summary>What a construct's automation amounts to on the status panel: the installations that want looking at, switched-off ones included, and how many logic systems it holds.</summary>
    /// <remarks>
    /// The installations are kept most wanting first: problems, then warnings, then what stopped for want of a big owner's
    /// programmable block, then what was paused, then what a player switched off, and the newest first within each.
    /// </remarks>
    public sealed class ConstructStatus
    {
        Dictionary<long, int> _kept = new Dictionary<long, int>();
        Dictionary<long, int> _keptFound = new Dictionary<long, int>();
        List<StatusItem> _items = new List<StatusItem>();
        List<StatusItem> _found = new List<StatusItem>();
        int _now;
        int _systems;
        int _troubled;

        /// <summary>Whether whoever runs the construct's automation has said how it stands.</summary>
        public bool Heard { get; private set; }

        /// <summary>What the construct is called, as its grid's name says.</summary>
        public string Construct { get; private set; } = "";

        /// <summary>How many logic systems the construct holds.</summary>
        public int Systems { get; private set; }

        /// <summary>How many of them need no looking at and are switched on.</summary>
        public int Normal { get; private set; }

        /// <summary>How many installations have a problem.</summary>
        public int Problems { get; private set; }

        /// <summary>How many installations want looking at for anything short of a problem: a warning, a stop for want of a big owner's programmable block, a pause, or being switched off.</summary>
        public int Warnings { get; private set; }

        /// <summary>Whether any installation wants looking at, a switched-off one included.</summary>
        public bool Troubled => _items.Count > 0;

        /// <summary>The installations that want looking at, then those switched off, most wanting first.</summary>
        public IReadOnlyList<StatusItem> Items => _items;

        /// <summary>Bumped whenever anything here changes, so a panel draws itself again only then.</summary>
        public int Revision { get; private set; }

        /// <summary>Says nothing has been heard of the automation on <paramref name="construct" /> yet.</summary>
        public void Unheard(string construct)
        {
            if (!Heard && Construct == construct)
                return;

            Heard = false;
            Construct = construct ?? "";
            Systems = 0;
            Normal = 0;
            Problems = 0;
            Warnings = 0;
            _items.Clear();
            Revision++;
        }

        /// <summary>Starts working out how the construct's automation stands at tick <paramref name="now" />, each program on it to be included and the look ended.</summary>
        public void Begin(int now)
        {
            _found.Clear();
            _keptFound.Clear();
            _now = now;
            _systems = 0;
            _troubled = 0;
        }

        /// <summary>Adds what <paramref name="program" />, run by <paramref name="runner" />, amounts to.</summary>
        /// <remarks>Costs a lookup per sequence on each installation, so it is asked only when the automation has changed.</remarks>
        public void Include(Automation program, AutomationRunner runner)
        {
            if (program.Paused)
                Found(Stopped(StatusLevel.Paused));

            foreach (AutomationSystem system in program.Systems)
            {
                var kept = SystemSummary.KeptTo(runner, system);
                if (kept != 0)
                {
                    int on;
                    _keptFound.TryGetValue(kept, out on);
                    _keptFound[kept] = on + 1;
                    continue;
                }

                _systems++;
                if (program.Paused || Look(runner, system))
                    _troubled++;
            }
        }

        /// <summary>The installations that want looking at as a list of their own, for a report to a client.</summary>
        public StatusItem[] Copy()
        {
            return _items.ToArray();
        }

        /// <summary>How many logic systems the screen of the block <paramref name="block" /> counts: every one, and those kept to that block.</summary>
        public int SystemsOn(long block)
        {
            int kept;
            _kept.TryGetValue(block, out kept);
            return Systems + kept;
        }

        /// <summary>The blocks that logic systems are kept to, for a report to a client.</summary>
        public long[] KeptBlocks()
        {
            var blocks = new long[_kept.Count];
            _kept.Keys.CopyTo(blocks, 0);
            return blocks;
        }

        /// <summary>How many logic systems are kept to each of <paramref name="blocks" />, for a report to a client.</summary>
        public int[] KeptCounts(long[] blocks)
        {
            var counts = new int[blocks.Length];
            for (var i = 0; i < blocks.Length; i++)
                counts[i] = _kept[blocks[i]];

            return counts;
        }

        /// <summary>Takes what the server says of the construct called <paramref name="construct" />: <paramref name="systems" /> logic systems, <paramref name="normal" /> of them normal, and <paramref name="items" />.</summary>
        public void Hear(string construct, int systems, int normal, StatusItem[] items, long[] keptBlocks, int[] keptCounts)
        {
            _found.Clear();
            _found.AddRange(items);
            _keptFound.Clear();
            for (var i = 0; i < keptBlocks.Length; i++)
                _keptFound[keptBlocks[i]] = keptCounts[i];

            _systems = systems;
            _troubled = systems - normal;
            End(construct);
        }

        /// <summary>Ends the look begun, for the construct called <paramref name="construct" />.</summary>
        public void End(string construct)
        {
            construct = construct ?? "";
            var changed = !Heard || Construct != construct || Systems != _systems || Normal != _systems - _troubled || !Alike() || !KeptAlike();

            Heard = true;
            Construct = construct;
            Systems = _systems;
            Normal = _systems - _troubled;

            var kept = _items;
            _items = _found;
            _found = kept;
            var keptOn = _kept;
            _kept = _keptFound;
            _keptFound = keptOn;
            Count();

            if (changed)
                Revision++;
        }

        /// <summary>Adds an item for each installation of <paramref name="system" /> that wants looking at, and answers whether any did.</summary>
        bool Look(AutomationRunner runner, AutomationSystem system)
        {
            var named = SystemSummary.Named(runner.Bindings, system);
            if (system.Halt != null)
            {
                Found(Halted(Item(StatusLevel.Problem, system, named, null), system, system.Halt));
                return true;
            }

            if (!system.Enabled)
            {
                Found(Item(StatusLevel.Off, system, named, null));
                return true;
            }

            var any = false;
            if (system.IsBlueprint)
            {
                SequenceFault own;
                var wrong = SystemSummary.SystemFailing(runner, system, out own) ?? SystemSummary.SystemWarned(runner, system, out own);
                if (wrong != null)
                {
                    Found(Faulted(Item(own.IsWarning ? StatusLevel.Warning : StatusLevel.Problem, system, named, null), own, wrong));
                    any = true;
                }
            }

            foreach (SystemInstance use in system.Uses)
            {
                var instance = system.IsBlueprint ? use : null;
                if (use.Halt != null)
                {
                    Found(Halted(Item(StatusLevel.Problem, system, named, instance), system, use.Halt));
                    any = true;
                    continue;
                }

                if (!use.Enabled)
                {
                    Found(Item(StatusLevel.Off, system, named, instance));
                    any = true;
                    continue;
                }

                SequenceFault fault;
                var failing = SystemSummary.Failing(runner, system, use, out fault) ?? SystemSummary.Warned(runner, system, use, out fault);
                if (failing == null)
                    continue;

                Found(Faulted(Item(fault.IsWarning ? StatusLevel.Warning : StatusLevel.Problem, system, named, instance), fault, failing));
                any = true;
            }

            return any;
        }

        /// <summary>An item at <paramref name="level" /> for the whole of a program's automation, which names no logic system.</summary>
        static StatusItem Stopped(StatusLevel level)
        {
            return new StatusItem { Level = level, Logic = "", Installation = "", Sequence = "", Fault = SequenceFault.None };
        }

        /// <summary>An item at <paramref name="level" /> naming <paramref name="system" />, called <paramref name="named" />, and <paramref name="instance" /> where it is one of a blueprint's.</summary>
        static StatusItem Item(StatusLevel level, AutomationSystem system, string named, SystemInstance instance)
        {
            return new StatusItem
            {
                Level = level,
                Logic = named,
                Number = system.Id,
                Blueprint = instance != null,
                Installation = instance != null && instance.HasName ? instance.Name : "",
                Position = instance == null ? 0 : SystemSummary.Position(system, instance),
                Sequence = "",
                Fault = SequenceFault.None
            };
        }

        /// <summary><paramref name="item" />, switched off because it could not work, as <paramref name="halt" /> recorded it.</summary>
        static StatusItem Halted(StatusItem item, AutomationSystem system, Halt halt)
        {
            var sequence = system.FindSequence(halt.Sequence);
            item.Halted = true;
            item.Fault = new SequenceFault(halt.Problem, halt.Step, halt.Because, halt.Block);
            item.Sequence = sequence != null && sequence.HasName ? sequence.Name : "";
            item.Row = SequenceText.Row(sequence, halt.Step);
            return item;
        }

        /// <summary><paramref name="item" />, saying <paramref name="fault" /> of <paramref name="sequence" />.</summary>
        static StatusItem Faulted(StatusItem item, SequenceFault fault, Sequence sequence)
        {
            item.Fault = fault;
            item.Sequence = sequence.HasName ? sequence.Name : "";
            item.Alone = sequence.IsAlone;
            item.Row = SequenceText.Row(sequence, fault.Step);
            return item;
        }

        /// <summary>Puts <paramref name="item" /> in its place among those found, keeping the tick it was first seen.</summary>
        void Found(StatusItem item)
        {
            item.Since = _now;
            foreach (StatusItem was in _items)
            {
                if (was.Same(item))
                {
                    item.Since = was.Since;
                    break;
                }
            }

            var at = 0;
            while (at < _found.Count && Before(_found[at], item))
                at++;

            _found.Insert(at, item);
        }

        /// <summary>Whether <paramref name="first" /> is shown before <paramref name="then" />: by level, then the newer first, then in program order.</summary>
        static bool Before(StatusItem first, StatusItem then)
        {
            if (first.Level != then.Level)
                return first.Level < then.Level;

            return first.Since >= then.Since;
        }

        /// <summary>Whether what was found is what was there, item for item.</summary>
        bool KeptAlike()
        {
            if (_keptFound.Count != _kept.Count)
                return false;

            foreach (var pair in _keptFound)
            {
                int count;
                if (!_kept.TryGetValue(pair.Key, out count) || count != pair.Value)
                    return false;
            }

            return true;
        }

        bool Alike()
        {
            if (_found.Count != _items.Count)
                return false;

            for (var i = 0; i < _found.Count; i++)
            {
                if (!_found[i].Same(_items[i]))
                    return false;
            }

            return true;
        }

        void Count()
        {
            Problems = 0;
            Warnings = 0;
            foreach (StatusItem item in _items)
            {
                if (item.Level == StatusLevel.Problem)
                    Problems++;
                else
                    Warnings++;
            }
        }
    }
}
