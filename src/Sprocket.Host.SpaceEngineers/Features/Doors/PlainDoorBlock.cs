using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Doors
{
    /// <summary>Sits on every plain door; see <see cref="DoorBlock" />.</summary>
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Door), useEntityUpdate: false)]
    public class PlainDoorBlock : DoorBlock
    {
        /// <inheritdoc />
        protected override void Controls(Texts texts)
        {
            DoorControls.DoOnce<IMyDoor>(texts);
        }
    }
}
