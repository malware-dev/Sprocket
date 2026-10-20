using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>One block, answering the small set of questions every block can answer.</summary>
    public sealed partial class GridEntity
    {
        /// <summary>What a block answers when it cannot say how long something will take.</summary>
        public const int Unestimated = -1;

        readonly IMyTerminalBlock _block;
        readonly EntityKind _kind;
        readonly List<Action> _hearing = new List<Action>();

        /// <summary><paramref name="block" />, of <paramref name="kind" />, answered for where it can be by the capabilities the kind was described by.</summary>
        public GridEntity(IMyTerminalBlock block, EntityKind kind)
        {
            _block = block;
            _kind = kind;
        }

        /// <summary>The block behind this, for the few things that need the game's own object.</summary>
        public IMyTerminalBlock Block => _block;

        /// <summary>The block's id in the game.</summary>
        public long Id => _block.EntityId;

        /// <summary>The block's name, as the player gave it.</summary>
        public string Name => _block.CustomName;

        /// <summary>What kind of block it is.</summary>
        public EntityKind Kind => _kind;

        /// <summary>
        /// Live state, for the player's own words.
        /// </summary>
        public Status Status
        {
            get
            {
                var capabilities = _kind.Capabilities;
                var doing = Status.None;
                for (var i = 0; i < capabilities.Count; i++)
                {
                    var told = capabilities[i].StatusOf(_block);
                    if (told.Trouble.Exists)
                        return told;

                    if (told.Exists && !doing.Exists)
                        doing = told;
                }

                if (doing.Exists)
                    return doing;

                if (!_block.IsFunctional)
                    return Status.Of(Condition.Damaged);

                IMyFunctionalBlock functional = _block as IMyFunctionalBlock;
                if (functional != null && !functional.Enabled)
                    return Status.Of(Condition.Off);

                return Status.Of(_block.IsWorking ? Condition.On : Condition.NoPower);
            }
        }

        /// <summary>
        /// Whether this block is doing something right now.
        /// </summary>
        /// <remarks>A block no capability knows is never busy.</remarks>
        public bool IsActive
        {
            get
            {
                var capabilities = _kind.Capabilities;
                for (var i = 0; i < capabilities.Count; i++)
                {
                    if (capabilities[i].IsBusy(_block))
                        return true;
                }

                return false;
            }
        }

        /// <summary>Whether it is switched off, where it is a block that has a switch.</summary>
        public bool IsSwitchedOff
        {
            get
            {
                var functional = _block as IMyFunctionalBlock;
                return functional != null && !functional.Enabled;
            }
        }

        /// <summary>Whether it can do anything at all: built, powered and switched on.</summary>
        public bool IsWorking => _block.IsWorking;

        /// <summary>The block's own value that <paramref name="value" /> writes, made ready to be read; null where it has none.</summary>
        public Gauge Gauge(EntityAction value)
        {
            return Toolbars.Gauge(_block, value.Id);
        }

        /// <summary>Whether the block is in <paramref name="moment" />: the flag it reads is set where the moment rises, clear where it falls.</summary>
        public bool In(EntityEvent moment, ReaderMemory seen)
        {
            return moment.Flag != null && moment.Flag.Holds(_block, seen) == (moment.Edge == SignalEdge.Rising);
        }

        /// <summary>Whether <paramref name="reading" /> reads true of the block; false where there is no reading, as for a signal.</summary>
        public bool Reads(Reading reading, ReaderMemory seen)
        {
            return reading != null && reading.Holds(_block, seen);
        }

        /// <summary>Tells <paramref name="told" /> each time this block may have changed, until the answer is disposed; null where it says nothing.</summary>
        /// <remarks>Told as the block announces a change, and as Sprocket itself asks the block for anything.</remarks>
        public IDisposable Listen(Action told)
        {
            List<IDisposable> listening = null;
            var capabilities = _kind.Capabilities;
            for (var i = 0; i < capabilities.Count; i++)
            {
                var one = capabilities[i].Listen(_block, told);
                if (one == null)
                    continue;

                if (listening == null)
                    listening = new List<IDisposable>();

                listening.Add(one);
            }

            if (listening == null)
                return null;

            _hearing.Add(told);
            return new Hearing(this, told, listening);
        }

        /// <summary>Tells everything listening that Sprocket has asked the block for something.</summary>
        public void Asked()
        {
            for (var i = 0; i < _hearing.Count; i++)
                _hearing[i]();
        }

        /// <summary>Whether a step asking this block for <paramref name="action" /> would be lost: it is waited for, and the block is switched off.</summary>
        public bool Refuses(EntityAction action)
        {
            return action.Lasts && IsSwitchedOff;
        }

        /// <summary>Whether this block is getting nowhere with <paramref name="action" />, which is waited for: switched off, unpowered or broken.</summary>
        public bool Stalls(EntityAction action)
        {
            return action.Lasts && !IsWorking;
        }

        /// <summary>
        /// What this block is told by a step doing <paramref name="action" />, made ready, where the game performs it: one
        /// of the game's actions or values, or one of Sprocket's own that the game's action does. Null where Sprocket
        /// does it itself, or the block does not have it.
        /// </summary>
        public BlockOrder Prepare(EntityAction action, BoundAction bound, IList<Literal> arguments)
        {
            if (action.OwnValue != null)
                return arguments == null || arguments.Count == 0 ? null : action.OwnValue.Order(_block, arguments[0]);

            if (action.Form != ReachKind.Native)
                return Toolbars.Prepare(_block, action.Id, action.Form, arguments, action.Primitives);

            if (bound != null && bound.Acts || action.GameActionId == null)
                return null;

            return Toolbars.Prepare(_block, action.GameActionId, ReachKind.Action, arguments, action.Primitives);
        }

        /// <summary>Tells the block to do <paramref name="actionId" />.</summary>
        public void Invoke(string actionId)
        {
            Invoke(actionId, null);
        }

        /// <summary>Tells the block to do <paramref name="actionId" /> with <paramref name="arguments" />.</summary>
        public void Invoke(string actionId, IList<Literal> arguments)
        {
            Invoke(actionId, arguments, ReachKind.Native);
        }

        /// <summary>Tells the block to do <paramref name="actionId" /> with <paramref name="arguments" />, <paramref name="form" /> saying which of the three ways the step reaches it.</summary>
        /// <remarks>For the editor and the harness, never the runtime: it finds the action by name on every call.</remarks>
        public void Invoke(string actionId, IList<Literal> arguments, ReachKind form)
        {
            Perform(actionId, arguments, form);
            Asked();
        }

        /// <summary>How many ticks this block should take to finish <paramref name="actionId" /> with <paramref name="arguments" />, or <see cref="Unestimated" />.</summary>
        /// <remarks>For the editor and the harness: it works out what the action was told on every call.</remarks>
        public int Expected(string actionId, IList<Literal> arguments)
        {
            var bound = Bind(actionId, arguments);
            return bound?.Expected(_block) ?? Unestimated;
        }

        /// <summary>Why this block cannot finish <paramref name="actionId" /> as told with <paramref name="arguments" />, or none while it might.</summary>
        /// <remarks>For the editor and the harness: it works out what the action was told on every call, and remembers nothing.</remarks>
        public BlockTrouble Trouble(string actionId, IList<Literal> arguments)
        {
            var bound = Bind(actionId, arguments);
            return bound?.Trouble(_block, new ReaderMemory()) ?? BlockTrouble.None;
        }

        /// <summary>One of Sprocket's own actions as told <paramref name="arguments" />, or null where the block has no such action.</summary>
        BoundAction Bind(string actionId, IList<Literal> arguments)
        {
            var action = _kind.FindAction(actionId, ReachKind.Native);
            return action == null || action.OwnAction == null ? null : action.OwnAction.Bind(arguments ?? new Literal[0]);
        }

        void Perform(string actionId, IList<Literal> arguments, ReachKind form)
        {
            if (form == ReachKind.Value)
            {
                if (arguments != null && arguments.Count > 0)
                    Toolbars.Set(_block, actionId, arguments[0]);

                return;
            }

            if (form == ReachKind.Action)
            {
                Toolbars.Perform(_block, actionId, arguments);
                return;
            }

            // An id this block does not have does nothing, as a reading of one does.
            var entry = _kind.FindAction(actionId, ReachKind.Native);
            if (entry == null)
                return;

            var bound = entry.OwnAction?.Bind(arguments ?? new Literal[0]);
            if (bound != null && bound.Acts)
                bound.Do(_block, new ReaderMemory());
            else if (entry.GameActionId != null)
                Toolbars.Perform(_block, entry.GameActionId, arguments);
        }
    }
}
