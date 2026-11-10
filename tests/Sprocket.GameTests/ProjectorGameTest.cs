using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing row "Projector rotation steps", run in the real game: a projector's terminal pitch holds degrees, one step being 90 on a
    /// plain projector and 1 on the scaling one, which is what Sprocket shows it as.
    /// </summary>
    public sealed class ProjectorGameTest
    {
        readonly GameTestReport _report;

        /// <summary>The test, answering into <paramref name="report" />.</summary>
        public ProjectorGameTest(GameTestReport report)
        {
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var plain = Build("LargeProjector", new Vector3D(0, 0, 200));
            var scaling = Build("LargeBlockConsole", new Vector3D(0, 0, 260));
            yield return PluginUpdates.PerSecond;

            if (plain == null || scaling == null)
            {
                _report.Fail("projectors", "a projector was not built");
                yield break;
            }

            // One step each way the game's own terminal would set it: RotX given in the unit its slider takes.
            plain.SetValueFloat("RotX", 90);
            scaling.SetValueFloat("RotX", 1);
            yield return PluginUpdates.PerSecond / 3;

            Read(plain, "plain projector", 90);
            Read(scaling, "scaling projector", 1);
        }

        // Sprocket shows RotX as it is, in degrees, moving by the definition's step; ProductionTests holds Sprocket to that, and
        // this holds the game to RotX being degrees.
        void Read(IMyProjector projector, string check, int step)
        {
            var said = projector.GetValueFloat("RotX");
            var stored = projector.ProjectionRotation;
            _report.Check(check, said == step && stored == new Vector3I(0, 1, 0),
                $"RotX set to {step} reads back {said}, and the projector stores {stored}: RotX is in degrees, one step of {step}");
        }

        static IMyProjector Build(string subtype, Vector3D at)
        {
            var block = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Projector>(subtype);
            block.Min = Vector3I.Zero;
            var grid = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();
            grid.GridSizeEnum = MyCubeSize.Large;
            grid.IsStatic = true;
            grid.PersistentFlags = MyPersistentEntityFlags2.InScene;
            grid.PositionAndOrientation = new MyPositionAndOrientation(at, Vector3.Forward, Vector3.Up);
            grid.CubeBlocks.Add(block);
            var built = (VRage.Game.ModAPI.IMyCubeGrid)MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(grid);
            var blocks = new List<VRage.Game.ModAPI.IMySlimBlock>();
            built.GetBlocks(blocks, slim => slim.FatBlock is IMyProjector);
            return blocks.Count == 0 ? null : (IMyProjector)blocks[0].FatBlock;
        }
    }
}
