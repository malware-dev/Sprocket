using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Sprocket.DoorAutoClose
{
    /// <summary>Sits on every airtight hangar door; see <see cref="DoorBlock" />.</summary>
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_AirtightHangarDoor), useEntityUpdate: true)]
    public class HangarDoorBlock : DoorBlock
    {
        /// <inheritdoc />
        protected override void Controls()
        {
            DoorControls.DoOnce<IMyAirtightHangarDoor>();
        }
    }
}
