using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Sprocket.Host.SpaceEngineers.Features.Gravity
{
    /// <summary>Sits on every box gravity generator, and adds the field's fitting buttons once the first one is properly alive.</summary>
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_GravityGenerator), useEntityUpdate: false)]
    public class GravityBlock : MyGameLogicComponent
    {
        /// <inheritdoc />
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        /// <inheritdoc />
        public override void UpdateOnceBeforeFrame()
        {
            // A dedicated server has no terminal to hold the buttons.
            if (MyAPIGateway.Utilities == null || MyAPIGateway.Utilities.IsDedicated)
                return;

            var texts = SprocketSession.Texts;
            if (texts != null)
                GravityFieldButtons.DoOnce(texts);
        }
    }
}
