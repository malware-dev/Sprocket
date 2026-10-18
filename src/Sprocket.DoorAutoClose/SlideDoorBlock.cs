using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Sprocket.DoorAutoClose
{
    /// <summary>Sits on every sliding door; see <see cref="DoorBlock" />.</summary>
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_AirtightSlideDoor), useEntityUpdate: true)]
    public class SlideDoorBlock : DoorBlock
    {
        /// <inheritdoc />
        protected override void Controls()
        {
            DoorControls.DoOnce<IMyAirtightSlideDoor>();
        }
    }
}
