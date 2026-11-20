using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>
    /// A blueprint shared as compact text (BlueprintText) survives a player pasting it into a programmable block's Custom Data
    /// through the game's own Custom Data editor, with the world's offensive-words filtering off and on, and Sprocket reads the
    /// same blueprint back from the Custom Data the game stores. The harness cannot show this: the editor, its paste, its
    /// filter and how the game stores what the editor holds are the game's own.
    /// </summary>
    /// <remarks>
    /// The text is put on the system clipboard, the block's Custom Data editor is opened as the terminal's Custom Data button
    /// opens it, and Ctrl+V is pressed with the keyboard while the game's window is in front, so the editor's own paste reads
    /// the clipboard. Where the game is not in front, or the keys reach nothing, the editor's paste is called directly, as
    /// Ctrl+V calls it. Where the game cannot use the clipboard at all, as a game started by a process denied the clipboard
    /// cannot, the editor's text is set as its paste sets it instead. The report says which of the three ran. The editor is
    /// closed with its OK, as its button does. The clipboard is put back as it was after.
    /// </remarks>
    public sealed class SharedTextClientTest
    {
        static readonly Vector3D At = new Vector3D(0, 0, -480);

        // The mod's own type that writes and reads the shared text, found by name in the game's compile of the mod.
        const string Text = "Sprocket.Host.SpaceEngineers.Features.Sharing.Text.BlueprintText";

        // A blueprint as Sprocket stores it, written by the editor (Airlock System, 2026-11-16).
        const string Blueprint = """
            sprocket 1 alpha 28

            # Control a fully functional airlock.
            #@ lastSequence: 5
            system blueprint "61da898f-e0f5-4b57-8c81-3194c3357d4e" as Malware.'Airlock System'
            changed "2026-11-16T15:11:51Z"
            {
                # The door on the pressurized side
                #@ category: "Inside"
                required slot 'Inner Door' expects Door

                # The cycle button on the pressurized side
                #@ short: "Cycle In"
                #@ category: "Inside"
                optional slot 'Inner Button' expects signal

                # The door on the unpressurized side
                #@ category: "Outside"
                required slot 'Outer Door' expects Door

                # The cycle button on the unpressurized side
                #@ short: "Cycle Out"
                #@ category: "Outside"
                optional slot 'Outer Button' expects signal

                # The button on the inside of the airlock
                #@ short: "Cycle"
                #@ category: "Interior"
                optional slot 'Interior Button' expects signal

                # The airvent inside the airlock, used to pressurize and depressurize
                #@ category: "Interior"
                required slot 'Interior Air Vent' expects AirVent

                # The light on the inside of the airlock
                #@ category: "Interior"
                required slot 'Interior Light' expects InteriorLight

                # A display used to show the current state of the airlock, also shows what the internal button currently will do when pressed
                #@ category: "Interior"
                required slot 'Interior Display' expects ext.screen

                whenever 'Inner Button' then run 4

                whenever 'Interior Button' then run 1

                whenever 'Outer Button' then run 3

                sequence 1 as Cycle {
                    if any ext.power on [ 'Inner Door' ] == on then {
                        run 3
                        await
                    }
                    else {
                        run 4
                        await
                    }
                }

                sequence 3 as 'Cycle Out' {
                    call ext.show_text("Cycling...") on [ 'Interior Display' ]
                    set Color on [ 'Interior Light' ] to #E8B323FF
                    set ext.power on [ 'Inner Door', 'Outer Door' ] to on
                    call ext.close on [ 'Outer Door', 'Inner Door' ]
                    await
                    set ext.power on [ 'Inner Door', 'Outer Door' ] to off
                    call ext.depressurize(1) on [ 'Interior Air Vent' ]
                    await
                    set Color on [ 'Interior Light' ] to #FF8080FF
                    set ext.power on [ 'Outer Door' ] to on
                    call ext.open on [ 'Outer Door' ]
                    await
                    call ext.show_text("# Cycle In&br;Press button to cycle in.") on [ 'Interior Display' ]
                }

                sequence 4 as 'Cycle In' {
                    call ext.show_text("Cycling...") on [ 'Interior Display' ]
                    set Color on [ 'Interior Light' ] to #E8B323FF
                    set ext.power on [ 'Inner Door', 'Outer Door' ] to on
                    call ext.close on [ 'Inner Door', 'Outer Door' ]
                    await
                    set ext.power on [ 'Outer Door', 'Inner Door' ] to off
                    call ext.pressurize(1) on [ 'Interior Air Vent' ]
                    await
                    set Color on [ 'Interior Light' ] to #80FF80FF
                    set ext.power on [ 'Inner Door' ] to on
                    call ext.open on [ 'Inner Door' ]
                    await
                    call ext.show_text("# Cycle Out&br;Press the button to cycle out") on [ 'Interior Display' ]
                }
            }
            """;

        const string Name = "Airlock System";
        const byte Control = 0x11;
        const byte V = 0x56;
        const uint KeyUp = 0x0002;

        readonly GameTestReport _report;

        /// <summary>The test, answering into <paramref name="report" />.</summary>
        public SharedTextClientTest(GameTestReport report)
        {
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var text = FindText();
            if (text == null)
            {
                _report.Fail("shared text", $"no type {Text} among the loaded assemblies");
                yield break;
            }

            var section = (string)text.GetMethod("Write").Invoke(null, new object[] { Name, Blueprint });
            var original = Blueprints(text, section);
            var braille = 0;
            foreach (var c in section)
            {
                if (c >= '\u2800' && c <= '\u28FF')
                    braille++;
            }

            _report.Check("shared text written", original.Count == 1 && original[0] != null,
                $"the mod wrote a section of {section.Length} characters, {braille} of them data, which it reads back as {original.Count} blueprint(s) before any paste");
            if (original.Count != 1 || original[0] == null)
                yield break;

            var programmable = BuiltGrid.Block<MyObjectBuilder_MyProgrammableBlock>("LargeProgrammableBlock", Vector3I.Zero);
            programmable.Owner = MySession.Static.LocalPlayerId;
            programmable.ShareMode = MyOwnershipShareModeEnum.All;
            programmable.CustomName = "Shared Text Block";
            var grid = new BuiltGrid(At, BuiltGrid.Battery(new Vector3I(0, 0, 1)), programmable);
            var block = grid.Find<MyTerminalBlock>();
            if (block == null)
            {
                _report.Fail("shared text block", "the programmable block was not built");
                yield break;
            }

            yield return PluginUpdates.PerSecond;

            var clipboard = ReadClipboard();
            var settings = MySession.Static.Settings;
            var filtering = settings.OffensiveWordsFiltering;
            try
            {
                foreach (var filtered in new[] { false, true })
                {
                    settings.OffensiveWordsFiltering = filtered;
                    var label = filtered ? "filtered" : "unfiltered";
                    if (filtered)
                        _report.Note(FilterState());

                    var round = Paste(block, section, label);
                    while (round.MoveNext())
                        yield return round.Current;

                    var stored = block.CustomData;
                    var differs = FirstDifference(section, stored);
                    _report.Check($"shared text {label} stored", differs < 0, differs < 0
                        ? $"the block's Custom Data is the pasted section exactly, {stored.Length} characters"
                        : $"the block's Custom Data ({stored.Length} characters) differs from the section ({section.Length}) at {differs}: {Around(section, differs)} became {Around(stored, differs)}");

                    var read = Blueprints(text, stored);
                    var same = read.Count == 1 && read[0] == original[0];
                    _report.Check($"shared text {label} read", same, read.Count != 1
                        ? $"the mod reads {read.Count} blueprint(s) from the stored Custom Data"
                        : read[0] == null ? "the mod finds the section but not its blueprint: its data or check is damaged"
                        : same ? "the mod reads the same blueprint back from the stored Custom Data"
                        : $"the mod reads a different blueprint back, differing at {FirstDifference(original[0], read[0])}");
                }
            }
            finally
            {
                settings.OffensiveWordsFiltering = filtering;
                if (!string.IsNullOrEmpty(clipboard))
                    WriteClipboard(clipboard);
                grid.Grid.Close();
            }
        }

        /// <summary>Clears <paramref name="block" />'s Custom Data, then pastes <paramref name="section" /> into it through the game's Custom Data editor and closes it with OK.</summary>
        IEnumerator<int> Paste(MyTerminalBlock block, string section, string label)
        {
            block.CustomData = "";
            yield return PluginUpdates.PerSecond / 2;

            // The editor's paste reads the clipboard on a thread of its own and fails the whole game where the clipboard cannot be
            // opened, so the clipboard is tried first: a game started from a process denied the clipboard is denied it too.
            WriteClipboard(section);
            var clipboard = ReadClipboard() == section;

            // The terminal's Custom Data button opens the editor this way, editable, through the server.
            block.OpenWindow(true, true);
            yield return PluginUpdates.PerSecond;
            var screen = MyScreenManager.GetFirstScreenOfType<MyGuiScreenTextPanel>();
            var editor = screen?.Description as MyGuiControlMultilineEditableText;
            if (editor == null)
            {
                _report.Fail($"shared text {label} editor", "the Custom Data button opened no editable text screen");
                yield break;
            }

            screen.FocusedControl = editor;
            yield return PluginUpdates.PerSecond / 2;

            var keyed = clipboard && InFront();
            if (keyed)
            {
                keybd_event(Control, 0, 0, UIntPtr.Zero);
                keybd_event(V, 0, 0, UIntPtr.Zero);
                yield return 2;
                keybd_event(V, 0, KeyUp, UIntPtr.Zero);
                keybd_event(Control, 0, KeyUp, UIntPtr.Zero);
                yield return PluginUpdates.PerSecond / 2;
            }

            if (editor.Text.Length > 0)
            {
                _report.Pass($"shared text {label} paste", $"Ctrl+V on the keyboard pasted {editor.Text.Length} characters into the Custom Data editor");
            }
            else if (clipboard)
            {
                // The editor's paste, as Ctrl+V calls it: it reads the clipboard and puts its text in at the caret.
                var selection = typeof(MyGuiControlMultilineText).GetField("m_selection", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(editor);
                var paste = selection?.GetType().GetMethod("PasteText");
                paste?.Invoke(selection, new object[] { editor });
                var pasted = editor.Text.Length > 0;
                _report.Check($"shared text {label} paste", pasted, (keyed ? "Ctrl+V on the keyboard reached nothing" : "the game's window was not in front, so no key was pressed") +
                    (pasted ? $"; the editor's own paste, called as Ctrl+V calls it, pasted {editor.Text.Length} characters from the clipboard" : paste == null ? "; the editor's paste was not found" : "; the editor's own paste pasted nothing"));
            }
            else
            {
                // A stand-in for the paste, which cannot run without the clipboard: the paste refuses a text holding any character
                // XML cannot hold, and otherwise sets the editor's text to what was before the caret, the text, and what was after,
                // its Windows line breaks made plain. The editor is empty, so its text is set to the section, through the same
                // setter, which runs the editor's offensive-words check.
                var refused = -1;
                for (var i = 0; i < section.Length && refused < 0; i++)
                {
                    if (!XmlConvert.IsXmlChar(section[i]))
                        refused = i;
                }

                _report.Check($"shared text {label} paste allowed", refused < 0, refused < 0
                    ? "every character of the section is one the editor's paste accepts"
                    : $"the editor's paste would paste nothing: the character at {refused} is not one XML can hold");
                editor.Text = new StringBuilder(section.Replace("\r\n", "\n"));
                _report.Note($"shared text {label}: the clipboard could not be used from this game, so the editor's text was set as its paste sets it, not pasted");
            }

            _report.Note($"shared text {label}: the editor {(editor.TextIsOffensive ? "finds the text offensive" : "finds nothing offensive in the text")}");

            // OK, as its button presses it, which stores what the editor holds as the block's Custom Data.
            typeof(MyGuiScreenText).GetMethod("OkButtonClicked", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(screen, new object[] { null });
            yield return PluginUpdates.PerSecond;
            var closed = MyScreenManager.GetFirstScreenOfType<MyGuiScreenTextPanel>() == null;
            _report.Check($"shared text {label} closed", closed, closed ? "OK closed the Custom Data editor" : "the Custom Data editor stayed open after OK");
        }

        /// <summary>What the world's offensive-words filter does with the first word of the game's own list, so a run shows the filter was live.</summary>
        static string FilterState()
        {
            var words = MyDefinitionManager.Static.GetOffensiveWordsForPlatform();
            if (words == null || words.Blacklist == null || words.Blacklist.Count == 0)
                return "the game has no offensive words for this platform, so its filter has nothing to find";

            var flagged = MyScreenManager.ValidateText(new StringBuilder("a " + words.Blacklist[0] + " b")) != null;
            return $"the game's list has {words.Blacklist.Count} offensive words, and with filtering on it {(flagged ? "flags" : "does not flag")} the first of them";
        }

        /// <summary>The mod's BlueprintText type, or null where no loaded assembly has it.</summary>
        static Type FindText()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(Text, false);
                if (type != null)
                    return type;
            }

            return null;
        }

        /// <summary>The blueprint of each section the mod reads from <paramref name="custom" />, null for one it finds damaged.</summary>
        static List<string> Blueprints(Type text, string custom)
        {
            var found = new List<string>();
            foreach (dynamic shared in (IEnumerable)text.GetMethod("Read").Invoke(null, new object[] { custom }))
                found.Add((string)shared.Blueprint);
            return found;
        }

        /// <summary>The first position where <paramref name="a" /> and <paramref name="b" /> differ, or -1 where they are the same.</summary>
        static int FirstDifference(string a, string b)
        {
            var length = Math.Min(a.Length, b.Length);
            for (var i = 0; i < length; i++)
            {
                if (a[i] != b[i])
                    return i;
            }

            return a.Length == b.Length ? -1 : length;
        }

        /// <summary>A few characters of <paramref name="text" /> from <paramref name="at" />, with their code points.</summary>
        static string Around(string text, int at)
        {
            var part = at < text.Length ? text.Substring(at, Math.Min(8, text.Length - at)) : "";
            var points = new StringBuilder();
            foreach (var c in part)
                points.Append(' ').Append(((int)c).ToString("X4"));
            return $"\"{part.Replace("\n", "\\n")}\" ({(points.Length == 0 ? "end" : points.ToString().Trim())})";
        }

        /// <summary>Whether the game's own window is the one in front, so keys pressed go to it.</summary>
        static bool InFront()
        {
            uint process;
            GetWindowThreadProcessId(GetForegroundWindow(), out process);
            return process == (uint)Process.GetCurrentProcess().Id;
        }

        /// <summary>The text on the system clipboard, or null where it holds none or another program keeps it open past a few tries.</summary>
        public static string ReadClipboard()
        {
            string text = null;
            var thread = new Thread(() =>
            {
                for (var tries = 0; tries < 10; tries++)
                {
                    try
                    {
                        text = MyVRage.Platform.System.Clipboard;
                        return;
                    }
                    catch (ExternalException)
                    {
                        Thread.Sleep(50);
                    }
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            return text;
        }

        /// <summary>Puts <paramref name="text" /> on the system clipboard.</summary>
        public static void WriteClipboard(string text)
        {
            MyVRage.Platform.System.Clipboard = text;
        }

        [DllImport("user32.dll")]
        static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    }
}
