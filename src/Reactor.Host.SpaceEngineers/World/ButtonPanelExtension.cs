using System.Collections.Generic;
using Reactor.World;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;

// The statuses live in the Ingame namespaces, which cannot be imported whole: IMyDoor and friends
// are declared in both, and the mod-side ones are the ones with the events on them.
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;
using VentStatus = SpaceEngineers.Game.ModAPI.Ingame.VentStatus;
using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Lists;
using Reactor.Host.SpaceEngineers.Presentation.Look;
using Reactor.Host.SpaceEngineers.Presentation.Picking;
using Reactor.Host.SpaceEngineers.Presentation.Sequences;
using Reactor.Host.SpaceEngineers.Terminal;

namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>
    /// A button panel, which is the one block here Reactor cannot read at all and has to be told about.
    /// </summary>
    /// <remarks>
    /// There is no "was pressed" to sample - a press is a moment the game reports and then forgets - so the
    /// listener catches it and holds it until a tick has seen it.
    ///
    /// <b>The button's toolbar slot has to have something in it.</b> <c>MyButtonPanel.ActivateButton</c>
    /// returns before raising anything when the slot is empty, and no mod can fill a slot. Reactor can at
    /// least see the problem: <c>IsButtonAssigned</c> says which buttons are wired, so an unassigned button
    /// can be explained rather than silently doing nothing.
    /// </remarks>
    sealed class ButtonPanelExtension : IBlockExtension
    {
        public const string Pressed = "pressed";

        public bool Claims(IMyTerminalBlock block)
        {
            return block is IMyButtonPanel;
        }

        public void Describe(EntityKind kind)
        {
            kind.WithProperty(Pressed, "pressed")
                .WithEvent("pressed", Pressed, Edge.Rising, "is pressed");
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
        /// The one place Reactor can warn somebody before they build a sequence on a button that will
        /// never fire. A panel's buttons only raise anything when their toolbar slot holds an item, and no
        /// mod can fill a slot.
        /// </remarks>
        public string StatusOf(IMyTerminalBlock block)
        {
            var panel = block as IMyButtonPanel;
            if (panel == null)
                return null;

            for (int i = 0; i < 4; i++)
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

        public IBlockListener Listen(IMyTerminalBlock block)
        {
            var panel = block as IMyButtonPanel;
            return panel == null ? null : new ButtonListener(panel);
        }

        /// <summary>Holds a press until a tick has read it.</summary>
        sealed class ButtonListener : IBlockListener
        {
            readonly IMyButtonPanel _panel;
            bool _pressed;

            public ButtonListener(IMyButtonPanel panel)
            {
                _panel = panel;
                _panel.ButtonPressed += OnPressed;
            }

            /// <summary>
            /// Any button, rather than one property per button.
            /// </summary>
            /// <remarks>
            /// "When the airlock button is pressed" is the sentence people write, and a panel by a door is
            /// almost always doing one job. Which button it was is carried by the event and can be offered
            /// later without changing what is already written.
            /// </remarks>
            void OnPressed(int index)
            {
                _pressed = true;
            }

            public bool TryRead(string propertyId, out EntityValue value)
            {
                value = default(EntityValue);
                if (propertyId != Pressed)
                    return false;

                value = EntityValue.Of(_pressed);
                return true;
            }

            public void Clear()
            {
                _pressed = false;
            }

            public void Dispose()
            {
                _panel.ButtonPressed -= OnPressed;
            }
        }
    }
}
