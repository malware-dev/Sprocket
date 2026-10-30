using System.Collections.Generic;
using System.Text;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Codex;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.Library
{
    /// <summary>What the library's chosen row holds, opened under it: who made it and when, its description, and the slots and installations it has.</summary>
    public partial class BlueprintDetail : VStack
    {
        readonly SprocketEditor _editor;
        readonly List<string> _names = new List<string>();

        /// <summary>The details of no blueprint, in the editor <paramref name="editor" />.</summary>
        public BlueprintDetail(SprocketEditor editor)
        {
            _editor = editor;
            CreateView();
            editor.Codex.Explain(_firstHeading);
            editor.Codex.Explain(_secondHeading);
        }

        /// <summary>Shows what <paramref name="chosen" /> holds, with <paramref name="refusal" /> where a move on it was just refused.</summary>
        public void Show(LibraryListing chosen, string refusal)
        {
            var texts = _editor.Texts;
            var live = chosen.LiveIn(_editor.Program);
            var system = chosen.Section == LibrarySection.OnConstruct ? live : chosen.Read;

            var trouble = refusal.Length > 0 ? refusal : system == null ? chosen.Trouble : "";
            _trouble.Text = trouble;
            _trouble.IsVisible = trouble.Length > 0;

            _line.IsVisible = false;
            _about.IsVisible = false;
            Say(_firstSection, _firstHeading, _first, null, null, null);
            Say(_secondSection, _secondHeading, _second, null, null, null);
            if (system == null)
                return;

            _line.Text = Made(texts, system);
            _line.IsVisible = _line.Text.Length > 0;
            _about.Markdown = system.Annotation;
            _about.IsVisible = system.Annotation.Length > 0;

            _names.Clear();
            var parts = system.Parts;
            for (var i = 0; i < parts.Count; i++)
            {
                if (!parts[i].Optional)
                    _names.Add(parts[i].Id);
            }

            Say(_firstSection, _firstHeading, _first, texts.BlueprintParts, CodexPages.Slot, _names);

            _names.Clear();
            for (var i = 0; live != null && i < live.Instances.Count; i++)
                _names.Add(SystemSummary.DescribeInstance(texts, _editor.Runner.Bindings, live, live.Instances[i]));

            Say(_secondSection, _secondHeading, _second, texts.LibraryInstallationsHeading, CodexPages.Installation, _names);
        }

        /// <summary>Shows <paramref name="names" /> under <paramref name="title" />, the term whose codex page is <paramref name="term" />, or neither where there are none.</summary>
        static void Say(Section section, RichText heading, Label list, string title, string term, List<string> names)
        {
            var shown = names != null && names.Count > 0;
            section.IsVisible = shown;
            if (!shown)
                return;

            heading.Markdown = CodexPages.Link(title.ToUpperInvariant(), term);
            var joined = new StringBuilder();
            for (var i = 0; i < names.Count; i++)
            {
                if (i > 0)
                    joined.Append(", ");

                joined.Append(names[i]);
            }

            list.Text = joined.ToString();
        }

        /// <summary>Who made <paramref name="system" /> and when it last changed, as far as either is known.</summary>
        static string Made(Texts texts, AutomationSystem system)
        {
            var by = system.Namespace;
            if (!system.Changed.HasValue)
                return by.Length == 0 ? "" : Template.Fill(texts.LibraryBy, "name", by);

            var time = LibraryPage.Time(system.Changed.Value);
            return by.Length == 0
                ? Template.Fill(texts.LibraryChanged, "time", time)
                : Template.Fill(Template.Fill(texts.LibraryByChanged, "name", by), "time", time);
        }
    }
}
