using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>One block, answering the small set of questions every block can answer.</summary>
    public sealed class GridEntity
    {
        internal const string OnProperty = "on";
        internal const string WorkingProperty = "working";

        /// <summary>What a block answers when it cannot say how long something will take.</summary>
        public const int Unestimated = -1;

        readonly IMyTerminalBlock _block;
        readonly EntityKind _kind;
        readonly IBlockExtension _extension;

        internal GridEntity(IMyTerminalBlock block, EntityKind kind, IBlockExtension extension)
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

        public long Id => _block.EntityId;

        public string Name => _block.CustomName;

        public EntityKind Kind => _kind;

        /// <summary>
        /// Live state in the player's own words.
        /// </summary>
        /// <remarks>
        /// Four answers rather than two, because "off" and "no power" are different problems and telling
        /// somebody the wrong one is worse than telling them nothing. Damage comes first because a damaged
        /// block is not working for a reason no switch will fix.
        /// </remarks>
        public string Status
        {
            get
            {
                // What this block is doing, when something knows. "closing" beats "on" on a list of forty.
                if (_extension != null)
                {
                    string told = _extension.StatusOf(_block);
                    if (!string.IsNullOrEmpty(told))
                        return told;
                }

                if (!_block.IsFunctional)
                    return "damaged";

                IMyFunctionalBlock functional = _block as IMyFunctionalBlock;
                if (functional != null && !functional.Enabled)
                    return "off";

                return _block.IsWorking ? "on" : "no power";
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

        public void Invoke(string actionId)
        {
            Invoke(actionId, null);
        }

        /// <summary>Tells <paramref name="told" /> each time this block may have changed, until the answer is disposed; null where it says nothing.</summary>
        public IDisposable Listen(Action told)
        {
            var announces = _extension as IAnnounces;
            return announces == null ? null : announces.Listen(_block, told);
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

        /// <summary>Why this block cannot do <paramref name="action" />, naming it, or null where it can.</summary>
        /// <remarks>
        /// A block that is switched off, or not working, gets nowhere, so nothing a step waits for can be asked of it;
        /// anything else it takes as ever, and switching it on is one of those. The switch is read before whether it works,
        /// because a switch just thrown, by a step before this one, shows at once.
        /// </remarks>
        public string Unable(EntityAction action)
        {
            if (!action.Lasts)
                return null;

            if (IsSwitchedOff)
                return Name + " is switched off, so it can't " + action.Text + ".";

            return IsWorking ? null : Name + " isn't working, so it can't " + action.Text + ".";
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
    }
}
