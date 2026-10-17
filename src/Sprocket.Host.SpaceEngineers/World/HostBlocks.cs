using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>The blocks a construct's automation is kept in, listened to for whether any of them is working.</summary>
    public sealed class HostBlocks
    {
        readonly GridWorld _world;
        readonly List<IMyTerminalBlock> _holders = new List<IMyTerminalBlock>();
        readonly List<IMyTerminalBlock> _was = new List<IMyTerminalBlock>();
        readonly List<IMyTerminalBlock> _parted = new List<IMyTerminalBlock>();

        int _heldAt = int.MinValue;

        /// <summary>The blocks the automation is kept in, of <paramref name="world" />.</summary>
        public HostBlocks(GridWorld world)
        {
            _world = world;
        }

        /// <summary>Raised when whether any of the blocks is working changes.</summary>
        public event Action Changed;

        /// <summary>Raised when programmable blocks leave or join the construct.</summary>
        public event Action Gathered;

        /// <summary>Whether any programmable block on the construct is working.</summary>
        public bool Working { get; private set; }

        /// <summary>The construct's programmable blocks, the hero among them.</summary>
        public IReadOnlyList<IMyTerminalBlock> All => _holders;

        /// <summary>The programmable blocks that just left the construct and still stand.</summary>
        public IReadOnlyList<IMyTerminalBlock> Parted => _parted;

        /// <summary>Listens to every programmable block on the construct and <paramref name="hero" /> for their working state, gathering them again only when the construct was read again.</summary>
        /// <remarks>Called on every tick: one comparison on a tick where nothing on the construct changed.</remarks>
        public void Hold(IMyTerminalBlock hero)
        {
            var revision = _world.Revision;
            if (revision == _heldAt)
                return;

            _heldAt = revision;
            foreach (var holder in _holders)
                holder.IsWorkingChanged -= Moved;

            _was.Clear();
            _was.AddRange(_holders);
            _holders.Clear();
            var entities = _world.Entities;
            for (var i = 0; i < entities.Count; i++)
            {
                if (entities[i].Block is IMyProgrammableBlock)
                    _holders.Add(entities[i].Block);
            }

            if (!_holders.Contains(hero))
                _holders.Add(hero);

            foreach (var holder in _holders)
                holder.IsWorkingChanged += Moved;

            Working = AnyWorking();
            Regathered();
        }

        /// <summary>Gathers the blocks again now, with <paramref name="hero" /> among them, whatever the construct's revision.</summary>
        public void Regather(IMyTerminalBlock hero)
        {
            _heldAt = int.MinValue;
            Hold(hero);
        }

        /// <summary>Stops listening to the blocks.</summary>
        public void Release()
        {
            foreach (var holder in _holders)
                holder.IsWorkingChanged -= Moved;
        }

        /// <summary>A programmable block on the construct losing or regaining power, or being switched off or on.</summary>
        void Moved(IMyCubeBlock block)
        {
            Working = AnyWorking();
            if (Changed != null)
                Changed();
        }

        void Regathered()
        {
            _parted.Clear();
            foreach (var was in _was)
            {
                if (!_holders.Contains(was) && !was.MarkedForClose && !was.Closed)
                    _parted.Add(was);
            }

            var arrived = false;
            foreach (var holder in _holders)
                arrived |= !_was.Contains(holder);

            if ((_parted.Count > 0 || arrived) && Gathered != null)
                Gathered();
        }

        /// <summary>Whether any programmable block on the construct is working.</summary>
        bool AnyWorking()
        {
            foreach (var holder in _holders)
            {
                if (holder.IsWorking)
                    return true;
            }

            return false;
        }
    }
}
