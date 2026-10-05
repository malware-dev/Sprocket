using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Sequences;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>A button panel's triggers: one for each button, which runs its own slot when it is pressed.</summary>
    /// <remarks>
    /// The game runs the slot at the button's own place in the toolbar, across pages where a panel has more buttons than a
    /// page has slots.
    /// </remarks>
    public sealed class ButtonPanelTriggers : ITriggers
    {
        /// <inheritdoc />
        public bool Claims(IMyTerminalBlock block)
        {
            return block is IMyButtonPanel;
        }

        /// <summary>"button 2 is pressed": every button by its number, on a panel with one button as on a panel with several.</summary>
        /// <remarks>The same trigger reads the same whatever panel it is on, so installations filled from different panels agree.</remarks>
        public void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            IMyToolbar toolbar = Toolbars.Of(block, 1);
            if (toolbar == null || toolbar.SlotCount < 1)
                return;

            // The game gives a panel one page more than its buttons fill, so every page but the last is buttons.
            var buttons = toolbar.PageCount <= 1 ? toolbar.SlotCount : toolbar.SlotCount * (toolbar.PageCount - 1);
            for (var button = 0; button < buttons; button++)
            {
                var at = new ToolbarSlot(1, button / toolbar.SlotCount + 1, button % toolbar.SlotCount + 1);
                kind.WithTrigger(at, "button " + (button + 1) + " is pressed");
            }
        }
    }
}
