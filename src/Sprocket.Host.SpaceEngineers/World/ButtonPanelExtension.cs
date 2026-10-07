using VRage.Game.ModAPI;
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Terminal;
using Sprocket.Host.SpaceEngineers.Sequences;

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
    public sealed class ButtonPanelExtension : IBlockExtension
    {
        /// <summary>How many buttons a panel has.</summary>
        /// <remarks>Its toolbar's slot count, which is the game's own answer to the same question.</remarks>
        static int Buttons(IMyTerminalBlock block)
        {
            IMyToolbar toolbar = Toolbars.Of(block);
            return toolbar == null ? 0 : toolbar.SlotCount;
        }

        /// <inheritdoc />
        public bool Claims(IMyTerminalBlock block)
        {
            return block is IMyButtonPanel;
        }

        /// <summary>Whether that button still starts that particular thing.</summary>
        public bool Raises(IMyTerminalBlock block, string eventId, string key)
        {
            var at = ToolbarSlot.Of(eventId);
            if (at == null)
                return true;

            IMyToolbar toolbar = Toolbars.Of(block, at.Toolbar);
            var button = Toolbars.Index(toolbar, at);
            if (button < 0)
                return false;

            MyToolbarItemInfo held;
            if (!toolbar.TryGetItem(button, out held) || held.ActionId != SignalAction.ActionId)
                return false;

            _read.Clear();
            Toolbars.Parameters(toolbar, button, _read);

            return _read.Count > 0
                && string.Equals(_read[0].Value as string, key, StringComparison.Ordinal);
        }

        readonly List<TerminalActionParameter> _read = new List<TerminalActionParameter>();

        /// <inheritdoc />
        public void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            // What is written on the panel for somebody standing in front of it to read, which is a
            // label on the machine rather than anything the machine does.
            kind.Hiding("ButtonName");
        }

        /// <inheritdoc />
        public bool TryRead(IMyTerminalBlock block, string propertyId, out EntityValue value)
        {
            // Answered by the listener, which is the only thing that knows a press happened.
            value = default(EntityValue);
            return false;
        }

        /// <inheritdoc />
        public bool TryInvoke(IMyTerminalBlock block, string actionId, IList<Literal> values)
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
        /// <inheritdoc />
        public Status StatusOf(IMyTerminalBlock block)
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

        /// <inheritdoc />
        public bool IsBusy(IMyTerminalBlock block)
        {
            return false;
        }


        /// <summary>Nothing: a button starts things and holds nothing up, so there is nothing to call off.</summary>
        public void Stop(IMyTerminalBlock block, string actionId)
        {
        }

        /// <inheritdoc />
        public int Expected(IMyTerminalBlock block, string actionId, IList<Literal> values)
        {
            return GridEntity.Unestimated;
        }
    }
}
