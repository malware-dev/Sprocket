using System.Collections.Generic;

namespace Sprocket.GameTests
{
    /// <summary>
    /// The editor in the game client on Hailstone Station, a station of Mal's whose airlocks one logic system runs, toured where the
    /// world's own player stands, for pictures of each kind of screen for the Workshop page. Run on a copy of that world given
    /// as the runner's template.
    /// </summary>
    public sealed class HailstoneClientTest
    {
        // The station's programmable block, its airlock logic system and that system's sequence out (Mal's Hailstone Station).
        const string Automation = "Lab Programmable Block";
        const string System = "Airlock System";
        const string Out = "Cycle Out";

        readonly EditorTour _tour;

        /// <summary>The test, pictured by <paramref name="tour" />.</summary>
        public HailstoneClientTest(EditorTour tour)
        {
            _tour = tour;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            // Each stop is its picture's name and what is pressed from the home screen to reach it.
            var stops = new[]
            {
                new[] { "home" },
                new[] { "triggers", System },
                new[] { "sequence", System, "off", "Sequences", Out },
                new[] { "installation panel", System, "off", "Installation panel" },
                new[] { "library", "Library" },
                new[] { "step menu", System, "off", "Sequences", Out, "open:PlusMenu" },

                // An open menu or list outlives its page, so the stops that open one come last, and the stop after one closes it with
                // Escape before it goes on.
                new[] { "installations", "escape", System, "open:ListField" }
            };

            var visit = _tour.Visit(Automation, stops, "hailstone-");
            while (visit.MoveNext())
                yield return visit.Current;
        }
    }
}
