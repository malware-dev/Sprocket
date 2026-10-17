using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>What a pick offers to be chosen for the blocks picked: each member, value or state, in order, under the heading of its group.</summary>
    /// <remarks>Kept as parallel lists, one entry per offer, filled again on every build of the pick.</remarks>
    public sealed class MemberOffers
    {
        const int Actions = 0;
        const int Values = 1;

        // The group a moment stands in, in a list of what starts something: under no heading, before the values.
        const int Moments = -1;

        // The group a check's status values stand in, first, past every category's.
        const int Statuses = (int)Category.Other + 1;

        static readonly TerminalNameOrder ByDeclared = new TerminalNameOrder();

        readonly SprocketEditor _editor;
        readonly bool _forEvents;
        readonly bool _forTests;
        readonly bool _signalsOnly;
        readonly List<string> _names = new List<string>();
        readonly List<string> _ids = new List<string>();
        readonly List<ReachKind> _forms = new List<ReachKind>();
        readonly List<int> _groups = new List<int>();
        readonly List<string> _marks = new List<string>();
        readonly List<bool> _sets = new List<bool>();
        readonly List<Readiness> _ready = new List<Readiness>();
        readonly List<string> _why = new List<string>();

        /// <summary>
        /// The offers of a pick in <paramref name="editor" />: of what starts something where <paramref name="forEvents" />, of
        /// what an if can check where <paramref name="forTests" />, and otherwise of what a step can do; only what the
        /// construct does to start something where <paramref name="signalsOnly" />.
        /// </summary>
        public MemberOffers(SprocketEditor editor, bool forEvents, bool forTests, bool signalsOnly)
        {
            _editor = editor;
            _forEvents = forEvents;
            _forTests = forTests;
            _signalsOnly = signalsOnly;
        }

        /// <summary>What each offer is called, in the view now showing.</summary>
        public List<string> Names => _names;

        /// <summary>The id of each offer.</summary>
        public List<string> Ids => _ids;

        /// <summary>How each offer reaches the block: an action, a value, or the block's own.</summary>
        public List<ReachKind> Forms => _forms;

        /// <summary>The group each offer is listed under.</summary>
        public List<int> Groups => _groups;

        /// <summary>The mark each offer is listed with.</summary>
        public List<string> Marks => _marks;

        /// <summary>Whether each offer sets a value.</summary>
        public List<bool> Sets => _sets;

        /// <summary>Whether each offer can be made into a step.</summary>
        public List<Readiness> Ready => _ready;

        /// <summary>Why each offer that cannot be made into a step cannot, or null.</summary>
        public List<string> Why => _why;

        /// <summary>
        /// Lists what <paramref name="selection" /> offers, numbers alone where <paramref name="totals" /> asks for a sum or an
        /// average, and only the member <paramref name="only" /> where that is not null.
        /// </summary>
        public void Fill(List<long> selection, bool totals, string only)
        {
            _names.Clear();
            _ids.Clear();
            _forms.Clear();
            _groups.Clear();
            _marks.Clear();
            _sets.Clear();
            _ready.Clear();
            _why.Clear();

            if (_forTests)
            {
                Testable(selection, totals);
            }
            else if (_forEvents)
            {
                // A total is taken of numbers alone, so while one is asked for no moment is listed.
                var events = totals ? new List<EntityEvent>() : Vocabulary.EventsOf(_editor.World, selection);
                foreach (var entityEvent in events)
                {
                    if (_signalsOnly && !entityEvent.Answered)
                        continue;

                    _names.Add(entityEvent.Text);
                    _marks.Add(MarkText.Of(entityEvent));

                    _ids.Add(entityEvent.Id);
                    _sets.Add(false);
                    _forms.Add(ReachKind.Native);
                    _groups.Add(Moments);
                    _ready.Add(Readiness.Ready);
                    _why.Add(null);
                }

                // A number on the blocks can start something as it comes to compare as asked: the states already come as
                // moments, so numbers alone are listed.
                if (!_signalsOnly)
                {
                    var actions = Vocabulary.ActionsOf(_editor.World, selection);
                    foreach (var action in actions)
                    {
                        if (!action.IsStatus && (action.Being != null || (action.Sets && Lists(action))) && Number(action))
                            Read(action, Values);
                    }
                }
            }
            else
            {
                var actions = Vocabulary.ActionsOf(_editor.World, selection);
                if (only != null)
                    actions.RemoveAll(action => action.Id != only);

                Grouped(actions);
            }
        }

        /// <summary>Whether <paramref name="value" /> is a number, which is what a sum can be taken of.</summary>
        public static bool Number(EntityAction value)
        {
            return value != null && (value.Holds == ValueKind.Integer || value.Holds == ValueKind.Real);
        }

        /// <summary>What a test can read of the blocks, in the view now showing.</summary>
        void Testable(List<long> selection, bool totals)
        {
            var actions = Vocabulary.ActionsOf(_editor.World, selection);
            if (_editor.ShowsTerminal)
            {
                actions.Sort(ByDeclared);
                foreach (var action in actions)
                {
                    if (action.Sets && action.Unaltered && (!totals || Number(action)))
                        Read(action, Values);
                }

                return;
            }

            // A sum or an average is taken of numbers alone, so while one is asked for nothing else is listed.
            var sums = totals;
            var events = Vocabulary.EventsOf(_editor.World, selection);
            for (var i = 0; i < actions.Count && !sums; i++)
            {
                if (actions[i].IsStatus)
                    States(actions[i], events);
            }

            for (var group = 0; group <= (int)Category.Other; group++)
            {
                foreach (var one in actions)
                {
                    if (!one.IsStatus && (one.Being != null || (one.Sets && Lists(one))) && (int)one.Category == group
                        && (!sums || Number(one)))
                        Read(one, group);
                }
            }
        }

        /// <summary>Lists each state the status <paramref name="status" /> can be in, in the words <paramref name="events" /> say it in, to be checked for.</summary>
        void States(EntityAction status, List<EntityEvent> events)
        {
            var fields = ComparisonQuestion.Fields(status, events);
            var states = fields.Count > 1 ? fields[1] : default(Field);
            if (states.Words == null)
                return;

            for (var i = 0; i < states.Words.Count; i++)
            {
                var word = states.Words[i];
                _names.Add(word);
                _ids.Add(ComparisonQuestion.Id(status.Id, states.Means == null ? word : states.Means[i]));
                _forms.Add(ReachKind.Native);
                _groups.Add(Statuses);
                _marks.Add(Symbols.Value);
                _sets.Add(false);
                _ready.Add(status.Readiness);
                _why.Add(ReadinessText.Why(_editor.Texts, status.Why));
            }
        }

        /// <summary>Lists the value <paramref name="one" /> to be read, under <paramref name="group" />.</summary>
        void Read(EntityAction one, int group)
        {
            _names.Add(Said(one));
            _ids.Add(one.Id);
            _forms.Add(ReachKind.Value);
            _groups.Add(group);
            _marks.Add(Symbols.Value);
            _sets.Add(true);
            _ready.Add(one.Readiness);
            _why.Add(ReadinessText.Why(_editor.Texts, one.Why));
        }

        /// <summary>Everything the blocks offer, values first and each group in its own order.</summary>
        void Grouped(List<EntityAction> actions)
        {
            if (_editor.ShowsTerminal)
            {
                actions.Sort(ByDeclared);
                Listed(actions, Values);
                Listed(actions, Actions);
                return;
            }

            // Sprocket's groups, in the order the categories are declared, so a member sits in the same place on every block.
            for (var group = 0; group <= (int)Category.Other; group++)
                Listed(actions, group);
        }

        void Listed(List<EntityAction> actions, int group)
        {
            foreach (var one in actions)
            {
                if (!Lists(one) || Group(one) != group)
                    continue;

                _names.Add(Said(one));
                _ids.Add(one.Id);
                _forms.Add(one.Form);
                _groups.Add(group);
                _marks.Add(MarkText.Of(one));
                _sets.Add(one.Sets);
                _ready.Add(one.Readiness);
                _why.Add(ReadinessText.Why(_editor.Texts, one.Why));
            }
        }

        /// <summary>What the view now showing calls <paramref name="one" />: the block's own words in the terminal view, as-is, and Sprocket's in the organized one, in lower case.</summary>
        string Said(EntityAction one)
        {
            if (_editor.ShowsTerminal)
                return one.Declared;

            var said = one.Sets ? one.Text : one.Named;
            if (one.Unaltered && !one.Renamed)
                said = Unlabelled(said);

            return SentenceText.Lowered(said);
        }

        /// <summary>The game's name for a member, without the colon it ends a terminal label with.</summary>
        /// <remarks>Only ever given the game's own names: a word Sprocket or a mod's definition chose is left as written.</remarks>
        static string Unlabelled(string named)
        {
            if (string.IsNullOrEmpty(named))
                return named;

            var trimmed = named.TrimEnd();
            return trimmed.EndsWith(":", StringComparison.Ordinal)
                ? trimmed.Substring(0, trimmed.Length - 1).TrimEnd()
                : named;
        }

        /// <summary>Whether what is on offer under <paramref name="id" />, reached as <paramref name="form" />, can be made into a step.</summary>
        public Readiness ReadinessOf(string id, ReachKind form)
        {
            var at = At(id, form);
            return at >= 0 && at < _ready.Count ? _ready[at] : Readiness.Ready;
        }

        /// <summary>Where what is on offer under <paramref name="id" />, reached as <paramref name="form" />, is in the list, or -1 where it is not listed.</summary>
        /// <remarks>By the id and by which of the three ways it reaches the block, because a switch's value and the act that flips it share one id.</remarks>
        public int At(string id, ReachKind form)
        {
            for (var i = 0; i < _ids.Count; i++)
            {
                if (_ids[i] == id && (_forms.Count > i ? _forms[i] : ReachKind.Native) == form)
                    return i;
            }

            return -1;
        }

        /// <summary>Whether the view now showing lists <paramref name="one" />.</summary>
        bool Lists(EntityAction one)
        {
            if (one.Being != null && !one.Sets)
                return false;

            if (_editor.ShowsTerminal)
                return one.Unaltered;

            return one.Vetted && !one.Hidden && one.Readiness == Readiness.Ready;
        }

        /// <summary>Which group of the view now showing <paramref name="one" /> is listed in: the terminal's two kinds of member, or the organized view's categories.</summary>
        int Group(EntityAction one)
        {
            if (_editor.ShowsTerminal)
                return one.Sets ? Values : Actions;

            return (int)one.Category;
        }

        /// <summary>What the group <paramref name="group" /> is headed, in the view now showing.</summary>
        public string Heading(int group)
        {
            // What starts something lists its moments under no heading, and its numbers under one.
            if (_forEvents)
                return _editor.Texts.PickValues;

            if (_editor.ShowsTerminal)
                return group == Values ? _editor.Texts.PickValues : _editor.Texts.PickToolbarActions;

            return group == Statuses ? _editor.Texts.PickStatus : CategoryText.Of(_editor.Texts, (Category)group);
        }
    }
}
