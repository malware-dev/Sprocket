using Reactor.Host.SpaceEngineers.Core.World;
using Sandbox.ModAPI;

namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>One block, answering the small set of questions every block can answer.</summary>
    sealed class GridEntity : IEntity
    {
        internal const string OnProperty = "on";
        internal const string WorkingProperty = "working";

        readonly IMyTerminalBlock _block;
        readonly EntityKind _kind;
        readonly IBlockExtension _extension;

        public GridEntity(IMyTerminalBlock block, EntityKind kind, IBlockExtension extension)
        {
            _block = block;
            _kind = kind;
            _extension = extension;
        }

        /// <summary>The block behind this, for the few things that need the game's own object.</summary>
        /// <remarks>
        /// Reactor works through <see cref="IEntity" /> everywhere else. Writing a toolbar is the
        /// exception: it is the game's own structure and there is nothing to abstract about it.
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
            if (_extension != null && _extension.TryInvoke(_block, actionId))
                return;

            IMyFunctionalBlock functional = _block as IMyFunctionalBlock;
            if (functional == null)
                return;

            if (actionId == BlockKinds.SwitchOn)
                functional.Enabled = true;
            else if (actionId == BlockKinds.SwitchOff)
                functional.Enabled = false;
        }
    }
}
