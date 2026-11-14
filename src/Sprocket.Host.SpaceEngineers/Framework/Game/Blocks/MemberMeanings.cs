using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Game.Wiring;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>
    /// What a particular kind of block means by its own members, where reading the id is not enough.
    /// </summary>
    public static class MemberMeanings
    {
        /// <summary>Gives the members of <paramref name="kind" />, which <paramref name="block" /> is of, their meanings, and hides those no sequence should be offered.</summary>
        public static void Settle(EntityKind kind, IMyTerminalBlock block)
        {
            Everywhere(kind, block);
            Setters(kind, block);

            if (block is IMyShipController)
                Controllers(kind, block);

            if (block is IMyLargeTurretBase || block is IMyTurretControlBlock)
                Turrets(kind, block);

            if (block is IMyUserControllableGun)
            {
                Withhold(kind, "SelectedAmmoType", Readiness.Pending, UnreadyReason.Ammo);
                HideValue(kind, "Shoot");
            }

            // An order whose two actions and toggle are offered, beside a value of Sprocket's own saying how it stands.
            if (block is IMyOffensiveCombatBlock || block is IMyDefensiveCombatBlock || block is IMyFlightMovementBlock
                || block is IMyPathRecorderBlock || block is IMyBasicMissionBlock)
            {
                HideValue(kind, "ActivateBehavior");
                In(kind, "ActivateBehavior", Category.Automation);
            }

            // How it attacks, beside every other member of the block, which is about shooting.
            if (block is IMyOffensiveCombatBlock)
            {
                In(kind, "AttackPattern", Category.Weapons);
                In(kind, "TargetPriority", Category.Weapons);
            }

            // How far it flees, beside where it flees to.
            if (block is IMyDefensiveCombatBlock)
                In(kind, "WaypointZoneSize", Category.Behaviour);

            // A dummy is shot at and shoots nothing: how it recovers is how it is set up.
            if (block is IMyTargetDummyBlock)
            {
                In(kind, "Enable Restoration", Category.Behaviour);
                In(kind, "Delay", Category.Behaviour);
            }

            // Who hears its messages, and how they go out.
            if (block is IMyBroadcastController)
            {
                In(kind, "BroadcastTarget", Category.Signals);
                In(kind, "UseAntenna", Category.Signals);
            }

            // Its pulse is fired by discharging it.
            if (Claimed<MicroEmitter>(kind))
                In(kind, "Discharge", Category.Weapons);

            // Arming is what a sequence does to a warhead beside detonating it, and its countdown is how long that takes.
            if (block is IMyWarhead)
            {
                In(kind, "Safety", Category.Weapons);
                In(kind, "DetonationTime", Category.Weapons);
            }

            // Whether the zone is up is whether it runs.
            if (block is IMySafeZoneBlock)
                In(kind, "SafeZoneCreate", Category.Power);

            // A charge lets go of the grid it holds.
            if (Claimed<SeparationCharge>(kind))
                In(kind, "Detach", Category.Locking);

            // How strongly it turns the ship.
            if (block is IMyGyro)
                In(kind, "Power", Category.Flight);

            // What a sensor looks for, and the noise it makes about it.
            if (block is IMySensorBlock)
            {
                In(kind, "Detect", Category.Detection);
                In(kind, "Audible Proximity Alert", Category.Sound);
            }

            // A drill's mode, beside the mode value it toggles.
            if (block is IMyShipDrill)
                In(kind, "TerrainClearingMode", Category.Production);

            // Helping another assembler make what it makes.
            if (block is IMyAssembler)
                In(kind, "slaveMode", Category.Production);

            // Filling bottles is something a sequence does; refilling them by itself is how it is set up.
            if (block is IMyGasTank || block is IMyGasGenerator)
                In(kind, "Refill", Category.Gas);

            // Whether a gun takes its ammunition from the conveyors is what it does with what it holds.
            if (block is IMyUserControllableGun)
                In(kind, "UseConveyor", Category.Stock);

            if (block is IMyPathRecorderBlock)
                HideValue(kind, "ID_PLAY_CHECKBOX");

            if (block is IMyRemoteControl)
                HideValue(kind, "AutoPilot");

            if (block is IMyEventControllerBlock)
            {
                Withhold(kind, "AvailableEvents", Readiness.Unsupported, UnreadyReason.Events);
                Withhold(kind, "Condition", Readiness.Unsupported, UnreadyReason.Events);
            }

            if (block is IMyRadioAntenna || block is IMyBeacon)
                In(kind, "Radius", Category.Signals);

            if (block is IMyLaserAntenna)
            {
                In(kind, "Range", Category.Behaviour);
                In(kind, "isPerm", Category.Behaviour);
            }

            if (block is IMyMechanicalConnectionBlock)
            {
                Building(kind);

                // Set once when the machine is built, however it moves after.
                In(kind, "ShareInertiaTensor", Category.Behaviour);
            }

            // Lock, unlock and toggle lock set it, and the rotor's own locked reads it.
            if (block is IMyMotorStator)
            {
                HideValue(kind, "RotorLock");
                HideValue(kind, "HingeLock");
            }

            // A parachute's switch says only whether a deploy was asked for, and the pair and the toggle already ask.
            if (block is IMyParachute)
                HideValue(kind, "Open");

            // A sequence brakes and drives a wheel while it runs.
            if (block is IMyMotorSuspension)
            {
                In(kind, "Speed Limit", Category.Movement);
                In(kind, "Braking", Category.Movement);
                In(kind, "Power", Category.Movement);
            }

            // The pull a block exerts belongs under no heading there is.
            if (block is IMyGravityGenerator || block is IMyGravityGeneratorSphere)
            {
                In(kind, "Width", Category.Other);
                In(kind, "Height", Category.Other);
                In(kind, "Depth", Category.Other);
                In(kind, "Radius", Category.Other);
                In(kind, "Gravity", Category.Other);

                // The game sets a jump drive's distance by this id too, where it means flight.
                In(kind, "SetGravityAcceleration", Category.Other);
            }

            // A searchlight is no gun: what it aims at and how it looks are how it is set up.
            if (block is IMySearchlight)
            {
                In(kind, "TargetingGroup_Selector", Category.Behaviour);
                In(kind, "ViewRotation", Category.Behaviour);
            }

            if (block is IMyCameraBlock)
            {
                In(kind, "ViewRotation", Category.Behaviour);

                // Looking through a camera is something a person does.
                Hide(kind, "View");
            }

            // How far it sees, and whether it shares what it finds.
            if (block is IMyOreDetector)
            {
                In(kind, "Range", Category.Detection);
                In(kind, "BroadcastUsingAntennas", Category.Signals);
            }

            // A survival kit's respawn name is the name a medical room's is.
            if (Claimed<RespawnPoint>(kind))
                In(kind, "SpawnName", Category.Identity);

            if (block is IMyMedicalRoom)
            {

                // The game keeps these two from before its faction system, and they set nothing a sequence wants.
                Hide(kind, "TakeOwnership");
                Hide(kind, "SetFaction");
            }

            if (block is IMyTextPanel)
                Screens(kind);

            // How far a sound carries, beside how loud it is.
            if (block is IMySoundBlock)
                In(kind, "RangeSlider", Category.Sound);

            // A sequence sets a timer's delay before starting it.
            if (block is IMyTimerBlock)
                In(kind, "TriggerDelay", Category.Automation);

            if (block is IMyProjector)
                Projectors(kind);

            if (block is IMyConveyorSorter)
            {
                In(kind, "blacklistWhitelist", Category.Stock);
                Hide(kind, "SearchField");
            }

            if (block is IMyStoreBlock)
                In(kind, "Searchable", Category.Behaviour);

            if (block is IMyGasTank)
                HideToggle(kind, "Stockpile");

            // On these two, how far the grid's load moves the smoke or the glow, which is how it looks.
            if (block is IMyExhaustBlock || block is IMyHeatVent)
                In(kind, "PowerDependency", Category.Appearance);

            if (block is IMyShipConnector)
            {
                HideToggle(kind, "Trading");
                HideToggle(kind, "PowerTransferOverride");
                HideToggle(kind, "CollectAll");
                HideToggle(kind, "ThrowOut");
            }

            // Charge mode says all of these. Recharge_On is left to Sprocket's recharge, which stands in for it by its id.
            if (block is IMyBatteryBlock)
                kind.Hiding("Recharge_Off", "Discharge_On", "Discharge_Off", "Auto");

            // A step runs a script by running the block, and Sprocket's own toolbar action is the wiring.
            if (block is IMyProgrammableBlock)
                kind.Hiding("RunWithDefaultArgument", SignalAction.ActionId);
        }

        /// <summary>Takes out the setters written by hand beside a value that says the same.</summary>
        /// <remarks>
        /// Transcribed from the game's own terminal data, one block at a time. A thruster's <c>SetOverride</c> takes a
        /// percentage, where the value takes newtons, so it stays.
        /// </remarks>
        static void Setters(EntityKind kind, IMyTerminalBlock block)
        {
            if (block is IMyLightingBlock || block is IMySearchlight)
                kind.Hiding("SetColor", "SetRadius", "SetBlinkInterval");

            if (block is IMyBeacon || block is IMyRadioAntenna)
                kind.Hiding("SetHudText", "SetRadius");

            if (block is IMySensorBlock)
                kind.Hiding("SetLeft", "SetRight", "SetTop", "SetBottom", "SetFront", "SetBack");

            if (block is IMyGyro)
                kind.Hiding("SetPitch", "SetYaw", "SetRoll", "SetPower");

            if (block is IMyMotorStator)
                kind.Hiding("SetLowerLimit", "SetUpperLimit", "SetVelocity");

            if (block is IMyPistonBase)
                kind.Hiding("SetVelocity");

            if (block is IMyMotorSuspension)
                kind.Hiding("SetSpeedLimit", "SetPropulsionOverride", "SetSteerOverride");

            if (block is IMyFlightMovementBlock)
                kind.Hiding("SetSpeedLimit");

            if (block is IMyTurretControlBlock)
                kind.Hiding("SetAngleDeviation");

            if (block is IMyHeatVent)
                kind.Hiding("SetColorMin", "SetColorMax");
        }

        /// <summary>What every block means by a word, where the table's answer is the wrong one.</summary>
        static void Everywhere(EntityKind kind, IMyTerminalBlock block)
        {
            In(kind, "Threshold", Category.Behaviour);
            In(kind, "Condition", Category.Behaviour);
            In(kind, "AvailableEvents", Category.Behaviour);
            In(kind, "ANDCheckbox", Category.Behaviour);
            In(kind, "ReceiveFrom", Category.Signals);
            Hide(kind, "SearchBox");

            // Set once when the machine is built, so flipping it is nothing a sequence does; the value stays, to be set.
            HideToggle(kind, "EnableParking");
            HideToggle(kind, "Auto-Refill");
            HideToggle(kind, "AutomaticallyPullBottles");

            // A store's and a button panel's are flipped to lock them to their faction while a sequence runs.
            if (!StoreBlock.Admits(block) && !(block is IMyButtonPanel))
                HideToggle(kind, StoreBlock.Access);

            // A reactor's is how a sequence keeps uranium back for another reactor, and the reactor renames it; a safe
            // zone's conveyors are what it eats its zone chips from; a gun's is flipped in a fight, to keep it on what it
            // holds.
            if (!(block is IMyReactor) && !(block is IMySafeZoneBlock) && !(block is IMyUserControllableGun)
                && !ConveyorUser.Carries(block))
                HideToggle(kind, ConveyorUser.Switch);

            // Where the game's own screens list the block, which is nothing a sequence does.
            Hide(kind, "ShowInInventory");
            Hide(kind, "ShowInTerminal");
            Hide(kind, "ShowInToolbarConfig");

            // A sequence names its blocks by name, so renaming one is not offered.
            HideValue(kind, "Name");

            // On a broadcast controller CustomName is the name chat shows its messages as coming from, a setting of its
            // own; its block name is Name, as everywhere.
            if (!(block is IMyBroadcastController))
                HideValue(kind, "CustomName");
        }

        /// <summary>A seat, a remote and a cryo chamber: what is flown, what is set, what needs a pilot.</summary>
        static void Controllers(EntityKind kind, IMyTerminalBlock block)
        {
            // Taking control is something a person does, wherever it is offered.
            Hide(kind, "Control");

            if (!(block is IMyRemoteControl))
                return;

            In(kind, "CameraList", Category.Behaviour);
            Withhold(kind, "CameraList", Readiness.Pending, UnreadyReason.Camera);
            In(kind, "SpeedLimit", Category.Behaviour);

            // How the autopilot flies its route, and the start of the route again.
            In(kind, "FlightMode", Category.Flight);
            In(kind, "Reset", Category.Flight);

            // Which way a remote calls forward, spelled once as a value and six times as actions.
            In(kind, "Left", Category.Behaviour);
            In(kind, "Right", Category.Behaviour);
        }

        /// <summary>
        /// A turret aims itself, so nearly all of it is what it does without being asked.
        /// </summary>
        static void Turrets(EntityKind kind, IMyTerminalBlock block)
        {
            In(kind, "Range", Category.Behaviour);
            In(kind, "TargetingGroup_Selector", Category.Behaviour);
            In(kind, "ViewRotation", Category.Behaviour);
            Hide(kind, "Control");

            if (!(block is IMyTurretControlBlock))
                return;

            In(kind, "AI", Category.Behaviour);
            In(kind, "AngleDeviation", Category.Behaviour);
            In(kind, "MultiplierAz", Category.Behaviour);
            In(kind, "MultiplierEl", Category.Behaviour);
            In(kind, "RotorAzimuth", Category.Behaviour);
            In(kind, "RotorElevation", Category.Behaviour);
            In(kind, "CameraList", Category.Behaviour);

            Withhold(kind, "RotorAzimuth", Readiness.Pending, UnreadyReason.Rotor);
            Withhold(kind, "RotorElevation", Readiness.Pending, UnreadyReason.Rotor);
            Withhold(kind, "CameraList", Readiness.Pending, UnreadyReason.Camera);
        }

        /// <summary>A screen: what is written on it, and how it was set up to write.</summary>
        static void Screens(EntityKind kind)
        {
            foreach (var one in new[]
                     {
                         "Font", "FontSize", "FontColor", "alignment", "TextPaddingSlider",
                         "BackgroundColor", "ScriptForegroundColor", "ScriptBackgroundColor",
                         "ChangeIntervalSlider", "PreserveAspectRatio", "Rotate"
                     })
            {
                In(kind, one, Category.Behaviour);
            }

            In(kind, "Title", Category.Identity);
        }

        /// <summary>A projector: one act, and a great many numbers describing where the ghost sits.</summary>
        static void Projectors(EntityKind kind)
        {
            foreach (var one in new[]
                     {
                         "X", "Y", "Z", "RotX", "RotY", "RotZ", "Scale", "GetOwnership",
                         "MarkMissingBlocks", "MarkUnfinishedBlocks"
                     })
            {
                In(kind, one, Category.Behaviour);
            }
        }

        /// <summary>Takes growing the machine a new part out of the list of what to tell it to do, where the block's capability has not brought it in.</summary>
        static void Building(EntityKind kind)
        {
            var actions = kind.Actions;
            for (var i = 0; i < actions.Count; i++)
            {
                EntityAction one = actions[i];
                if (!one.Sets && !one.Renamed && Categories.Builds(one.GameActionId))
                    one.Hide();
            }
        }

        /// <summary>Keeps every member the game named <paramref name="stem" /> from being made into a step.</summary>
        static void Withhold(EntityKind kind, string stem, Readiness readiness, UnreadyReason why)
        {
            var actions = kind.Actions;
            for (var i = 0; i < actions.Count; i++)
            {
                EntityAction one = actions[i];
                if (Categories.Names(one.Sets ? one.GamePropertyId : one.GameActionId, stem))
                    one.Withhold(readiness, why);
            }
        }

        /// <summary>Whether a capability of the kind <typeparamref name="T" /> claimed <paramref name="kind" />, for a block known by a component rather than an interface.</summary>
        static bool Claimed<T>(EntityKind kind) where T : Capability
        {
            foreach (var capability in kind.Capabilities)
            {
                if (capability is T)
                    return true;
            }

            return false;
        }

        /// <summary>Puts every member the game named <paramref name="stem" /> in its true category.</summary>
        static void In(EntityKind kind, string stem, Category category)
        {
            var actions = kind.Actions;
            for (var i = 0; i < actions.Count; i++)
            {
                EntityAction one = actions[i];
                if (Categories.Names(one.Sets ? one.GamePropertyId : one.GameActionId, stem))
                    one.Category = category;
            }
        }

        /// <summary>Takes the value the game named <paramref name="stem" /> out of the organized view.</summary>
        static void HideValue(EntityKind kind, string stem)
        {
            var actions = kind.Actions;
            for (var i = 0; i < actions.Count; i++)
            {
                EntityAction one = actions[i];
                if (one.GamePropertyId != null && Categories.Names(one.GamePropertyId, stem))
                    one.Hide();
            }
        }

        /// <summary>Takes the game's act that flips a switch out of the organized view, and nothing else of it, where the block's capability has not brought it in.</summary>
        /// <remarks>Matched by its exact id, which the switch's value shares.</remarks>
        static void HideToggle(EntityKind kind, string id)
        {
            string word;
            if (kind.Renames(id, false, out word))
                return;

            var actions = kind.Actions;
            for (var i = 0; i < actions.Count; i++)
            {
                EntityAction one = actions[i];
                if (one.Form == ReachKind.Action && !one.Renamed && one.GameActionId == id)
                    one.Hide();
            }
        }

        /// <summary>Takes every member the game named <paramref name="stem" /> out of the organized view.</summary>
        static void Hide(EntityKind kind, string stem)
        {
            var actions = kind.Actions;
            for (var i = 0; i < actions.Count; i++)
            {
                EntityAction one = actions[i];
                if (Categories.Names(one.Sets ? one.GamePropertyId : one.GameActionId, stem))
                    one.Hide();
            }
        }
    }
}
