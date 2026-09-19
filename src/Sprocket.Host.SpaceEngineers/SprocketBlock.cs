using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using Sprocket.Host.SpaceEngineers.Terminal;
using Sprocket.Host.SpaceEngineers.World;


namespace Sprocket.Host.SpaceEngineers
{
    /// <summary>
    /// Sits on every programmable block, and does two things once the block is properly alive.
    /// </summary>
    /// <remarks>
    /// The terminal button is registered from here rather than from the session, because a control added
    /// before the block type has fully spawned makes the vanilla controls vanish - Name, On/Off, Show On
    /// HUD - both from the panel and from the list other mods are handed. A game logic component's first
    /// update is the earliest point that is safe, and <c>Init</c> is not late enough.
    ///
    /// The second thing is why a Sprocket keeps running after a reload: a block carrying a program adopts
    /// itself into the session on load, so an airlock works without anybody opening the editor to wake it.
    /// A block with no program costs one dictionary lookup and is then left alone.
    /// </remarks>
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MyProgrammableBlock), useEntityUpdate: false)]
    public class SprocketBlock : MyGameLogicComponent
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (MyAPIGateway.Utilities == null)
                return;

            // The button belongs to a terminal, and a dedicated server has none.
            if (!MyAPIGateway.Utilities.IsDedicated)
                SprocketControls.DoOnce();

            IMyTerminalBlock block = Entity as IMyTerminalBlock;
            if (block != null && SprocketUnit.HasProgram(block))
                SprocketSession.Adopt(block);
        }
    }
}
