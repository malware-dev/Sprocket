using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Blocks;
using Sprocket.Host.SpaceEngineers.Features.Terminal;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// What a particular kind of block means by its own members, where reading the id is not enough.
    /// </summary>
    /// <remarks>
    /// <see cref="Categories" /> answers by id, which is what lets one table cover every block in the
    /// game. It is a default and nothing more: the same word means a glow on a light and a reach on
    /// an antenna, and a great many members are only placeable by somebody who knows what the block
    /// is for. This is where that knowledge goes until the block gets a capability of its own.
    /// <para>
    /// <b>Hiding is the other half.</b> Three kinds of member have no business in a list of things a
    /// sequence can do: one that needs somebody sitting in the block, one the game keeps for
    /// compatibility and no longer honours, and one that is furniture for the terminal's own screen.
    /// They stay in the terminal view, where they are what the block declares.
    /// </para>
    /// </remarks>
    public static class Meanings
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
                Withhold(kind, "SelectedAmmoType", Readiness.Pending, Holdback.Ammo);
                HideValue(kind, "Shoot");
            }

            // An order whose two actions and toggle are offered, beside a value of Sprocket's own saying how it stands.
            if (block is IMyOffensiveCombatBlock || block is IMyDefensiveCombatBlock || block is IMyFlightMovementBlock
                || block is IMyPathRecorderBlock || block is IMyBasicMissionBlock)
                HideValue(kind, "ActivateBehavior");

            if (block is IMyPathRecorderBlock)
                HideValue(kind, "ID_PLAY_CHECKBOX");

            if (block is IMyRemoteControl)
                HideValue(kind, "AutoPilot");

            if (block is IMyEventControllerBlock)
            {
                Withhold(kind, "AvailableEvents", Readiness.Unsupported, Holdback.Events);
                Withhold(kind, "Condition", Readiness.Unsupported, Holdback.Events);
            }

            if (block is IMyRadioAntenna || block is IMyBeacon)
                In(kind, "Radius", Category.Signals);

            if (block is IMyLaserAntenna)
            {
                In(kind, "Range", Category.Behaviour);
                In(kind, "isPerm", Category.Behaviour);
            }

            if (block is IMyMechanicalConnectionBlock)
                Building(kind);

            // Lock, unlock and toggle lock set it, and the rotor's own locked reads it.
            if (block is IMyMotorStator)
            {
                HideValue(kind, "RotorLock");
                HideValue(kind, "HingeLock");
            }

            // A parachute's switch says only whether a deploy was asked for, and the pair and the toggle already ask.
            if (block is IMyParachute)
                HideValue(kind, "Open");

            if (block is IMyMotorSuspension)
                In(kind, "Speed Limit", Category.Behaviour);

            if (block is IMyGravityGenerator || block is IMyGravityGeneratorSphere)
            {
                In(kind, "Height", Category.Behaviour);
                In(kind, "Radius", Category.Behaviour);

                // The game sets a jump drive's distance by this id too, where it means flight.
                In(kind, "SetGravityAcceleration", Category.Behaviour);
            }

            if (block is IMyCameraBlock)
            {
                In(kind, "ViewRotation", Category.Behaviour);

                // Looking through a camera is something a person does.
                Hide(kind, "View");
            }

            if (block is IMyOreDetector)
            {
                In(kind, "Range", Category.Behaviour);
                In(kind, "BroadcastUsingAntennas", Category.Behaviour);
            }

            if (block is IMyMedicalRoom)
            {
                In(kind, "SpawnName", Category.Identity);

                // Ownership moved to the faction system years ago; these two are what is left.
                Hide(kind, "TakeOwnership");
                Hide(kind, "SetFaction");
            }

            if (block is IMyTextPanel)
                Screens(kind);

            if (block is IMySoundBlock)
                In(kind, "RangeSlider", Category.Behaviour);

            if (block is IMyTimerBlock)
                In(kind, "TriggerDelay", Category.Behaviour);

            if (block is IMyProjector)
                Projectors(kind);

            if (block is IMyConveyorSorter)
            {
                In(kind, "blacklistWhitelist", Category.Behaviour);
                Hide(kind, "SearchField");
            }

            if (block is IMyWarhead)
                In(kind, "DetonationTime", Category.Behaviour);

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

            // Charge mode says all of these, one value and three words. Recharge On is left to Sprocket's
            // recharge, which stands in for it by its id; the two toggles stay, renamed by the battery.
            if (block is IMyBatteryBlock)
                kind.Hiding("Recharge_Off", "Discharge_On", "Discharge_Off", "Auto");

            // What starts a programmable block's own script. A step runs a script by running the block,
            // and Sprocket's own toolbar action is the wiring of the thing.
            if (block is IMyProgrammableBlock)
                kind.Hiding("RunWithDefaultArgument", SignalAction.ActionId);
        }

        /// <summary>Takes out the setters written by hand beside a value that says the same.</summary>
        /// <remarks>
        /// Each was made by hand for its own block, so each is said here rather than read off its id -
        /// from the game's own terminal data, one block at a time. A thruster's <c>SetOverride</c> is
        /// not among them though it looks it: it takes a percentage, where the value takes newtons.
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
        /// <remarks>
        /// An event controller is nothing but the setting up of a watcher, and a transponder's filter
        /// is who it listens to rather than what it says.
        /// </remarks>
        static void Everywhere(EntityKind kind, IMyTerminalBlock block)
        {
            In(kind, "Threshold", Category.Behaviour);
            In(kind, "Condition", Category.Behaviour);
            In(kind, "AvailableEvents", Category.Behaviour);
            In(kind, "ANDCheckbox", Category.Behaviour);
            In(kind, "ReceiveFrom", Category.Behaviour);
            Hide(kind, "SearchBox");

            // Set once when the machine is built, so flipping it is nothing a sequence does. The value
            // stays, to be set. The production blocks and the stores offer theirs, for a button that flips one.
            HideToggle(kind, "EnableParking");
            HideToggle(kind, "Auto-Refill");
            HideToggle(kind, "AutomaticallyPullBottles");

            // A store's and a button panel's are flipped to lock them to their faction while a sequence runs.
            if (!Store.Admits(block) && !(block is IMyButtonPanel))
                HideToggle(kind, Store.Access);

            // A reactor's is how a sequence keeps uranium back for another reactor, and the reactor renames it; a safe
            // zone's conveyors are what it eats its zone chips from; a gun's is flipped in a fight, to keep it on what it
            // holds.
            if (!(block is IMyReactor) && !(block is IMySafeZoneBlock) && !(block is IMyUserControllableGun)
                && !Conveyors.Carries(block))
                HideToggle(kind, Conveyors.Switch);

            // Where the game's own screens list the block, which is nothing a sequence does.
            Hide(kind, "ShowInInventory");
            Hide(kind, "ShowInTerminal");
            Hide(kind, "ShowInToolbarConfig");

            // A sequence says which block it means by the block's name, so a sequence that renamed
            // one would be cutting the ground from under itself and every other sequence besides.
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
            Withhold(kind, "CameraList", Readiness.Pending, Holdback.Camera);
            In(kind, "SpeedLimit", Category.Behaviour);

            // Which way a remote calls forward, spelled once as a value and six times as actions.
            In(kind, "Left", Category.Behaviour);
            In(kind, "Right", Category.Behaviour);
        }

        /// <summary>
        /// A turret aims itself, so nearly all of it is what it does without being asked.
        /// </summary>
        /// <remarks>
        /// What is left under Weapons is what a sequence can actually say: shoot, shoot once, reload,
        /// change ammunition. A turret controller has none of those, which is why it reads as a block
        /// of settings with a weapon somewhere behind it.
        /// </remarks>
        static void Turrets(EntityKind kind, IMyTerminalBlock block)
        {
            In(kind, "Range", Category.Behaviour);
            In(kind, "TargetingGroup_Selector", Category.Behaviour);
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

            Withhold(kind, "RotorAzimuth", Readiness.Pending, Holdback.Rotor);
            Withhold(kind, "RotorElevation", Readiness.Pending, Holdback.Rotor);
            Withhold(kind, "CameraList", Readiness.Pending, Holdback.Camera);
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
        /// <remarks>
        /// A rotor offers to grow itself a head in three sizes and a hinge's head as well. Asked of the id rather than
        /// listed, because the game has more spellings of it than a list would hold. One a capability gave a word of its
        /// own is offered under that word.
        /// </remarks>
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
        static void Withhold(EntityKind kind, string stem, Readiness readiness, Holdback why)
        {
            var actions = kind.Actions;
            for (var i = 0; i < actions.Count; i++)
            {
                EntityAction one = actions[i];
                if (Categories.Names(one.Sets ? one.GamePropertyId : one.GameActionId, stem))
                    one.Withhold(readiness, why);
            }
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
        /// <remarks>
        /// A switch reaches the list three times - the pair that says which way, the act that flips
        /// it, and the value it settles on - and the pair is two of those said better. The value is
        /// the one that goes, because the act is the only one of the three a person cannot write out
        /// of a pair of steps.
        /// </remarks>
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
        /// <remarks>
        /// By its exact id, because the switch's value and the act share it and the value stays. One a capability or a kind gave
        /// a word of its own is offered under that word, as a tank's toggle stockpile.
        /// </remarks>
        static void HideToggle(EntityKind kind, string id)
        {
            string word;
            if (kind.Renames(id, false, out word))
                return;

            var actions = kind.Actions;
            for (var i = 0; i < actions.Count; i++)
            {
                EntityAction one = actions[i];
                if (one.Form == Form.Action && !one.Renamed && one.GameActionId == id)
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
