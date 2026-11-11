using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing row "A sensor near planet ground with only asteroids on", run in the real game: a moon and an asteroid spawned by
    /// the test far from everything else, and three sensors that detect asteroids and nothing else, one just over the moon's
    /// ground, one beside the asteroid and one in empty space, with a fourth that detects stations to show they see at all.
    /// </summary>
    /// <remarks>Runs last, since the moon it spawns stays and pulls whatever comes near it.</remarks>
    public sealed class SensorGameTest
    {
        static readonly Vector3D Moon = new Vector3D(2000000, 0, 0);
        static readonly Vector3D Rock = new Vector3D(0, 0, -2000);
        static readonly Vector3D Space = new Vector3D(0, 0, -4000);

        readonly GameTestReport _report;

        /// <summary>The test, answering into <paramref name="report" />.</summary>
        public SensorGameTest(GameTestReport report)
        {
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var planet = MyAPIGateway.Session.VoxelMaps.SpawnPlanet("Moon", 19000, 1, Moon) as MyPlanet;
            var rock = MyAPIGateway.Session.VoxelMaps.CreateProceduralVoxelMap(1, 60, MatrixD.CreateTranslation(Rock), 0);
            if (planet == null)
            {
                _report.Fail("sensor moon", "the game spawned no moon");
                yield break;
            }

            yield return 5 * PluginUpdates.PerSecond;

            var center = planet.PositionComp.WorldAABB.Center;
            var probe = center + Vector3D.Up * 20000;
            var ground = planet.GetClosestSurfacePointGlobal(ref probe);
            var up = Vector3D.Normalize(ground - center);

            var onGround = Sensor(ground + up * 3);
            var byRock = Sensor(Rock + Vector3D.Up * 30);
            var alone = Sensor(Space);
            if (onGround == null || byRock == null || alone == null)
            {
                _report.Fail("sensor", "a sensor was not built");
                yield break;
            }

            // The game builds a voxel's physics only near something moving, as a player or a ship is in play, and a sensor finds a
            // voxel through that physics; a grid free to move beside each sensor stands in for them.
            new BuiltGrid(ground + up * 3 + Vector3D.Normalize(Vector3D.CalculatePerpendicularVector(up)) * 20, false, BuiltGrid.Battery(Vector3I.Zero));
            new BuiltGrid(Rock + Vector3D.Up * 30 + Vector3D.Right * 20, false, BuiltGrid.Battery(Vector3I.Zero));

            yield return 5 * PluginUpdates.PerSecond;

            // A sensor that sees stations, beside one, to show the sensors see at all.
            var station = Sensor(Space + new Vector3D(0, 0, 200));
            station.DetectStations = true;
            station.DetectAsteroids = false;
            new BuiltGrid(Space + new Vector3D(0, 0, 210), BuiltGrid.Battery(Vector3I.Zero));
            yield return 5 * PluginUpdates.PerSecond;
            _report.Note($"the asteroid is {(rock == null ? "missing" : $"{rock.PositionComp.WorldAABB.Size.Length():0} m across at {Vector3D.Distance(rock.PositionComp.WorldAABB.Center, Rock):0} m from where it was asked")}");
            _report.Note($"the sensor by the asteroid is working: {byRock.IsWorking}, reaching {byRock.LeftExtend}, {byRock.RightExtend}, {byRock.TopExtend}, {byRock.BottomExtend}, {byRock.FrontExtend}, {byRock.BackExtend} m");
            _report.Note($"a sensor beside a station with only stations on is {Said(station)}");
            yield return 10 * PluginUpdates.PerSecond;
            var sections = 0;
            var near = 0;
            foreach (var entity in MyEntities.GetEntities())
            {
                // A planet's physics sections are the game's own internal type, so they are found by its name.
                var physics = entity as MyVoxelBase;
                if (physics == null || physics.GetType().Name != "MyVoxelPhysics" || physics.RootVoxel != planet)
                    continue;

                sections++;
                if (physics.PositionComp.WorldAABB.Contains(onGround.GetPosition()) != ContainmentType.Disjoint)
                    near++;
            }

            _report.Note($"the moon has {sections} physics sections, {near} of them around the sensor over its ground");
            _report.Note($"the moon's ground is {Vector3D.Distance(ground, center):0} m from its center, and the sensor over it {Vector3D.Distance(onGround.GetPosition(), ground):0} m above it");
            _report.Check("a sensor in empty space with only asteroids on", !alone.IsActive, $"it is {(alone.IsActive ? "active" : "inactive")}");
            _report.Check("a sensor beside an asteroid with only asteroids on", byRock.IsActive, $"it is {Said(byRock)}");
            // The game counts a planet's ground as no asteroid, measured: inactive with the moon's physics around it.
            _report.Check("a sensor over a moon's ground with only asteroids on stays inactive", !onGround.IsActive, $"it is {Said(onGround)}");
        }

        /// <summary>A sensor on a grid of its own at <paramref name="at" />, reaching 15 m every way, that detects asteroids and nothing else.</summary>
        static IMySensorBlock Sensor(Vector3D at)
        {
            var sensor = BuiltGrid.Block<MyObjectBuilder_SensorBlock>("LargeBlockSensor", new Vector3I(1, 0, 0));
            sensor.FieldMin = new Vector3(-15, -15, -15);
            sensor.FieldMax = new Vector3(15, 15, 15);
            var built = new BuiltGrid(at, BuiltGrid.Battery(Vector3I.Zero), sensor).Find<IMySensorBlock>();
            if (built == null)
                return null;

            built.DetectAsteroids = true;
            built.DetectPlayers = false;
            built.DetectFloatingObjects = false;
            built.DetectSmallShips = false;
            built.DetectLargeShips = false;
            built.DetectStations = false;
            built.DetectSubgrids = false;
            return built;
        }

        static string Said(IMySensorBlock sensor)
        {
            var last = sensor.LastDetectedEntity;
            return sensor.IsActive ? $"active, last seeing {last.Name} of {last.Type}" : "inactive";
        }
    }
}
