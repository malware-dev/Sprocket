using System;
using System.Collections.Generic;
using Microsoft.CSharp.RuntimeBinder;

namespace Sprocket.GameTests
{
    /// <summary>
    /// A setting made, answered and used in the game client on Kept Automation's logic system: made from the installation panel's
    /// "+ slot" menu, its row showing its default, answered for this installation, and given to a step's value through the
    /// value's mark. The harness cannot show this: it is the real panel and its popups drawn by the game's renderer on a real
    /// construct, with the real pictures of each stage for the maintainer to look at.
    /// </summary>
    public sealed class SettingsClientTest
    {
        const string Block = "Kept Automation";
        const string KeptSystem = "Kept Here";
        const string Setting = "Greeting";
        const string Default = "hello";
        const string Own = "ahoy";

        readonly EditorTour _tour;
        readonly GameTestReport _report;
        readonly EditorInput _input;

        /// <summary>The test, pictured by <paramref name="tour" />, answering into <paramref name="report" />, pressing with <paramref name="input" />.</summary>
        public SettingsClientTest(EditorTour tour, GameTestReport report, EditorInput input)
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
            string add = editor.Texts.SlotAddSetting;

            // Switched off so the panel can be changed, and the panel's "+ slot" menu offers a setting.
            var opened = _tour.Tour(editor, new[] { new[] { "settings panel opened", KeptSystem, "off", "Installation panel", "add:" + add } }, "");
            while (opened.MoveNext())
                yield return opened.Current;

            dynamic asking = editor.Current.Asking;
            if (asking == null)
            {
                _report.Fail("settings panel", $"\"{add}\" in the + slot menu opened no panel for a setting; the page offers {editor.Buttons()}");
                _tour.Close();
                yield break;
            }

            asking.Form.Fields[0].Text = Setting;
            asking.Form.Fields[2].Text = Default;
            asking.Done();
            yield return 2 * PluginUpdates.PerSecond;

            dynamic row = RowOf(editor, Setting);
            _report.Check("settings row made", row != null, row == null
                ? $"no row called {Setting} among the installation panel's slots after Done"
                : $"the panel shows a row called {Setting}, its mark {row.Mark}");
            if (row == null)
            {
                _tour.Close();
                yield break;
            }

            string shown = row.ValueChip.Text;
            _report.Check("settings row shows the default", shown != null && shown.Contains(Default) && !(bool)row.ValueChip.IsEmpty,
                $"the row's value says \"{shown}\", with the default {Default} given and the part {((bool)row.ValueChip.IsEmpty ? "empty" : "filled")}");
            var made = _tour.Tour(editor, new[] { new[] { "settings row with default", "stay" } }, "");
            while (made.MoveNext())
                yield return made.Current;

            // This installation's own value, pressed on the row's value.
            row.Fills.Execute();
            yield return 2 * PluginUpdates.PerSecond;
            asking = editor.Current.Asking;
            if (asking == null)
            {
                _report.Fail("settings answer", "pressing the value of the setting's row opened no panel");
            }
            else
            {
                string started = asking.Form.Fields[0].Text;
                _report.Check("settings answer starts at the default", started != null && started.Contains(Default), $"the panel for this installation's value starts at \"{started}\"");
                var panel = _tour.Tour(editor, new[] { new[] { "settings answer panel", "stay" } }, "");
                while (panel.MoveNext())
                    yield return panel.Current;

                asking.Form.Fields[0].Text = Own;
                asking.Done();
                yield return 2 * PluginUpdates.PerSecond;

                row = RowOf(editor, Setting);
                shown = row == null ? null : row.ValueChip.Text;
                _report.Check("settings row shows the answer", shown != null && shown.Contains(Own), $"the row's value now says \"{shown}\"");
                var answered = _tour.Tour(editor, new[] { new[] { "settings row answered", "stay" } }, "");
                while (answered.MoveNext())
                    yield return answered.Current;
            }

            // A percentage, chosen in what the setting holds: its default set with a slider, its row said with its unit.
            var percent = _tour.Tour(editor, new[] { new[] { "settings percentage panel", "stay", "add:" + add } }, "");
            while (percent.MoveNext())
                yield return percent.Current;

            asking = editor.Current.Asking;
            string percentage = editor.Texts.SettingKindPercent;
            var chose = false;
            if (asking != null)
            {
                foreach (dynamic item in asking.Form.Fields[1].Items)
                {
                    if ((string)item.Text != percentage)
                        continue;

                    item.Command.Execute();
                    chose = true;
                    break;
                }
            }

