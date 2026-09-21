using SpaceEngineers.Game.ModAPI;
using VRage.Game.ModAPI;
using System.Collections.Generic;
using System;
using Sprocket.Host.SpaceEngineers.Editing;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Components;
using Sprocket.Host.SpaceEngineers.Terminal;

// The Ingame one, which is where action arguments live. Aliased rather than imported whole, the
// way TriggerAction does, because the Ingame namespaces collide with the mod-side ones.
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// One Sprocket, on one construct: its world, its program, the runtime that runs it, and the editor.
    /// </summary>
    /// <remarks>
    /// The program and its runtime belong to the block, not to the screen: the session ticks them whether
    /// or not anything is open, and the screen is only a way to look at them. Owned by the screen, a
    /// sequence would run only while somebody was watching it, and an airlock is worth nothing on those
    /// terms.
    /// </remarks>
    public sealed class SprocketUnit : IDisposable
    {
        /// <summary>One object answering two interfaces: how a sequence reads, and how a time reads.</summary>

        /// <summary>
        /// Where the program sits in the block's mod storage.
        /// </summary>
        /// <remarks>
        /// Mod storage is shared with every other mod on the entity, so the key has to be ours and has to
        /// never change: a new one would read as an empty Sprocket and quietly discard somebody's ship.
        /// <para>
        /// <b>It also has to be claimed in <c>Content/Data/EntityComponents.sbc</c>, and the two must
        /// match.</b> The game writes out only the keys some mod has registered there and drops the rest
        /// with a line in the log, so an unclaimed key works perfectly all session and vanishes on save.
        /// </para>
        /// </remarks>
        internal static readonly Guid ProgramKey = new Guid("9F1E4A2C-7D53-4B18-9C6E-2A0D5F3B81E7");

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
        readonly SprocketEditor _editor;

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

        public SprocketUnit(IMyTerminalBlock block, Settings settings)
        {
            _block = block;
            _world = new GridWorld(block);
            _runner = new AutomationRunner(_program, _world);

            // Before the editor, which builds its first screen out of whatever the program already says.
            Load();

            _editor = new SprocketEditor(_world, _program, _runner);

            // The player's own name is only where a namespace starts; choosing another is theirs to do.
            // What the game draws in front of it is not part of that name - see Player.Plain.
            if (MyAPIGateway.Multiplayer != null)
                _editor.Author = Player.Plain(MyAPIGateway.Multiplayer.MyName);

            _editor.DeveloperMode = settings != null && settings.DeveloperMode;

            // Asked once and then listened for. Whether this machine is working decides whether the
            // automation runs at all, and it changes when the lights go out rather than on a tick -
            // so asking the block sixty times a second would be two questions for one answer that
            // hardly ever moves. Released in Dispose, because a listener left on a block the game has
            // let go of is a leak that holds the whole unit alive.
            _runner.Live = _block.IsWorking;
            _block.IsWorkingChanged += Working;
        }

        /// <summary>
        /// The machine losing or regaining power, or being switched off.
        /// </summary>
        /// <remarks>
        /// The automation holds while it is not working, rather than stopping: a cycle interrupted by
        /// a blackout carries on when the lights come back, which is the same promise as one
        /// interrupted by a reload. The programmable block refuses to instantiate a script on a block
        /// that is not working, and this is the same rule for a machine that is already running one.
        /// </remarks>
        void Working(IMyCubeBlock block)
        {
            _runner.Live = _block.IsWorking;
        }

        /// <summary>The block this belongs to, for anything asking which construct it is on.</summary>
        internal IMyTerminalBlock Block => _block;

        public SprocketEditor Editor => _editor;

        public GridWorld World => _world;

        public AutomationRunner Runner => _runner;

        /// <summary>
        /// Advances everything by one frame.
        /// </summary>
        /// <remarks>
        /// Through the editor rather than straight to the runner, because <c>SprocketEditor.Tick</c> ticks
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

        /// <summary>What a key names: a sequence, which one of its system, and what to do to it.</summary>
        struct Target
        {
            public Sequence Sequence;
            public SystemInstance Instance;
            public bool Stops;
        }

        /// <summary>
        /// What each key in a toolbar slot names, on this construct.
        /// </summary>
        /// <remarks>
        /// <b>Ordinal, and nothing is ever parsed.</b> The key arrives as a string the game already
        /// holds, so a fire is one hash and one compare with no allocation - what was worth fearing
        /// about "string lookup" was always the splitting and joining, never the lookup. A key Sprocket
        /// did not build has nothing behind it to take apart anyway.
        /// <para>
        /// Held per construct, which is the scope the slot's other half establishes: the slot carries
        /// the Sprocket block's entity id, so a key is only ever asked of the one program that block
        /// owns.
        /// </para>
        /// <para>
        /// Rebuilt from the program rather than remembered across a save, so a slot written in an
        /// earlier session needs nothing done to it. A key naming something since deleted is simply
        /// absent, and firing it does nothing - which is the right answer to a stale slot, and to a
        /// slot somebody copied onto a button Sprocket never asked for.
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

        /// <summary>
        /// The same, for a button that stops the sequence rather than starting it.
        /// </summary>
        /// <remarks>
        /// The verb is on the end rather than the front, so every key written before stopping existed
        /// still says what it always said. A slot is never rewritten, so that is not a courtesy - it
        /// is the only way the old ones keep working.
        /// </remarks>
        static string KeyOf(AutomationSystem system, SystemInstance instance, Sequence sequence,
            bool stops)
        {
            var key = KeyOf(system, instance, sequence);
            return stops ? key + "/stop" : key;
        }

        /// <summary>A block fired one of the actions Sprocket put in it.</summary>
        public void Fire(string key)
        {
            Target target;
            if (!_byKey.TryGetValue(key, out target))
                return;

            if (target.Stops)
                _runner.Halt(target.Sequence, target.Instance);
            else
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

        /// <summary>
        /// Both names a sequence answers to: the one that runs it and the one that stops it.
        /// </summary>
        /// <remarks>
        /// Both always, rather than only the ones some trigger asks for, because a key is a name and
        /// not a permission - a slot naming one is a slot somebody made, whatever the program has
        /// since become.
        /// </remarks>
        void Register(AutomationSystem system, SystemInstance instance, Sequence sequence)
        {
            var target = new Target();
            target.Sequence = sequence;
            target.Instance = instance;
            _byKey[KeyOf(system, instance, sequence)] = target;

            var halting = new Target();
            halting.Sequence = sequence;
            halting.Instance = instance;
            halting.Stops = true;
            _byKey[KeyOf(system, instance, sequence, true)] = halting;
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

            /// <summary>
            /// The block whose own action goes in the slot, or zero where Sprocket's action does.
            /// </summary>
            /// <remarks>
            /// A trigger that does one plain thing is written as the game's action on the block that
            /// act names, so the button keeps working with the mod removed.
            /// </remarks>
            public long ActionBlockId;

            /// <summary>The game's name for that action, or null where Sprocket's action goes in.</summary>
            public string GameActionId;

            public bool Same(Want other)
            {
                return PanelId == other.PanelId && Slot == other.Slot
                    && ActionBlockId == other.ActionBlockId
                    && string.Equals(GameActionId, other.GameActionId, StringComparison.Ordinal)
                    && string.Equals(Key, other.Key, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// What was asked for last time, and what is being asked for now.
        /// </summary>
        /// <remarks>
        /// Two lists swapped rather than one rebuilt, so the comparison costs nothing and neither does
        /// keeping it. <b>The previous set is the whole of what Sprocket remembers about wiring</b> -
        /// what is actually in a panel is the panel's business, and the game's.
        /// </remarks>
        List<Want> _wanted = new List<Want>();

        List<Want> _wanting = new List<Want>();

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
        void Reconcile()
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

            Reindex();
            Wanted(_wanting);

            // Nothing is written for a program that has just been loaded. Every button it asks for was
            // wired by the session that built it and the game has owned those slots ever since; a slot
            // that is empty now is somebody having cleared it by hand, which is an instruction and not
            // a gap to fill. Sprocket used to write them all back here, which undid that silently.
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
        /// starts the port one. <see cref="SystemInstance.Resolve" /> is what turns the block
        /// slots a sequence names into the blocks this particular one uses, and a slot nobody
        /// filled resolves to what the system was built with - which is right for the first of
        /// them and an honest failure for a later one.
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

        /// <summary>
        /// The buttons one sequence asks for on one of its system.
        /// </summary>
        /// <remarks>
        /// Every trigger that runs the sequence, not the first of them. Two buttons starting one
        /// sequence is the ordinary case - the one inside the airlock and the one outside - and a
        /// slot is wanted for each, on its own panel.
        /// </remarks>
        void WantedFor(List<Want> into, AutomationSystem system, SystemInstance instance,
            Sequence sequence)
        {
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                if (triggers[i].Runs != sequence.Number)
                    continue;

                var needed = triggers[i] as RequiredTrigger;
                if (needed != null)
                    WantedForTrigger(into, system, instance, sequence, needed);
            }
        }

        void WantedForTrigger(List<Want> into, AutomationSystem system, SystemInstance instance,
            Sequence sequence, RequiredTrigger needed)
        {
            var slot = ButtonPanelExtension.ButtonOf(needed.EventId);
            if (slot < 0)
                return;

            // Reused rather than made per sequence: this runs whenever the program changes, and a
            // fresh list per trigger would be a drip of garbage for no reason.
            _sources.Clear();
            if (instance == null)
            {
                for (var i = 0; i < needed.Sources.Count; i++)
                    _sources.Add(needed.Sources[i]);
            }
            else
            {
                instance.Resolve(needed.Sources, _sources);
            }

            for (var source = 0; source < _sources.Count; source++)
            {
                IMyToolbar toolbar = ToolbarOf(_sources[source]);
                if (toolbar == null || slot >= toolbar.SlotCount)
                    continue;

                var want = new Want();
                want.PanelId = PanelId(_sources[source]);
                want.Slot = slot;
                want.Key = KeyOf(system, instance, sequence, needed.Stops);
                want.Reference = _sources[source];
                want.EventId = needed.EventId;
                want.SequenceId = sequence.Id;

                long acts;
                string named;
                if (Plain(instance, sequence, out acts, out named))
                {
                    want.ActionBlockId = acts;
                    want.GameActionId = named;
                }

                into.Add(want);
            }
        }

        readonly List<string> _sources = new List<string>();

        /// <summary>
        /// The game's own action a sequence amounts to, where it amounts to exactly one.
        /// </summary>
        /// <remarks>
        /// An unnamed sequence of one act, on one block, whose kind knows the game's name for that
        /// act. Then the slot holds the game's action rather than Sprocket's, and the button works
        /// with the mod removed - which is the point of it.
        /// <para>
        /// Only a sequence nobody named. A named one is a thing on screen with a switch of its own,
        /// and switching it off has to stop its buttons, which the game's action in a slot would sail
        /// straight past. Anything else - several steps, several blocks, or an action Sprocket performs
        /// out of property writes - is Sprocket's to run.
        /// </para>
        /// <para>
        /// Asked of the block as well as of the kind, because a name is data and the block is the
        /// authority: a subtype that does not offer it costs a Sprocket trigger rather than a slot
        /// that silently does nothing.
        /// </para>
        /// </remarks>
        bool Plain(SystemInstance instance, Sequence sequence, out long blockId, out string actionId)
        {
            blockId = 0;
            actionId = null;

            if (sequence.HasName)
                return false;

            // One act, and nothing else that does anything. Awaits are allowed because the editor
            // writes one after every step, so a sequence of one action is an act and an await.
            Act act = null;
            var steps = sequence.Steps;
            for (var i = 0; i < steps.Count; i++)
            {
                if (steps[i] is Await)
                    continue;

                var one = steps[i] as Act;
                if (one == null || act != null)
                    return false;

                act = one;
            }

            if (act == null || !act.HasAction)
                return false;

            _acting.Clear();
            if (instance == null)
            {
                for (var i = 0; i < act.Targets.Count; i++)
                    _acting.Add(act.Targets[i]);
            }
            else
            {
                instance.Resolve(act.Targets, _acting);
            }

            if (_acting.Count != 1)
                return false;

            GridEntity entity = _runner.Bindings.Entity(_acting[0]);
            if (entity == null)
                return false;

            EntityAction action = act.Names(entity.Kind.Actions);
            if (action == null || string.IsNullOrEmpty(action.GameActionId))
                return false;

            if (!Toolbars.Offers(entity.Block, action.GameActionId))
                return false;

            blockId = entity.Block.EntityId;
            actionId = action.GameActionId;
            return true;
        }

        readonly List<string> _acting = new List<string>();

        /// <summary>What a slot holding the game's own action is counted under.</summary>
        static string PlainKey(long blockId, string actionId)
        {
            return "do/" + blockId + "/" + actionId;
        }

        static bool Holds(List<Want> wants, Want one)
        {
            for (var i = 0; i < wants.Count; i++)
            {
                if (wants[i].Same(one))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Whether the block this want names is arranged to start exactly this.
        /// </summary>
        /// <remarks>
        /// A want written as the game's own action is checked here rather than by the block's
        /// extension: what is in the slot is not Sprocket's, so there is no key to compare. A slot
        /// somebody has since changed answers no, and the trigger then stops claiming that button,
        /// which is the same rule Sprocket's own slots follow.
        /// </remarks>
        bool Wired(Want want)
        {
            GridEntity grid = _runner.Bindings.Entity(want.Reference);
            if (grid == null)
                return true;

            if (want.GameActionId != null)
            {
                IMyToolbar held = Toolbars.Of(grid.Block);
                MyToolbarItemInfo present;
                return held != null && held.TryGetItem(want.Slot, out present) && Asked(held, present, want);
            }

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

        /// <summary>How many slots each key is asked for, while one sequence is being judged.</summary>
        readonly Dictionary<string, int> _needed = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Which key a trigger's slot holds: Sprocket's, the stopping one, or the game's.</summary>
        string Wanting(AutomationSystem system, SystemInstance instance, Sequence sequence,
            Trigger trigger)
        {
            if (trigger.Stops)
                return KeyOf(system, instance, sequence, true);

            long acts;
            string named;
            return Plain(instance, sequence, out acts, out named)
                ? PlainKey(acts, named)
                : KeyOf(system, instance, sequence);
        }

        void Claims()
        {
            _claims.Clear();

            for (var i = 0; i < _world.Entities.Count; i++)
            {
                GridEntity grid = _world.Entities[i];
                IMyToolbar toolbar = Toolbars.Of(grid.Block);
                if (toolbar == null)
                    continue;

                for (var slot = 0; slot < toolbar.SlotCount; slot++)
                {
                    MyToolbarItemInfo present;
                    if (!toolbar.TryGetItem(slot, out present))
                        continue;

                    // A slot holding the game's own action on a block is a claim as well. It is how
                    // a trigger that does one plain thing is wired, and without counting it such a
                    // trigger reads as a sequence nothing starts and is parked a second later.
                    //
                    // Sprocket's action counts only where it points here. One pointed at another
                    // Sprocket runs that one's program, and one pointed at a block that is gone runs
                    // nothing at all, so counting either would be this program reporting a button
                    // that answers somebody else - or nobody - as its own.
                    if (!Mine(present))
                    {
                        if (present.Kind == MyToolbarItemKind.BlockAction
                            && !string.IsNullOrEmpty(present.ActionId))
                        {
                            Claimed(PlainKey(present.BlockEntityId, present.ActionId));
                        }

                        continue;
                    }

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
        }

        void Claimed(string key)
        {
            int held;
            _claims[key] = _claims.TryGetValue(key, out held) ? held + 1 : 1;
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
            // Counted per key rather than per sequence: a button that stops this sequence names it
            // by a different key from one that runs it, and one slot cannot answer for both.
            _needed.Clear();

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                if (!(triggers[i] is RequiredTrigger) || triggers[i].Runs != sequence.Number)
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

                var needed = sequence.Trigger as RequiredTrigger;
                if (needed == null || !needed.Sources.Remove(want.Reference))
                    return;

                Told.Say("dropped " + want.Reference + " from sequence " + want.SequenceId
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
            for (var w = 0; w < _wanting.Count; w++)
            {
                Want want = _wanting[w];

                IMyToolbar toolbar = ToolbarOf(want.PanelId);
                MyToolbarItemInfo present;

                if (toolbar == null || !toolbar.TryGetItem(want.Slot, out present)
                    || (!Mine(present) && !Orphaned(present)))
                {
                    continue;
                }

                // One of ours that already says something this program can resolve is left alone.
                // An orphan is rewritten whatever its key says, because the id is the broken part:
                // the block it names is gone, so nothing it says can be honoured by anybody.
                if (Mine(present) && Understood(toolbar, want.Slot))
                    continue;

                if (!Toolbars.Write(toolbar, want.Slot, TriggerAction.Slot(_block.EntityId),
                        TriggerAction.Argument(want.Key)))
                {
                    Told.Say("the write was refused mending slot " + want.Slot);
                    continue;
                }

                Told.Say("mended slot " + want.Slot + ", now " + want.Key);
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
        /// Only ever clears a slot Sprocket still holds. Somebody who re-pointed the slot themselves
        /// has said what they want it to do, and taking it back would be Sprocket arguing with them.
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
                    || !Asked(toolbar, present, had))
                {
                    // Already empty, already somebody else's, or no longer what this want asked for.
                    // None of those is Sprocket's to clear, and whoever changed it has said what they
                    // wanted it to do.
                    continue;
                }

                Toolbars.Clear(toolbar, had.Slot);
                Told.Say("released slot " + had.Slot + " on panel " + had.PanelId);
            }
        }

        /// <summary>Claims every button the program asks for and Sprocket does not already hold.</summary>
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
                var filled = toolbar.TryGetItem(want.Slot, out present);

                // Already exactly this. The ordinary case for a trigger written as the game's own
                // action, where what is in the slot is not Sprocket's - so the test below would read
                // Sprocket's own writing as somebody else's work.
                if (filled && Asked(toolbar, present, want))
                    continue;

                if (filled && !Mine(present) && !Orphaned(present))
                {
                    // Somebody's panel already does something. Overwriting it would configure
                    // Sprocket by breaking their ship, which is worse than not working - and another
                    // Sprocket holding it is somebody, whose program that button belongs to.
                    Told.Say("slot " + want.Slot + " in use by " + present.Kind + " " + present.ActionId);
                    continue;
                }

                if (!Toolbars.Write(toolbar, want.Slot, Item(want), Arguments(want)))
                {
                    Told.Say("the write was refused for slot " + want.Slot);
                    continue;
                }

                Told.Say("wired slot " + want.Slot + " to " + Said(want));
            }
        }

        /// <summary>What goes in the slot: the game's own action, or Sprocket's carrying a key.</summary>
        MyToolbarItemInfo Item(Want want)
        {
            return want.GameActionId == null
                ? TriggerAction.Slot(_block.EntityId)
                : MyToolbarItemInfo.BlockAction(want.ActionBlockId, want.GameActionId);
        }

        /// <summary>
        /// What the item is given to run with: the key, or nothing.
        /// </summary>
        /// <remarks>
        /// The game's own action takes no argument. Sprocket's takes the key saying which sequence of
        /// which one of which system, because its action is the same action on every construct.
        /// </remarks>
        List<TerminalActionParameter> Arguments(Want want)
        {
            return want.GameActionId == null ? TriggerAction.Argument(want.Key) : _nothing;
        }

        readonly List<TerminalActionParameter> _nothing = new List<TerminalActionParameter>();

        /// <summary>Whether a slot already holds exactly what a want asks for.</summary>
        /// <remarks>
        /// Sprocket's own action is exactly this only when it carries this want's key. The action on
        /// this block with any other key is this block's writing for something else - a program it has
        /// replaced, or a sequence it no longer has - and is to be written over, not taken as done.
        /// </remarks>
        bool Asked(IMyToolbar toolbar, MyToolbarItemInfo present, Want want)
        {
            if (want.GameActionId != null)
            {
                return present.BlockEntityId == want.ActionBlockId
                    && string.Equals(present.ActionId, want.GameActionId, StringComparison.Ordinal);
            }

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
        /// the action it puts there - see <see cref="Item" />.
        /// </para>
        /// </remarks>
        bool Mine(MyToolbarItemInfo present)
        {
            return TriggerAction.IsOurs(present) && present.BlockEntityId == _block.EntityId;
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
            return TriggerAction.IsOurs(present)
                && present.BlockEntityId != _block.EntityId
                && !Toolbars.Exists(present.BlockEntityId);
        }

        /// <summary>What a want reads as in the log.</summary>
        static string Said(Want want)
        {
            return want.GameActionId == null
                ? want.Key
                : want.GameActionId + " on " + want.ActionBlockId;
        }

        long PanelId(string reference)
        {
            GridEntity grid = _runner.Bindings.Entity(reference);
            return grid == null ? 0 : grid.Block.EntityId;
        }

        IMyToolbar ToolbarOf(string reference)
        {
            GridEntity grid = _runner.Bindings.Entity(reference);
            return grid == null ? null : ToolbarOfBlock(grid.Block);
        }

        IMyToolbar ToolbarOf(long panelId)
        {
            for (var i = 0; i < _world.Entities.Count; i++)
            {
                GridEntity grid = _world.Entities[i];
                if (grid.Block.EntityId == panelId)
                    return ToolbarOfBlock(grid.Block);
            }

            return null;
        }

        static IMyToolbar ToolbarOfBlock(IMyTerminalBlock block)
        {
            return block is IMyButtonPanel ? Toolbars.Of(block) : null;
        }

        public void Tick(int frame)
        {
            Reconcile();

            // Only when something asks. Reading a toolbar is the expensive thing Sprocket does, and what
            // it is read for is what a screen says about a sequence - so a screen asks when it opens,
            // and can ask again when somebody suspects another player has wired something. Nothing is
            // read on a timer, and nothing is read while nobody is looking.
            //
            // Firing does not depend on any of it: a slot invokes Sprocket's action itself and arrives
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
        /// A block with no storage, or storage with nothing of ours in it, is a Sprocket nobody has written
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
                Told.Say(_block.CustomName + " has a program with a warning: " + warning);

            // What is switched off, read after the program, because it names what the program
            // declared. A construct that has nothing parked has nothing here at all.
            string state;
            if (storage.TryGetValue(StateKey, out state) && !string.IsNullOrEmpty(state))
            {
                var wrong = StateText.Load(state, _program, _runner);
                if (wrong.Length > 0)
                    Told.Say(_block.CustomName + " has switches that would not read: " + wrong);
            }

            if (trouble.Length == 0)
                return;

            // Logged and nowhere else. The program is marked not to be saved over, which is what
            // actually protects somebody's work; there is no screen for this yet, and inventing
            // wording for one in here would put core's words on a host's screen.
            Told.Say(_block.CustomName + " has a program that would not read: " + trouble);
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
                Told.Say("mod storage would not attach to " + _block.CustomName + "; program not saved");
                return;
            }

            // A failed load is failed, and nothing is written over it. What lifts that is the person
            // answering the question their first edit raises - see SprocketEditor - so by the time
            // anything reaches here the decision has been taken, one way or the other.
            if (!_program.Intact)
                return;

            storage.SetValue(ProgramKey, ProgramText.Save(_program, _world.Values));
            storage.SetValue(StateKey, StateText.Save(_program, _runner));
        }

        public void Dispose()
        {
            _block.IsWorkingChanged -= Working;
            _world.Dispose();
        }
    }
}
