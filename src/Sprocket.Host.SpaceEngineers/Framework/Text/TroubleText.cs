using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Text
{
    /// <summary>What a block says is stopping it, and what it is doing, in words.</summary>
    public static class TroubleText
    {
        /// <summary>The words for <paramref name="trouble" /> in <paramref name="texts" />, or empty where it says nothing.</summary>
        public static string Of(Texts texts, BlockTrouble trouble)
        {
            switch (trouble.Cause)
            {
                case Cause.VentNowhere: return texts.TroubleVentNowhere;
                case Cause.VentNotAirtight: return texts.TroubleVentNotAirtight;
                case Cause.VentNoOxygen: return texts.TroubleVentNoOxygen;
                case Cause.ItemsNotConnected: return texts.TroubleItemsNotConnected;
                case Cause.ItemsNoRoom: return texts.TroubleItemsNoRoom;
                case Cause.ItemsNoStock: return texts.TroubleItemsNoStock;
                case Cause.ItemsNotAccepted: return texts.TroubleItemsNotAccepted;
                case Cause.ItemsAlreadyThere: return texts.TroubleItemsAlreadyThere;
                case Cause.VentToldToFill: return texts.TroubleVentToldToFill;
                case Cause.VentToldToEmpty: return texts.TroubleVentToldToEmpty;
                case Cause.BatteryToldToDischarge: return texts.TroubleBatteryToldToDischarge;
                case Cause.BatteryToldToAuto: return texts.TroubleBatteryToldToAuto;
                case Cause.BatteryOff: return texts.TroubleBatteryOff;
                case Cause.BatteryDamaged: return texts.TroubleBatteryDamaged;
                case Cause.BatteryNoPower: return texts.TroubleBatteryNoPower;
                case Cause.PistonNoPower: return texts.TroublePistonNoPower;
                case Cause.PistonNoHead: return texts.TroublePistonNoHead;
                case Cause.PistonStopped: return texts.TroublePistonStopped;
                case Cause.PistonReversedExtending: return texts.TroublePistonReversedExtending;
                case Cause.PistonReversedRetracting: return texts.TroublePistonReversedRetracting;
                case Cause.PistonHeld: return texts.TroublePistonHeld;
                case Cause.RotorNoPower: return texts.TroubleRotorNoPower;
                case Cause.RotorNoHead: return texts.TroubleRotorNoHead;
                case Cause.RotorLocked: return texts.TroubleRotorLocked;
                case Cause.RotorNoTorque: return texts.TroubleRotorNoTorque;
                case Cause.RotorStopped: return texts.TroubleRotorStopped;
                case Cause.RotorHeld: return texts.TroubleRotorHeld;
                case Cause.TankStockpileOff: return texts.TroubleTankStockpileOff;
                case Cause.TankNoPower: return texts.TroubleTankNoPower;
                case Cause.TankNoGas: return Template.Fill(texts.TroubleTankNoGas, "gas", trouble.About ?? "");
                case Cause.JumpDriveRechargeOff: return texts.TroubleJumpDriveRechargeOff;
                case Cause.ScriptNotRun: return texts.TroubleScriptNotRun;
                case Cause.DoorToldToOpen: return texts.TroubleDoorToldToOpen;
                case Cause.DoorToldToClose: return texts.TroubleDoorToldToClose;
                case Cause.WheelNoWheel: return texts.TroubleWheelNoWheel;
                default: return "";
            }
        }

        /// <summary>The words for <paramref name="trouble" /> as a sentence about <paramref name="block" />, by its name, or the words alone where no block is named.</summary>
        public static string Of(Texts texts, BlockTrouble trouble, string block)
        {
            var words = Of(texts, trouble);
            return string.IsNullOrEmpty(block) || words.Length == 0 ? words : Template.Fill(Template.Fill(texts.ProblemBlockTrouble, "block", block), "trouble", words);
        }

        /// <summary>The words for <paramref name="status" /> in <paramref name="texts" />, or empty where there is nothing to say.</summary>
        public static string Of(Texts texts, Status status)
        {
            if (status.Trouble.Exists)
                return Of(texts, status.Trouble);

            switch (status.Condition)
            {
                case Condition.Damaged: return texts.StatusDamaged;
                case Condition.Off: return texts.StatusOff;
                case Condition.On: return texts.StatusOn;
                case Condition.NoPower: return texts.StatusNoPower;
                case Condition.Open: return texts.StatusOpen;
                case Condition.Shut: return texts.StatusShut;
                case Condition.Opening: return texts.StatusOpening;
                case Condition.Closing: return texts.StatusClosing;
                case Condition.Pressurized: return texts.StatusPressurized;
                case Condition.Empty: return texts.StatusEmpty;
                case Condition.Filling: return texts.StatusFilling;
                case Condition.Emptying: return texts.StatusEmptying;
                case Condition.Full: return texts.StatusFull;
                case Condition.Charging: return texts.StatusCharging;
                case Condition.Extended: return texts.StatusExtended;
                case Condition.Retracted: return texts.StatusRetracted;
                case Condition.Extending: return texts.StatusExtending;
                case Condition.Retracting: return texts.StatusRetracting;
                case Condition.NotWired: return texts.StatusNotWired;
                default: return "";
            }
        }
    }
}
