using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Doors
{
    /// <summary>Sits on every advanced door; see <see cref="DoorBlock" />.</summary>
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_AdvancedDoor), useEntityUpdate: false)]
    public class AdvancedDoorBlock : DoorBlock
    {
        /// <inheritdoc />
        protected override void Controls(Texts texts)
        {
            DoorControls.DoOnce<IMyAdvancedDoor>(texts);
        }
    }
}
