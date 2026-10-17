using System.Collections.Generic;
using Sandbox.ModAPI;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Fields;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>The person at the screen, as far as Sprocket needs to know them.</summary>
    public static class Player
    {
        const char MarksFrom = '';

        const char MarksTo = '';

        /// <summary>
        /// A display name without the mark the game draws in front of it.
        /// </summary>
        public static string Plain(string shown)
        {
            if (string.IsNullOrEmpty(shown))
                return shown;

            var at = 0;
            // Space Engineers keeps its own marks in the private use area and draws one in front of a player's
            // display name, so MyName answers with it attached.
            while (at < shown.Length && shown[at] >= MarksFrom && shown[at] <= MarksTo)
                at++;

            return at == 0 ? shown : shown.Substring(at);
        }

        /// <summary>
        /// The colors this player keeps to paint with, copied out into <paramref name="into" />.
        /// </summary>
        /// <remarks>Adds none on a machine with no session. The game's getter hands back the live list, so it is copied.</remarks>
        public static void Colours(IList<Hsv> into)
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Player == null)
                return;

            List<Vector3> kept = MyAPIGateway.Session.Player.BuildColorSlots;
            if (kept == null)
                return;

            foreach (var color in kept)
                into.Add(Hsv.Painted(color));
        }
    }
}
