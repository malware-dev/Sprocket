using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Doors
{
    /// <summary>Sits on every door, adds its family's auto-close slider once the first one is properly alive, and tells auto-close the door is here.</summary>
    public abstract class DoorBlock : MyGameLogicComponent
    {
        IMyDoor _door;

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
            var texts = SprocketSession.Texts;
            if (!MyAPIGateway.Utilities.IsDedicated && texts != null)
                Controls(texts);

            _door = Entity as IMyDoor;
            var doors = SprocketSession.Doors;
            if (_door != null && doors != null)
                doors.Arrived(_door);
        }

        /// <inheritdoc />
        public override void Close()
        {
            var doors = SprocketSession.Doors;
            if (_door != null && doors != null)
                doors.Left(_door);

            _door = null;
        }

        /// <summary>Makes the auto-close slider for this family of door, once.</summary>
        protected abstract void Controls(Texts texts);
    }
}
