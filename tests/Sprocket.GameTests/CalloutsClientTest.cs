using System.Collections.Generic;
using VRage.Input;

namespace Sprocket.GameTests
{
    /// <summary>
    /// The what's-new bubbles of release 1.1, in the game client on Kept Automation, with the test's own user data of a player who
    /// last saw release 1.0 (Run-ClientTests.ps1 writes it), so every bubble newer than that is due: the
    /// share bubble hangs at home, Escape and a press elsewhere leave it due, a press on it dismisses it for good, and the import
    /// and settings bubbles hang where they belong. The harness cannot show this: it is the real game window, with its own
    /// pointer, Escape and player file, drawing the bubble over the real editor.
    /// </summary>
    /// <remarks>
    /// Runs before any other editor test, which would otherwise meet the bubbles first. The bubble is pressed with the pointer, as a
    /// player presses it, at the place its automation id (the words it says) is found in the editor's windows.
    /// </remarks>
    public sealed class CalloutsClientTest
    {
        const string Block = "Kept Automation";
        const string KeptSystem = "Kept Here";
        const string InstallationPanel = "Installation panel";
        const string Library = "Library";

        readonly EditorTour _tour;
        readonly GameTestReport _report;
        readonly EditorInput _input;

        /// <summary>The test, pictured by <paramref name="tour" />, answering into <paramref name="report" />, pressing with <paramref name="input" />.</summary>
        public CalloutsClientTest(EditorTour tour, GameTestReport report, EditorInput input)
        {
            _tour = tour;
            _report = report;
            _input = input;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var editor = _tour.Open(EditorTour.Named(Block), Block);
            if (editor == null)
                yield break;

            yield return 2 * PluginUpdates.PerSecond;

            var due = string.Join(", ", editor.DueCallouts());
            _report.Check("callouts due", editor.DueCallouts().Contains("share") && editor.DueCallouts().Contains("import") && editor.DueCallouts().Contains("settings"),
                $"for a player who last saw release 1.0 the editor has due: {due}");

            // The share bubble at home.
            _report.Check("callout share at home", editor.ShownCallout() == "share" && _input.Shows(editor.ShownCalloutSaid() ?? "?"),
                $"the bubble hanging at home is {editor.ShownCallout() ?? "none"}, saying \"{editor.ShownCalloutSaid()}\"");
            var shot = _tour.Tour(editor, new[] { new[] { "callout share home", "stay" } }, "");
            while (shot.MoveNext())
                yield return shot.Current;

            // Escape goes back from a logic system's page and leaves the bubble due.
            editor.Press(KeptSystem);
            yield return PluginUpdates.PerSecond;
            _input.Press(MyKeys.Escape);
            yield return 2 * PluginUpdates.PerSecond;
            _report.Check("callout escape leaves it due", !editor.CalloutDismissed("share") && !editor.CanGoBack && editor.ShownCallout() == "share",
                $"after Escape from {KeptSystem}: at home {!editor.CanGoBack}, share dismissed {editor.CalloutDismissed("share")}, bubble hanging {editor.ShownCallout() ?? "none"}");

            // A press elsewhere, on a tab, leaves it hanging and reaches the tab.
            editor.Press(KeptSystem);
            yield return PluginUpdates.PerSecond;
            var pressed = _input.PressNamed(InstallationPanel);
            yield return 2 * PluginUpdates.PerSecond;
            _report.Check("callout press elsewhere leaves it", pressed && editor.IsTabShown(InstallationPanel) && !editor.CalloutDismissed("share") && editor.ShownCallout() == "share",
                $"the pointer {(pressed ? "found" : "did not find")} the {InstallationPanel} tab, which {(editor.IsTabShown(InstallationPanel) ? "shows" : "does not show")}; share dismissed {editor.CalloutDismissed("share")}, bubble hanging {editor.ShownCallout() ?? "none"}");
            shot = _tour.Tour(editor, new[] { new[] { "callout share after a press elsewhere", "stay" } }, "");
            while (shot.MoveNext())
                yield return shot.Current;

            // A press on the bubble dismisses it. The system is switched on, so the panel shows no add slot, and the settings
            // bubble, which hangs from it, does not hang.
            var said = editor.ShownCalloutSaid() ?? "?";
            pressed = _input.PressNamed(said);
            yield return 2 * PluginUpdates.PerSecond;
            _report.Check("callout press dismisses it", pressed && editor.CalloutDismissed("share") && editor.ShownCallout() != "share",
                $"the pointer {(pressed ? "found" : "did not find")} the bubble saying \"{said}\"; share dismissed {editor.CalloutDismissed("share")}, bubble hanging now {editor.ShownCallout() ?? "none"}");
            _report.Check("callout settings waits for the add slot", editor.ShownCallout() == null && editor.DueCallouts().Contains("settings"),
                $"on a switched-on system's installation panel the bubble hanging is {editor.ShownCallout() ?? "none"}, due: {string.Join(", ", editor.DueCallouts())}");
            shot = _tour.Tour(editor, new[] { new[] { "callout settings waits while on", "stay" } }, "");
            while (shot.MoveNext())
                yield return shot.Current;

            // Leaving for the library, the settings bubble puts itself away and the import bubble hangs at the library's import mark.
            editor.Home();
            yield return PluginUpdates.PerSecond;
            editor.Press(Library);
            yield return 2 * PluginUpdates.PerSecond;
            _report.Check("callout import at the library", editor.ShownCallout() == "import",
                $"the bubble hanging at the library is {editor.ShownCallout() ?? "none"}, due: {string.Join(", ", editor.DueCallouts())}");
            shot = _tour.Tour(editor, new[] { new[] { "callout import library", "stay" } }, "");
            while (shot.MoveNext())
                yield return shot.Current;

            said = editor.ShownCalloutSaid() ?? "?";
            pressed = _input.PressNamed(said);
            yield return 2 * PluginUpdates.PerSecond;
            _report.Check("callout import dismissed", pressed && editor.CalloutDismissed("import"),
                $"the pointer {(pressed ? "found" : "did not find")} the bubble saying \"{said}\"; import dismissed {editor.CalloutDismissed("import")}, bubble hanging now {editor.ShownCallout() ?? "none"}");

            // Switched off, the installation panel shows its add slot and the settings bubble hangs from it; the last press
            // records the release as seen.
            editor.Home();
            yield return PluginUpdates.PerSecond;
            editor.Press(KeptSystem);
            yield return PluginUpdates.PerSecond;
            var switched = editor.Switch(false);
            yield return PluginUpdates.PerSecond;
            editor.Press(InstallationPanel);
            yield return 2 * PluginUpdates.PerSecond;
            _report.Check("callout settings at the installation panel", switched && editor.ShownCallout() == "settings",
                $"switched off {switched}; the bubble hanging at the installation panel is {editor.ShownCallout() ?? "none"}");
            shot = _tour.Tour(editor, new[] { new[] { "callout settings installation panel", "stay" } }, "");
            while (shot.MoveNext())
                yield return shot.Current;
            said = editor.ShownCalloutSaid() ?? "?";
            pressed = _input.PressNamed(said);
            yield return 2 * PluginUpdates.PerSecond;
            _report.Check("callout settings dismissed", pressed && editor.CalloutDismissed("settings") && editor.DueCallouts().Count == 0,
                $"the pointer {(pressed ? "found" : "did not find")} the bubble saying \"{said}\"; settings dismissed {editor.CalloutDismissed("settings")}, still due: {string.Join(", ", editor.DueCallouts())}, release seen {editor.SeenRelease() ?? "none"}");
            shot = _tour.Tour(editor, new[] { new[] { "callout none left", "stay" } }, "");
            while (shot.MoveNext())
                yield return shot.Current;

            editor.Home();
            yield return PluginUpdates.PerSecond;
            editor.Press(KeptSystem);
            yield return PluginUpdates.PerSecond;
            editor.Switch(true);
            yield return PluginUpdates.PerSecond;
            _tour.Close();
            yield return PluginUpdates.PerSecond;
        }
    }
}
