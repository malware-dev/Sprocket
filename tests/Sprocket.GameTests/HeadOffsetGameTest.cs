using System;
using System.Collections.Generic;
using System.Globalization;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Where the game puts the head of a large piston, rotor and hinge, each built by the test at rest and given its head by the
    /// game's own action: the head's grid and the head block in it, said in the base block's own axes, and checked against where
    /// the harness's blueprints put it.
    /// </summary>
    public sealed class HeadOffsetGameTest
    {
        // How far up its base's up the harness's blueprints put each head's grid (Blueprint.Heads).
        static readonly Dictionary<string, double> Expected = new Dictionary<string, double>
        {
            ["LargePistonBase"] = 1.41,
            ["LargeStator"] = 0.421,
            ["LargeHinge"] = 0
        };

        readonly GameTestReport _report;

        /// <summary>The test, answering into <paramref name="report" />.</summary>
        public HeadOffsetGameTest(GameTestReport report)
        {
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var bases = new List<IMyMechanicalConnectionBlock>();
            var at = new Vector3D(0, 0, 900);
            foreach (var kind in new[] { "LargePistonBase", "LargeStator", "LargeHinge" })
            {
                var block = kind == "LargePistonBase"
                    ? (MyObjectBuilder_CubeBlock)BuiltGrid.Block<MyObjectBuilder_ExtendedPistonBase>(kind, new Vector3I(1, 0, 0))
                    : kind == "LargeStator"
                        ? BuiltGrid.Block<MyObjectBuilder_MotorStator>(kind, new Vector3I(1, 0, 0))
                        : BuiltGrid.Block<MyObjectBuilder_MotorAdvancedStator>(kind, new Vector3I(1, 0, 0));
                var grid = new BuiltGrid(at, BuiltGrid.Battery(Vector3I.Zero), block);
                bases.Add(grid.Find<IMyMechanicalConnectionBlock>());
                at += new Vector3D(0, 0, 40);
            }

            yield return 3 * PluginUpdates.PerSecond;
            foreach (var mechanical in bases)
            {
                var subtype = mechanical.BlockDefinition.SubtypeName;
                var add = subtype == "LargePistonBase" ? "Add Top Part" : subtype == "LargeHinge" ? "AddHingeTopPart" : "AddRotorTopPart";
                mechanical.GetActionWithName(add)?.Apply(mechanical);
            }

            yield return 3 * PluginUpdates.PerSecond;
            foreach (var mechanical in bases)
            {
                var top = mechanical.Top;
                if (top == null)
                {
                    _report.Fail("head offset", $"{mechanical.BlockDefinition.SubtypeName} got no head");
                    continue;
                }

                var frame = mechanical.WorldMatrix;
                var grid = top.CubeGrid;
                var slim = (IMySlimBlock)top.SlimBlock;
                var offset = grid.WorldMatrix.Translation - frame.Translation;
                var rise = Vector3D.Dot(offset, frame.Up);
                var aside = (offset - frame.Up * rise).Length();
                var expected = Expected[mechanical.BlockDefinition.SubtypeName];
                _report.Check($"the head of a {mechanical.BlockDefinition.SubtypeName} where blueprints put it",
                    Math.Abs(rise - expected) < 0.01 && aside < 0.01 && Vector3D.Dot(grid.WorldMatrix.Up, frame.Up) > 0.999 && Vector3D.Dot(grid.WorldMatrix.Forward, frame.Forward) > 0.999,
                    string.Format(CultureInfo.InvariantCulture, "its grid stands {0:0.###} m up the base and {1:0.###} m aside, where blueprints put it {2} m up", rise, aside, expected));
                _report.Note(string.Format(CultureInfo.InvariantCulture,
                    "{0} at cell {1} {2}/{3}: head {4} at cell {5} min {6} {7}/{8} on its grid; grid origin {9}, forward {10}, up {11}; head block {12}",
                    mechanical.BlockDefinition.SubtypeName, mechanical.Position, mechanical.Orientation.Forward, mechanical.Orientation.Up,
                    top.BlockDefinition.SubtypeName, top.Position, slim.Min, top.Orientation.Forward, top.Orientation.Up,
                    Local(grid.WorldMatrix.Translation - frame.Translation, frame), Local(grid.WorldMatrix.Forward, frame), Local(grid.WorldMatrix.Up, frame),
                    Local(top.WorldMatrix.Translation - frame.Translation, frame)));
            }
        }

        /// <summary><paramref name="vector" /> in the axes of <paramref name="frame" />, as right, up and back, to the millimeter.</summary>
        static string Local(Vector3D vector, MatrixD frame)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:0.###}, {1:0.###}, {2:0.###})",
                Vector3D.Dot(vector, frame.Right), Vector3D.Dot(vector, frame.Up), Vector3D.Dot(vector, frame.Backward));
        }
    }
}
