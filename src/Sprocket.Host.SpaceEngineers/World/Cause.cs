namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>What a block says is stopping it.</summary>
    /// <remarks>Kept with a halt and worded where it is read, so every player reads it in their own language.</remarks>
    public enum Cause
    {
        /// <summary>Nothing.</summary>
        None,

        /// <summary>An air vent cannot empty the room: nowhere to put the air.</summary>
        VentNowhere,

        /// <summary>An air vent cannot fill the room: it is not airtight.</summary>
        VentNotAirtight,

        /// <summary>An air vent cannot fill the room: no oxygen to fill it with.</summary>
        VentNoOxygen,

        /// <summary>Something else told the vent to fill the room.</summary>
        VentToldToFill,

        /// <summary>Something else told the vent to empty the room.</summary>
        VentToldToEmpty,

        /// <summary>Something else set the battery to discharge.</summary>
        BatteryToldToDischarge,

        /// <summary>Something else set the battery to auto.</summary>
        BatteryToldToAuto,

        /// <summary>The battery cannot charge: it is switched off.</summary>
        BatteryOff,

        /// <summary>The battery cannot charge: it is damaged.</summary>
        BatteryDamaged,

        /// <summary>The battery cannot charge: no power coming in.</summary>
        BatteryNoPower,

        /// <summary>The piston cannot move: switched off or no power.</summary>
        PistonNoPower,

        /// <summary>The piston cannot move: it has no head.</summary>
        PistonNoHead,

        /// <summary>The piston stopped: its speed was set to nought.</summary>
        PistonStopped,

        /// <summary>The piston stopped extending: it was sent the other way.</summary>
        PistonReversedExtending,

        /// <summary>The piston stopped retracting: it was sent the other way.</summary>
        PistonReversedRetracting,

        /// <summary>The rotor or hinge cannot turn: switched off or no power.</summary>
        RotorNoPower,

        /// <summary>The rotor or hinge cannot turn: it has no head.</summary>
        RotorNoHead,

        /// <summary>The rotor or hinge cannot turn: it is locked.</summary>
        RotorLocked,

        /// <summary>The rotor or hinge cannot turn: its torque is nought.</summary>
        RotorNoTorque,

        /// <summary>The rotor or hinge stopped: its speed was set to nought.</summary>
        RotorStopped,

        /// <summary>The rotor or hinge cannot turn: something is holding it.</summary>
        RotorHeld,

        /// <summary>Something else switched the tank's stockpile off.</summary>
        TankStockpileOff,

        /// <summary>The tank cannot fill: switched off or no power.</summary>
        TankNoPower,

        /// <summary>The tank cannot fill: none of its gas is coming in.</summary>
        TankNoGas,

        /// <summary>Something else switched the jump drive's recharging off.</summary>
        JumpDriveRechargeOff
    }
}
