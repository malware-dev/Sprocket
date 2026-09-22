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

        /// <summary>
        /// The same, with the values the action declared it wanted.
        /// </summary>
        /// <remarks>
        /// Nothing of Sprocket's own takes any, so values only ever reach the game's own actions -
        /// which is also the only place anything knows what to turn them into.
        /// </remarks>
        /// <summary>How many ticks this block should take to finish what the action started, or <see cref="Unestimated" />.</summary>
        public int Expected(string actionId)
        {
            return _extension == null ? Unestimated : _extension.Expected(_block, actionId);
        }

        /// <summary>
        /// Why this block cannot reach <paramref name="stateId" />, or null while it might.
        /// </summary>
        public Trouble Trouble(string stateId)
        {
            return _extension == null ? World.Trouble.None : _extension.TroubleWith(_block, stateId);
        }

        public void Invoke(string actionId, IList<Literal> values)
        {
            Invoke(actionId, values, false);
        }

        /// <summary>The same, told whether the step sets one of the block's values.</summary>
        /// <remarks>
        /// A value is written as a value and nothing else is tried. The id it shares with the game's
        /// bare toggle would otherwise find that toggle first, which takes no values, so the value
        /// would be dropped and the block flipped instead.
        /// </remarks>
        public void Invoke(string actionId, IList<Literal> values, bool sets)
        {
            if (sets)
            {
                if (values != null && values.Count > 0)
                    Toolbars.Set(_block, actionId, values[0]);

                return;
            }

            if (_extension != null && _extension.TryInvoke(_block, actionId))
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

            // Anything the block declares for itself, performed by the game. An id it does not have
            // is a sequence written against a vocabulary this block no longer offers, and it does
            // nothing rather than throwing - the same answer a reading gives.
            Toolbars.Perform(_block, actionId, values);
        }
    }
}
