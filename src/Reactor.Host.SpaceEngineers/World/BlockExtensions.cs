using System.Collections.Generic;
using Reactor.World;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;

// The statuses live in the Ingame namespaces, which cannot be imported whole: IMyDoor and friends
// are declared in both, and the mod-side ones are the ones with the events on them.
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;
using VentStatus = SpaceEngineers.Game.ModAPI.Ingame.VentStatus;

namespace Reactor.Host.SpaceEngineers
{
    /// <summary>
    /// What Reactor knows about one kind of block, over and above the floor every block stands on.
    /// </summary>
    /// <remarks>
    /// Keyed on the interface rather than on a block type id, which is what makes a modded door that
    /// implements <c>IMyDoor</c> inherit the whole door vocabulary without its author knowing Reactor
    /// exists. See docs/Catalogue.md.
    ///
    /// <b>The four-valued statuses are flattened to flags here.</b> A door is really Opening, Open, Closing
    /// or Closed, and saying so properly needs a reading that declares its own permitted values and a way to
    /// ask whether it is one of several - the first two needs in the catalogue. Until those exist, a flag
    /// per answer says the same things in the sentences an airlock actually uses.
    /// </remarks>
    interface IBlockExtension
    {
        bool Claims(IMyTerminalBlock block);

        void Describe(EntityKind kind);

        bool TryRead(IMyTerminalBlock block, string propertyId, out EntityValue value);

        bool TryInvoke(IMyTerminalBlock block, string actionId);

        /// <summary>
        /// This block's live state in the player's own words, or null to fall back to the floor.
        /// </summary>
        /// <remarks>
        /// "closing" beats "on" by a mile on a list of forty blocks, and it is the only thing a ship
        /// browser is for.
        /// </remarks>
        string StatusOf(IMyTerminalBlock block);

        /// <summary>
        /// Whether this block is doing something <em>right now</em>, which is not whether it works.
        /// </summary>
        /// <remarks>
        /// A row lights up on this, so answering it with "powered and switched on" lights every row on
        /// the ship and the light stops meaning anything. Only a thing in motion is busy.
        /// </remarks>
        bool IsBusy(IMyTerminalBlock block);

        /// <summary>
        /// Starts listening to a block, for anything the game will tell us rather than make us look.
        /// </summary>
        /// <remarks>
        /// Returns something to drop when the block goes, or null when there was nothing to hook. An event
        /// has to become an edge on a reading before core can use it, so what a listener does is hold a
        /// latch that the next tick reads and then clears.
        /// </remarks>
        IBlockListener Listen(IMyTerminalBlock block);
    }

    /// <summary>Holds whatever a block told us since the last tick.</summary>
    interface IBlockListener
    {
        bool TryRead(string propertyId, out EntityValue value);

        /// <summary>Forgets a moment, once a tick has had the chance to see it.</summary>
        void Clear();

        void Dispose();
    }

    static class BlockExtensions
    {
        static readonly IBlockExtension[] All =
        {
            new DoorExtension(),
            new AirVentExtension(),
            new ButtonPanelExtension()
        };

        public static IBlockExtension For(IMyTerminalBlock block)
        {
            for (var i = 0; i < All.Length; i++)
            {
                if (All[i].Claims(block))
                    return All[i];
            }

            return null;
        }
    }

    /// <summary>
    /// Doors, and everything shaped like one - sliding, airtight, hangar, and the advanced door.
    /// </summary>
    /// <remarks>
    /// The terminal has one <c>Open</c> control, which is the switch a player throws rather than where the
    /// door has got to. Reactor needs the difference: "close the inner door" is finished when the door is
    /// shut, not when the order was given.
    /// </remarks>
    sealed class DoorExtension : IBlockExtension
    {
        public const string Open = "open";
        public const string Shut = "shut";
        public const string Moving = "moving";

        public bool Claims(IMyTerminalBlock block)
        {
            return block is IMyDoor;
        }

        public void Describe(EntityKind kind)
        {
            kind.WithProperty(Open, "open")
                .WithProperty(Shut, "shut")
                .WithProperty(Moving, "moving")
                .WithEvent("opened", Open, Edge.Rising, "finishes opening")
                .WithEvent("closed", Shut, Edge.Rising, "finishes closing")
                .WithState("is-open", Open, true, "is open")
                .WithState("is-shut", Shut, true, "is shut")
                .WithState("is-moving", Moving, true, "is moving")
                .WithAction("open", "open", "is-open")
                .WithAction("close", "close", "is-shut");
        }

