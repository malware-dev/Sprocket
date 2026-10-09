using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// A button panel: one signal for each button, which runs its own slot when it is pressed.
    /// </summary>
    /// <remarks>
    /// There is no "was pressed" to sample - a press is a moment the game reports and then forgets - so the
    /// slot runs Sprocket's own action, which is how the press arrives. The game runs the slot at the button's
    /// own place in the toolbar, across pages where a panel has more buttons than a page has slots.
    /// <para>
    /// <b>The button's toolbar slot has to have something in it.</b> <c>MyButtonPanel.ActivateButton</c>
    /// returns before raising anything when the slot is empty, and no mod can fill a slot. Sprocket can at
    /// least see the problem: <c>IsButtonAssigned</c> says which buttons are wired, so an unassigned button
    /// can be explained rather than silently doing nothing.
    /// </para>
    /// </remarks>
    public sealed class ButtonPanel : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyButtonPanel;
        }

        /// <summary>"button 2 is pressed": every button by its number, on a panel with one button as on a panel with several.</summary>
        /// <remarks>
        /// The same signal reads the same whatever panel it is on, so installations filled from different panels agree.
        /// What is written on the panel for somebody standing in front of it to read is a label on the machine rather than
        /// anything the machine does, so it is kept out.
        /// </remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Hiding("ButtonName");

            IMyToolbar toolbar = Toolbars.Of(block, 1);
            if (toolbar == null || toolbar.SlotCount < 1)
                return;

            // The game gives a panel one page more than its buttons fill, so every page but the last is buttons.
            var buttons = toolbar.PageCount <= 1 ? toolbar.SlotCount : toolbar.SlotCount * (toolbar.PageCount - 1);
            for (var button = 0; button < buttons; button++)
            {
                var at = new ToolbarSlot(1, button / toolbar.SlotCount + 1, button % toolbar.SlotCount + 1);
                kind.WithSignal(at, "button " + (button + 1) + " is pressed");
            }
        }

        /// <summary>Not wired where none of its buttons is, because an unwired button cannot report a press.</summary>
        /// <remarks>
        /// The one place Sprocket can warn somebody before they build a sequence on a button that will never fire. A
        /// panel's buttons only raise anything when their toolbar slot holds an item, and no mod can fill a slot.
        /// </remarks>
        public override Status StatusOf(IMyTerminalBlock block)
        {
            var panel = block as IMyButtonPanel;
            if (panel == null)
                return Status.None;

            var buttons = Buttons(block);
            for (int i = 0; i < buttons; i++)
            {
                if (panel.IsButtonAssigned(i))
                    return Status.None;
            }

            return Status.Of(Condition.NotWired);
        }

        /// <summary>How many buttons a panel has.</summary>
        /// <remarks>Its toolbar's slot count, which is the game's own answer to the same question.</remarks>
        static int Buttons(IMyTerminalBlock block)
        {
            IMyToolbar toolbar = Toolbars.Of(block);
            return toolbar == null ? 0 : toolbar.SlotCount;
        }
    }
}
