using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>Every capability Sprocket knows, in the order a kind of block is described by them.</summary>
    public sealed class Capabilities
    {
        readonly Capability[] _blocks =
        {
            new Door(),
            new Vent(),
            new ButtonPanel(),
            new Rotor(),
            new Piston(),
            new Suspension(),
            new LandingGear(),
            new MergeBlock(),
            new Connector(),
            new Parachute(),
            new Battery(),
            new Reactor(),
            new SolarPanel(),
            new WindTurbine(),
            new Thruster(),
            new Gyroscope(),
            new JumpDrive(),
            new GravityGenerator(),
            new ArtificialMass(),
            new SpaceBall(),
            new Tank(),
            new GasGenerator(),
            new OxygenFarm(),
            new GasEngine(),
            new ExhaustPipe(),
            new HeatVent(),
            new CryoChamber(),
            new RespawnPoint(),
            new Speaker(),
            new Jukebox(),
            new Light(),
            new Searchlight(),
            new TextPanel(),
            new EmotionController(),
            new Camera(),
            new Timer(),
            new Sensor(),
            new Cockpit(),
            new Weapon(),
            new TargetingBlock(),
            new Turret(),
            new TurretController(),
            new Railgun(),
            new Warhead(),
            new TargetDummy(),
            new AiBlock(),
            new CombatBlock(),
            new OffensiveCombat(),
            new DefensiveCombat(),
            new FlightMovement(),
            new PathRecorder(),
            new BasicMission(),
            new RemoteControl(),
            new ShipController(),
            new Production(),
            new Sorter(),
            new Drill(),
            new ShipTool(),
            new Projector(),
            new UpgradeModule(),
            new StoreBlock(),
            new ConveyorUser(),
            new OreDetector(),
            new ProgrammableBlock(),
            new Beacon(),
            new RadioAntenna(),
            new LaserAntenna(),
            new BroadcastController(),
            new Transponder(),
            new SafeZone(),
            new SeparationCharge(),
            new MicroEmitter(),
            new FarmPlot(),
            new ServicesTerminal()
        };

        readonly Capability[] _floor =
        {
            new Functional(),
            new Terminal(),
            new PowerConsumer(),
            new BlockIntegrity(),
            new Inventory()
        };

        /// <summary>What particular kinds of block are.</summary>
        public IList<Capability> Blocks => _blocks;

        /// <summary>What every block that claims it is, described after what it is in particular.</summary>
        public IList<Capability> Floor => _floor;
    }
}
