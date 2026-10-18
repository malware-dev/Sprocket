using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Terminal;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using VRage.Game.ModAPI;

// Aliased because the game's Ingame namespaces collide with the mod-side ones.
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>The toolbar slots a construct's automation writes, each on the block whose toolbar it is: wired the moment the program asks for a button, let go of when it stops asking, and mended where Sprocket's own writing has gone stale.</summary>
    public sealed class WiredButtons
    {
        readonly GridWorld _world;
        readonly Automation _program;
        readonly AutomationRunner _runner;
        readonly SlotKeys _keys;
        IMyTerminalBlock _hero;
        int _reconciled = -1;

        bool _adopted;

        long _from;

        /// <summary>
        /// The slots <paramref name="program" /> writes on <paramref name="world" />, run by <paramref name="runner" />, holding
        /// the keys of <paramref name="keys" />, kept in <paramref name="hero" />.
        /// </summary>
        public WiredButtons(GridWorld world, Automation program, AutomationRunner runner, SlotKeys keys, IMyTerminalBlock hero)
        {
            _world = world;
            _program = program;
            _runner = runner;
            _keys = keys;
            _hero = hero;
        }

        /// <summary>Every button the program asked for on its last pass.</summary>
        public List<WantedWiring> Wants => _wanted;

        /// <summary>Takes on what <paramref name="from" />, the slots of the automation this one is built again from, had wired and had picked and not written yet.</summary>
        public void Carry(WiredButtons from)
        {
            _adopted = from._adopted;
            _wanted.AddRange(from._wanted);
            _asserted.AddRange(from._asserted);
        }

        /// <summary>Writes from here on in the name of <paramref name="hero" />, the block the automation is kept in now, mending on the next pass the buttons that name the one before.</summary>
        public void Rehomed(IMyTerminalBlock hero)
        {
            _hero = hero;
            _adopted = false;
            _reconciled = int.MinValue;
        }

        readonly List<Sequence> _runs = new List<Sequence>();

        /// <summary>The sequences of <paramref name="system" />, and the runtime's sequence for each trigger that acts itself.</summary>
        public List<Sequence> Runs(AutomationSystem system)
        {
            _runs.Clear();

            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
                _runs.Add(sequences[i]);

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var alone = _runner.Alone(system, triggers[i]);
                if (alone != null)
                    _runs.Add(alone);
            }

            return _runs;
        }

        List<WantedWiring> _wanted = new List<WantedWiring>();

        List<WantedWiring> _wanting = new List<WantedWiring>();

        /// <summary>
        /// Wires a button the moment the program asks for one, and never touches it again.
        /// </summary>
        /// <remarks>Called every tick: a tick with no change of wiring is one integer comparison, and reads no toolbar.</remarks>
        public void Reconcile()
        {
            if (_program.Wiring == _reconciled)
                return;

            _reconciled = _program.Wiring;

            _world.LookAgain();

            _keys.Index();
            Wanted(_wanting);

            // A program just loaded only mends: a slot of its that is empty now was cleared by hand, and stays empty.
            if (_adopted)
            {
                Release();
                Take();
                Relabel();
            }
            else
            {
                _adopted = true;
                Mend();
            }

            // Two lists swapped rather than one rebuilt, so comparing costs nothing.
            List<WantedWiring> was = _wanted;
            _wanted = _wanting;
            _wanting = was;

            // Here alone, because what the program wants changes only when its wiring does.
            if (_asserted.Count > 0)
                Assert();
        }

        /// <summary>
        /// Every button the program's triggers ask for, once per one of each system.
        /// </summary>
        void Wanted(List<WantedWiring> into)
        {
            into.Clear();

            for (var s = 0; s < _program.Systems.Count; s++)
            {
                AutomationSystem system = _program.Systems[s];
                var sequences = Runs(system);
                var instances = system.Uses;

                foreach (var sequence in sequences)
                {
                    for (var n = 0; n < instances.Count; n++)
                        WantedFor(into, system, instances[n], sequence);
                }
            }
        }

        /// <summary>
        /// The buttons one sequence asks for on one of its system.
        /// </summary>
        void WantedFor(List<WantedWiring> into, AutomationSystem system, SystemInstance instance,
            Sequence sequence)
        {
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                if (!triggers[i].IsAbout(sequence))
                    continue;

                var needed = triggers[i] as SignalTrigger;
                if (needed != null && !needed.IsWatched)
                    WantedForSignal(into, system, instance, sequence, needed);
            }
        }

        /// <summary>The toolbar slots <paramref name="instance" /> answers <paramref name="needed" /> with, each a want.</summary>
        void WantedForSignal(List<WantedWiring> into, AutomationSystem system, SystemInstance instance,
            Sequence sequence, SignalTrigger needed)
        {
            var blocks = _answering;
            var positions = _answeringAt;
            _program.Answers(system, instance, needed, blocks, positions);

            for (var i = 0; i < blocks.Count && i < positions.Count; i++)
            {
                var at = positions[i];
                var slot = Toolbars.Index(ToolbarOf(blocks[i], at.Toolbar), at);
                if (slot < 0)
                    continue;

                var want = new WantedWiring();
                want.PanelId = PanelId(blocks[i]);
                want.Toolbar = at.Toolbar;
                want.Slot = slot;
                want.Key = needed.Acts != null ? SlotKeys.KeyOf(system, instance, needed) : SlotKeys.KeyOf(system, instance, sequence, needed.Stops);
                want.Reference = blocks[i];
                want.Start = GridWorld.Start(sequence.Id, instance);
                want.Name = needed.IsWritten ? null : Shown(system, needed.Part);
                into.Add(want);
            }
        }

        /// <summary>What a toolbar slot answering the signal slot <paramref name="part" /> of <paramref name="system" /> says: its short name, or its name.</summary>
        static string Shown(AutomationSystem system, string part)
        {
            var slot = system.FindPart(part);
            return slot != null && slot.Short.Length > 0 ? slot.Short : part;
        }

        readonly List<string> _answering = new List<string>();
        readonly List<ToolbarSlot> _answeringAt = new List<ToolbarSlot>();

        static bool Holds(List<WantedWiring> wants, WantedWiring one)
        {
            for (var i = 0; i < wants.Count; i++)
            {
                if (wants[i].Same(one))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Rewrites the slots that are Sprocket's and say nothing it understands.
        /// </summary>
        /// <remarks>Called on the first pass after a load; every saved slot with a stale key mends then, so a key format can change without breaking saves.</remarks>
        void Mend()
        {
            foreach (var want in _wanting)
            {
                IMyToolbar toolbar = ToolbarOf(want);
                MyToolbarItemInfo present;

                if (toolbar == null || !toolbar.TryGetItem(want.Slot, out present)
                    || (!Mine(present, want.PanelId) && !Orphaned(present) && !Handed(present)))
                {
                    continue;
                }

                // An orphan is rewritten whatever its key says: the block it names is gone.
                if (Mine(present, want.PanelId) && Understood(toolbar, want.Slot))
                    continue;

                if (!Toolbars.Write(toolbar, want.Slot, SignalAction.Slot(want.PanelId, want.Name),
                        SignalAction.Argument(want.Key)))
                {
                    GameLog.Say($"the write was refused mending slot {want.Slot}");
                    continue;
                }

                GameLog.Say($"mended slot {want.Slot}, now {want.Key}");
                Name(want);
            }

            _from = 0;
        }

        /// <summary>Whether a slot of Sprocket's carries a key this program can resolve.</summary>
        /// <remarks>Read once per wanted button on the first pass after a load, never on a tick.</remarks>
        bool Understood(IMyToolbar toolbar, int slot)
        {
            _arguments.Clear();
            Toolbars.Parameters(toolbar, slot, _arguments);

            if (_arguments.Count == 0)
                return false;

            var key = _arguments[0].Value as string;
            return !string.IsNullOrEmpty(key) && _keys.Knows(key);
        }

        readonly List<TerminalActionParameter> _arguments = new List<TerminalActionParameter>();

        /// <summary>
        /// Lets go of every button the program no longer asks for.
        /// </summary>
        void Release()
        {
            foreach (var had in _wanted)
            {
                if (Holds(_wanting, had))
                    continue;

                IMyToolbar toolbar = ToolbarOf(had);
                MyToolbarItemInfo present;

                if (toolbar == null || !toolbar.TryGetItem(had.Slot, out present)
                    || !Asked(toolbar, present, had))
                {
                    continue;
                }

                Toolbars.Clear(toolbar, had.Slot);
                GameLog.Say($"released slot {had.Slot} on panel {had.PanelId}");
                GiveBack(toolbar, had);
            }
        }

        /// <summary>
        /// Notes what a player had in the slot <paramref name="want" /> is about to write over, where it is their own item, so
        /// letting go of the slot gives it back.
        /// </summary>
        void Keep(IMyToolbar toolbar, WantedWiring want, MyToolbarItemInfo present)
        {
            var kept = Overwritten(want);
            if (kept == null)
            {
                kept = new Taken { PanelId = want.PanelId, Toolbar = want.Toolbar, Slot = want.Slot };
                _taken.Add(kept);
            }

            kept.Key = want.Key;
            kept.Back = false;
            kept.Item = present;
            kept.Parameters.Clear();
            Toolbars.Parameters(toolbar, want.Slot, kept.Parameters);

            var panel = want.Toolbar != 1 ? null : PanelOf(want);
            kept.Named = false;
            kept.Name = null;
            if (panel == null || !panel.HasCustomButtonName(want.Slot))
                return;

            kept.Named = true;
            kept.Name = panel.GetButtonName(want.Slot);
        }

        /// <summary>Puts back in the slot <paramref name="had" /> let go of the player's own item it held before Sprocket took it, where there was one.</summary>
        void GiveBack(IMyToolbar toolbar, WantedWiring had)
        {
            var kept = Overwritten(had);
            if (kept == null || kept.Back)
                return;

            if (!Toolbars.Write(toolbar, had.Slot, kept.Item, kept.Parameters))
            {
                _taken.Remove(kept);
                return;
            }

            kept.Back = true;
            var panel = had.Toolbar != 1 ? null : PanelOf(had);
            if (panel == null)
                return;

            if (kept.Named)
                panel.SetCustomButtonName(had.Slot, kept.Name);
            else
                panel.ClearCustomButtonName(had.Slot);
        }

        /// <summary>What a player had in the slot of <paramref name="want" /> before Sprocket wrote over it, or null.</summary>
        Taken Overwritten(WantedWiring want)
        {
            foreach (var kept in _taken)
            {
                if (kept.PanelId == want.PanelId && kept.Toolbar == want.Toolbar && kept.Slot == want.Slot)
                    return kept;
            }

            return null;
        }

        /// <summary>The button panel <paramref name="want" /> is on, or null where it is another kind of block.</summary>
        IMyButtonPanel PanelOf(WantedWiring want)
        {
            var grid = _runner.Bindings.Entity(want.Reference);
            return grid?.Block as IMyButtonPanel;
        }

        /// <summary>Claims every button the program asks for and Sprocket does not already hold.</summary>
        void Take()
        {
            foreach (var want in _wanting)
            {
                if (Holds(_wanted, want))
                    continue;

                IMyToolbar toolbar = ToolbarOf(want);
                if (toolbar == null)
                    continue;

                MyToolbarItemInfo present;
                var filled = toolbar.TryGetItem(want.Slot, out present);

                if (filled && Asked(toolbar, present, want))
                    continue;

                var given = Overwritten(want);
                if (filled && given != null && given.Back && string.Equals(given.Key, want.Key, StringComparison.Ordinal))
                {
                    Keep(toolbar, want, present);
                }
                else if (filled && !Mine(present, want.PanelId) && !Orphaned(present))
                {
                    GameLog.Say($"slot {want.Slot} in use by {present.Kind} {present.ActionId}");
                    continue;
                }

                if (!Toolbars.Write(toolbar, want.Slot, SignalAction.Slot(want.PanelId, want.Name), SignalAction.Argument(want.Key)))
                {
                    GameLog.Say($"the write was refused for slot {want.Slot}");
                    continue;
                }

                GameLog.Say($"wired slot {want.Slot} to {want.Key}");
                Name(want);
            }
        }

        /// <summary>Writes again each slot it holds whose short name changed, so its caption and its button say the new one.</summary>
        void Relabel()
        {
            foreach (var want in _wanting)
            {
                var had = -1;
                for (var i = 0; i < _wanted.Count && had < 0; i++)
                {
                    if (_wanted[i].Same(want))
                        had = i;
                }

                if (had < 0 || string.Equals(_wanted[had].Name, want.Name, StringComparison.Ordinal))
                    continue;

                IMyToolbar toolbar = ToolbarOf(want);
                MyToolbarItemInfo present;
                if (toolbar == null || !toolbar.TryGetItem(want.Slot, out present) || !Asked(toolbar, present, want))
                    continue;

                if (!Toolbars.Write(toolbar, want.Slot, SignalAction.Slot(want.PanelId, want.Name), SignalAction.Argument(want.Key)))
                {
                    GameLog.Say($"the write was refused naming slot {want.Slot}");
                    continue;
                }

                Name(want);
            }
        }

        /// <summary>Whether a slot already holds exactly what a want asks for.</summary>
        public bool Asked(IMyToolbar toolbar, MyToolbarItemInfo present, WantedWiring want)
        {
            if (!Mine(present, want.PanelId))
                return false;

            _arguments.Clear();
            Toolbars.Parameters(toolbar, want.Slot, _arguments);

            return _arguments.Count > 0
                && string.Equals(_arguments[0].Value as string, want.Key, StringComparison.Ordinal);
        }

        /// <summary>
        /// Whether a slot of <paramref name="panel" />'s toolbar holds Sprocket's action for <em>this</em> Sprocket: on the
        /// panel itself, as it is written, or naming the block the automation is kept in, as a slot an older build wrote does.
        /// </summary>
        public bool Mine(MyToolbarItemInfo present, long panel)
        {
            return SignalAction.IsOurs(present) && (present.BlockEntityId == panel || present.BlockEntityId == _hero.EntityId);
        }

        /// <summary>
        /// Whether a slot holds Sprocket's action pointed at a block that is no longer there.
        /// </summary>
        bool Orphaned(MyToolbarItemInfo present)
        {
            return SignalAction.IsOurs(present)
                && present.BlockEntityId != _hero.EntityId
                && !Toolbars.Exists(present.BlockEntityId);
        }

        bool Handed(MyToolbarItemInfo present)
        {
            return _from != 0 && SignalAction.IsOurs(present) && present.BlockEntityId == _from;
        }

        /// <summary>Takes over, on its next pass, its program's buttons that still name <paramref name="from" />, the block its automation came from.</summary>
        public void TakeOver(long from)
        {
            _from = from;
            _adopted = false;
            _reconciled = int.MinValue;
        }

        /// <summary>The block the variable <paramref name="reference" /> names, by its entity id, or 0.</summary>
        public long PanelId(string reference)
        {
            GridEntity grid = _runner.Bindings.Entity(reference);
            return grid == null ? 0 : grid.Block.EntityId;
        }

        /// <summary>Toolbar <paramref name="toolbar" /> of the block the variable <paramref name="reference" /> names, or null.</summary>
        public IMyToolbar ToolbarOf(string reference, int toolbar)
        {
            GridEntity grid = _runner.Bindings.Entity(reference);
            return grid == null ? null : ToolbarOfBlock(grid.Block, toolbar);
        }

        /// <summary>The toolbar a want is on.</summary>
        /// <remarks>
        /// Found through the name the want was made from, one lookup; the construct's blocks are walked only where that name
        /// has come to stand for another block since, as a want let go of may.
        /// </remarks>
        public IMyToolbar ToolbarOf(WantedWiring want)
        {
            GridEntity named = _runner.Bindings.Entity(want.Reference);
            if (named != null && named.Block.EntityId == want.PanelId)
                return ToolbarOfBlock(named.Block, want.Toolbar);

            for (var i = 0; i < _world.Entities.Count; i++)
            {
                GridEntity grid = _world.Entities[i];
                if (grid.Block.EntityId == want.PanelId)
                    return ToolbarOfBlock(grid.Block, want.Toolbar);
            }

            return null;
        }

        /// <summary>Toolbar <paramref name="toolbar" /> of <paramref name="block" />, from 1, whatever block it is, or null where it has none.</summary>
        static IMyToolbar ToolbarOfBlock(IMyTerminalBlock block, int toolbar)
        {
            return Toolbars.Of(block, toolbar);
        }

        /// <summary>Writes every slot the program wants on <paramref name="panel" /> on the next pass, whatever the slot holds by then.</summary>
        /// <remarks>Called when a person picks a button in the editor, after being asked about anything its slot already held.</remarks>
        public void Wire(long panel)
        {
            if (!_asserted.Contains(panel))
                _asserted.Add(panel);
        }

        /// <summary>Lets go of every panel <see cref="Wire" /> asked for and no pass has written yet, as a pick is backed out of.</summary>
        public void Unwire()
        {
            _asserted.Clear();
        }

        /// <summary>Brings the toolbars into line with the program on the next tick, as though its wiring had changed.</summary>
        /// <remarks>Called for a button picked on a client's copy.</remarks>
        public void Revisit()
        {
            _reconciled = int.MinValue;
        }

        /// <summary>Writes the slots <see cref="Wire" /> asked for that do not hold what the program wants of them.</summary>
        /// <remarks>Called on the program's own pass, after it.</remarks>
        void Assert()
        {
            _answered.Clear();
            foreach (var want in _wanted)
            {
                if (!_asserted.Contains(want.PanelId))
                    continue;

                if (!_answered.Contains(want.PanelId))
                    _answered.Add(want.PanelId);

                IMyToolbar toolbar = ToolbarOf(want);
                if (toolbar == null)
                    continue;

                MyToolbarItemInfo present;
                var filled = toolbar.TryGetItem(want.Slot, out present);
                if (filled && Asked(toolbar, present, want))
                    continue;

                if (filled && !Mine(present, want.PanelId) && !Orphaned(present))
                    Keep(toolbar, want, present);

                if (!Toolbars.Write(toolbar, want.Slot, SignalAction.Slot(want.PanelId, want.Name), SignalAction.Argument(want.Key)))
                {
                    GameLog.Say($"the write was refused for slot {want.Slot}");
                    continue;
                }

                GameLog.Say($"wired slot {want.Slot} to {want.Key}, as picked");
                Name(want);
            }

            if (_answered.Count == 0)
                return;

            foreach (var answered in _answered)
                _asserted.Remove(answered);

            _world.LookAgain();
        }

        readonly List<long> _asserted = new List<long>();
        readonly List<long> _answered = new List<long>();

        // Kept for the life of the session.
        readonly List<Taken> _taken = new List<Taken>();

        /// <summary>A player's own item in a toolbar slot, as it was before Sprocket took the slot.</summary>
        sealed class Taken
        {
            public long PanelId;
            public int Toolbar;
            public int Slot;
            public string Key;
            public bool Back;
            public MyToolbarItemInfo Item;
            public readonly List<TerminalActionParameter> Parameters = new List<TerminalActionParameter>();
            public bool Named;
            public string Name;
        }

        /// <summary>Names the button <paramref name="want" /> has just wired after the signal slot it answers.</summary>
        /// <remarks>Called right after the slot is written: the game names a button after any action put in its slot.</remarks>
        void Name(WantedWiring want)
        {
            if (string.IsNullOrEmpty(want.Name) || want.Toolbar != 1)
                return;

            var grid = _runner.Bindings.Entity(want.Reference);
            var panel = grid?.Block as IMyButtonPanel;
            if (panel != null)
                panel.SetCustomButtonName(want.Slot, want.Name);
        }
    }
}
