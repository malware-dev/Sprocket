using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>Every capability Sprocket knows, in the order a kind of block is described by them.</summary>
    /// <remarks>
    /// What a block is comes before what every block is, so a door offers "open" above "switch on": the block's own
    /// capabilities first, and the floor every block stands on after them. A block is every capability that claims it.
    /// </remarks>
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
            new Generator(),
            new Farm(),
            new Engine(),
            new Exhaust(),
            new HeatVent(),
            new Cryo(),
            new Respawn(),
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
            new Aiming(),
            new Turret(),
            new TurretController(),
            new Railgun(),
            new Warhead(),
            new TargetDummy(),
            new AiBlock(),
            new Combat(),
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
            new Store(),
            new Conveyors(),
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
            new ServicesTerminal()
        };

        readonly Capability[] _floor =
        {
            new Functional(),
            new Terminal(),
            new Electric(),
            new Damage(),
            new Inventory(),
            new Screen(),
            new Screens()
        };

        /// <summary>What particular kinds of block are.</summary>
        public IList<Capability> Blocks => _blocks;

        /// <summary>What every block that claims it is, described after what it is in particular.</summary>
        public IList<Capability> Floor => _floor;
    }
}
