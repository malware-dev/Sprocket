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
    /// Testing row "A kept system counts only on its own block's screen", in the game client on the Sprocket Kept Count blueprint:
    /// the automation panel set on Kept Automation's own screen and on Kept Panel, and a picture of both for the count on each.
    /// </summary>
    public sealed class KeptCountClientTest
    {
        // The automation panel's id among the game's screen apps (StatusApp.Id).
        const string StatusApp = "Sprocket_Status";

        static readonly Vector3D At = new Vector3D(0, 0, -260);

        readonly string _blueprint;
        readonly string _shots;
        readonly GameTestReport _report;

        /// <summary>The test on the blueprints in <paramref name="blueprints" />, writing pictures to <paramref name="shots" /> and answering into <paramref name="report" />.</summary>
        public KeptCountClientTest(string blueprints, string shots, GameTestReport report)
        {
            _blueprint = Path.Combine(blueprints ?? "", "Sprocket Kept Count", "bp.sbc");
            _shots = shots;
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var pasted = new PastedBlueprint(_blueprint, At);
            var automation = pasted.Find("Kept Automation") as IMyTextSurfaceProvider;
            var panel = pasted.Find("Kept Panel") as IMyTextPanel;
            if (automation == null || panel == null)
            {
                _report.Fail("kept count", "Kept Automation or Kept Panel is missing after the paste");
                yield break;
            }

            // Five seconds for Sprocket to take up the pasted programmable block and its two logic systems.
            yield return 5 * PluginUpdates.PerSecond;
            foreach (var surface in new[] { automation.GetSurface(0), panel })
            {
                surface.ContentType = ContentType.SCRIPT;
                surface.Script = StatusApp;
            }

            var middle = (((IMyTerminalBlock)automation).GetPosition() + panel.GetPosition()) / 2;
            // The panel lies on the deck with its screen to the panel's back, which the scenario lays facing up, so it is seen from above.
            var eye = middle + panel.WorldMatrix.Backward * 6 + panel.WorldMatrix.Up * 1.5;
            MySession.Static.SetCameraController(MyCameraControllerEnum.Spectator, null, eye);
            MySpectatorCameraController.Static.SetTarget(middle, panel.WorldMatrix.Up);
            yield return 3 * PluginUpdates.PerSecond;

            var picture = Path.Combine(_shots, "kept-count-panel.png");
            if (File.Exists(picture))
                File.Delete(picture);

            MyRenderProxy.TakeScreenshot(Vector2.One, picture, false, false, false);
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("kept count picture of the panel", File.Exists(picture), File.Exists(picture) ? picture : $"no picture at {picture}");

            // The block's own screen, close, from the side it faces as the scenario lays it: towards the panel's up, seen from above.
            var block = (IMyTerminalBlock)automation;
            var close = block.GetPosition() + panel.WorldMatrix.Up * 2.5;
            MySession.Static.SetCameraController(MyCameraControllerEnum.Spectator, null, close);
            MySpectatorCameraController.Static.SetTarget(block.GetPosition(), panel.WorldMatrix.Backward);
            yield return 3 * PluginUpdates.PerSecond;

            var own = Path.Combine(_shots, "kept-count-block.png");
            if (File.Exists(own))
                File.Delete(own);

            MyRenderProxy.TakeScreenshot(Vector2.One, own, false, false, false);
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("kept count picture of the block's own screen", File.Exists(own), File.Exists(own) ? own : $"no picture at {own}");
        }
    }
}
