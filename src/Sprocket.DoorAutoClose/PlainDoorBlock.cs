using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Sprocket.DoorAutoClose
{
    /// <summary>Sits on every plain door; see <see cref="DoorBlock" />.</summary>
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Door), useEntityUpdate: true)]
    public class PlainDoorBlock : DoorBlock
    {
        /// <inheritdoc />
        protected override void Controls()
        {
            DoorControls.DoOnce<IMyDoor>();
        }
    }
}
