namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>The node a kind of block hangs under in the picker's list of kinds.</summary>
    public enum Family
    {
        /// <summary>Hangs under nothing: a type the game uses for unrelated blocks, or one not listed.</summary>
        None,

        /// <summary>The game's Door, AirtightSlideDoor, AdvancedDoor.</summary>
        Door,

        /// <summary>The game's AirtightHangarDoor.</summary>
        HangarDoor,

        /// <summary>The game's InteriorLight, ReflectorLight, Searchlight, EmissiveBlock.</summary>
        Light,

        /// <summary>The game's TextPanel, LCDPanelsBlock.</summary>
        Screen,

        /// <summary>The game's LargeGatlingTurret, LargeMissileTurret, InteriorTurret.</summary>
        Turret,

        /// <summary>The game's SmallGatlingGun, SmallMissileLauncher, SmallMissileLauncherReload.</summary>
        Weapon,

        /// <summary>The game's MotorStator, MotorAdvancedStator.</summary>
        Rotor,

        /// <summary>The game's GravityGenerator, GravityGeneratorSphere.</summary>
        GravityGenerator,

        /// <summary>The game's FlightMovementBlock, PathRecorderBlock, BasicMissionBlock, OffensiveCombatBlock, DefensiveCombatBlock.</summary>
        AiBlock,

        /// <summary>The game's Projector.</summary>
        Projector,

        /// <summary>The game's Thrust.</summary>
        Thruster,

        /// <summary>The game's BatteryBlock.</summary>
        Battery,

        /// <summary>The game's OxygenTank.</summary>
        Tank,

        /// <summary>The game's Cockpit.</summary>
        Cockpit,

        /// <summary>The game's CryoChamber.</summary>
        Cryo,

        /// <summary>The game's RemoteControl.</summary>
        RemoteControl,

        /// <summary>The game's AirVent.</summary>
        AirVent,

        /// <summary>The game's ShipConnector.</summary>
        Connector,

        /// <summary>The game's MergeBlock.</summary>
        MergeBlock,

        /// <summary>The game's LandingGear.</summary>
        LandingGear,

        /// <summary>The game's ExtendedPistonBase.</summary>
        Piston,

        /// <summary>The game's MotorSuspension.</summary>
        Suspension,

        /// <summary>The game's Assembler.</summary>
        Assembler,

        /// <summary>The game's Refinery.</summary>
        Refinery,

        /// <summary>The game's SurvivalKit.</summary>
        SurvivalKit,

        /// <summary>The game's OxygenGenerator.</summary>
        GasGenerator,

        /// <summary>The game's OxygenFarm.</summary>
        OxygenFarm,

        /// <summary>The game's HydrogenEngine.</summary>
        HydrogenEngine,

        /// <summary>The game's Reactor.</summary>
        Reactor,

        /// <summary>The game's SolarPanel.</summary>
        SolarPanel,

        /// <summary>The game's WindTurbine.</summary>
        WindTurbine,

        /// <summary>The game's ConveyorSorter.</summary>
        Sorter,

        /// <summary>The game's Drill.</summary>
        Drill,

        /// <summary>The game's ShipWelder.</summary>
        Welder,

        /// <summary>The game's ShipGrinder.</summary>
        Grinder,

        /// <summary>The game's ButtonPanel.</summary>
        ButtonPanel,

        /// <summary>The game's TimerBlock.</summary>
        Timer,

        /// <summary>The game's SensorBlock.</summary>
        Sensor,

        /// <summary>The game's MyProgrammableBlock.</summary>
        ProgrammableBlock,

        /// <summary>The game's RadioAntenna.</summary>
        Antenna,

        /// <summary>The game's LaserAntenna.</summary>
        LaserAntenna,

        /// <summary>The game's Beacon.</summary>
        Beacon,

        /// <summary>The game's CameraBlock.</summary>
        Camera,

        /// <summary>The game's OreDetector.</summary>
        OreDetector,

        /// <summary>The game's Gyro.</summary>
        Gyroscope,

        /// <summary>The game's JumpDrive.</summary>
        JumpDrive,

        /// <summary>The game's CargoContainer.</summary>
        Cargo,

        /// <summary>The game's Collector.</summary>
        Collector,

        /// <summary>The game's UpgradeModule.</summary>
        UpgradeModule,

        /// <summary>The game's Decoy.</summary>
        Decoy,

        /// <summary>The game's SoundBlock.</summary>
        SoundBlock,

        /// <summary>The game's Jukebox.</summary>
        Jukebox,

        /// <summary>The game's TurretControlBlock.</summary>
        TurretController,

        /// <summary>The game's EventControllerBlock.</summary>
        EventController,

        /// <summary>The game's BroadcastController.</summary>
        BroadcastController,

        /// <summary>The game's TransponderBlock.</summary>
        ActionRelay,

        /// <summary>The game's VendingMachine.</summary>
        VendingMachine,

        /// <summary>The game's StoreBlock.</summary>
        Store,

        /// <summary>The game's SafeZoneBlock.</summary>
        SafeZone,

        /// <summary>The game's VirtualMass.</summary>
        ArtificialMass,

        /// <summary>The game's SpaceBall.</summary>
        SpaceBall,

        /// <summary>The game's EmotionControllerBlock.</summary>
        EmotionController,

        /// <summary>The game's Parachute.</summary>
        Parachute,

        /// <summary>The game's HeatVentBlock.</summary>
        HeatVent,

        /// <summary>The game's ExhaustBlock.</summary>
        Exhaust,

        /// <summary>The game's TargetDummyBlock.</summary>
        TargetDummy,

        /// <summary>The game's Kitchen.</summary>
        Kitchen,

        /// <summary>The game's Planter.</summary>
        Planter,

        /// <summary>The game's ContractBlock.</summary>
        Contracts,

        /// <summary>The game's MedicalRoom.</summary>
        MedicalRoom,

        /// <summary>The game's Warhead.</summary>
        Warhead
    }
}
