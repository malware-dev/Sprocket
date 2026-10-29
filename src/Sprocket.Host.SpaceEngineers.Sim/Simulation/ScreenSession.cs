using System.Collections.Generic;
using Lumel.Sim;
using Sandbox.ModAPI;
using Sprocket.Sim.Content;
using Sprocket.Sim.Fakes;
using Sprocket.Sim.Machine;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Sprocket.Sim.Simulation
{
    /// <summary>The game's session as a screen reads it: a viewport, and the person at it, whose saved colors a color is chosen from.</summary>
    public static class ScreenSession
    {
        static readonly List<Vector3> Painted = new List<Vector3>
        {
            new Vector3(0.000f, -0.800f, 0.170f), new Vector3(0.000f, -0.800f, -0.260f),
            new Vector3(0.575f, -0.220f, 0.070f), new Vector3(0.545f, -0.020f, 0.410f),
            new Vector3(0.083f, 0.080f, 0.470f), new Vector3(0.128f, 0.120f, 0.510f),
            new Vector3(0.015f, 0.040f, 0.310f), new Vector3(0.925f, -0.250f, 0.270f),
            new Vector3(0.335f, -0.140f, 0.130f), new Vector3(0.275f, 0.060f, 0.350f),
            new Vector3(0.700f, -0.220f, 0.230f), new Vector3(0.495f, -0.460f, -0.010f),
            new Vector3(0.000f, -0.800f, 0.490f), new Vector3(0.070f, -0.380f, -0.090f)
        };

        /// <summary>A surface drawing Sprocket's screens, with the session a screen reads put up around it.</summary>
        public static Surface Surface()
        {
            var surface = new Surface(SprocketContent.Instance);
            Install(surface.Device);
            return surface;
        }

        /// <summary>Puts the harness behind the members of the gateway a screen reads, its viewport <paramref name="device" />'s.</summary>
        public static void Install(ScreenSpaceDevice device)
        {
            var camera = Pretend.To<IMyCamera>()
                .Gives("ViewportSize", () => device.ViewportSize)
                .Done();

            var player = Pretend.To<IMyPlayer>()
                // Offsets rather than colors, in the form the game keeps a build color slot in: the hue as it stands, and
                // saturation and value shifted so that nought is unpainted armor. The mod converts these as it would a
                // real player's.
                .Gives("BuildColorSlots", () => Painted)
                .Gives("IdentityId", () => Machine.Simulation.Viewer)
                // The game plays the screens' click sounds from the player's character, and the stand-in player is bodiless.
                .Gives("Character", () => null)
                .Done();

            MyAPIGateway.Session = Pretend.To<IMySession>()
                .Gives("Camera", () => camera)
                .Gives("Player", () => player)
                .Gives("GameplayFrameCounter", () => Clock.Frame)
                .Gives("Factions", () => Factions.Installed?.Collection)
                .Answers("TryGetAdminSettings", Rig.NoAdmin)
                .Done();
        }
    }
}
