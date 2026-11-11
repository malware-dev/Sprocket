using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing row "How a loaded wheel takes a height change", run in the real game on a ship built by the test: a plate on four
    /// large wheels carrying a container of stone and four artificial masses, stood on a floor with a gravity generator. Each wheel's height is changed a long
    /// way, one wheel and then all four, and a short way, and the ship is watched until it settles: how long that takes, how fast it
    /// moves up, and how far past where it ends it goes.
    /// </summary>
    public sealed class WheelHeightGameTest
    {
        const int Stone = 100000;

        static readonly Vector3D At = new Vector3D(0, 0, 700);

        readonly GameTestReport _report;
        readonly List<IMyMotorSuspension> _wheels = new List<IMyMotorSuspension>();
        IMyCubeGrid _ship;

        /// <summary>The test, answering into <paramref name="report" />.</summary>
        public WheelHeightGameTest(GameTestReport report)
        {
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        /// <remarks>Allows gravity drives in the world while it runs: a generator pulls an artificial mass only where they are allowed.</remarks>
        public IEnumerator<int> Run()
        {
            var settings = MySession.Static.Settings;
            var allowed = settings.AllowGravityDrives;
            settings.AllowGravityDrives = true;
            try
            {
                var steps = Measure();
                while (steps.MoveNext())
                    yield return steps.Current;
            }
            finally
            {
                settings.AllowGravityDrives = allowed;
            }
        }

        IEnumerator<int> Measure()
        {
            var floor = new List<MyObjectBuilder_CubeBlock>();
            for (var x = -5; x <= 7; x++)
            for (var z = -4; z <= 8; z++)
                floor.Add(BuiltGrid.Armor(new Vector3I(x, 0, z)));

            floor.Add(BuiltGrid.Battery(new Vector3I(0, -1, 0)));
            var gravity = BuiltGrid.Block<MyObjectBuilder_GravityGenerator>("", new Vector3I(1, -1, 2));
            gravity.FieldSize = new SerializableVector3(150, 150, 150);
            gravity.GravityAcceleration = 9.81f;
            floor.Add(gravity);
            var generator = new BuiltGrid(At, floor.ToArray()).Find<IMyGravityGenerator>();

            // The plate's cells are 6 m over the floor's top, so the wheels fall clear onto it.
            var ship = new List<MyObjectBuilder_CubeBlock>();
            for (var x = 0; x <= 2; x++)
            for (var z = 0; z <= 4; z++)
                ship.Add(BuiltGrid.Armor(new Vector3I(x, 0, z)));

            ship.Add(BuiltGrid.Battery(new Vector3I(1, 1, 0)));
            // A gravity generator pulls a ship only through its artificial mass, switched off until the wheels are on so the ship
            // hangs clear of the floor while they are put on.
            foreach (var corner in new[] { new Vector3I(0, 1, 0), new Vector3I(2, 1, 0), new Vector3I(0, 1, 4), new Vector3I(2, 1, 4) })
            {
                var mass = BuiltGrid.Block<MyObjectBuilder_VirtualMass>("VirtualMassLarge", corner);
                mass.Enabled = false;
                ship.Add(mass);
            }

            var container = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CargoContainer>("LargeBlockLargeContainer");
            container.Min = new Vector3I(0, 1, 1);
            ship.Add(container);
            foreach (var z in new[] { 0, 4 })
            {
                ship.Add(Suspension(new Vector3I(-2, 0, z), Base6Directions.Direction.Left));
                ship.Add(Suspension(new Vector3I(3, 0, z), Base6Directions.Direction.Right));
            }

            var built = new BuiltGrid(At + new Vector3D(0, 7.5, 0), false, ship.ToArray());
            _ship = built.Grid;
            _wheels.AddRange(built.All<IMyMotorSuspension>());
            var cargo = built.Find<IMyCargoContainer>();
            if (_wheels.Count != 4 || cargo == null)
            {
                _report.Fail("wheeled ship", $"{_wheels.Count} of 4 suspensions and {(cargo == null ? "no" : "the")} container were built");
                yield break;
            }

            cargo.GetInventory(0).AddItems(Stone, MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>("Stone"));

            // A wheel on each suspension, by the game's own action, once the grid has settled (PistonStatusGameTest).
            yield return 3 * PluginUpdates.PerSecond;
            foreach (var wheel in _wheels)
            {
                wheel.Height = 0;
                var add = wheel.GetActionWithName("Add Top Part");
                if (add == null)
                {
                    var names = new List<Sandbox.ModAPI.Interfaces.ITerminalAction>();
                    wheel.GetActions(names);
                    _report.Fail("wheel", "the suspension has no Add Top Part; its actions are " + string.Join(", ", names.Select(action => action.Id)));
                    yield break;
                }

                add.Apply(wheel);
            }

            yield return 3 * PluginUpdates.PerSecond;
            if (_wheels.Any(wheel => wheel.Top == null))
            {
                _report.Fail("wheel", $"{_wheels.Count(wheel => wheel.Top != null)} of 4 suspensions got a wheel");
                yield break;
            }

            foreach (var mass in built.All<IMyVirtualMass>())
                mass.Enabled = true;

            // A grid made at rest sleeps in the physics engine, where gravity moves nothing until something wakes it.
            _ship.Physics.LinearVelocity = _ship.WorldMatrix.Down * 0.1;

            var landed = Settle(new List<IMyMotorSuspension>(), 0f);
            while (landed.MoveNext())
                yield return landed.Current;

            _report.Note($"the gravity generator is {(generator == null ? "missing" : $"working: {((IMyCubeBlock)generator).IsWorking}, field {generator.FieldSize}, {generator.GravityAcceleration} m/s2")}");
            _report.Note($"the ship stands on its wheels at {Rise:0.##} m, with {Stone} kg of stone, gravity {_gravity:0.##} m/s2, {Pose}");
            if (_gravity < 1)
            {
                _report.Fail("wheeled ship", "the ship is in no gravity, so its wheels carry nothing");
                yield break;
            }

            foreach (var change in new[] { new Change("one wheel from 0 to 1.3 m", 1, 1.3f), new Change("one wheel back to 0", 1, 0f),
                new Change("all four from 0 to 1.3 m", 4, 1.3f), new Change("all four back to 0", 4, 0f), new Change("all four from 0 to 0.3 m", 4, 0.3f) })
            {
                var settle = Settle(_wheels.Take(change.Wheels).ToList(), change.Height);
                while (settle.MoveNext())
                    yield return settle.Current;

                _report.Note(string.Format(CultureInfo.InvariantCulture, "{0}: the ship moved {1:0.##} m, settled after {2:0.#} s, rose at most {3:0.##} m/s and went {4:0.##} m past where it ended, {5}",
                    change.Name, _moved, _settled, _fastest, _past, Pose));
            }

            _report.Check("wheels taking a height change settle", _settled < 15, $"the last change settled after {_settled:0.#} s");
        }

        // Measured up the floor, which is built upright, so a ship that tilts reads its true height.
        double Rise => _ship.Physics.CenterOfMassWorld.Y - At.Y;

        string Pose => string.Format(CultureInfo.InvariantCulture, "its up {0:0.##} of the floor's, moving {1:0.##} m/s, {2} wheels attached", Vector3D.Dot(_ship.WorldMatrix.Up, Vector3D.Up),
            _ship.Physics.LinearVelocity.Length(), _wheels.Count(wheel => wheel.Top != null && wheel.Top.CubeGrid.Physics != null));

        double _gravity;
        double _moved;
        double _settled;
        double _fastest;
        double _past;

        /// <summary>
        /// Sets <paramref name="wheels" /> to <paramref name="height" /> and watches the ship every tenth of a second until it has
        /// moved less than a centimeter for a whole second, for at most fifteen seconds.
        /// </summary>
        IEnumerator<int> Settle(List<IMyMotorSuspension> wheels, float height)
        {
            foreach (var wheel in wheels)
                wheel.Height = height;

            var start = Rise;
            var heights = new List<double> { start };
            _fastest = 0;
            var still = 0;
            var tenth = 0;
            for (; tenth < 150 && still < 10; tenth++)
            {
                yield return PluginUpdates.PerSecond / 10;
                var now = Rise;
                var step = now - heights[heights.Count - 1];
                heights.Add(now);
                _fastest = System.Math.Max(_fastest, step * 10);
                still = System.Math.Abs(step) < 0.001 ? still + 1 : 0;
            }

            var end = heights[heights.Count - 1];
            // A grid's own gravity counts only planets; a generator's field is asked for where the ship is, with no planet to weaken it.
            _gravity = MyAPIGateway.Physics.CalculateArtificialGravityAt(_ship.Physics.CenterOfMassWorld, 0f).Length();
            _moved = end - start;
            _settled = (tenth - still) / 10.0;
            _past = end >= start ? heights.Max() - end : end - heights.Min();
        }

        /// <summary>A large 3x3 suspension at <paramref name="cell" />, its wheel to the <paramref name="side" /> and its travel up and down.</summary>
        static MyObjectBuilder_MotorSuspension Suspension(Vector3I cell, Base6Directions.Direction side)
        {
            var suspension = BuiltGrid.Block<MyObjectBuilder_MotorSuspension>("Suspension3x3", cell);
            suspension.BlockOrientation = new SerializableBlockOrientation(Base6Directions.Direction.Down, side);
            return suspension;
        }

        /// <summary>One change of height: its name, how many wheels it is made on, and the height they are set to.</summary>
        struct Change
        {
            public readonly string Name;
            public readonly int Wheels;
            public readonly float Height;

            public Change(string name, int wheels, float height)
            {
                Name = name;
                Wheels = wheels;
                Height = height;
            }
        }
    }
}
