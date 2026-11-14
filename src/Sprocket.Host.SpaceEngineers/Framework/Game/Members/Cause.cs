namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>What a block says is stopping it.</summary>
    /// <remarks>Persisted: each value's number is kept in saves, so a number is never changed or given to another value.</remarks>
    public enum Cause
    {
        /// <summary>Nothing.</summary>
        None = 0,

        /// <summary>An air vent cannot empty the room: nowhere to put the air.</summary>
        VentNowhere = 1,

        /// <summary>An air vent cannot fill the room: it is not airtight.</summary>
        VentNotAirtight = 2,

        /// <summary>An air vent cannot fill the room: no oxygen to fill it with.</summary>
        VentNoOxygen = 3,

        /// <summary>Something else told the vent to fill the room.</summary>
        VentToldToFill = 4,

        /// <summary>Something else told the vent to empty the room.</summary>
        VentToldToEmpty = 5,

        /// <summary>Something else set the battery to discharge.</summary>
        BatteryToldToDischarge = 6,

        /// <summary>Something else set the battery to auto.</summary>
        BatteryToldToAuto = 7,

        /// <summary>The battery cannot charge: it is switched off.</summary>
        BatteryOff = 8,

        /// <summary>The battery cannot charge: it is damaged.</summary>
        BatteryDamaged = 9,

        /// <summary>The battery cannot charge: no power coming in.</summary>
        BatteryNoPower = 10,

        /// <summary>The piston cannot move: switched off or no power.</summary>
        PistonNoPower = 11,

        /// <summary>The piston cannot move: it has no head.</summary>
        PistonNoHead = 12,

        /// <summary>The piston stopped: its speed was set to nought.</summary>
        PistonStopped = 13,

        /// <summary>The piston stopped extending: it was sent the other way.</summary>
        PistonReversedExtending = 14,

        /// <summary>The piston stopped retracting: it was sent the other way.</summary>
        PistonReversedRetracting = 15,

        /// <summary>The rotor or hinge cannot turn: switched off or no power.</summary>
        RotorNoPower = 16,

        /// <summary>The rotor or hinge cannot turn: it has no head.</summary>
        RotorNoHead = 17,

        /// <summary>The rotor or hinge cannot turn: it is locked.</summary>
        RotorLocked = 18,

        /// <summary>The rotor or hinge cannot turn: its torque is nought.</summary>
        RotorNoTorque = 19,

        /// <summary>The rotor or hinge stopped: its speed was set to nought.</summary>
        RotorStopped = 20,

        /// <summary>The rotor or hinge cannot turn: something is holding it.</summary>
        RotorHeld = 21,

        /// <summary>Something else switched the tank's stockpile off.</summary>
        TankStockpileOff = 22,

        /// <summary>The tank cannot fill: switched off or no power.</summary>
        TankNoPower = 23,

        /// <summary>The tank cannot fill: none of its gas is coming in.</summary>
        TankNoGas = 24,

        /// <summary>Something else switched the jump drive's recharging off.</summary>
        JumpDriveRechargeOff = 25,

        /// <summary>The programmable block did not run its script: it has none, it would not compile, or it is already running.</summary>
        ScriptNotRun = 26,

        /// <summary>Something else told the door to open while a step waited for it to close.</summary>
        DoorToldToOpen = 27,

        /// <summary>Something else told the door to close while a step waited for it to open.</summary>
        DoorToldToClose = 28,

        /// <summary>No conveyor connects the inventory to where a step moves its items, or none takes the item.</summary>
        ItemsNotConnected = 29,

        /// <summary>Where a step moves the items has no room for them.</summary>
        ItemsNoRoom = 30,

        /// <summary>The inventory holds none of the items a step moves.</summary>
        ItemsNoStock = 31,

        /// <summary>No inventory the items could go to takes that item at all, as a refinery's input takes only ore.</summary>
        ItemsNotAccepted = 32,

        /// <summary>Everywhere a step moves the items to is the inventory they are already in.</summary>
        ItemsAlreadyThere = 33,

        /// <summary>A wheel suspension a step moves the wheel of has no wheel.</summary>
        WheelNoWheel = 34,

        /// <summary>The piston cannot move: something is holding it.</summary>
        PistonHeld = 35
    }
}
