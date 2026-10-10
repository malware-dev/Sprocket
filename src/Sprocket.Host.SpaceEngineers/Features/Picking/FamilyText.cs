using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>What each family of blocks is called in the list of kinds.</summary>
    public static class FamilyText
    {
        /// <summary>What <paramref name="family" /> is called, in the words <paramref name="texts" /> give, or empty for none.</summary>
        public static string Of(Texts texts, Family family)
        {
            switch (family)
            {
                case Family.Door: return texts.FamilyDoor;
                case Family.HangarDoor: return texts.FamilyHangarDoor;
                case Family.Light: return texts.FamilyLight;
                case Family.Screen: return texts.FamilyScreen;
                case Family.Turret: return texts.FamilyTurret;
                case Family.Weapon: return texts.FamilyWeapon;
                case Family.Rotor: return texts.FamilyRotor;
                case Family.GravityGenerator: return texts.FamilyGravityGenerator;
                case Family.AiBlock: return texts.FamilyAiBlock;
                case Family.Projector: return texts.FamilyProjector;
                case Family.Thruster: return texts.FamilyThruster;
                case Family.Battery: return texts.FamilyBattery;
                case Family.Tank: return texts.FamilyTank;
                case Family.Cockpit: return texts.FamilyCockpit;
                case Family.Cryo: return texts.FamilyCryo;
                case Family.RemoteControl: return texts.FamilyRemoteControl;
                case Family.AirVent: return texts.FamilyAirVent;
                case Family.Connector: return texts.FamilyConnector;
                case Family.MergeBlock: return texts.FamilyMergeBlock;
                case Family.LandingGear: return texts.FamilyLandingGear;
                case Family.Piston: return texts.FamilyPiston;
                case Family.Suspension: return texts.FamilySuspension;
                case Family.Assembler: return texts.FamilyAssembler;
                case Family.Refinery: return texts.FamilyRefinery;
                case Family.SurvivalKit: return texts.FamilySurvivalKit;
                case Family.GasGenerator: return texts.FamilyGasGenerator;
                case Family.OxygenFarm: return texts.FamilyOxygenFarm;
                case Family.HydrogenEngine: return texts.FamilyHydrogenEngine;
                case Family.Reactor: return texts.FamilyReactor;
                case Family.SolarPanel: return texts.FamilySolarPanel;
                case Family.WindTurbine: return texts.FamilyWindTurbine;
                case Family.Sorter: return texts.FamilySorter;
                case Family.Drill: return texts.FamilyDrill;
                case Family.Welder: return texts.FamilyWelder;
                case Family.Grinder: return texts.FamilyGrinder;
                case Family.ButtonPanel: return texts.FamilyButtonPanel;
                case Family.Timer: return texts.FamilyTimer;
                case Family.Sensor: return texts.FamilySensor;
                case Family.ProgrammableBlock: return texts.FamilyProgrammableBlock;
                case Family.Antenna: return texts.FamilyAntenna;
                case Family.LaserAntenna: return texts.FamilyLaserAntenna;
                case Family.Beacon: return texts.FamilyBeacon;
                case Family.Camera: return texts.FamilyCamera;
                case Family.OreDetector: return texts.FamilyOreDetector;
                case Family.Gyroscope: return texts.FamilyGyroscope;
                case Family.JumpDrive: return texts.FamilyJumpDrive;
                case Family.Cargo: return texts.FamilyCargo;
                case Family.Collector: return texts.FamilyCollector;
                case Family.UpgradeModule: return texts.FamilyUpgradeModule;
                case Family.Decoy: return texts.FamilyDecoy;
                case Family.SoundBlock: return texts.FamilySoundBlock;
                case Family.Jukebox: return texts.FamilyJukebox;
                case Family.TurretController: return texts.FamilyTurretController;
                case Family.EventController: return texts.FamilyEventController;
                case Family.BroadcastController: return texts.FamilyBroadcastController;
                case Family.ActionRelay: return texts.FamilyActionRelay;
                case Family.VendingMachine: return texts.FamilyVendingMachine;
                case Family.Store: return texts.FamilyStore;
                case Family.SafeZone: return texts.FamilySafeZone;
                case Family.ArtificialMass: return texts.FamilyArtificialMass;
                case Family.SpaceBall: return texts.FamilySpaceBall;
                case Family.EmotionController: return texts.FamilyEmotionController;
                case Family.Parachute: return texts.FamilyParachute;
                case Family.HeatVent: return texts.FamilyHeatVent;
                case Family.Exhaust: return texts.FamilyExhaust;
                case Family.TargetDummy: return texts.FamilyTargetDummy;
                case Family.Kitchen: return texts.FamilyKitchen;
                case Family.Planter: return texts.FamilyPlanter;
                case Family.Contracts: return texts.FamilyContracts;
                case Family.MedicalRoom: return texts.FamilyMedicalRoom;
                case Family.Warhead: return texts.FamilyWarhead;
                default: return "";
            }
        }
    }
}
