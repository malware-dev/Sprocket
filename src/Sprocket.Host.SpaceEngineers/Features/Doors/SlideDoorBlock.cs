using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Doors
{
    /// <summary>Sits on every sliding door; see <see cref="DoorBlock" />.</summary>
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_AirtightSlideDoor), useEntityUpdate: false)]
    public class SlideDoorBlock : DoorBlock
    {
        /// <inheritdoc />
        protected override void Controls(Texts texts)
        {
            DoorControls.DoOnce<IMyAirtightSlideDoor>(texts);
        }
    }
}