        public bool TryRead(IMyTerminalBlock block, string propertyId, out EntityValue value)
        {
            value = default(EntityValue);

            var door = block as IMyDoor;
            if (door == null)
                return false;

            switch (propertyId)
            {
                case Open:
                    value = EntityValue.Of(door.Status == DoorStatus.Open);
                    return true;
                case Shut:
                    value = EntityValue.Of(door.Status == DoorStatus.Closed);
                    return true;
                case Moving:
                    value = EntityValue.Of(door.Status == DoorStatus.Opening
                        || door.Status == DoorStatus.Closing);
                    return true;
            }

            return false;
        }

        public bool TryInvoke(IMyTerminalBlock block, string actionId)
        {
            var door = block as IMyDoor;
            if (door == null)
                return false;

            if (actionId == "open")
            {
                door.OpenDoor();
                return true;
            }

            if (actionId == "close")
            {
                door.CloseDoor();
                return true;
            }

            return false;
        }

        public string StatusOf(IMyTerminalBlock block)
        {
            var door = block as IMyDoor;
            if (door == null)
                return null;

            switch (door.Status)
            {
                case DoorStatus.Open: return "open";
                case DoorStatus.Closed: return "shut";
                case DoorStatus.Opening: return "opening…";
                case DoorStatus.Closing: return "closing…";
            }

            return null;
        }

        public bool IsBusy(IMyTerminalBlock block)
        {
            var door = block as IMyDoor;
            return door != null
                && (door.Status == DoorStatus.Opening || door.Status == DoorStatus.Closing);
        }

        public IBlockListener Listen(IMyTerminalBlock block)
        {
            // A door does raise events, and using them would spare the sampling. It is not done here
            // because the status is read every tick regardless, so the events would save nothing yet.
            return null;
        }
    }

    /// <summary>
    /// The air vent, which the terminal describes only as a switch and which the airlock is built around.
    /// </summary>
    /// <remarks>
    /// It has no events of any kind - see docs/Catalogue.md - so all of this is sampled, and a room that
    /// empties and refills between two ticks is invisible. That bound was accepted knowingly; it is worth
    /// remembering that the vent is the block it bites.
    /// </remarks>
    sealed class AirVentExtension : IBlockExtension
    {
        public const string Air = "air";
        public const string Empty = "empty";
        public const string Level = "level";
        public const string Sealed_ = "sealed";

        public bool Claims(IMyTerminalBlock block)
        {
            return block is IMyAirVent;
        }

        public void Describe(EntityKind kind)
        {
            kind.WithProperty(Air, "has air")
                .WithProperty(Empty, "is empty")
                .WithProperty(Level, ValueKind.Number, "oxygen level")
                .WithProperty(Sealed_, "airtight")
                .WithEvent("filled", Air, Edge.Rising, "finishes filling")
                .WithEvent("emptied", Empty, Edge.Rising, "finishes emptying")
                .WithState("has-air", Air, true, "has air")
                .WithState("is-empty", Empty, true, "is empty")
                .WithState("is-sealed", Sealed_, true, "is airtight")
                .WithState("is-leaking", Sealed_, false, "is not airtight")
                .WithAction("pressurize", "pressurize", "has-air")
                .WithAction("depressurize", "depressurize", "is-empty");
        }

        public bool TryRead(IMyTerminalBlock block, string propertyId, out EntityValue value)
        {
            value = default(EntityValue);

            var vent = block as IMyAirVent;
            if (vent == null)
                return false;

            switch (propertyId)
            {
                case Air:
                    value = EntityValue.Of(vent.Status == VentStatus.Pressurized);
                    return true;
                case Empty:
                    value = EntityValue.Of(vent.Status == VentStatus.Depressurized);
                    return true;
                case Level:
                    value = EntityValue.Of(vent.GetOxygenLevel());
                    return true;
                case Sealed_:
                    value = EntityValue.Of(vent.CanPressurize);
                    return true;
            }

            return false;
        }

        public bool TryInvoke(IMyTerminalBlock block, string actionId)
        {
            var vent = block as IMyAirVent;
            if (vent == null)
                return false;

            if (actionId == "pressurize")
            {
                vent.Depressurize = false;
                return true;
            }

            if (actionId == "depressurize")
            {
                vent.Depressurize = true;
                return true;
            }

            return false;
        }

        public string StatusOf(IMyTerminalBlock block)
        {
            var vent = block as IMyAirVent;
            if (vent == null)
                return null;

            switch (vent.Status)
            {
                case VentStatus.Pressurized: return "pressurized";
                case VentStatus.Depressurized: return "empty";
                case VentStatus.Pressurizing: return "filling…";
                case VentStatus.Depressurizing: return "emptying…";
            }

            return null;
        }

        public bool IsBusy(IMyTerminalBlock block)
        {
            var vent = block as IMyAirVent;
            return vent != null
                && (vent.Status == VentStatus.Pressurizing || vent.Status == VentStatus.Depressurizing);
        }

        public IBlockListener Listen(IMyTerminalBlock block)
        {
            return null;
        }
    }

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
