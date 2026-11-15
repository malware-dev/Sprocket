using Sprocket.Host.SpaceEngineers.Features.Terminal;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Sprocket.Host.SpaceEngineers
{
    /// <summary>
    /// The component on every programmable block that registers Sprocket's terminal controls and adopts a block carrying a program.
    /// </summary>
    /// <remarks>
    /// A block with no program costs one lookup on its first frame.
    /// </remarks>
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MyProgrammableBlock), useEntityUpdate: false)]
    public class SprocketBlock : MyGameLogicComponent
    {
        /// <inheritdoc />
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        /// <inheritdoc />
        public override void UpdateOnceBeforeFrame()
        {
            if (MyAPIGateway.Utilities == null || SprocketSession.StandsDown)
                return;

            // A control added before the block type has fully spawned makes the vanilla controls vanish; the first update is
            // the earliest safe point, Init is too early (THDigi's terminal control examples).
            var texts = SprocketSession.Texts;

            // The button belongs to a terminal, and a dedicated server has none.
            if (!MyAPIGateway.Utilities.IsDedicated)
                SprocketControls.DoOnce(texts, SprocketSession.DeveloperMode);

            IMyTerminalBlock block = Entity as IMyTerminalBlock;
            if (block != null && BlockStorage.HasProgram(block))
                SprocketSession.Adopt(block);
        }
    }
}
