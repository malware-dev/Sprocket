using System;
using Sandbox.ModAPI;
using Mal.MdkModMixin.Network;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Sprocket.DoorAutoClose
{
    /// <summary>Sits on every door, adds its family's auto-close slider once the first one is properly alive, and on the server closes it.</summary>
    public abstract class DoorBlock : MyGameLogicComponent
    {
        IMyDoor _door;

        /// <summary>What closes this door, on the server once the door is alive; null elsewhere.</summary>
        public DoorCloser Closer { get; private set; }

        /// <inheritdoc />
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        /// <inheritdoc />
        public override void UpdateOnceBeforeFrame()
        {
            if (MyAPIGateway.Utilities == null)
                return;

            // A dedicated server has no terminal to hold the slider.
            if (!MyAPIGateway.Utilities.IsDedicated)
                Controls();

            _door = Entity as IMyDoor;
            if (_door == null || !NetworkChannel.Network.IsServer)
                return;

            Closer = new DoorCloser(_door, Now, Changed);
            var left = CloseDue.Take(_door);
            Closer.Start(left < 0 ? -1 : Now() + left);
        }

        /// <summary>Closes the door once its close has come.</summary>
        /// <remarks>Runs every tenth frame, and only while a close is coming.</remarks>
        public override void UpdateBeforeSimulation10()
        {
            Closer?.Check();
        }

        /// <summary>Keeps what is left of a coming close in the door, as the game writes it into a save.</summary>
        public override bool IsSerialized()
        {
            if (Closer != null)
                CloseDue.Store(_door, Closer.Waiting ? Math.Max(0, Closer.ClosesAt - Now()) : -1);

            return base.IsSerialized();
        }

        /// <inheritdoc />
        public override void Close()
        {
            Closer?.Dispose();
            Closer = null;
            _door = null;
        }

        /// <summary>Makes the auto-close slider for this family of door, once.</summary>
        protected abstract void Controls();

        /// <summary>The frame the game is on.</summary>
        static int Now()
        {
            return MyAPIGateway.Session == null ? 0 : MyAPIGateway.Session.GameplayFrameCounter;
        }

        void Changed()
        {
            NeedsUpdate = Closer != null && Closer.Waiting ? MyEntityUpdateEnum.EACH_10TH_FRAME : MyEntityUpdateEnum.NONE;
        }
    }
}
