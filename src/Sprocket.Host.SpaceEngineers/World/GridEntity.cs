using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>One block, answering the small set of questions every block can answer.</summary>
    public sealed class GridEntity
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
        /// <remarks>
        /// Almost nothing needs it: what a block reads and what it can be told to do are answered
        /// here. Writing a toolbar is the exception, because a toolbar is the game's own structure and
        /// there is nothing to say about it in any other terms.
        /// </remarks>
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
        /// <remarks>
        /// What stops the block, where anything that knows it says something does, then what the first that knows it says
        /// it is doing: "closing" beats "on" on a list of forty. Then four answers rather than two, because "off" and "no
        /// power" are different problems and telling somebody the wrong one is worse than telling them nothing. Damage
        /// comes first because a damaged block is not working for a reason no switch will fix.
        /// </remarks>
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
        /// <remarks>
        /// Not whether it works. A row in the ship list lights up on this, and answering it with "powered
        /// and switched on" lit every row on the ship, which made the light mean nothing at all. Only
        /// something that knows the block can say it is in motion, so a block nothing knows is never busy.
        /// </remarks>
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
        public bool In(EntityEvent moment, Recall seen)
        {
            return moment.Flag != null && moment.Flag.Holds(_block, seen) == (moment.Edge == Edge.Rising);
        }

        /// <summary>Whether <paramref name="reading" /> reads true of the block; false where there is no reading, as for a signal.</summary>
        public bool Reads(Reading reading, Recall seen)
        {
            return reading != null && reading.Holds(_block, seen);
        }

        /// <summary>Tells <paramref name="told" /> each time this block may have changed, until the answer is disposed; null where it says nothing.</summary>
        /// <remarks>
        /// Told as the block announces a change, by whatever knows it, and as Sprocket itself asks the block for anything.
        /// </remarks>
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
        /// <remarks>What Sprocket asks of a block is a change it need not wait for the block to announce.</remarks>
        public void Asked()
        {
            for (var i = 0; i < _hearing.Count; i++)
                _hearing[i]();
        }

        /// <summary>Whether a step asking this block for <paramref name="action" /> would be lost: it is waited for, and the block is switched off.</summary>
        /// <remarks>
        /// Only the switch, because a switch just thrown, by a step before this one, shows at once, where whether a block
        /// is powered may not until the game next works it out. Anything a step does not wait for a block takes as ever,
        /// and switching it on is one of those.
        /// </remarks>
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
        public Order Prepare(EntityAction action, Bound bound, IList<Literal> arguments)
        {
            if (action.Being != null)
                return arguments == null || arguments.Count == 0 ? null : action.Being.Order(_block, arguments[0]);

            if (action.Form != Nature.Native)
                return Toolbars.Prepare(_block, action.Id, action.Form, arguments, action.Primitives);

            if (bound != null && bound.Acts || action.GameActionId == null)
                return null;

            return Toolbars.Prepare(_block, action.GameActionId, Nature.Action, arguments, action.Primitives);
        }

        /// <summary>Tells the block to do <paramref name="actionId" />.</summary>
        public void Invoke(string actionId)
        {
            Invoke(actionId, null);
        }

        /// <summary>Tells the block to do <paramref name="actionId" /> with <paramref name="arguments" />.</summary>
        public void Invoke(string actionId, IList<Literal> arguments)
        {
            Invoke(actionId, arguments, Nature.Native);
        }

        /// <summary>The same, told which of the three ways the step reaches the block.</summary>
        /// <remarks>
        /// <b>For the editor and the harness, never the runtime,</b> which compiles what a step does once and does that.
        /// This finds it by name and works it out on every call.
        /// <para>
        /// Each is tried as what it is and nothing else. A value and the game's act that flips it share
        /// an id, and one of Sprocket's own words may be spelled like one of the game's, so a step
        /// that went looking for whatever answered to its id could find the wrong one.
        /// </para>
        /// </remarks>
        public void Invoke(string actionId, IList<Literal> arguments, Nature form)
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
        public Trouble Trouble(string actionId, IList<Literal> arguments)
        {
            var bound = Bind(actionId, arguments);
            return bound?.Trouble(_block, new Recall()) ?? World.Trouble.None;
        }

        /// <summary>One of Sprocket's own actions as told <paramref name="arguments" />, or null where the block has no such action.</summary>
        Bound Bind(string actionId, IList<Literal> arguments)
        {
            var action = _kind.FindAction(actionId, Nature.Native);
            return action == null || action.Doing == null ? null : action.Doing.Bind(arguments ?? new Literal[0]);
        }

        void Perform(string actionId, IList<Literal> arguments, Nature form)
        {
            // The block's own members are named by the block's ids, so those go straight to the game.
            if (form == Nature.Value)
            {
                if (arguments != null && arguments.Count > 0)
                    Toolbars.Set(_block, actionId, arguments[0]);

                return;
            }

            if (form == Nature.Action)
            {
                Toolbars.Perform(_block, actionId, arguments);
                return;
            }

            // An id this block does not have is a sequence written against a vocabulary it no longer offers, and it does
            // nothing rather than throwing - the same answer a reading gives.
            var entry = _kind.FindAction(actionId, Nature.Native);
            if (entry == null)
                return;

            var bound = entry.Doing?.Bind(arguments ?? new Literal[0]);
            if (bound != null && bound.Acts)
                bound.Do(_block, new Recall());
            else if (entry.GameActionId != null)
                // One of Sprocket's own that is the game's act said another way.
                Toolbars.Perform(_block, entry.GameActionId, arguments);
        }

        /// <summary>A listener to this block, until disposed.</summary>
        sealed class Hearing : IDisposable
        {
            readonly GridEntity _entity;
            readonly Action _told;
            readonly List<IDisposable> _listening;

            public Hearing(GridEntity entity, Action told, List<IDisposable> listening)
            {
                _entity = entity;
                _told = told;
                _listening = listening;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _entity._hearing.Remove(_told);
                foreach (var listener in _listening)
                    listener.Dispose();
            }
        }
    }
}
