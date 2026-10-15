using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>What each of the game's types of block is called where a slot says what it expects.</summary>
    public static class TypeText
    {
        /// <summary>
        /// What the type <paramref name="type" />, written as a slot writes it, is called in the words <paramref name="texts" />
        /// give, or the type as written where Sprocket has no name for it, which a modded type is.
        /// </summary>
        public static string Of(Texts texts, string type)
        {
            switch (type)
            {
                case "AdvancedDoor": return texts.TypeAdvancedDoor;
                case "AirtightDoorGeneric": return texts.TypeAirtightDoorGeneric;
                case "AirtightHangarDoor": return texts.TypeAirtightHangarDoor;
                case "AirtightSlideDoor": return texts.TypeAirtightSlideDoor;
                case "AirVent": return texts.TypeAirVent;
                case "Assembler": return texts.TypeAssembler;
                case "BasicMissionBlock": return texts.TypeBasicMissionBlock;
                case "BatteryBlock": return texts.TypeBatteryBlock;
                case "Beacon": return texts.TypeBeacon;
                case "BroadcastController": return texts.TypeBroadcastController;
                case "ButtonPanel": return texts.TypeButtonPanel;
                case "CameraBlock": return texts.TypeCameraBlock;
                case "CargoContainer": return texts.TypeCargoContainer;
                case "Cockpit": return texts.TypeCockpit;
                case "Collector": return texts.TypeCollector;
                case "ContractBlock": return texts.TypeContractBlock;
                case "ControlPanel": return texts.TypeControlPanel;
                case "ConveyorSorter": return texts.TypeConveyorSorter;
                case "ConveyorTurretBase": return texts.TypeConveyorTurretBase;
                case "CryoChamber": return texts.TypeCryoChamber;
                case "Decoy": return texts.TypeDecoy;
                case "DefensiveCombatBlock": return texts.TypeDefensiveCombatBlock;
                case "Door": return texts.TypeDoor;
                case "DoorBase": return texts.TypeDoorBase;
                case "Drill": return texts.TypeDrill;
                case "EmotionControllerBlock": return texts.TypeEmotionControllerBlock;
                case "EventControllerBlock": return texts.TypeEventControllerBlock;
                case "ExhaustBlock": return texts.TypeExhaustBlock;
                case "ExtendedPistonBase": return texts.TypeExtendedPistonBase;
                case "FlightMovementBlock": return texts.TypeFlightMovementBlock;
                case "FueledPowerProducer": return texts.TypeFueledPowerProducer;
                case "FunctionalBlock": return texts.TypeFunctionalBlock;
                case "GasTank": return texts.TypeGasTank;
                case "GravityGenerator": return texts.TypeGravityGenerator;
                case "GravityGeneratorBase": return texts.TypeGravityGeneratorBase;
                case "GravityGeneratorSphere": return texts.TypeGravityGeneratorSphere;
                case "Gyro": return texts.TypeGyro;
                case "HeatVentBlock": return texts.TypeHeatVentBlock;
                case "HydrogenEngine": return texts.TypeHydrogenEngine;
                case "InteriorLight": return texts.TypeInteriorLight;
                case "InteriorTurret": return texts.TypeInteriorTurret;
                case "JumpDrive": return texts.TypeJumpDrive;
                case "Jukebox": return texts.TypeJukebox;
                case "LandingGear": return texts.TypeLandingGear;
                case "LargeGatlingTurret": return texts.TypeLargeGatlingTurret;
                case "LargeMissileTurret": return texts.TypeLargeMissileTurret;
                case "LaserAntenna": return texts.TypeLaserAntenna;
                case "LCDPanelsBlock": return texts.TypeLCDPanelsBlock;
                case "LightingBlock": return texts.TypeLightingBlock;
                case "MechanicalConnectionBlock": return texts.TypeMechanicalConnectionBlock;
                case "MedicalRoom": return texts.TypeMedicalRoom;
                case "MergeBlock": return texts.TypeMergeBlock;
                case "MotorAdvancedStator": return texts.TypeMotorAdvancedStator;
                case "MotorBase": return texts.TypeMotorBase;
                case "MotorStator": return texts.TypeMotorStator;
                case "MotorSuspension": return texts.TypeMotorSuspension;
                case "MyProgrammableBlock": return texts.TypeMyProgrammableBlock;
                case "OffensiveCombatBlock": return texts.TypeOffensiveCombatBlock;
                case "OreDetector": return texts.TypeOreDetector;
                case "OxygenFarm": return texts.TypeOxygenFarm;
                case "OxygenGenerator": return texts.TypeOxygenGenerator;
                case "OxygenTank": return texts.TypeOxygenTank;
                case "Parachute": return texts.TypeParachute;
                case "PathRecorderBlock": return texts.TypePathRecorderBlock;
                case "PistonBase": return texts.TypePistonBase;
                case "ProductionBlock": return texts.TypeProductionBlock;
                case "Projector": return texts.TypeProjector;
                case "ProjectorBase": return texts.TypeProjectorBase;
                case "RadioAntenna": return texts.TypeRadioAntenna;
                case "Reactor": return texts.TypeReactor;
                case "ReflectorLight": return texts.TypeReflectorLight;
                case "Refinery": return texts.TypeRefinery;
                case "RemoteControl": return texts.TypeRemoteControl;
                case "SafeZoneBlock": return texts.TypeSafeZoneBlock;
                case "Searchlight": return texts.TypeSearchlight;
                case "SensorBlock": return texts.TypeSensorBlock;
                case "ShipConnector": return texts.TypeShipConnector;
                case "ShipController": return texts.TypeShipController;
                case "ShipGrinder": return texts.TypeShipGrinder;
                case "ShipToolBase": return texts.TypeShipToolBase;
                case "ShipWelder": return texts.TypeShipWelder;
                case "SmallGatlingGun": return texts.TypeSmallGatlingGun;
                case "SmallMissileLauncher": return texts.TypeSmallMissileLauncher;
                case "SmallMissileLauncherReload": return texts.TypeSmallMissileLauncherReload;
                case "SolarPanel": return texts.TypeSolarPanel;
                case "SoundBlock": return texts.TypeSoundBlock;
                case "SpaceBall": return texts.TypeSpaceBall;
                case "StoreBlock": return texts.TypeStoreBlock;
                case "SurvivalKit": return texts.TypeSurvivalKit;
                case "TargetDummyBlock": return texts.TypeTargetDummyBlock;
                case "TextPanel": return texts.TypeTextPanel;
                case "Thrust": return texts.TypeThrust;
                case "TimerBlock": return texts.TypeTimerBlock;
                case "TransponderBlock": return texts.TypeTransponderBlock;
                case "TurretBase": return texts.TypeTurretBase;
                case "TurretControlBlock": return texts.TypeTurretControlBlock;
                case "UpgradeModule": return texts.TypeUpgradeModule;
                case "UserControllableGun": return texts.TypeUserControllableGun;
                case "VendingMachine": return texts.TypeVendingMachine;
                case "VirtualMass": return texts.TypeVirtualMass;
                case "Warhead": return texts.TypeWarhead;
                case "WindTurbine": return texts.TypeWindTurbine;
                default: return type;
            }
        }
    }
}
