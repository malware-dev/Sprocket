using System.Collections.Generic;
using System.IO;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using VRage.Game;
using VRageMath;
using VRageRender;

namespace Sprocket.GameTests
{
    /// <summary>
    /// The first client test: the Sprocket Piston Status blueprint pasted in front of the spectator camera and a picture taken of
    /// it, which shows the client can paste, aim and take pictures for the visual rows.
    /// </summary>
    public sealed class SceneShotClientTest
    {
        static readonly Vector3D At = new Vector3D(0, 0, -200);

        readonly string _blueprint;
        readonly string _shots;
        readonly GameTestReport _report;

        /// <summary>The test on the blueprints in <paramref name="blueprints" />, writing pictures to <paramref name="shots" /> and answering into <paramref name="report" />.</summary>
        public SceneShotClientTest(string blueprints, string shots, GameTestReport report)
        {
            _blueprint = Path.Combine(blueprints ?? "", "Sprocket Piston Status", "bp.sbc");
            _shots = shots;
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            if (!File.Exists(_blueprint) || string.IsNullOrEmpty(_shots))
            {
                _report.Fail("client scene", $"no blueprint at {_blueprint} or no folder for pictures");
                yield break;
            }

            var pasted = new PastedBlueprint(_blueprint, At);
            var piston = pasted.Find("Status Piston");
            _report.Check("client paste", piston != null, piston == null ? "Status Piston is missing after the paste" : $"Status Piston stands at {piston.GetPosition()}");
            if (piston == null)
                yield break;

            var look = piston.GetPosition();
            MySession.Static.SetCameraController(MyCameraControllerEnum.Spectator, null, look + new Vector3D(10, 6, 14));
            MySpectatorCameraController.Static.SetTarget(look, Vector3D.Up);
            yield return 3 * PluginUpdates.PerSecond;

            var camera = MySession.Static.CameraController;
            _report.Note($"the camera is {camera?.GetType().Name ?? "none"} before the picture");
            if (!(camera is MySpectatorCameraController))
            {
                MySession.Static.SetCameraController(MyCameraControllerEnum.Spectator, null, look + new Vector3D(10, 6, 14));
                MySpectatorCameraController.Static.SetTarget(look, Vector3D.Up);
                yield return PluginUpdates.PerSecond;
                _report.Note($"set again, the camera is {MySession.Static.CameraController?.GetType().Name ?? "none"}");
            }

            Directory.CreateDirectory(_shots);
            var picture = Path.Combine(_shots, "piston-status.png");
            if (File.Exists(picture))
                File.Delete(picture);

            // The pictures are of the world, without the character's HUD over it.
            MyHud.MinimalHud = true;
            yield return PluginUpdates.PerSecond;
            MyRenderProxy.TakeScreenshot(Vector2.One, picture, false, false, false);
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("client picture", File.Exists(picture), File.Exists(picture) ? $"{picture}, {new FileInfo(picture).Length} bytes" : $"no picture at {picture}");
        }
    }
}
