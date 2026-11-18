using Sprocket.Host.SpaceEngineers.Features.Slots;
using Sprocket.Host.SpaceEngineers.Features.Slots.Edits;
using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Format.Repairs;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Selector.Toolbar
{
    /// <summary>
    /// A picker's heading where the pick names slots of a blueprint: the slots the picks fill already in italics, the slot they
    /// change by its name, or the new slot they make, named in place; worked out again only when the picks change.
    /// </summary>
    public sealed class SlotHeading
    {
        readonly SprocketEditor _editor;
        readonly AutomationSystem _system;
        readonly string _title;
        readonly bool _forEvents;
        readonly Action<string, string> _renamed;
        string _keeps;
        readonly IList<string> _replacing;
        readonly List<long> _stated = new List<long>();
        readonly List<string> _statedSlots = new List<string>();
        string _statedChosen;
        string _statedFacet;
        SlotForecast _forecast;
        List<string> _fills;
        string _fillsText;
        string _slotName = "";
        string _newName;
        string _newKind;
        HostType _holds;
        readonly List<string> _offered = new List<string>();
        bool _named;
        bool _arriving;
        HeadingPart _part;
        Func<string, bool> _accepts;
        Action<string> _renames;
        Part _existing;
        Func<string, bool> _existingAccepts;
        Action<string> _existingRenames;

        /// <summary>
        /// The heading of a pick in <paramref name="editor" /> naming slots of <paramref name="system" />, called
        /// <paramref name="title" /> where it names none yet; of what starts something where <paramref name="forEvents" />, in place
        /// of the list <paramref name="replacing" /> or null for a new one, and of the trigger's signal slot <paramref name="keeps" />.
        /// </summary>
        public SlotHeading(SprocketEditor editor, AutomationSystem system, string title, bool forEvents, string keeps, IList<string> replacing,
            Action<string, string> renamed)
        {
            _editor = editor;
            _system = system;
            _title = title;
            _forEvents = forEvents;
            _keeps = keeps ?? "";
            _replacing = replacing;
            _renamed = renamed;
        }

        /// <summary>A name the new slot is kept from, as another about to be made has it, or empty.</summary>
        public string Avoiding { get; set; } = "";

        /// <summary>What the heading says.</summary>
        public string Text => _part != null ? _part.Text : _title;

        /// <summary>What a slot the picks make is called: what was given in the heading, or the numbered name it shows.</summary>
        public string NewName => _slotName.Length > 0 ? _slotName : _newName ?? "";

        /// <summary>Adds the heading's one part to <paramref name="parts" />, as the screen is built.</summary>
        public void Head(List<HeadingPart> parts)
        {
            _part = new HeadingPart("");
            parts.Add(_part);
            Reword();
        }

        /// <summary>
        /// Works out again which slots the picks name, where they have changed since: the blocks <paramref name="selection" />
        /// picked for <paramref name="chosen" /> on the facet <paramref name="facet" />, at the toolbar slot <paramref name="at" />
        /// where a toolbar sends it, or the slots <paramref name="picked" /> by name where <paramref name="byName" />; whether
        /// the heading changes with it.
        /// </summary>
        /// <remarks>Asked as the page is built, which every change to the picks brings about.</remarks>
        public bool Restate(IList<long> selection, string chosen, string facet, ToolbarSlot at, IList<string> picked, bool byName)
        {
            if (_statedChosen != null && _statedChosen == chosen && _statedFacet == facet && ListComparison.Same(_stated, selection)
                && ListComparison.Same(_statedSlots, picked))
                return false;

            _statedChosen = chosen;
            _statedFacet = facet;
            _stated.Clear();
            _stated.AddRange(selection);
            _statedSlots.Clear();
            _statedSlots.AddRange(picked);

            var signal = _forEvents && Signals(selection, chosen);
            var forecast = _editor.Picks.Forecast(_system, selection, chosen, facet, _forEvents, at, signal, _replacing, _keeps);
            var matched = byName ? new List<string>(picked) : forecast.Matched;
            // Named for what it will hold, which a pick can change until somebody names it themselves.
            // A forecast knows a type only once a facet is chosen, which the page does after asking.
            _holds = forecast.Type ?? (_forEvents || chosen.Length == 0 ? null : Vocabulary.TypeOf(_editor.World, selection, chosen));
            var kind = forecast.Signal ? "signal" : _holds?.Id ?? "";
            var renamed = false;
            if (forecast.Makes && (_newName == null || kind != _newKind))
            {
                renamed = _newName != null;
                _newName = _editor.Picks.NewSlotName(_system, "", forecast.Signal, _holds, Avoiding);
                _offered.Add(_newName);
                _newKind = kind;
            }

            // The name is in hand to be given the first time the picks come to make a slot of their own.
            if (!byName && forecast.Makes && selection.Count > 0 && !_named)
            {
                _named = true;
                _arriving = true;
            }

            var changed = renamed || _arriving || _forecast == null || forecast.Signal != _forecast.Signal || forecast.Changes != _forecast.Changes
                || !ListComparison.Same(matched, _fills);
            _forecast = forecast;
            _fills = matched;
            _fillsText = matched == null ? null : SentenceText.Slots(matched);
            return changed;
        }

        /// <summary>Puts the slot the picks name into the heading: the slots already made in italics, the slot they change, or the new slot's name to be given.</summary>
        public void Reword()
        {
            if (_part == null)
                return;

            var texts = _editor.Texts;

            // Slots picked by name leave the heading the picker's own name until one is.
            if (_fills != null && _fills.Count == 0)
            {
                Fixed(_title, false, null);
                _part.Mark = null;
                return;
            }

            _part.Mark = _forecast == null ? Symbols.Block : MarkText.OfSlot(_forecast.Signal, _holds);

            if (_fills != null)
            {
                var only = _fills.Count == 1 ? _system.FindPart(_fills[0]) : null;
                if (only != null)
                    Existing(only, true, texts.Codex.PickSlotMatchedOne);
                else
                    Fixed(_fillsText, true, texts.Codex.PickSlotMatched);

                return;
            }

            if (_forecast?.Changes != null)
            {
                Existing(_forecast.Changes, false, texts.Codex.PickSlotChanges);
                return;
            }

            // Made once, so the heading read again on its cadence finds nothing changed.
            if (_accepts == null)
            {
                _accepts = name => NameRules.Clean(name).Length == 0 || _editor.Picks.Frees(_system, name);
                _renames = _editor.Renaming(name =>
                {
                    // A numbered name this offered, handed back as it stood, is not a name somebody gave.
                    var given = NameRules.Clean(name);
                    _slotName = _offered.Contains(given) ? "" : given;
                    Reword();
                }, _accepts);
            }

            _part.Text = NewName;
            _part.Name = NewName;
            _part.After = ForScreens(texts);
            _part.Emphasized = false;
            _part.About = texts.Codex.PickSlotNameAbout;
            _part.Accepts = _accepts;
            _part.Renames = _renames;

            // Set once and left, as the editor takes it up once for the part it is set on.
            if (!_arriving)
                return;

            _part.NamedOnArrival = true;
            _arriving = false;
        }

        /// <summary>Has the heading name <paramref name="slot" />, the one slot the picks come to, renamed in place, in italics where <paramref name="emphasized" />.</summary>
        void Existing(Part slot, bool emphasized, string about)
        {
            // Made once for a slot, so the heading read again on its cadence finds nothing changed.
            if (_existing != slot)
            {
                _existing = slot;
                _existingAccepts = name =>
                {
                    var wanted = NameRules.Clean(name);
                    return wanted.Length > 0 && (_system.FindPartInAnyCase(wanted) == slot || _system.FindPartInAnyCase(wanted) == null);
                };
                _existingRenames = _editor.Renaming(name => Rename(slot, NameRules.Clean(name)), _existingAccepts);
            }

            _part.Text = slot.Id;
            _part.Name = slot.Id;
            _part.After = "";
            _part.Emphasized = emphasized;
            _part.About = about;
            _part.Accepts = _existingAccepts;
            _part.Renames = _existingRenames;
        }

        /// <summary>Calls <paramref name="slot" /> <paramref name="wanted" /> wherever its logic system names it, and in what this heading holds of the picks.</summary>
        void Rename(Part slot, string wanted)
        {
            var was = slot.Id;
            if (wanted == was)
                return;

            _editor.Do(_system, new RenamePart(slot, wanted));
            if (_keeps == was)
                _keeps = wanted;

            Respell(_replacing, was, wanted);
            Respell(_fills, was, wanted);
            Respell(_statedSlots, was, wanted);
            _fillsText = _fills == null ? null : SentenceText.Slots(_fills);
            _renamed(was, wanted);
            Reword();
        }

        static void Respell(IList<string> names, string was, string now)
        {
            for (var i = 0; names != null && i < names.Count; i++)
            {
                if (names[i] == was)
                    names[i] = now;
            }
        }

        /// <summary>Has the heading say <paramref name="text" />, in italics where <paramref name="emphasized" />, with nothing to rename and <paramref name="about" /> as its note.</summary>
        void Fixed(string text, bool emphasized, string about)
        {
            _part.Renames = null;
            _part.Accepts = null;
            _part.After = "";
            _part.Name = "";
            _part.Text = text;
            _part.Emphasized = emphasized;
            _part.About = about;
        }

        /// <summary>What a repair says about the new slot being made in place of the one the step names, or empty.</summary>
        string ForScreens(Texts texts)
        {
            Repair by;
            return _forecast?.Type == null ? "" : ProgramRepairs.Instead(texts, _system, _replacing, _forecast.Type, out by);
        }

        /// <summary>Whether <paramref name="chosen" /> is a signal a toolbar sends on the blocks <paramref name="selection" />, which a signal slot carries rather than the blocks.</summary>
        bool Signals(IList<long> selection, string chosen)
        {
            var picked = chosen.Length == 0 ? null : Vocabulary.Find(Vocabulary.EventsOf(_editor.World, selection), chosen);
            return picked != null && picked.Answered;
        }
    }
}
