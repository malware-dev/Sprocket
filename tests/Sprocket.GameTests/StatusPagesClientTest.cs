using System.Collections.Generic;
using System.IO;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using VRageRender;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing row #50's turning pages, in the game client on the Sprocket Status Pages blueprint: the automation panel set on
    /// Pages Panel, and a picture of it every half second across several turns, so some catch a page sliding out as the next
    /// slides in.
    /// </summary>
    public sealed class StatusPagesClientTest
    {
        // The automation panel's id among the game's screen apps (StatusApp.Id).
        const string StatusApp = "Sprocket_Status";

        // A turn slides for 24 ticks, 0.4 s, and turns come about eight seconds apart in the game. A full 4K picture takes 1.8 s
        // to write (measured 2026-11-13), longer than a slide, so these are a quarter of the size, asked for every tenth of a
        // second across four turns; a request made while one is being written is dropped.
        const int Pictures = 320;

        static readonly Vector2 Quarter = new Vector2(0.25f, 0.25f);

        static readonly Vector3D At = new Vector3D(0, 0, -400);

        readonly string _blueprint;
        readonly string _shots;
        readonly GameTestReport _report;

        /// <summary>The test on the blueprints in <paramref name="blueprints" />, writing pictures to <paramref name="shots" /> and answering into <paramref name="report" />.</summary>
        public StatusPagesClientTest(string blueprints, string shots, GameTestReport report)
        {
            _blueprint = Path.Combine(blueprints ?? "", "Sprocket Status Pages", "bp.sbc");
            _shots = shots;
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var pasted = new PastedBlueprint(_blueprint, At);
            var panel = pasted.Find("Pages Panel") as IMyTextPanel;
            if (panel == null)
            {
                _report.Fail("status pages", "Pages Panel is missing after the paste");
                yield break;
            }

            // Five seconds for Sprocket to take up the pasted programmable block and its logic systems.
            yield return 5 * PluginUpdates.PerSecond;
            panel.ContentType = ContentType.SCRIPT;
            panel.Script = StatusApp;

            // The panel lies on the deck with its screen to the panel's back, which the scenario lays facing up, so it is seen
            // from above, near enough for the panel to draw its motion.
            var middle = panel.GetPosition();
            var eye = middle + panel.WorldMatrix.Backward * 4 + panel.WorldMatrix.Up * 0.5;
            MySession.Static.SetCameraController(MyCameraControllerEnum.Spectator, null, eye);
            MySpectatorCameraController.Static.SetTarget(middle, panel.WorldMatrix.Up);
            yield return 3 * PluginUpdates.PerSecond;

            for (var i = 0; i < Pictures; i++)
            {
                var picture = Path.Combine(_shots, $"status-pages-{i:000}.png");
                if (File.Exists(picture))
                    File.Delete(picture);

                MyRenderProxy.TakeScreenshot(Quarter, picture, false, false, false);
                yield return PluginUpdates.PerSecond / 10;
            }

            yield return PluginUpdates.PerSecond;
            var taken = 0;
            for (var i = 0; i < Pictures; i++)
            {
                if (File.Exists(Path.Combine(_shots, $"status-pages-{i:000}.png")))
                    taken++;
            }

            _report.Check("status pages pictures", taken > 0, $"{taken} of {Pictures} pictures of Pages Panel written to {_shots}");
        }
    }
}
