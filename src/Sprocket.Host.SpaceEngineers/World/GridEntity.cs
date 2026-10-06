using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>One block, answering the small set of questions every block can answer.</summary>
    public sealed class GridEntity
    {
        /// <summary>The reading of whether a block is switched on.</summary>
        public const string OnProperty = "on";

        /// <summary>The reading of whether a block is working, as the game judges it.</summary>
        public const string WorkingProperty = "working";

        /// <summary>What a block answers when it cannot say how long something will take.</summary>
        public const int Unestimated = -1;

        readonly IMyTerminalBlock _block;
        readonly EntityKind _kind;
        readonly IBlockExtension _extension;
        readonly List<Action> _hearing = new List<Action>();

        /// <summary><paramref name="block" />, of <paramref name="kind" />, answered for where it can be by <paramref name="extension" />.</summary>
        public GridEntity(IMyTerminalBlock block, EntityKind kind, IBlockExtension extension)
        {
            _block = block;
            _kind = kind;
            _extension = extension;
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
        /// Four answers rather than two, because "off" and "no power" are different problems and telling
        /// somebody the wrong one is worse than telling them nothing. Damage comes first because a damaged
        /// block is not working for a reason no switch will fix.
        /// </remarks>
        public Status Status
        {
            get
            {
                // What this block is doing, when something knows. "closing" beats "on" on a list of forty.
                if (_extension != null)
                {
                    var told = _extension.StatusOf(_block);
                    if (told.Exists)
                        return told;
                }

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
        /// something that knows the block can say it is in motion, so a block nobody has written an
        /// extension for is never busy.
        /// </remarks>
        public bool IsActive => _extension != null && _extension.IsBusy(_block);

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

        /// <summary>What the block reads for <paramref name="propertyId" />, or false where it has no such reading.</summary>
        public EntityValue GetValue(string propertyId)
        {
            EntityValue told;

            // What this sort of block can be asked, then the floor every block stands on.
            if (_extension != null && _extension.TryRead(_block, propertyId, out told))
                return told;

            if (propertyId == OnProperty)
            {
                IMyFunctionalBlock functional = _block as IMyFunctionalBlock;
                return EntityValue.Of(functional != null && functional.Enabled);
            }

            if (propertyId == WorkingProperty)
                return EntityValue.Of(_block.IsWorking);

            // An unknown name gives back a default rather than throwing: a saved sequence can outlive the
            // vocabulary it was written against, and that is a thing to surface calmly.
            return EntityValue.Of(false);
        }

        /// <summary>Whether the block's own value that <paramref name="value" /> writes is <paramref name="literal" />.</summary>
        public bool Holds(EntityAction value, Literal literal)
        {
            return Toolbars.Holds(_block, value.Id, literal);
        }

        /// <summary>Whether the block is in <paramref name="moment" />: the flag it reads is set where the moment rises, clear where it falls.</summary>
        public bool In(Values values, EntityEvent moment)
        {
            return Flag(values, moment.PropertyId) == (moment.Edge == Edge.Rising);
        }

        /// <summary>Whether there is anything in <paramref name="propertyId" />, read as the kind the block declares it, or as a flag.</summary>
        public bool Flag(Values values, string propertyId)
        {
            var property = Kind.FindProperty(propertyId);
            return values.Flag(property == null ? ValueKind.Flag : property.Kind, GetValue(propertyId));
        }

        /// <summary>Tells the block to do <paramref name="actionId" />.</summary>
        public void Invoke(string actionId)
        {
            Invoke(actionId, null);
        }

        /// <summary>Tells <paramref name="told" /> each time this block may have changed, until the answer is disposed; null where it says nothing.</summary>
        /// <remarks>
        /// Told as the block announces a change, as it is switched on or off, which every functional block announces, and as
        /// Sprocket itself asks the block for anything.
        /// </remarks>
        public IDisposable Listen(Action told)
        {
            var announces = _extension as IAnnounces;
            var listening = announces == null ? null : announces.Listen(_block, told);
            var functional = _block as IMyFunctionalBlock;
            if (listening == null && functional == null)
                return null;

            _hearing.Add(told);
            return new Hearing(this, told, listening, functional);
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

        /// <summary>Calls off what the action started, where this block's own knowledge says how.</summary>
        public void Stop(string actionId)
        {
            if (_extension != null)
                _extension.Stop(_block, actionId);
        }

        /// <summary>How many ticks this block should take to finish what the action started, or <see cref="Unestimated" />.</summary>
        public int Expected(string actionId, IList<Literal> values)
        {
            return _extension == null ? Unestimated : _extension.Expected(_block, actionId, values);
        }

        /// <summary>
        /// Why this block cannot reach <paramref name="stateId" />, or null while it might.
        /// </summary>
        public Trouble Trouble(string stateId)
        {
            return _extension == null ? World.Trouble.None : _extension.TroubleWith(_block, stateId);
        }

        /// <summary>
        /// What this block is told by a step doing one of the game's actions or values, made ready, or
        /// null for one of Sprocket's own or one the block does not have.
        /// </summary>
        public Order Prepare(EntityAction action, IList<Literal> values)
        {
            if (action.Form == Form.Native)
                return null;

            return Toolbars.Prepare(_block, action.Id, action.Form, values, action.Primitives);
        }

        /// <summary>Tells the block to do <paramref name="actionId" /> with <paramref name="values" />.</summary>
        public void Invoke(string actionId, IList<Literal> values)
        {
            Invoke(actionId, values, Form.Native);
        }

        /// <summary>The same, told which of the three ways the step reaches the block.</summary>
        /// <remarks>
        /// Each is tried as what it is and nothing else. A value and the game's act that flips it share
        /// an id, and one of Sprocket's own words may be spelled like one of the game's, so a step
        /// that went looking for whatever answered to its id could find the wrong one.
        /// </remarks>
        public void Invoke(string actionId, IList<Literal> values, Form form)
        {
            Perform(actionId, values, form);

            // What Sprocket asks of a block is a change it need not wait for the block to announce.
            for (var i = 0; i < _hearing.Count; i++)
                _hearing[i]();
        }

        void Perform(string actionId, IList<Literal> values, Form form)
        {
            // The block's own members are named by the block's ids, so those go straight to the game.
            if (form == Form.Value)
            {
                if (values != null && values.Count > 0)
                    Toolbars.Set(_block, actionId, values[0]);

                return;
            }

            if (form == Form.Action)
            {
                Toolbars.Perform(_block, actionId, values);
                return;
            }

            if (_extension != null && _extension.TryInvoke(_block, actionId, values))
                return;

            IMyFunctionalBlock functional = _block as IMyFunctionalBlock;
            if (functional != null)
            {
                if (actionId == BlockKinds.SwitchOn)
                {
                    functional.Enabled = true;
                    return;
                }

                if (actionId == BlockKinds.SwitchOff)
                {
                    functional.Enabled = false;
                    return;
                }
            }

            // One of Sprocket's own that is the game's act said another way. An id this block does not
            // have is a sequence written against a vocabulary it no longer offers, and it does nothing
            // rather than throwing - the same answer a reading gives.
            var entry = _kind.FindAction(actionId, Form.Native);
            if (entry != null && entry.GameActionId != null)
                Toolbars.Perform(_block, entry.GameActionId, values);
        }

        /// <summary>A listener to this block, until disposed.</summary>
        sealed class Hearing : IDisposable
        {
            readonly GridEntity _entity;
            readonly Action _told;
            readonly IDisposable _listening;
            readonly IMyFunctionalBlock _functional;

            public Hearing(GridEntity entity, Action told, IDisposable listening, IMyFunctionalBlock functional)
            {
                _entity = entity;
                _told = told;
                _listening = listening;
                _functional = functional;
                if (_functional != null)
                    _functional.EnabledChanged += Switched;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _entity._hearing.Remove(_told);
                if (_listening != null)
                    _listening.Dispose();

                if (_functional != null)
                    _functional.EnabledChanged -= Switched;
            }

            void Switched(IMyTerminalBlock block)
            {
                _told();
            }
        }
    }
}
