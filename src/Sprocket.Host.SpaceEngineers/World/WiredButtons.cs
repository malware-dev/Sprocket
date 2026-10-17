using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Terminal;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using VRage.Game.ModAPI;

// The Ingame one, which is where action arguments live. Aliased rather than imported whole, the
// way SignalAction does, because the Ingame namespaces collide with the mod-side ones.
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>The toolbar slots a construct's automation writes: wired the moment the program asks for a button, let go of when it stops asking, and mended where Sprocket's own writing has gone stale.</summary>
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
        /// the keys of <paramref name="keys" />, each naming <paramref name="hero" />, the block the automation is kept in.
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
        /// <remarks>
        /// Sprocket is told nothing about this: core says a sequence is started by a block, and this
        /// side works out that a button panel needs a slot set for that to be true. The program's
        /// revision is the whole signal, so an ordinary tick is one integer comparison, and reading a
        /// toolbar is never on the tick path.
        /// <para>
        /// <b>Assignment happens once, where the block is bound, and then the game owns the slot.</b>
        /// Sprocket's part afterwards is to answer when the action is invoked. Anything that kept
        /// asserting what a slot should hold is Sprocket arguing with a player who can edit that slot
        /// whenever they like - and losing the argument is the only acceptable outcome, so it is
        /// better not to have it.
        /// </para>
        /// <para>
        /// A diff rather than a sweep, because the interesting case is subtraction: somebody who picks
        /// button 2 after button 1 leaves button 1 wired to something nothing references, which is a
        /// button on their ship that does something they cannot see.
        /// </para>
        /// <para>
        /// <b>A slot somebody else is using is left alone.</b> Overwriting it would configure Sprocket
        /// by breaking their ship, which is worse than not working.
        /// </para>
        /// </remarks>
        public void Reconcile()
        {
            // Wiring, not Revision. Revision is about declarations moving; this is about somebody
            // having said what a trigger is, which is the thing that has to be acted on - and which
            // happens even when the trigger ends up identical to what it was.
            if (_program.Wiring == _reconciled)
                return;

            _reconciled = _program.Wiring;

            // Somebody has just said what a trigger is, so what is wired has changed under whatever a
            // screen is showing - including by Sprocket writing a slot itself a moment ago. Read it
            // again, or a sequence the player has just mended goes on being reported as unstartable
            // and parked a second later.
            _world.LookAgain();

            _keys.Index();
            Wanted(_wanting);

            // Nothing is written for a program that has just been loaded. Every button it asks for was
            // wired by the session that built it and the game has owned those slots ever since; a slot
            // that is empty now is somebody having cleared it by hand, which is an instruction and not
            // a gap to fill.
            if (_adopted)
            {
                Release();
                Take();
            }
            else
            {
                _adopted = true;
                Mend();
            }

            // Two lists swapped rather than one rebuilt, so comparing costs nothing. The previous set is all that is
            // remembered about wiring: what a panel actually holds is the panel's business, and the game's.
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
        /// <remarks>
        /// Per one of a system rather than per sequence, because a button belongs to one of them:
        /// the panel by the front airlock starts the front airlock, and the panel by the port one
        /// starts the port one. <see cref="SystemInstance.Resolve" /> is what turns the block
        /// slots a sequence names into the blocks this particular one uses, and a slot nobody
        /// filled resolves to what the system was built with - which is right for the first of
        /// them and an honest failure for a later one.
        /// </remarks>
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
        /// <remarks>
        /// Every trigger that runs the sequence, not the first of them. Two buttons starting one
        /// sequence is the ordinary case - the one inside the airlock and the one outside - and a
        /// slot is wanted for each, on its own panel.
        /// </remarks>
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
                want.Name = needed.IsWritten ? null : needed.Part;
                into.Add(want);
            }
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
        /// <remarks>
        /// The one exception to leaving a loaded program's slots alone, and it is not a loophole in
        /// that rule - it is the rule's missing case. Three things a slot can be, and only one of them
        /// is a person's decision:
        /// <list type="bullet">
        /// <item>empty, or holding somebody else's action - a person changed it, so it stands;</item>
        /// <item>Sprocket's, with a key that resolves - working, so leave it;</item>
        /// <item><b>Sprocket's, with a key that means nothing</b> - nobody typed that, and nobody could.</item>
        /// </list>
        /// The third can only be Sprocket's own writing gone stale: a key format that has changed
        /// underneath it, or a payload of the wrong type entirely. Leaving it would leave somebody's
        /// buttons permanently dead with no way back but re-picking each one, and calling that
        /// "respecting their choice" would be dishonest about whose writing it is.
        /// <para>
        /// Which makes this the migration path for the key, once and for all. Change what a key looks
        /// like and every slot in every saved world mends itself on the next load, with nothing to
        /// version and nothing to ask anybody.
        /// </para>
        /// </remarks>
        void Mend()
        {
            foreach (var want in _wanting)
            {
                IMyToolbar toolbar = ToolbarOf(want);
                MyToolbarItemInfo present;

                if (toolbar == null || !toolbar.TryGetItem(want.Slot, out present)
                    || (!Mine(present) && !Orphaned(present) && !Handed(present)))
                {
                    continue;
                }

                // One of Sprocket's that already says something this program can resolve is left alone.
                // An orphan is rewritten whatever its key says, because the id is the broken part:
                // the block it names is gone, so nothing it says can be honoured by anybody.
                if (Mine(present) && Understood(toolbar, want.Slot))
                    continue;

                if (!Toolbars.Write(toolbar, want.Slot, SignalAction.Slot(_hero.EntityId),
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
        /// <remarks>
        /// Read once per wanted button on the first pass after a load, never on a tick. A slot holds
        /// whatever was written into it rather than what the action declares, so anything that is not
        /// a key this program knows answers no - wrong type included.
        /// </remarks>
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
        /// <remarks>
        /// Only ever clears a slot Sprocket still holds. Somebody who re-pointed the slot themselves
        /// has said what they want it to do, and taking it back would be Sprocket arguing with them.
        /// </remarks>
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
                    // Already empty, already somebody else's, or no longer what this want asked for.
                    // None of those is Sprocket's to clear, and whoever changed it has said what they
                    // wanted it to do.
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
        /// <remarks>
        /// Remembered as taken back, so the same button taken over again, by a step made again, is taken again rather than
        /// left as somebody's.
        /// </remarks>
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
                // Only what is newly asked for. A button somebody cleared is no longer asked for at
                // all - the trigger stopped naming it - so there is nothing here that could put one
                // back, and picking it again is an honest new want.
                if (Holds(_wanted, want))
                    continue;

                IMyToolbar toolbar = ToolbarOf(want);
                if (toolbar == null)
                    continue;

                MyToolbarItemInfo present;
                var filled = toolbar.TryGetItem(want.Slot, out present);

                // Already exactly this.
                if (filled && Asked(toolbar, present, want))
                    continue;

                var given = Overwritten(want);
                if (filled && given != null && given.Back && string.Equals(given.Key, want.Key, StringComparison.Ordinal))
                {
                    Keep(toolbar, want, present);
                }
                else if (filled && !Mine(present) && !Orphaned(present))
                {
                    // Somebody's panel already does something. Overwriting it would configure
                    // Sprocket by breaking their ship, which is worse than not working - and another
                    // Sprocket holding it is somebody, whose program that button belongs to.
                    GameLog.Say($"slot {want.Slot} in use by {present.Kind} {present.ActionId}");
                    continue;
                }

                if (!Toolbars.Write(toolbar, want.Slot, SignalAction.Slot(_hero.EntityId), SignalAction.Argument(want.Key)))
                {
                    GameLog.Say($"the write was refused for slot {want.Slot}");
                    continue;
                }

                GameLog.Say($"wired slot {want.Slot} to {want.Key}");
                Name(want);
            }
        }

        /// <summary>Whether a slot already holds exactly what a want asks for.</summary>
        /// <remarks>
        /// Sprocket's own action is exactly this only when it carries this want's key. The action on
        /// this block with any other key is this block's writing for something else - a program it has
        /// replaced, or a sequence it no longer has - and is to be written over, not taken as done.
        /// </remarks>
        public bool Asked(IMyToolbar toolbar, MyToolbarItemInfo present, WantedWiring want)
        {
            if (!Mine(present))
                return false;

            _arguments.Clear();
            Toolbars.Parameters(toolbar, want.Slot, _arguments);

            return _arguments.Count > 0
                && string.Equals(_arguments[0].Value as string, want.Key, StringComparison.Ordinal);
        }

        /// <summary>
        /// Whether a slot holds Sprocket's action pointed at <em>this</em> Sprocket.
        /// </summary>
        /// <remarks>
        /// The action's name alone is not enough, and answering with it was a real fault: cut the
        /// programmable block and paste it back and the game gives it a new id, so every slot on the
        /// construct names a block that is gone. Reading only the name, Sprocket found its own action
        /// in each of them and reported every button as wired while none of them did anything.
        /// <para>
        /// The id it needs was in the slot the whole time, because Sprocket writes its own block into
        /// the action it puts there.
        /// </para>
        /// </remarks>
        public bool Mine(MyToolbarItemInfo present)
        {
            return SignalAction.IsOurs(present) && present.BlockEntityId == _hero.EntityId;
        }

        /// <summary>
        /// Whether a slot holds Sprocket's action pointed at a block that is no longer there.
        /// </summary>
        /// <remarks>
        /// Wiring nothing can honour, and nobody decided on: the block it names is gone, so there is
        /// no Sprocket left for that button to reach and no owner to argue with. It is adopted rather
        /// than reported, which is what makes a programmable block survive being cut and pasted, or
        /// ground down while another holds the same systems.
        /// <para>
        /// A live block is somebody's, even when that somebody is another Sprocket, and a slot that is
        /// simply empty is somebody having cleared it - which is an instruction. Neither is touched.
        /// </para>
        /// </remarks>
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

        long PanelId(string reference)
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
        public IMyToolbar ToolbarOf(WantedWiring want)
        {
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
        /// <remarks>
        /// A person picking a button in the editor, after being asked about anything its slot already held. That pick is the
        /// instruction a slot changed by hand waits for, so it is written even where Sprocket believes it already did.
        /// </remarks>
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
        /// <remarks>
        /// For a button picked on a client's copy. The client's edit arrives as a program, and none arrives where the same
        /// button was picked again, so nothing here changes to bring that pass about.
        /// </remarks>
        public void Revisit()
        {
            _reconciled = int.MinValue;
        }

        /// <summary>Writes the slots <see cref="Wire" /> asked for that do not hold what the program wants of them.</summary>
        /// <remarks>
        /// On the program's own pass, after it, so what is wanted is what the edit that picked the button left. A panel nothing
        /// wants yet stays asked for until <see cref="Unwire" />: a trigger joins the program only once what it does is chosen,
        /// some screens after its button.
        /// </remarks>
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

                if (filled && !Mine(present) && !Orphaned(present))
                    Keep(toolbar, want, present);

                if (!Toolbars.Write(toolbar, want.Slot, SignalAction.Slot(_hero.EntityId), SignalAction.Argument(want.Key)))
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

        /// <summary>The players' own items Sprocket wrote over because somebody took their button, kept for the life of the session to be given back.</summary>
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
        /// <remarks>
        /// Right after the slot is written, since the game names a button after any action put in its slot, Sprocket's
        /// included; the game passes a button's name on to every player.
        /// </remarks>
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
