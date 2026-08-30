using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;
using Reactor.Host.SpaceEngineers.Ui;
using Reactor.Host.SpaceEngineers.World;

namespace Reactor.Host.SpaceEngineers
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
    /// The second thing is why a Reactor keeps running after a reload: a block carrying a program adopts
    /// itself into the session on load, so an airlock works without anybody opening the editor to wake it.
    /// A block with no program costs one dictionary lookup and is then left alone.
    /// </remarks>
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MyProgrammableBlock), useEntityUpdate: false)]
    public class ReactorBlock : MyGameLogicComponent
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
                ReactorControls.DoOnce();

            IMyTerminalBlock block = Entity as IMyTerminalBlock;
            if (block != null && ReactorUnit.HasProgram(block))
                ReactorSession.Adopt(block);
        }
    }
}
