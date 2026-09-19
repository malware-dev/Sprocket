using VRage.Game.ModAPI;
using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Terminal;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;

// The statuses live in the Ingame namespaces, which cannot be imported whole: IMyDoor and friends
// are declared in both, and the mod-side ones are the ones with the events on them.
// The Ingame one, which is where action arguments live.
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// A button panel, which is the one block here Sprocket cannot read at all and has to be told about.
    /// </summary>
    /// <remarks>
    /// There is no "was pressed" to sample - a press is a moment the game reports and then forgets - so the
    /// listener catches it and holds it until a tick has seen it.
    ///
    /// <b>The button's toolbar slot has to have something in it.</b> <c>MyButtonPanel.ActivateButton</c>
    /// returns before raising anything when the slot is empty, and no mod can fill a slot. Sprocket can at
    /// least see the problem: <c>IsButtonAssigned</c> says which buttons are wired, so an unassigned button
    /// can be explained rather than silently doing nothing.
    /// </remarks>
    sealed class ButtonPanelExtension : IBlockExtension
    {
        /// <summary>
        /// One property per button, named by its number from one.
        /// </summary>
        /// <remarks>
        /// A property each rather than one for the panel, because "when the airlock button is
        /// pressed" is only the right sentence on a panel with one button. On a panel with four it
        /// has to say which, and the picker offers whatever a kind declares - so declaring four
        /// events is the whole of making them selectable.
        /// </remarks>
        public static string Pressed(int button)
        {
            return "pressed-" + button;
        }

        /// <summary>
        /// Which button an event names, counting from zero, or -1 for anything else.
        /// </summary>
        /// <remarks>
        /// The only place that knows how a button event is spelled. A toolbar slot has to be claimed
        /// on the button somebody chose, and the event id is where that choice is recorded.
        /// </remarks>
        public static int ButtonOf(string eventId)
        {
            if (string.IsNullOrEmpty(eventId) || !eventId.StartsWith("pressed-"))
                return -1;

            int number;
            return int.TryParse(eventId.Substring("pressed-".Length), out number) && number >= 1
                ? number - 1
                : -1;
        }

        /// <summary>How many buttons a panel has.</summary>
        /// <remarks>Its toolbar's slot count, which is the game's own answer to the same question.</remarks>
        static int Buttons(IMyTerminalBlock block)
        {
            IMyToolbar toolbar = Toolbars.Of(block);
            return toolbar == null ? 0 : toolbar.SlotCount;
        }

        public bool Claims(IMyTerminalBlock block)
        {
            return block is IMyButtonPanel;
        }

        /// <summary>Whether that button still starts that particular thing.</summary>
        public bool Raises(IMyTerminalBlock block, string eventId, string key)
        {
            var button = ButtonOf(eventId);
            if (button < 0)
                return true;

            IMyToolbar toolbar = Toolbars.Of(block);
            if (toolbar == null || button >= toolbar.SlotCount)
                return false;

            MyToolbarItemInfo held;
            if (!toolbar.TryGetItem(button, out held) || held.ActionId != TriggerAction.ActionId)
                return false;

            _read.Clear();
            Toolbars.Parameters(toolbar, button, _read);

            return _read.Count > 0
                && string.Equals(_read[0].Value as string, key, StringComparison.Ordinal);
        }

        /// <summary>Reused: asked once per wired button per second, so it must not allocate.</summary>
        readonly List<TerminalActionParameter> _read = new List<TerminalActionParameter>();

        public void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            var buttons = Buttons(block);

            for (var i = 1; i <= buttons; i++)
            {
                // "button 2 is pressed" on a panel with several, and plain "is pressed" on a panel
                // with one, where saying which would be saying something nobody needs to know.
                var words = buttons == 1 ? "is pressed" : "button " + i + " is pressed";
                kind.WithAnsweredEvent(Pressed(i), words);
            }
        }

        public bool TryRead(IMyTerminalBlock block, string propertyId, out EntityValue value)
        {
            // Answered by the listener, which is the only thing that knows a press happened.
            value = default(EntityValue);
            return false;
        }

        public bool TryInvoke(IMyTerminalBlock block, string actionId)
        {
            return false;
        }

        /// <summary>
        /// How many of its buttons are wired, because an unwired one cannot report a press.
        /// </summary>
        /// <remarks>
        /// The one place Sprocket can warn somebody before they build a sequence on a button that will
        /// never fire. A panel's buttons only raise anything when their toolbar slot holds an item, and no
        /// mod can fill a slot.
        /// </remarks>

        /// <summary>Nothing: nothing known about this block says why it would not finish.</summary>
        public Trouble TroubleWith(IMyTerminalBlock block, string stateId)
        {
            return Trouble.None;
        }
        public string StatusOf(IMyTerminalBlock block)
        {
            var panel = block as IMyButtonPanel;
            if (panel == null)
                return null;

            var buttons = Buttons(block);
            for (int i = 0; i < buttons; i++)
            {
                if (panel.IsButtonAssigned(i))
                    return null;
            }

            return "not wired";
        }

        public bool IsBusy(IMyTerminalBlock block)
        {
            return false;
        }

    }
}
