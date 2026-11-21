using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;

// Aliased because the game's Ingame namespaces collide with the mod-side ones.
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Wiring
{
    /// <summary>Which sequences of a construct's automation nothing on the construct can start, and whose button is why, for a screen to say.</summary>
    public sealed class UnreachableSequences
    {
        readonly GridWorld _world;
        readonly Automation _program;
        readonly AutomationRunner _runner;
        readonly WiredButtons _buttons;
        readonly List<TerminalActionParameter> _arguments = new List<TerminalActionParameter>();
        readonly List<string> _answering = new List<string>();
        readonly List<ToolbarSlot> _answeringAt = new List<ToolbarSlot>();
        bool _read;

        /// <summary>What can start the sequences of <paramref name="program" /> on <paramref name="world" />, run by <paramref name="runner" />, through the slots of <paramref name="buttons" />.</summary>
        public UnreachableSequences(GridWorld world, Automation program, AutomationRunner runner, WiredButtons buttons)
        {
            _world = world;
            _program = program;
            _runner = runner;
            _buttons = buttons;
        }

        /// <summary>Reads the toolbars for what can start each sequence where something asked, and the first time.</summary>
        public void Look()
        {
            // Reading a toolbar is costly, so it is read only when a screen asks, and never on a timer. Firing does not
            // depend on it: a slot invokes Sprocket's action itself, carrying its key.
            if (_world.TakeLook() || !_read)
            {
                _read = true;
                Find();
            }
        }

        /// <summary>
        /// Whether the block this want names is arranged to start exactly this.
        /// </summary>
        bool Wired(WantedWiring want)
        {
            GridEntity grid = _runner.Bindings.Entity(want.Reference);
            if (grid == null)
                return true;

            IMyToolbar held = _buttons.ToolbarOf(want);
            MyToolbarItemInfo present;
            return held != null && held.TryGetItem(want.Slot, out present) && _buttons.Asked(held, present, want);
        }

        readonly Dictionary<string, int> _claims = new Dictionary<string, int>(StringComparer.Ordinal);

        readonly Dictionary<string, int> _needed = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Which key a trigger's slot holds: the one that runs, or the stopping one.</summary>
        string Wanting(AutomationSystem system, SystemInstance instance, Sequence sequence,
            Trigger trigger)
        {
            if (trigger.Stops)
                return SlotKeys.KeyOf(system, instance, sequence, true);

            return SlotKeys.KeyOf(system, instance, sequence);
        }

        void Claims()
        {
            _claims.Clear();

            // Every slot of every page of every toolbar, since a trigger may be on any of them: a cockpit's target lock is
            // its third toolbar, and a timer runs all of its pages.
            for (var i = 0; i < _world.Entities.Count; i++)
            {
                GridEntity grid = _world.Entities[i];
                for (var t = 1; Toolbars.Of(grid.Block, t) != null; t++)
                    Claims(Toolbars.Of(grid.Block, t), grid.Block.EntityId);
            }
        }

        /// <summary>Counts what each slot of <paramref name="toolbar" />, of <paramref name="block" />, holds that answers this program.</summary>
        void Claims(IMyToolbar toolbar, long block)
        {
            for (var slot = 0; slot < toolbar.ItemCount; slot++)
            {
                MyToolbarItemInfo present;
                if (!toolbar.TryGetItem(slot, out present))
                    continue;

                // Sprocket's action counts only where it points here: one pointed at another Sprocket runs that one's program.
                if (!_buttons.Mine(present, block))
                    continue;

                _arguments.Clear();
                Toolbars.Parameters(toolbar, slot, _arguments);
                if (_arguments.Count == 0)
                    continue;

                var key = _arguments[0].Value as string;
                if (string.IsNullOrEmpty(key))
                    continue;

                Claimed(key);
            }
        }

        void Claimed(string key)
        {
            int held;
            _claims[key] = _claims.TryGetValue(key, out held) ? held + 1 : 1;
        }

        /// <summary>
        /// Whether enough of this construct answers what a sequence says it needs.
        /// </summary>
        bool Answered(AutomationSystem system, SystemInstance instance, Sequence sequence)
        {
            // Counted per key: a button that stops this sequence holds a different key from one that runs it.
            _needed.Clear();

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var signal = triggers[i] as SignalTrigger;
                if (signal == null || signal.IsWatched || !triggers[i].IsAbout(sequence))
                    continue;

                var wanted = Wanting(system, instance, sequence, triggers[i]);

                int asked;
                _needed[wanted] = _needed.TryGetValue(wanted, out asked) ? asked + 1 : 1;
            }

            if (_needed.Count == 0)
                return true;

            foreach (var pair in _needed)
            {
                int found;
                _claims.TryGetValue(pair.Key, out found);
                if (found < pair.Value)
                    return false;
            }

            return true;
        }

        void Find()
        {
            _unstartable.Clear();
            _reachable.Clear();
            _why.Clear();
            Claims();

            for (var s = 0; s < _program.Systems.Count; s++)
            {
                AutomationSystem system = _program.Systems[s];
                var sequences = system.Sequences;
                var instances = system.Uses;

                foreach (var sequence in sequences)
                {
                    for (var n = 0; n < instances.Count; n++)
                    {
                        if (Answered(system, instances[n], sequence))
                            continue;

                        var start = UnstartableSequences.Key(sequence.Id, instances[n]);
                        if (_unstartable.Contains(start))
                            continue;

                        _unstartable.Add(start);
                        _why[start] = Unanswer(system, instances[n], sequence);
                    }
                }
            }

            foreach (var want in _buttons.Wants)
            {
                // A slot that no longer holds what the program wants leaves the program naming it: the trigger is still the
                // reference, and what it names is reported as not answering, until picking it again writes it back.
                if (Wired(want))
                    _reachable.Add(want.Start);
            }

            foreach (var want in _buttons.Wants)
            {
                if (_reachable.Contains(want.Start) || _unstartable.Contains(want.Start))
                    continue;

                _unstartable.Add(want.Start);
                _why[want.Start] = Unanswer(_buttons.ToolbarOf(want), want.Slot, want.Key, want.Reference);
            }

            _world.Unstartable.Tell(_unstartable, _why);
        }

        /// <summary>Whose button leaves <paramref name="sequence" /> unstarted on <paramref name="instance" />, and what its slot holds instead.</summary>
        /// <remarks>Asked only of a sequence already found unanswered, which is after the wiring changes and never on a tick.</remarks>
        UnansweredButton Unanswer(AutomationSystem system, SystemInstance instance, Sequence sequence)
        {
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var needed = triggers[i] as SignalTrigger;
                if (needed == null || needed.IsWatched || !needed.IsAbout(sequence))
                    continue;

                var wanted = Wanting(system, instance, sequence, needed);
                var blocks = _answering;
                var positions = _answeringAt;
                _program.Answers(system, instance, needed, blocks, positions);
                for (var s = 0; s < blocks.Count && s < positions.Count; s++)
                {
                    IMyToolbar toolbar = _buttons.ToolbarOf(blocks[s], positions[s].Toolbar);
                    var why = Unanswer(toolbar, Toolbars.Index(toolbar, positions[s]), wanted, blocks[s]);
                    if (why.Why != Unwired.Unknown)
                        return why;
                }
            }

            return new UnansweredButton(null, Unwired.Unknown);
        }

        /// <summary>What the slot of the button of <paramref name="reference" /> holds, where it is not <paramref name="wanted" />.</summary>
        UnansweredButton Unanswer(IMyToolbar toolbar, int slot, string wanted, string reference)
        {
            var block = _program.Called(reference, _world.Values);
            if (toolbar == null || slot < 0)
                return new UnansweredButton(block, Unwired.Unknown);

            MyToolbarItemInfo present;
            if (!toolbar.TryGetItem(slot, out present))
                return new UnansweredButton(block, Unwired.Cleared);

            return new UnansweredButton(block, KeyIn(toolbar, slot, present, _buttons.PanelId(reference)) == wanted ? Unwired.Unknown : Unwired.Taken);
        }

        /// <summary>The key a slot of <paramref name="panel" />'s toolbar holds where it holds this Sprocket's action, or null.</summary>
        string KeyIn(IMyToolbar toolbar, int slot, MyToolbarItemInfo present, long panel)
        {
            if (!_buttons.Mine(present, panel))
                return null;

            _arguments.Clear();
            Toolbars.Parameters(toolbar, slot, _arguments);
            return _arguments.Count == 0 ? null : _arguments[0].Value as string;
        }

        readonly List<long> _unstartable = new List<long>();
        readonly HashSet<long> _reachable = new HashSet<long>();
        readonly Dictionary<long, UnansweredButton> _why = new Dictionary<long, UnansweredButton>();
    }
}
