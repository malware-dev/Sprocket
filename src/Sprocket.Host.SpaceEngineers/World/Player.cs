using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Ui.Look;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>The person at the screen, as far as Sprocket needs to know them.</summary>
    static class Player
    {
        /// <summary>The private use area, where a font is free to draw whatever it likes.</summary>
        /// <remarks>
        /// Space Engineers keeps its own marks here and draws one in front of a player's display name,
        /// so <c>MyName</c> answers with it attached. See <see cref="Plain" />.
        /// </remarks>
        const char MarksFrom = '';

        const char MarksTo = '';

        /// <summary>
        /// A display name without the mark the game draws in front of it.
        /// </summary>
        /// <remarks>
        /// <b>Only from the front, and only the game's own marks.</b> What the game prefixes is not part
        /// of anybody's name and means nothing in another font, so it has no business becoming the
        /// namespace a system is written under. Everything after it is what the player typed, and people
        /// put all sorts into a name - so the rest is left exactly as it arrived, whatever it holds.
        /// </remarks>
        public static string Plain(string shown)
        {
            if (string.IsNullOrEmpty(shown))
                return shown;

            var at = 0;
            while (at < shown.Length && shown[at] >= MarksFrom && shown[at] <= MarksTo)
                at++;

            return at == 0 ? shown : shown.Substring(at);
        }

        /// <summary>
        /// The colours this player keeps to paint with, copied out into <paramref name="into" />.
        /// </summary>
        /// <remarks>
        /// <b>Read and never written.</b> These are the fourteen swatches somebody paints their ship
        /// with, and a colour chosen for a light has no business rewriting them. The property does have
        /// a setter, and what it writes is local: it fills the slots and refreshes a client-side cache,
        /// and tells the server nothing.
        /// <para>
        /// Copied rather than held, because the getter hands back the live list. Anything that kept it
        /// and changed an entry would have edited the player's palette without going through the setter
        /// at all.
        /// </para>
        /// <para>
        /// Empty where there is nobody to ask, which is every machine with no session on it. A dialog
        /// with no swatches to show leaves the row out rather than drawing an empty one.
        /// </para>
        /// </remarks>
        public static void Colours(IList<Hsv> into)
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Player == null)
                return;

            List<Vector3> kept = MyAPIGateway.Session.Player.BuildColorSlots;
            if (kept == null)
                return;

            for (var i = 0; i < kept.Count; i++)
                into.Add(Hsv.Painted(kept[i]));
        }
    }
}
