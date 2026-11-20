using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Sprocket.GameTests
{
    /// <summary>
    /// A logic system copied as text from the editor's Share menu and imported into the library from a programmable block's Custom
    /// Data, in the game client on Kept Automation. The harness cannot show this: the menu is pressed with the real pointer in
    /// the game's window, the clipboard is the game's own, and the Custom Data is the game's block storage.
    /// </summary>
    /// <remarks>
    /// Where the game cannot use the clipboard, as a game started by a process denied it cannot, the text the menu item would put
    /// there is taken from the offer the page makes, and the report says so. The copy is given a new identity before it is imported, as
    /// a copy made in another library has one, since the library already holding this one's identity would leave it out.
    /// </remarks>
    public sealed class ShareImportClientTest
    {
        const string Block = "Kept Automation";
        const string KeptSystem = "Kept Here";
        const string Library = "Library";

        readonly EditorTour _tour;
        readonly GameTestReport _report;
        readonly EditorInput _input;

        /// <summary>The test, pictured by <paramref name="tour" />, answering into <paramref name="report" />, pressing with <paramref name="input" />.</summary>
        public ShareImportClientTest(EditorTour tour, GameTestReport report, EditorInput input)
        {
            _tour = tour;
            _report = report;
            _input = input;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var block = EditorTour.Named(Block);
            var editor = _tour.Open(block, Block);
            if (editor == null)
                yield break;

            yield return 2 * PluginUpdates.PerSecond;
            string copyText = editor.Texts.ShareCopyText;
            var share = InGameEditor.Symbol("Share");
            var original = SharedTextClientTest.ReadClipboard();
            var sentinel = "sprocket share test " + Guid.NewGuid().ToString("N");
            SharedTextClientTest.WriteClipboard(sentinel);
            var usable = SharedTextClientTest.ReadClipboard() == sentinel;

            // Copy as text, from the logic system's page.
            var page = _tour.Tour(editor, new[] { new[] { "share menu", KeptSystem } }, "");
            while (page.MoveNext())
                yield return page.Current;

            var opened = share != null && _input.PressNamed(share);
            yield return PluginUpdates.PerSecond;
            var offered = _input.Shows(copyText);
            _report.Check("share menu opens", opened && offered, $"the Share mark {(opened ? "was" : "was not")} pressed, and the menu {(offered ? "offers" : "does not offer")} \"{copyText}\"");
            var menu = _tour.Tour(editor, new[] { new[] { "share menu open", "stay" } }, "");
            while (menu.MoveNext())
                yield return menu.Current;

            string offeredText = null;
            try
            {
                dynamic shares = editor.Current.Shares;
                offeredText = (string)((Delegate)shares.FnText).DynamicInvoke();
            }
            catch (Exception exception)
            {
                _report.Note("share: the page's own offer could not be read: " + exception.Message);
            }

            var copied = offered && _input.PressNamed(copyText);
            yield return PluginUpdates.PerSecond;
            var asked = editor.Answers().Length > 0;
            _report.Check("share copy asks", copied && asked, $"\"{copyText}\" {(copied ? "was" : "was not")} pressed; the editor then offers the answers: {editor.Answers()}");
            var question = _tour.Tour(editor, new[] { new[] { "share copied", "stay" } }, "");
            while (question.MoveNext())
                yield return question.Current;

            editor.Answer();
            yield return PluginUpdates.PerSecond;

            string text;
            if (usable)
            {
                text = SharedTextClientTest.ReadClipboard();
                _report.Check("share copy on clipboard", text != sentinel && text != null && text.StartsWith("sprocket "),
                    $"the clipboard now holds {(text == null ? "nothing" : text.Length + " characters starting \"" + text.Substring(0, Math.Min(24, text.Length)).Replace("\n", "\\n") + "\"")}");
                if (offeredText != null)
                    _report.Check("share copy is the offer's text", text == offeredText, text == offeredText ? "the clipboard holds exactly what the page offers" : "the clipboard differs from what the page's offer writes");
            }
            else
            {
                text = offeredText;
                _report.Note("share: the clipboard could not be used from this game, so the text the menu item copies was taken from the page's offer");
                _report.Check("share offer text", text != null && text.StartsWith("sprocket "), $"the page's offer writes {(text == null ? "nothing" : text.Length + " characters")}");
            }

            if (original != null)
                SharedTextClientTest.WriteClipboard(original);

            if (text == null || !text.StartsWith("sprocket "))
            {
                _tour.Close();
                yield break;
            }

            // The copy as another library would have it: the same blueprint under an identity this library does not hold.
            var identity = Guid.NewGuid();
            var imported = Regex.Replace(text, "(system blueprint \")[0-9a-fA-F-]{36}(\")", "${1}" + identity.ToString("D") + "${2}");
            _report.Check("share copy renamed identity", imported != text, imported != text ? $"the copy's identity is now {identity}" : "the copy holds no 'system blueprint \"<id>\"' line to give a new identity");

            var customData = block.CustomData;
            block.CustomData = imported;
            yield return PluginUpdates.PerSecond;

            var library = _tour.Tour(editor, new[] { new[] { "import library", Library } }, "");
            while (library.MoveNext())
                yield return library.Current;

            dynamic shelf = editor.Shelf;
            if (shelf == null)
            {
                _report.Fail("import library", "the editor has no library to import into");
                block.CustomData = customData;
                _tour.Close();
                yield break;
            }

            var before = (int)shelf.All.Count;
            var pressed = _input.PressNamed(InGameEditor.Symbol("Import") ?? "?");
            yield return 2 * PluginUpdates.PerSecond;
            string confirm = editor.Texts.LibraryImportConfirm;
            var asking = editor.Answers();
            _report.Check("import asks", pressed && asking.Contains(confirm), $"the Import mark {(pressed ? "was" : "was not")} pressed; the editor offers: {asking}");
            var ask = _tour.Tour(editor, new[] { new[] { "import question", "stay" } }, "");
            while (ask.MoveNext())
                yield return ask.Current;

            var confirmed = editor.Answer(confirm);
            yield return 2 * PluginUpdates.PerSecond;

            dynamic kept = shelf.Find(identity);
            _report.Check("import adds a blueprint", confirmed && kept != null && (int)shelf.All.Count == before + 1,
                $"{confirm} {(confirmed ? "was" : "was not")} answered; the library holds {(int)shelf.All.Count} blueprints (was {before}), and {(kept == null ? "none" : "one")} has the copy's identity");
            if (kept != null)
            {
                string said = kept.Text;
                var difference = FirstDifference(Steps(text), Steps(said));
                _report.Check("import has the same steps", difference == null, difference ?? $"the imported blueprint \"{(string)kept.Name}\" has the same lines as the copy apart from its identity, date and format line");
            }

            var shown = _tour.Tour(editor, new[] { new[] { "import done", "stay" } }, "");
            while (shown.MoveNext())
                yield return shown.Current;

            block.CustomData = customData;
            _tour.Close();
            yield return PluginUpdates.PerSecond;
        }

        /// <summary>The lines of <paramref name="blueprint" /> that say what it does: all but its format, identity, date and counter lines.</summary>
        static List<string> Steps(string blueprint)
        {
            var lines = new List<string>();
            foreach (var line in blueprint.Replace("\r\n", "\n").Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("sprocket ") || trimmed.StartsWith("system blueprint") || trimmed.StartsWith("changed ") || trimmed.StartsWith("#@ lastSequence"))
                    continue;

                lines.Add(trimmed);
            }

            return lines;
        }

        /// <summary>Where <paramref name="imported" /> first departs from <paramref name="copied" />, said in words, or null where they are the same.</summary>
        static string FirstDifference(List<string> copied, List<string> imported)
        {
            var length = Math.Min(copied.Count, imported.Count);
            for (var i = 0; i < length; i++)
            {
                if (copied[i] != imported[i])
                    return $"the imported blueprint departs from the copy at line {i}: \"{copied[i]}\" became \"{imported[i]}\"";
            }

            return copied.Count == imported.Count ? null : $"the copy has {copied.Count} lines and the imported blueprint {imported.Count}";
        }
    }
}
