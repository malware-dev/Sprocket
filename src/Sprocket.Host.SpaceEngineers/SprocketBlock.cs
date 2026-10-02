using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Terminal;


namespace Sprocket.Host.SpaceEngineers
{
    /// <summary>
    /// Sits on every programmable block, and does two things once the block is properly alive.
    /// </summary>
    /// <remarks>
    /// The terminal button and the trigger action are registered from here rather than from the session,
    /// because a control or an action added before the block type has fully spawned makes the vanilla
    /// controls vanish - Name, On/Off, Custom Data - both from the panel and from the list other mods are
    /// handed: the first addition creates the type's entry, and the game then takes the type as already
    /// built. A game logic component's first update is the earliest point that is safe, and <c>Init</c> is
    /// not late enough. See THDigi's terminal control examples.
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

            var texts = SprocketSession.Texts;
            TriggerAction.Register(texts);

            // The button belongs to a terminal, and a dedicated server has none.
            if (!MyAPIGateway.Utilities.IsDedicated)
                SprocketControls.DoOnce(texts, SprocketSession.DeveloperMode);

            IMyTerminalBlock block = Entity as IMyTerminalBlock;
            if (block != null && SprocketUnit.HasProgram(block))
                SprocketSession.Adopt(block);
        }
    }
}