            yield return PluginUpdates.PerSecond;
            asking = editor.Current.Asking;
            var slider = asking != null && chose && asking.Form.Fields[2].GetType().Name == "SliderField";
            _report.Check("settings percentage is slid", slider,
                $"\"{percentage}\" {(chose ? "was" : "was not")} chosen in what the setting holds, and its default is {(asking == null ? "not asked" : (string)asking.Form.Fields[2].GetType().Name)}");
            var slid = _tour.Tour(editor, new[] { new[] { "settings percentage slider", "stay" } }, "");
            while (slid.MoveNext())
                yield return slid.Current;

            if (asking != null)
            {
                asking.Form.Fields[0].Text = "Opening";
                asking.Form.Fields[2].Text = "50";
                asking.Done();
                yield return 2 * PluginUpdates.PerSecond;
            }

            dynamic opening = RowOf(editor, "Opening");
            string said = opening == null ? null : opening.ValueChip.Text;
            _report.Check("settings percentage says its unit", said == "50 %", $"the row of Opening says \"{said ?? "nothing, no row"}\"");
            var unit = _tour.Tour(editor, new[] { new[] { "settings percentage row", "stay" } }, "");
            while (unit.MoveNext())
                yield return unit.Current;

            // A step's value given by the setting, through the mark after its field in the selector's arguments.
            var picking = _tour.Tour(editor, new[]
            {
                new[] { "settings step picker", KeptSystem, "Sequences", "Sequence 1", "add:task", "wait", "hover:" + Block, "click", "hover:show text", "click" }
            }, "");
            while (picking.MoveNext())
                yield return picking.Current;

            var link = InGameEditor.Symbol("Link");
            if (link == null || !_input.PressNamed(link))
            {
                _report.Fail("settings value mark", $"no value mark (symbol {link ?? "unknown"}) is showing among the selector's windows; the page offers {editor.Buttons()}");
            }
            else
            {
                yield return 2 * PluginUpdates.PerSecond;
                var offers = editor.AnswerTexts();
                var sources = string.Join(" | ", offers);
                _report.Note($"settings value mark offers: {sources}");
                string chosen = null;
                foreach (var offer in offers)
                {
                    if (offer.StartsWith(Setting))
                        chosen = offer;
                }

                var offered = _tour.Tour(editor, new[] { new[] { "settings value mark menu", "stay" } }, "");
                while (offered.MoveNext())
                    yield return offered.Current;

                _report.Check("settings offered for the value", chosen != null, chosen == null ? $"the mark offers {sources}, none of them {Setting}" : $"the mark offers {chosen}");
                if (chosen != null)
                {
                    editor.Answer(chosen);
                    yield return PluginUpdates.PerSecond;
                    var given = _tour.Tour(editor, new[] { new[] { "settings value given", "stay" } }, "");
                    while (given.MoveNext())
                        yield return given.Current;

                    var done = editor.Done();
                    yield return 2 * PluginUpdates.PerSecond;
                    var used = Uses(editor, Setting);
                    _report.Check("settings used by a step", done && used, $"Done {(done ? "was" : "could not be")} pressed; a step of {KeptSystem} {(used ? "now takes" : "does not take")} its value from {Setting}");
                    var step = _tour.Tour(editor, new[] { new[] { "settings step uses it", "stay" } }, "");
                    while (step.MoveNext())
                        yield return step.Current;
                }
            }

            // Left as it was found: switched on.
            var restored = _tour.Tour(editor, new[] { new[] { "settings system on again", KeptSystem, "on" } }, "");
            while (restored.MoveNext())
                yield return restored.Current;

            _tour.Close();
            yield return PluginUpdates.PerSecond;
        }

        /// <summary>The installation panel's row of the slot or setting called <paramref name="name" />, or null where there is none.</summary>
        static object RowOf(InGameEditor editor, string name)
        {
            foreach (dynamic row in editor.Current.Slots)
            {
                if (row.GetType().Name == "PartRow" && (string)row.Name == name)
                    return row;
            }

            return null;
        }

        /// <summary>Whether a step of the sequences of <see cref="KeptSystem" /> gives a value from the setting <paramref name="name" />.</summary>
        static bool Uses(InGameEditor editor, string name)
        {
            foreach (dynamic sequence in editor.Program.Sequences)
            {
                if ((string)sequence.System.Name != KeptSystem)
                    continue;

                foreach (dynamic step in sequence.Steps)
                {
                    try
                    {
                        foreach (dynamic argument in step.Arguments)
                        {
                            if ((bool)argument.IsSetting && (string)argument.Text == name)
                                return true;
                        }
                    }
                    catch (RuntimeBinderException)
                    {
                    }
                }
            }

            return false;
        }
    }
}
