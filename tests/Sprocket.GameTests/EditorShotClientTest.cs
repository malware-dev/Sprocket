using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Sprocket's editor opened in the game client on a pasted programmable block, by the Automation button of its control panel,
    /// and a picture of the screen it opens on, for the visual rows of the Testing artifact.
    /// </summary>
    public sealed class EditorShotClientTest
    {
        // The English codex's words for what the tips stops rest on (MyTextsCodex.resx): the term home's heading holds, the tips switch's
        // tip while tips are on and while they are off, a trigger's bin, the undo arrow, a step's pause button, and the arrow that makes a backup a blueprint.
        const string Term = "Term.LogicSystem";
        const string TipsOn = "Show fewer tips";
        const string TipsOff = "Show every tip";
        const string TriggerBin = "Delete this trigger";
        const string Undo = "[![](mark:undo)";
        const string PauseStep = "Pause this step";
        const string MakeBlueprint = "Make this [backup]";

        // A codex page with code in it (MyTextsCodex.resx, Sprocket.Term.Branch).
        const string CodePage = "Term.Branch";

        // The logic system the editor opens on and its blueprint, the one with no blueprint (KeptCount), the logic system of the construct Destroy takes the
        // programmable block of (PistonStatus), and the library button's Norwegian word (MyTexts.no.resx, Sprocket.Systems.Load).
        const string Kept = "Kept Here";
        const string Unkept = "Listed Everywhere";
        const string Backup = "Piston";
        const string NorwegianLibrary = "Bibliotek";

        // Kept Count's button panel and the signal its buttons send, its programmable block, which has two screens, and the action
        // that shows text on one of a block's screens.
        const string Button = "Kept Button";
        const string Automation = "Kept Automation";
        const string ShowText = "show text";
        const string Pressed = "button is pressed";

        readonly EditorTour _tour;
        readonly GameTestReport _report;

        /// <summary>The test, pictured by <paramref name="tour" /> and answering into <paramref name="report" />.</summary>
        public EditorShotClientTest(EditorTour tour, GameTestReport report)
        {
            _tour = tour;
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        /// <remarks>Opens the editor on the block called <paramref name="name" />, which an earlier test pasted.</remarks>
        public IEnumerator<int> Run(string name)
        {
            var block = EditorTour.Named(name);
            var editor = _tour.Open(block, name);
            if (editor == null)
                yield break;

            yield return 2 * PluginUpdates.PerSecond;

            // Each stop is its picture's name and what is pressed from the home screen to reach it. Tips switched off stay off until
            // switched on again, so the stops from "tips switch off" on are seen with tips off.
            var stops = new[]
            {
                new[] { "home" },
                new[] { "library", "Library" },
                new[] { "logic system", "Kept Here" },
                new[] { "installation panel", "Kept Here", "Installation panel" },
                new[] { "slots switched off", "Kept Here", "off", "Installation panel" },
                new[] { "optimize slots asks", "Kept Here", "Installation panel", "Optimize slots" },
                new[] { "heading renaming", "Kept Here", "Kept Here" },
                new[] { "blueprint made", "Make blueprint" },
                new[] { "term tip", "hover:has:" + Term },
                new[] { "codex code page", "hover:has:" + Term, "page:" + CodePage },
                new[] { "switch sliding on 1", "Kept Here", "on", "frame:1" },
                new[] { "switch sliding off 1", "Kept Here", "off", "frame:1" },
                new[] { "switch sliding on 2", "Kept Here", "on", "frame:2" },
                new[] { "switch sliding off 2", "Kept Here", "off", "frame:2" },
                new[] { "switch sliding on 3", "Kept Here", "on", "frame:3" },
                new[] { "switch sliding off 3", "Kept Here", "off", "frame:3" },
                new[] { "tips switch on", "hover:tip:" + TipsOn },
                new[] { "tips switch off", "tip:" + TipsOn, "hover:tip:" + TipsOff },
                new[] { "tips off home" },
                new[] { "tips off term", "hover:has:" + Term },
                new[] { "tips off logic system", "Kept Here" },
                new[] { "tips off sequences", "Kept Here", "Sequences" },
                new[] { "tips off bin", "Kept Here", "Triggers", "hover:tip:" + TriggerBin },
                new[] { "tips off strip to library tip", "Library", "hover:Moves.ToLibrary" },
                new[] { "tips off undo", "Kept Here", "Triggers", "tip:" + TriggerBin, "answer", "hover:tip:" + Undo },
                new[] { "tips off sequence steps", "Kept Here", "Sequences", "Sequence 1", "tip:" + PauseStep },
                new[] { "tips off clicked tab left", "Kept Here", "hover:Installation panel", "click" },
                new[] { "tips off keys", "Kept Here", "tab", "tab", "tab" },
                new[] { "tips on again term", "tip:" + TipsOff, "hover:has:" + Term },
                new[] { "tips on again sequences", "Kept Here", "Sequences" },
                new[] { "tips on again sequence steps", "Kept Here", "Sequences", "Sequence 1" },
                new[] { "term tip pinned", "alt", "hover:has:" + Term }
            };

            var tour = _tour.Tour(editor, stops, "");
            while (tour.MoveNext())
                yield return tour.Current;

            editor.HoldAlt(false);

            // A backup is kept of a construct's logic systems as its last programmable block is destroyed.
            var lost = Destroy(block);
            _report.Check("backup made", lost > 0, $"{lost} programmable blocks destroyed on other constructs");
            yield return 2 * PluginUpdates.PerSecond;

            var english = _tour.Tour(editor, Strip("Library"), "");
            while (english.MoveNext())
                yield return english.Current;

            var norwegian = EditorTour.Speak(MyLanguagesEnum.Norwegian);
            while (norwegian.MoveNext())
                yield return norwegian.Current;

            var strip = _tour.Tour(editor, Strip(NorwegianLibrary), "no ");
            while (strip.MoveNext())
                yield return strip.Current;

            var back = EditorTour.Speak(MyLanguagesEnum.English);
            while (back.MoveNext())
                yield return back.Current;

            // The backup made a blueprint, so the library holds one whose logic system is on no construct here, seen in both languages.
            var promoted = _tour.Tour(editor, new[] { new[] { "backup made a blueprint", "Library", Backup, "tip:" + MakeBlueprint, "answer" } }, "");
            while (promoted.MoveNext())
                yield return promoted.Current;

            var added = _tour.Tour(editor, NewBlueprint("Library"), "");
            while (added.MoveNext())
                yield return added.Current;

            var norwegianAgain = EditorTour.Speak(MyLanguagesEnum.Norwegian);
            while (norwegianAgain.MoveNext())
                yield return norwegianAgain.Current;

            var addedNorwegian = _tour.Tour(editor, NewBlueprint(NorwegianLibrary), "no ");
            while (addedNorwegian.MoveNext())
                yield return addedNorwegian.Current;

            var englishAgain = EditorTour.Speak(MyLanguagesEnum.English);
            while (englishAgain.MoveNext())
                yield return englishAgain.Current;

            // The library's panes, ending with each search narrowed, so nothing after them is seen through a search.
            var panes = _tour.Tour(editor, Panes(), "");
            while (panes.MoveNext())
                yield return panes.Current;

            // Last, since each leaves a selector open: the spines set in under a signal's toolbar slots and a block's screens.
            var spines = _tour.Tour(editor, new[]
            {
                new[] { "spine toolbar slots", Kept, "Triggers", "add:when", "wait", "hover:" + Button, "click", "hover:" + Pressed, "click" },
                new[] { "spine screen choices", Kept, "Sequences", "Sequence 1", "add:task", "wait", "hover:" + Automation, "click", "hover:" + ShowText, "click" }
            }, "");
            while (spines.MoveNext())
                yield return spines.Current;

            _tour.Close();
            yield return PluginUpdates.PerSecond;
        }

        /// <summary>
        /// Testing row #62's menus and lists, on the block called <paramref name="name" />, which an earlier test pasted: a sequence's
        /// plus menu opened and walked with Up, Down and Enter, and the block selector's Source list opened, walked and chosen from.
        /// </summary>
        public IEnumerator<int> Menus(string name)
        {
            var editor = _tour.Open(EditorTour.Named(name), name);
            if (editor == null)
                yield break;

            yield return 2 * PluginUpdates.PerSecond;
            var stops = new[]
            {
                new[] { "menu open", Kept, "off", "Sequences", "Sequence 1", "open:PlusMenu" },
                new[] { "menu down", "stay", "arrowdown" },
                new[] { "menu down twice", "stay", "arrowdown" },
                new[] { "menu up", "stay", "arrowup" },
                new[] { "menu enter", "stay", "enter" },
                new[] { "source list open", Kept, "Sequences", "Sequence 1", "add:task", "wait", "open:ListField" },
                new[] { "source list down", "stay", "arrowdown" },
                new[] { "source list chosen", "stay", "enter" }
            };

            var tour = _tour.Tour(editor, stops, "");
            while (tour.MoveNext())
                yield return tour.Current;

            _tour.Close();
            yield return PluginUpdates.PerSecond;
        }

        /// <summary>The library's two panes: a logic system with no blueprint chosen and its blueprint made by the arrow, then each pane's search typed into.</summary>
        static string[][] Panes()
        {
            return new[]
            {
                new[] { "library chosen", "Library", Unkept },
                new[] { "library unkept to library tip", "Library", Unkept, "hover:Moves.ToLibrary" },
                new[] { "library blueprint made", "Library", Unkept, "hover:Moves.ToLibrary", "click", "answer" },
                new[] { "library search here", "Library", "hover:ConstructSearchBox", "click", "type:" + Unkept.Substring(0, 6) },
                new[] { "library search storage", "Library", "hover:SearchBox", "click", "type:" + Kept.Substring(0, 4) }
            };
        }

        /// <summary>The library's strip rested on with the blueprint made of the backup chosen, reached by the library button that says <paramref name="library" />.</summary>
        static string[][] NewBlueprint(string library)
        {
            return new[]
            {
                new[] { "strip new blueprint to construct tip", library, Backup, "hover:Moves.ToConstruct" },
                new[] { "strip new blueprint to library tip", library, Backup, "hover:Moves.ToLibrary" }
            };
        }

        /// <summary>
        /// The library's strip, its tips rested on with a logic system on this construct chosen, the blueprint of it, and the backup
        /// <see cref="Destroy" /> made, reached by the library button that says <paramref name="library" />.
        /// </summary>
        static string[][] Strip(string library)
        {
            return new[]
            {
                new[] { "strip copy tip", library, "hover:Moves.Copies" },
                new[] { "strip to library tip", library, "hover:Moves.ToLibrary" },
                new[] { "strip to construct tip", library, "hover:Moves.ToConstruct" },
                new[] { "strip blueprint to construct tip", library, "last:" + Kept, "hover:Moves.ToConstruct" },
                new[] { "strip blueprint to library tip", library, "last:" + Kept, "hover:Moves.ToLibrary" },
                new[] { "strip backup to library tip", library, Backup, "hover:Moves.ToLibrary" },
                new[] { "strip backup to construct tip", library, Backup, "hover:Moves.ToConstruct" }
            };
        }

        /// <summary>
        /// Destroys every programmable block on the constructs other than <paramref name="block" />'s, as grinding them away would,
        /// and says how many there were.
        /// </summary>
        static int Destroy(IMyTerminalBlock block)
        {
            var mine = new List<IMyCubeGrid>();
            MyAPIGateway.GridGroups.GetGroup(block.CubeGrid, GridLinkTypeEnum.Mechanical, mine);
            var grids = new HashSet<VRage.ModAPI.IMyEntity>();
            MyAPIGateway.Entities.GetEntities(grids, entity => entity is IMyCubeGrid grid && !mine.Contains(grid));
            var destroyed = 0;
            foreach (var grid in grids.OfType<IMyCubeGrid>())
            {
                var blocks = new List<IMySlimBlock>();
                grid.GetBlocks(blocks, slim => slim.FatBlock is IMyProgrammableBlock);
                foreach (var slim in blocks)
                {
                    grid.RazeBlock(slim.Position);
                    destroyed++;
                }
            }

            return destroyed;
        }

    }
}
