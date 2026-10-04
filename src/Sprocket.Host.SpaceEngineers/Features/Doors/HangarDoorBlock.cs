using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Doors
{
    /// <summary>Sits on every airtight hangar door; see <see cref="DoorBlock" />.</summary>
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_AirtightHangarDoor), useEntityUpdate: false)]
    public class HangarDoorBlock : DoorBlock
    {
        /// <inheritdoc />
        protected override void Controls(Texts texts)
        {
            DoorControls.DoOnce<IMyAirtightHangarDoor>(texts);
        }
    }
}
