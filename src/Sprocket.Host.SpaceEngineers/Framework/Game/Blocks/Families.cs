using System;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>Which <see cref="Family" /> each of the game's block types hangs under.</summary>
    public static class Families
    {
        const string Prefix = "MyObjectBuilder_";

        /// <summary>The family of a block of the type <paramref name="typeId" />, as the game's ids spell it.</summary>
        public static Family Of(string typeId)
        {
            if (typeId.StartsWith(Prefix, StringComparison.Ordinal))
                typeId = typeId.Substring(Prefix.Length);

            switch (typeId)
            {
                case "Door":
                case "AirtightSlideDoor":
                case "AdvancedDoor":
                    return Family.Door;
                case "AirtightHangarDoor":
                    return Family.HangarDoor;
                case "InteriorLight":
                case "ReflectorLight":
                case "Searchlight":
                case "EmissiveBlock":
                    return Family.Light;
                case "TextPanel":
                case "LCDPanelsBlock":
                    return Family.Screen;
                case "LargeGatlingTurret":
                case "LargeMissileTurret":
                case "InteriorTurret":
                    return Family.Turret;
                case "SmallGatlingGun":
                case "SmallMissileLauncher":
                case "SmallMissileLauncherReload":
                    return Family.Weapon;
                case "MotorStator":
                case "MotorAdvancedStator":
                    return Family.Rotor;
                case "GravityGenerator":
                case "GravityGeneratorSphere":
                    return Family.GravityGenerator;
                case "FlightMovementBlock":
                case "PathRecorderBlock":
                case "BasicMissionBlock":
                case "OffensiveCombatBlock":
                case "DefensiveCombatBlock":
                    return Family.AiBlock;
                case "Projector":
                    return Family.Projector;
                case "Thrust":
                    return Family.Thruster;
                case "BatteryBlock":
                    return Family.Battery;
                case "OxygenTank":
                    return Family.Tank;
                case "Cockpit":
                    return Family.Cockpit;
                case "CryoChamber":
                    return Family.Cryo;
                case "RemoteControl":
                    return Family.RemoteControl;
                case "AirVent":
                    return Family.AirVent;
                case "ShipConnector":
                    return Family.Connector;
                case "MergeBlock":
                    return Family.MergeBlock;
                case "LandingGear":
                    return Family.LandingGear;
                case "ExtendedPistonBase":
                    return Family.Piston;
                case "MotorSuspension":
                    return Family.Suspension;
                case "Assembler":
                    return Family.Assembler;
                case "Refinery":
                    return Family.Refinery;
                case "SurvivalKit":
                    return Family.SurvivalKit;
                case "OxygenGenerator":
                    return Family.GasGenerator;
                case "OxygenFarm":
                    return Family.OxygenFarm;
                case "HydrogenEngine":
                    return Family.HydrogenEngine;
                case "Reactor":
                    return Family.Reactor;
                case "SolarPanel":
                    return Family.SolarPanel;
                case "WindTurbine":
                    return Family.WindTurbine;
                case "ConveyorSorter":
                    return Family.Sorter;
                case "Drill":
                    return Family.Drill;
                case "ShipWelder":
                    return Family.Welder;
                case "ShipGrinder":
                    return Family.Grinder;
                case "ButtonPanel":
                    return Family.ButtonPanel;
                case "TimerBlock":
                    return Family.Timer;
                case "SensorBlock":
                    return Family.Sensor;
                case "MyProgrammableBlock":
                    return Family.ProgrammableBlock;
                case "RadioAntenna":
                    return Family.Antenna;
                case "LaserAntenna":
                    return Family.LaserAntenna;
                case "Beacon":
                    return Family.Beacon;
                case "CameraBlock":
                    return Family.Camera;
                case "OreDetector":
                    return Family.OreDetector;
                case "Gyro":
                    return Family.Gyroscope;
                case "JumpDrive":
                    return Family.JumpDrive;
                case "CargoContainer":
                    return Family.Cargo;
                case "Collector":
                    return Family.Collector;
                case "UpgradeModule":
                    return Family.UpgradeModule;
                case "Decoy":
                    return Family.Decoy;
                case "SoundBlock":
                    return Family.SoundBlock;
                case "Jukebox":
                    return Family.Jukebox;
                case "TurretControlBlock":
                    return Family.TurretController;
                case "EventControllerBlock":
                    return Family.EventController;
                case "BroadcastController":
                    return Family.BroadcastController;
                case "TransponderBlock":
                    return Family.ActionRelay;
                case "VendingMachine":
                    return Family.VendingMachine;
                case "StoreBlock":
                    return Family.Store;
                case "SafeZoneBlock":
                    return Family.SafeZone;
                case "VirtualMass":
                    return Family.ArtificialMass;
                case "SpaceBall":
                    return Family.SpaceBall;
                case "EmotionControllerBlock":
                    return Family.EmotionController;
                case "Parachute":
                    return Family.Parachute;
                case "HeatVentBlock":
                    return Family.HeatVent;
                case "ExhaustBlock":
                    return Family.Exhaust;
                case "TargetDummyBlock":
                    return Family.TargetDummy;
                case "Kitchen":
                    return Family.Kitchen;
                case "Planter":
                    return Family.Planter;
                case "ContractBlock":
                    return Family.Contracts;
                case "MedicalRoom":
                    return Family.MedicalRoom;
                case "Warhead":
                    return Family.Warhead;
                default:
                    return Family.None;
            }
        }
    }
}
