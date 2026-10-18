using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Sprocket.DoorAutoClose
{
    /// <summary>Sits on every advanced door; see <see cref="DoorBlock" />.</summary>
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_AdvancedDoor), useEntityUpdate: true)]
    public class AdvancedDoorBlock : DoorBlock
    {
        /// <inheritdoc />
        protected override void Controls()
        {
            DoorControls.DoOnce<IMyAdvancedDoor>();
        }
    }
}
