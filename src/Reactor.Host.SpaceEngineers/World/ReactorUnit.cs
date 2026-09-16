using SpaceEngineers.Game.ModAPI;
using VRage.Game.ModAPI;
using System.Collections.Generic;
using System;
using Reactor.Host.SpaceEngineers.Core.Editing;
using Reactor.Host.SpaceEngineers.Core.Runtime;
using Reactor.Host.SpaceEngineers.Core.Sequences;
using Reactor.Host.SpaceEngineers.Core.Storage;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;
using Reactor.Host.SpaceEngineers.Presentation.Words;
using Reactor.Host.SpaceEngineers.Terminal;

// The Ingame one, which is where action arguments live. Aliased rather than imported whole, the
// way TriggerAction does, because the Ingame namespaces collide with the mod-side ones.
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>
    /// One Reactor, on one construct: its world, its program, the runtime that runs it, and the editor.
    /// </summary>
    /// <remarks>
    /// The program and its runtime belong to the block, not to the screen: the session ticks them whether
    /// or not anything is open, and the screen is only a way to look at them. Owned by the screen, a
    /// sequence would run only while somebody was watching it, and an airlock is worth nothing on those
    /// terms.
    /// </remarks>
    public sealed class ReactorUnit : IDisposable
    {
        /// <summary>One object answering two interfaces: how a sequence reads, and how a time reads.</summary>
        readonly SequenceText _sequenceWords = new SequenceText();
        readonly TimeText _timeWords = new TimeText();

        /// <summary>
        /// Where the program sits in the block's mod storage.
        /// </summary>
        /// <remarks>
        /// Mod storage is shared with every other mod on the entity, so the key has to be ours and has to
        /// never change: a new one would read as an empty Reactor and quietly discard somebody's ship.
        /// <para>
        /// <b>It also has to be claimed in <c>Content/Data/EntityComponents.sbc</c>, and the two must
        /// match.</b> The game writes out only the keys some mod has registered there and drops the rest
        /// with a line in the log, so an unclaimed key works perfectly all session and vanishes on save.
        /// </para>
        /// </remarks>
        static readonly Guid ProgramKey = new Guid("9F1E4A2C-7D53-4B18-9C6E-2A0D5F3B81E7");

        /// <summary>
        /// Where what is switched off sits, beside the program.
        /// </summary>
        /// <remarks>
        /// A second key rather than part of the program, because the program is what somebody shares
        /// and a switch is this construct's business. Claimed in <c>EntityComponents.sbc</c> the same
        /// way, and for the same reason: an unclaimed key works all session and vanishes on save.
        /// </remarks>
        static readonly Guid StateKey = new Guid("C4B7D2F6-1A85-4E39-8D07-6B92F5C1E4A3");

        readonly IMyTerminalBlock _block;
        readonly GridWorld _world;
        readonly Automation _program = new Automation();
        readonly AutomationRunner _runner;
        readonly ReactorEditor _editor;

        /// <summary>
        /// Whether this block already carries a program, without building anything to find out.
        /// </summary>
        /// <remarks>
        /// Asked of every programmable block in the world as it comes alive, so it has to be one lookup and
        /// no allocation. A block that answers false is left entirely alone.
        /// </remarks>
        public static bool HasProgram(IMyTerminalBlock block)
        {
            MyModStorageComponentBase storage = block == null ? null : block.Storage;
            if (storage == null)
                return false;

            string text;
            return storage.TryGetValue(ProgramKey, out text) && !string.IsNullOrEmpty(text);
        }

        public ReactorUnit(IMyTerminalBlock block)
        {
            _block = block;
            _world = new GridWorld(block);
            _runner = new AutomationRunner(_program, _world);

            // Before the editor, which builds its first screen out of whatever the program already says.
            Load();

            _editor = new ReactorEditor(_world, _program, _runner, _sequenceWords, _timeWords);

            // The player's own name is only where a namespace starts; choosing another is theirs to do.
            if (MyAPIGateway.Multiplayer != null)
                _editor.Author = MyAPIGateway.Multiplayer.MyName;
        }

        public ReactorEditor Editor => _editor;

        public GridWorld World => _world;

        public AutomationRunner Runner => _runner;

        /// <summary>
        /// Advances everything by one frame.
        /// </summary>
        /// <remarks>
        /// Through the editor rather than straight to the runner, because <c>ReactorEditor.Tick</c> ticks
        /// the runner itself and then brings its screen up to date. Doing both would run every sequence
        /// twice on the frames somebody had the editor open, and the runner takes an absolute frame number
        /// rather than ignoring a repeat.
        ///
        /// The cost is that a closed editor still refreshes a screen nobody is looking at. That is cheap by
        /// construction - core compares before it writes and says nothing when nothing differs - and it is
        /// worth more than a flag saying which of two things is doing the ticking this frame.
        /// </remarks>
        int _reconciled = -1;

        /// <summary>Whether the panels have been looked at once, which is not the same as written to.</summary>
        bool _adopted;

        /// <summary>Whether anything has asked for a reading yet. Nothing is read until something does.</summary>
        bool _read;

        /// <summary>What a key names: a sequence, and which one of its system to run it on.</summary>
        struct Target
        {
            public Sequence Sequence;
            public SystemInstance Instance;
        }

        /// <summary>
        /// What each key in a toolbar slot names, on this construct.
        /// </summary>
        /// <remarks>
        /// <b>Ordinal, and nothing is ever parsed.</b> The key arrives as a string the game already
        /// holds, so a fire is one hash and one compare with no allocation - what was worth fearing
        /// about "string lookup" was always the splitting and joining, never the lookup. A key Reactor
        /// did not build has nothing behind it to take apart anyway.
        /// <para>
        /// Held per construct, which is the scope the slot's other half establishes: the slot carries
        /// the Reactor block's entity id, so a key is only ever asked of the one program that block
        /// owns.
        /// </para>
        /// <para>
        /// Rebuilt from the program rather than remembered across a save, so a slot written in an
        /// earlier session needs nothing done to it. A key naming something since deleted is simply
        /// absent, and firing it does nothing - which is the right answer to a stale slot, and to a
        /// slot somebody copied onto a button Reactor never asked for.
        /// </para>
        /// </remarks>
        readonly Dictionary<string, Target> _byKey =
            new Dictionary<string, Target>(StringComparer.Ordinal);

        /// <summary>
        /// What goes in a slot: which system, which one of it, which sequence.
        /// </summary>
        /// <remarks>
        /// <b>The system is named by its identity</b>, because a number cannot say which system it means once
        /// systems are exported, imported, or on constructs that join, and a name changes. So a button keeps
        /// working through a rename. Built once, at bind, and read back verbatim.
        /// </remarks>
        static string KeyOf(AutomationSystem system, SystemInstance instance, Sequence sequence)
        {
            return system.Identity.ToString("D") + "/" + (instance == null ? 0 : instance.Number) + "/" + sequence.Number;
        }

        /// <summary>A block fired one of the actions Reactor put in it.</summary>
        public void Fire(string key)
        {
            Target target;
            if (!_byKey.TryGetValue(key, out target))
                return;

            _runner.Call(target.Sequence, target.Instance);
        }

        /// <summary>
        /// Rebuilds what the keys name, from the program.
        /// </summary>
        /// <remarks>
        /// Cleared rather than merged, so a deleted sequence stops answering. Runs with the rest of
        /// the reconcile, on the same one-integer signal, because a sequence or one of a system
        /// appears and disappears only by an edit to the program.
        /// </remarks>
        void Reindex()
        {
            _byKey.Clear();

            for (var s = 0; s < _program.Systems.Count; s++)
            {
                AutomationSystem system = _program.Systems[s];
                var sequences = system.Sequences;
                var instances = system.Instances;

                for (var q = 0; q < sequences.Count; q++)
                {
                    if (instances.Count == 0)
                    {
                        Register(system, null, sequences[q]);
                        continue;
                    }

                    for (var n = 0; n < instances.Count; n++)
                        Register(system, instances[n], sequences[q]);
                }
            }
        }

        void Register(AutomationSystem system, SystemInstance instance, Sequence sequence)
        {
            var target = new Target();
            target.Sequence = sequence;
            target.Instance = instance;

            _byKey[KeyOf(system, instance, sequence)] = target;
        }

        /// <summary>
        /// What the program wants wired right now: this button of this panel runs this sequence.
        /// </summary>
        struct Want
        {
            public long PanelId;
            public int Slot;
            public string Key;

            /// <summary>What the trigger named, so the block can be resolved again without a search.</summary>
            public string Reference;

            /// <summary>Which event of it, so the block can be asked about the right button.</summary>
            public string EventId;

            /// <summary>Which sequence this is for, so reachability can be reported per sequence.</summary>
            public int SequenceId;

            public bool Same(Want other)
            {
                return PanelId == other.PanelId && Slot == other.Slot
                    && string.Equals(Key, other.Key, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// What was asked for last time, and what is being asked for now.
        /// </summary>
        /// <remarks>
        /// Two lists swapped rather than one rebuilt, so the comparison costs nothing and neither does
        /// keeping it. <b>The previous set is the whole of what Reactor remembers about wiring</b> -
        /// what is actually in a panel is the panel's business, and the game's.
        /// </remarks>
        List<Want> _wanted = new List<Want>();

        List<Want> _wanting = new List<Want>();

        /// <summary>
        /// Wires a button the moment the program asks for one, and never touches it again.
        /// </summary>
        /// <remarks>
        /// Reactor is told nothing about this: core says a sequence is started by a block, and this
        /// side works out that a button panel needs a slot set for that to be true. The program's
        /// revision is the whole signal, so an ordinary tick is one integer comparison, and reading a
        /// toolbar is never on the tick path.
        /// <para>
        /// <b>Assignment happens once, where the block is bound, and then the game owns the slot.</b>
        /// Reactor's part afterwards is to answer when the action is invoked. Anything that kept
        /// asserting what a slot should hold is Reactor arguing with a player who can edit that slot
        /// whenever they like - and losing the argument is the only acceptable outcome, so it is
        /// better not to have it.
        /// </para>
        /// <para>
        /// A diff rather than a sweep, because the interesting case is subtraction: somebody who picks
        /// button 2 after button 1 leaves button 1 wired to something nothing references, which is a
        /// button on their ship that does something they cannot see.
        /// </para>
        /// <para>
        /// <b>A slot somebody else is using is left alone.</b> Overwriting it would configure Reactor
        /// by breaking their ship, which is worse than not working.
        /// </para>
        /// </remarks>
        void Reconcile()
        {
            // Wiring, not Revision. Revision is about declarations moving; this is about somebody
            // having said what a trigger is, which is the thing that has to be acted on - and which
            // happens even when the trigger ends up identical to what it was.
            if (_program.Wiring == _reconciled)
                return;

            _reconciled = _program.Wiring;

            // Somebody has just said what a trigger is, so what is wired has changed under whatever a
            // screen is showing - including by Reactor writing a slot itself a moment ago. Read it
            // again, or a sequence the player has just mended goes on being reported as unstartable
            // and parked a second later.
            _world.LookAgain();

            Reindex();
            Wanted(_wanting);

            // Nothing is written for a program that has just been loaded. Every button it asks for was
            // wired by the session that built it and the game has owned those slots ever since; a slot
            // that is empty now is somebody having cleared it by hand, which is an instruction and not
            // a gap to fill. Reactor used to write them all back here, which undid that silently.
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

            List<Want> was = _wanted;
            _wanted = _wanting;
            _wanting = was;
        }

        /// <summary>
        /// Every button the program's triggers ask for, once per one of each system.
        /// </summary>
        /// <remarks>
        /// Per one of a system rather than per sequence, because a button belongs to one of them:
        /// the panel by the front airlock starts the front airlock, and the panel by the port one
        /// starts the port one. <see cref="SystemInstance.Resolve" /> is what turns the roles a
        /// sequence names into the blocks this particular one uses, and a part nobody filled in
        /// resolves to what the system was built with - which is right for the first of them and
        /// an honest failure for a later one.
        /// </remarks>
        void Wanted(List<Want> into)
        {
            into.Clear();

            for (var s = 0; s < _program.Systems.Count; s++)
            {
                AutomationSystem system = _program.Systems[s];
                var sequences = system.Sequences;
                var instances = system.Instances;

                for (var q = 0; q < sequences.Count; q++)
                {
                    if (instances.Count == 0)
                    {
                        WantedFor(into, system, null, sequences[q]);
                        continue;
                    }

                    for (var n = 0; n < instances.Count; n++)
                        WantedFor(into, system, instances[n], sequences[q]);
                }
            }
        }

        /// <summary>The buttons one sequence asks for on one of its system.</summary>
        void WantedFor(List<Want> into, AutomationSystem system, SystemInstance instance,
            Sequence sequence)
        {
            var when = sequence.Trigger as WhenTrigger;
            if (when == null)
                return;

            var slot = ButtonPanelExtension.ButtonOf(when.EventId);
            if (slot < 0)
                return;

            // Reused rather than made per sequence: this runs whenever the program changes, and a
            // fresh list per trigger would be a drip of garbage for no reason.
            _sources.Clear();
            if (instance == null)
            {
                for (var i = 0; i < when.Sources.Count; i++)
                    _sources.Add(when.Sources[i]);
            }
            else
            {
                instance.Resolve(when.Sources, _sources);
            }

            for (var source = 0; source < _sources.Count; source++)
            {
                IMyToolbar toolbar = ToolbarOf(_sources[source]);
                if (toolbar == null || slot >= toolbar.SlotCount)
                    continue;

                var want = new Want();
                want.PanelId = PanelId(_sources[source]);
                want.Slot = slot;
                want.Key = KeyOf(system, instance, sequence);
                want.Reference = _sources[source];
                want.EventId = when.EventId;
                want.SequenceId = sequence.Id;
                into.Add(want);
            }
        }

        readonly List<string> _sources = new List<string>();

        static bool Holds(List<Want> wants, Want one)
        {
            for (var i = 0; i < wants.Count; i++)
            {
                if (wants[i].Same(one))
                    return true;
            }

            return false;
        }

        /// <summary>Whether the block this want names is arranged to start exactly this.</summary>
        bool Wired(Want want)
        {
            var grid = _runner.Bindings.Entity(want.Reference) as GridEntity;
            if (grid == null)
                return true;

            IBlockExtension extension = BlockExtensions.For(grid.Block);
            return extension == null || extension.Raises(grid.Block, want.EventId, want.Key);
        }

        /// <summary>
        /// Tells the world which sequences nothing can currently start.
        /// </summary>
        /// <remarks>
        /// <b>The whole picture in one pass, on a cadence, rather than a toolbar read per question.</b>
        /// The editor asks about a sequence whenever a screen is refreshing, which is often; this is
        /// asked once a second however many screens are open, and what they get back is a lookup in a
        /// set that is almost always empty.
        /// <para>
        /// A sequence counts as startable if <i>any</i> of the buttons it names is wired to it, which
        /// is the same "any of them" a trigger already means when it lists several blocks.
        /// </para>
        /// </remarks>
        /// <summary>
        /// How many slots on this construct claim each key.
        /// </summary>
        /// <remarks>
        /// <b>Counted rather than remembered.</b> A system says it needs something to start a sequence;
        /// which button, timer or sensor answers that is the construct's business and lives only in the
        /// toolbars. So the answer is arrived at by looking, and two triggers running one sequence need
        /// two slots running it.
        /// </remarks>
        readonly Dictionary<string, int> _claims = new Dictionary<string, int>(StringComparer.Ordinal);

        void Claims()
        {
            _claims.Clear();

            for (var i = 0; i < _world.Entities.Count; i++)
            {
                var grid = _world.Entities[i] as GridEntity;
                if (grid == null)
                    continue;

                IMyToolbar toolbar = Toolbars.Of(grid.Block);
                if (toolbar == null)
                    continue;

                for (var slot = 0; slot < toolbar.SlotCount; slot++)
                {
                    MyToolbarItemInfo present;
                    if (!toolbar.TryGetItem(slot, out present) || !TriggerAction.IsOurs(present))
                        continue;

                    _arguments.Clear();
                    Toolbars.Parameters(toolbar, slot, _arguments);
                    if (_arguments.Count == 0)
                        continue;

                    var key = _arguments[0].Value as string;
                    if (string.IsNullOrEmpty(key))
                        continue;

                    int held;
                    _claims[key] = _claims.TryGetValue(key, out held) ? held + 1 : 1;
                }
            }
        }

        /// <summary>
        /// Whether enough of this construct answers what a sequence says it needs.
        /// </summary>
        /// <remarks>
        /// Nothing records which slot answers which declaration, so this is a count: three declared
        /// triggers and two slots means one of them is unanswered, and which one is a question only the
        /// person standing in front of it can settle.
        /// </remarks>
        bool Answered(AutomationSystem system, SystemInstance instance, Sequence sequence)
        {
            var needed = 0;
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                if (triggers[i] is RequiredTrigger && triggers[i].Runs == sequence.Number)
                    needed++;
            }

            if (needed == 0)
                return true;

            int found;
            _claims.TryGetValue(KeyOf(system, instance, sequence), out found);
            return found >= needed;
        }

        void Reach()
        {
            _unstartable.Clear();
            _reachable.Clear();
            _broken.Clear();
            Claims();

            // What the construct has to provide, counted. A sequence nothing answers is reported the
            // same way one whose button was cleared is.
            for (var s = 0; s < _program.Systems.Count; s++)
            {
                AutomationSystem system = _program.Systems[s];
                var sequences = system.Sequences;
                var instances = system.Instances;

                for (var q = 0; q < sequences.Count; q++)
                {
                    for (var n = 0; n < instances.Count; n++)
                    {
                        if (Answered(system, instances[n], sequences[q]))
                            continue;

                        if (!_unstartable.Contains(sequences[q].Id))
                            _unstartable.Add(sequences[q].Id);
                    }
                }
            }

            for (var w = 0; w < _wanted.Count; w++)
            {
                Want want = _wanted[w];
                if (Wired(want))
                {
                    _reachable.Add(want.SequenceId);
                    continue;
                }

                // Not wired, and we could see that for ourselves - so the trigger stops naming it.
                // A screen has to show what is true, and "this sequence starts from that button" is
                // not true once the button no longer points here. Marking it instead left the same
                // untruth in three places: the sentence, the rows above it, and the picker opening
                // with a button preselected that nothing was going to honour.
                _dropped.Add(want);
            }

            for (var d = 0; d < _dropped.Count; d++)
                Drop(_dropped[d]);

            _dropped.Clear();

            for (var w = 0; w < _wanted.Count; w++)
            {
                Want want = _wanted[w];
                if (!_reachable.Contains(want.SequenceId) && !_unstartable.Contains(want.SequenceId))
                    _unstartable.Add(want.SequenceId);
            }

            _world.Unstartable(_unstartable, _broken);
        }

        readonly List<int> _unstartable = new List<int>();
        readonly HashSet<int> _reachable = new HashSet<int>();
        readonly Dictionary<int, List<string>> _broken = new Dictionary<int, List<string>>();
        readonly List<Want> _dropped = new List<Want>();

        /// <summary>
        /// Takes a block out of a trigger, because what reached it is gone.
        /// </summary>
        /// <remarks>
        /// <b>This is the host changing the program, which it does nowhere else</b>, so it is worth
        /// being plain about why. A block reference that has come adrift is kept, because the block
        /// may come back and nothing was decided by anybody. This is not that: somebody cleared a
        /// button or pointed it at something else, which is a decision, and honouring it means the
        /// sequence stops claiming that button starts it.
        /// <para>
        /// Only ever done from a toolbar we could actually read - see <see cref="Reach" />. A panel
        /// that cannot be looked at right now says nothing, rather than saying no.
        /// </para>
        /// </remarks>
        void Drop(Want want)
        {
            for (var i = 0; i < _program.Sequences.Count; i++)
            {
                Sequence sequence = _program.Sequences[i];
                if (sequence.Id != want.SequenceId)
                    continue;

                var when = sequence.Trigger as WhenTrigger;
                if (when == null || !when.Sources.Remove(want.Reference))
                    return;

                Trace("dropped " + want.Reference + " from sequence " + want.SequenceId
                    + ": slot " + want.Slot + " is no longer ours");

                // So the next pass works from what the trigger now says rather than from what it
                // said when this one started.
                _program.Rewire();
                return;
            }
        }

        /// <summary>
        /// Rewrites the slots that are ours and say nothing we understand.
        /// </summary>
        /// <remarks>
        /// The one exception to leaving a loaded program's slots alone, and it is not a loophole in
        /// that rule - it is the rule's missing case. Three things a slot can be, and only one of them
        /// is a person's decision:
        /// <list type="bullet">
        /// <item>empty, or holding somebody else's action - a person changed it, so it stands;</item>
        /// <item>ours, with a key that resolves - working, so leave it;</item>
        /// <item><b>ours, with a key that means nothing</b> - nobody typed that, and nobody could.</item>
        /// </list>
        /// The third can only be Reactor's own writing gone stale: a key format that has changed
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
            for (var w = 0; w < _wanting.Count; w++)
            {
                Want want = _wanting[w];

                IMyToolbar toolbar = ToolbarOf(want.PanelId);
                MyToolbarItemInfo present;

                if (toolbar == null || !toolbar.TryGetItem(want.Slot, out present)
                    || !TriggerAction.IsOurs(present))
                {
                    continue;
                }

                if (Understood(toolbar, want.Slot))
                    continue;

                if (!Toolbars.Write(toolbar, want.Slot, TriggerAction.Slot(_block.EntityId),
                        TriggerAction.Argument(want.Key)))
                {
                    Trace("the write was refused mending slot " + want.Slot);
                    continue;
                }

                Trace("mended slot " + want.Slot + ", now " + want.Key);
            }
        }

        /// <summary>Whether a slot of ours carries a key this program can resolve.</summary>
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
            return !string.IsNullOrEmpty(key) && _byKey.ContainsKey(key);
        }

        readonly List<TerminalActionParameter> _arguments = new List<TerminalActionParameter>();

        /// <summary>
        /// Lets go of every button the program no longer asks for.
        /// </summary>
        /// <remarks>
        /// Only ever clears a slot Reactor still holds. Somebody who re-pointed the slot themselves
        /// has said what they want it to do, and taking it back would be Reactor arguing with them.
        /// </remarks>
        void Release()
        {
            for (var i = 0; i < _wanted.Count; i++)
            {
                Want had = _wanted[i];
                if (Holds(_wanting, had))
                    continue;

                IMyToolbar toolbar = ToolbarOf(had.PanelId);
                MyToolbarItemInfo present;

                if (toolbar == null || !toolbar.TryGetItem(had.Slot, out present)
                    || !TriggerAction.IsOurs(present))
                {
                    // Already empty, or already somebody else's. Either way it is not Reactor's to
                    // clear, and the person who changed it has said what they wanted it to do.
                    continue;
                }

                Toolbars.Clear(toolbar, had.Slot);
                Trace("released slot " + had.Slot + " on panel " + had.PanelId);
            }
        }

        /// <summary>Claims every button the program asks for and Reactor does not already hold.</summary>
        void Take()
        {
            for (var w = 0; w < _wanting.Count; w++)
            {
                Want want = _wanting[w];

                // Only what is newly asked for. A button somebody cleared is no longer asked for at
                // all - the trigger stopped naming it - so there is nothing here that could put one
                // back, and picking it again is an honest new want.
                if (Holds(_wanted, want))
                    continue;

                IMyToolbar toolbar = ToolbarOf(want.PanelId);
                if (toolbar == null)
                    continue;

                MyToolbarItemInfo present;
                if (toolbar.TryGetItem(want.Slot, out present) && !TriggerAction.IsOurs(present))
                {
                    // Somebody's panel already does something. Overwriting it would configure
                    // Reactor by breaking their ship, which is worse than not working.
                    Trace("slot " + want.Slot + " in use by " + present.Kind + " " + present.ActionId);
                    continue;
                }

                if (!Toolbars.Write(toolbar, want.Slot, TriggerAction.Slot(_block.EntityId),
                        TriggerAction.Argument(want.Key)))
                {
                    Trace("the write was refused for slot " + want.Slot);
                    continue;
                }

                Trace("wired slot " + want.Slot + " to " + want.Key);
            }
        }

        long PanelId(string reference)
        {
            var grid = _runner.Bindings.Entity(reference) as GridEntity;
            return grid == null ? 0 : grid.Block.EntityId;
        }

        IMyToolbar ToolbarOf(string reference)
        {
            var grid = _runner.Bindings.Entity(reference) as GridEntity;
            return grid == null ? null : ToolbarOfBlock(grid.Block);
        }

        IMyToolbar ToolbarOf(long panelId)
        {
            for (var i = 0; i < _world.Entities.Count; i++)
            {
                var grid = _world.Entities[i] as GridEntity;
                if (grid != null && grid.Block.EntityId == panelId)
                    return ToolbarOfBlock(grid.Block);
            }

            return null;
        }

        static IMyToolbar ToolbarOfBlock(IMyTerminalBlock block)
        {
            return block is IMyButtonPanel ? Toolbars.Of(block) : null;
        }

        /// <summary>
        /// Says something in the game's log, where there is one.
        /// </summary>
        /// <remarks>
        /// The log belongs to a running game and the harness runs this same code outside one, so
        /// there may be nothing to write to. Everything here goes through this for that reason.
        /// </remarks>
        static void Trace(string what)
        {
            if (MyLog.Default != null)
                MyLog.Default.WriteLineAndConsole("[Reactor] " + what);
        }

        public void Tick(int frame)
        {
            Reconcile();

            // Only when something asks. Reading a toolbar is the expensive thing Reactor does, and what
            // it is read for is what a screen says about a sequence - so a screen asks when it opens,
            // and can ask again when somebody suspects another player has wired something. Nothing is
            // read on a timer, and nothing is read while nobody is looking.
            //
            // Firing does not depend on any of it: a slot invokes Reactor's action itself and arrives
            // carrying its key, so a button works whether or not anything has ever looked at it.
            if (_world.TakeLook() || !_read)
            {
                _read = true;
                Reach();
            }

            _editor.Tick(frame);
        }

        /// <summary>
        /// Reads the program back out of the block.
        /// </summary>
        /// <remarks>
        /// A block with no storage, or storage with nothing of ours in it, is a Reactor nobody has written
        /// a program on yet, which is an empty ship rather than an error.
        /// </remarks>
        void Load()
        {
            MyModStorageComponentBase storage = _block.Storage;
            if (storage == null)
                return;

            string text;
            if (!storage.TryGetValue(ProgramKey, out text) || string.IsNullOrEmpty(text))
                return;

            string warning;
            var trouble = ProgramText.Load(text, _program, _world.Values, out warning);
            if (warning.Length > 0)
                Trace(_block.CustomName + " has a program with a warning: " + warning);

            // What is switched off, read after the program, because it names what the program
            // declared. A construct that has nothing parked has nothing here at all.
            string state;
            if (storage.TryGetValue(StateKey, out state) && !string.IsNullOrEmpty(state))
            {
                var wrong = StateText.Load(state, _program);
                if (wrong.Length > 0)
                    Trace(_block.CustomName + " has switches that would not read: " + wrong);
            }

            if (trouble.Length == 0)
                return;

            // Logged and nowhere else. The program is marked not to be saved over, which is what
            // actually protects somebody's work; there is no screen for this yet, and inventing
            // wording for one in here would put core's words on a host's screen.
            Trace(_block.CustomName + " has a program that would not read: " + trouble);
        }

        /// <summary>
        /// Writes the program into the block, so the world save carries it.
        /// </summary>
        /// <remarks>
        /// Written whenever the program might have changed rather than only when the world saves. Storage
        /// is a dictionary the game serialises along with the block, so putting the text there early costs
        /// a string and removes any question about whether a session component's save runs before or after
        /// the entities are written out.
        /// </remarks>
        public void Persist()
        {
            MyModStorageComponentBase storage = _block.Storage;

            // Shared with every other mod on this block, so one may already be here - and if it is, it is
            // the one to use rather than one to replace. Assigning it is what registers it with the
            // block's component container, which is what makes the game write it out at all; the getter
            // only answers once that has happened, so it is worth asking again rather than assuming.
            if (storage == null)
            {
                _block.Storage = new MyModStorageComponent();
                storage = _block.Storage;
            }

            if (storage == null)
            {
                // Logged rather than thrown. This runs on the way out of a screen and from the world
                // save, and taking the game down over it would cost far more than the program does.
                Trace("mod storage would not attach to " + _block.CustomName + "; program not saved");
                return;
            }

            // Never over a program that did not read cleanly. Persist runs whenever the program might
            // have changed, so one edit after a half-read load would write the damage into the save.
            if (!_program.Intact)
                return;

            storage.SetValue(ProgramKey, ProgramText.Save(_program, _world.Values));
            storage.SetValue(StateKey, StateText.Save(_program));
        }

        public void Dispose()
        {
            _world.Dispose();
        }
    }
}
