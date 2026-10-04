using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Sprocket.Host.SpaceEngineers.Features.Gravity
{
    /// <summary>Sits on every box gravity generator, and adds the field's fitting buttons once the first one is properly alive.</summary>
    /// <remarks>
    /// A control added before the block type has fully spawned makes the type's own controls vanish, for the reason given on
    /// <see cref="SprocketBlock" />; a game logic component's first update is the earliest point that is safe.
    /// </remarks>
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_GravityGenerator), useEntityUpdate: false)]
    public class GravityBlock : MyGameLogicComponent
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            // The buttons belong to a terminal, and a dedicated server has none.
            if (MyAPIGateway.Utilities == null || MyAPIGateway.Utilities.IsDedicated)
                return;

            var texts = SprocketSession.Texts;
            if (texts != null)
                FieldControls.DoOnce(texts);
        }
    }
}
