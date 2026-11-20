using Sprocket.Host.SpaceEngineers.Features.Selector.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Selector.Members
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
        readonly List<MemberKey> _keys = new List<MemberKey>();
        readonly List<string> _owners = new List<string>();
        readonly Dictionary<string, int> _valueIds = new Dictionary<string, int>();
        readonly Dictionary<string, int> _otherIds = new Dictionary<string, int>();
        readonly List<ReachKind> _forms = new List<ReachKind>();
        readonly List<int> _groups = new List<int>();
        readonly List<string> _marks = new List<string>();
        readonly List<bool> _sets = new List<bool>();
        readonly List<Readiness> _ready = new List<Readiness>();
        readonly List<string> _why = new List<string>();
        readonly List<HostType> _types = new List<HostType>();
        readonly List<bool> _slotted = new List<bool>();
        readonly List<string> _ownGroups = new List<string>();

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

        /// <summary>Whether the offer at <paramref name="at" /> is a status word of the block.</summary>
        public bool IsStatusWord(int at)
        {
            return _groups[at] == Statuses;
        }

        /// <summary>What names each offer: its namespace and its id.</summary>
        public List<MemberKey> Keys => _keys;

        /// <summary>
        /// The name of what adds each offer, shown under it where another offer shares its id, as the title its namespace is
        /// registered with or the namespace as written; empty where its id is the only one of its kind listed.
        /// </summary>
        public List<string> Owners => _owners;

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

        /// <summary>The type the host provides each offer is a member of, such as a screen, or null where it is the block's.</summary>
        public List<HostType> Types => _types;

        /// <summary>Whether each offer is listed only for a slot of its type.</summary>
        public List<bool> InSlotsOnly => _slotted;

        /// <summary>
        /// Lists what <paramref name="selection" /> offers, numbers alone where <paramref name="totals" /> asks for a sum or an
        /// average, and only the member <paramref name="only" /> where that is not null.
        /// </summary>
        public void Fill(List<long> selection, bool totals, MemberKey? only)
        {
            _names.Clear();
            _keys.Clear();
            _owners.Clear();
            _forms.Clear();
            _groups.Clear();
            _marks.Clear();
            _sets.Clear();
            _ready.Clear();
            _why.Clear();
            _types.Clear();
            _slotted.Clear();
            _ownGroups.Clear();

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
                    if (_signalsOnly && !entityEvent.Answered || entityEvent.Vetted == _editor.ShowsTerminal)
                        continue;

                    _names.Add(entityEvent.Text);
                    _marks.Add(MarkText.Of(entityEvent));

                    _keys.Add(entityEvent.Key);
                    _sets.Add(false);
                    _forms.Add(ReachKind.Native);
                    _groups.Add(Moments);
                    _ready.Add(Readiness.Ready);
                    _why.Add(null);
                    _types.Add(null);
                    _slotted.Add(false);
                }

                // A number on the blocks can start something as it comes to compare as asked: the states already come as
                // moments, so numbers alone are listed.
                if (!_signalsOnly)
                {
                    var actions = Vocabulary.ActionsOf(_editor.World, selection);
                    foreach (var action in actions)
                    {
                        if (!action.IsStatus && (action.OwnValue != null || (action.Sets && Lists(action))) && Number(action))
                            Read(action, Values);
                    }
                }
            }
            else
            {
                var actions = Vocabulary.ActionsOf(_editor.World, selection);
                if (only != null)
                    actions.RemoveAll(action => action.Key != only.Value);

                Grouped(actions);
            }

            Own();
        }

        /// <summary>Names what adds each offer whose id another offer of the same kind shares, so the two are told apart.</summary>
        /// <remarks>Once for each fill of the pick: one count of the offers, then one look for each.</remarks>
        void Own()
        {
            _valueIds.Clear();
            _otherIds.Clear();
            for (var i = 0; i < _keys.Count; i++)
            {
                var counted = _forms[i] == ReachKind.Value ? _valueIds : _otherIds;
                int had;
                counted.TryGetValue(_keys[i].Id, out had);
                counted[_keys[i].Id] = had + 1;
            }

            for (var i = 0; i < _keys.Count; i++)
            {
                var counted = _forms[i] == ReachKind.Value ? _valueIds : _otherIds;
                _owners.Add(counted[_keys[i].Id] > 1 ? Owner(_keys[i].Namespace) : "");
            }
        }

        /// <summary>What a list calls what adds the members of the namespace <paramref name="space" />: the title it is registered with, such as a mod's or the game's, or the namespace as written where none is known.</summary>
        string Owner(string space)
        {
            var known = HostNamespaces.Find(space);
            return known != null && !string.IsNullOrEmpty(known.Title) ? known.Title : space;
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

            Gather(actions);
            for (var place = 0; place < GroupCount; place++)
            {
                var group = GroupAt(place);
                foreach (var one in actions)
                {
                    if (!one.IsStatus && (one.OwnValue != null || (one.Sets && Lists(one))) && Grouping(one) == group
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
                _keys.Add(new MemberKey(status.Namespace, ComparisonQuestion.Id(status.Id, states.Means == null ? word : states.Means[i])));
                _forms.Add(ReachKind.Native);
                _groups.Add(Statuses);
                _marks.Add(Symbols.Value);
                _sets.Add(false);
                _ready.Add(status.Readiness);
                _why.Add(ReadinessText.Why(_editor.Texts, status.Why));
                _types.Add(null);
                _slotted.Add(false);
            }
        }

        /// <summary>Lists the value <paramref name="one" /> to be read, under <paramref name="group" />.</summary>
        void Read(EntityAction one, int group)
        {
            _names.Add(Said(one));
            _keys.Add(one.Key);
            _forms.Add(ReachKind.Value);
            _groups.Add(group);
            _marks.Add(Symbols.Value);
            _sets.Add(true);
            _ready.Add(one.Readiness);
            _why.Add(ReadinessText.Why(_editor.Texts, one.Why));
            _types.Add(one.Type);
            _slotted.Add(one.InSlotsOnly);
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
            Gather(actions);
            for (var place = 0; place < GroupCount; place++)
                Listed(actions, GroupAt(place));
        }

        void Listed(List<EntityAction> actions, int group)
        {
            foreach (var one in actions)
            {
                if (!Lists(one) || Group(one) != group)
                    continue;

                _names.Add(Said(one));
                _keys.Add(one.Key);
                _forms.Add(one.Form);
                _groups.Add(group);
                _marks.Add(MarkText.Of(one));
                _sets.Add(one.Sets);
                _ready.Add(one.Readiness);
                _why.Add(ReadinessText.Why(_editor.Texts, one.Why));
                _types.Add(one.Type);
                _slotted.Add(one.InSlotsOnly);
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

        /// <summary>Whether what is on offer under <paramref name="key" />, reached as <paramref name="form" />, can be made into a step.</summary>
        public Readiness ReadinessOf(MemberKey key, ReachKind form)
        {
            var at = At(key, form);
            return at >= 0 && at < _ready.Count ? _ready[at] : Readiness.Ready;
        }

        /// <summary>Where what is on offer under <paramref name="key" />, reached as <paramref name="form" />, is in the list, or -1 where it is not listed.</summary>
        /// <remarks>By the key and by which of the three ways it reaches the block, because a switch's value and the act that flips it share one id.</remarks>
        public int At(MemberKey key, ReachKind form)
        {
            for (var i = 0; i < _keys.Count; i++)
            {
                if (_keys[i] == key && (_forms.Count > i ? _forms[i] : ReachKind.Native) == form)
                    return i;
            }

            return -1;
        }

        /// <summary>Whether the view now showing lists <paramref name="one" />.</summary>
        bool Lists(EntityAction one)
        {
            if (one.OwnValue != null && !one.Sets)
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

            return Grouping(one);
        }

        /// <summary>The organized view's group <paramref name="one" /> stands in: its category, or the mod's own group it names.</summary>
        int Grouping(EntityAction one)
        {
            return one.Group == null ? (int)one.Category : Statuses + 1 + _ownGroups.IndexOf(one.Group);
        }

        /// <summary>Notes the groups of their own that mods stand any of <paramref name="actions" /> in, in the order they are first met.</summary>
        void Gather(List<EntityAction> actions)
        {
            foreach (var one in actions)
            {
                if (one.Group != null && !_ownGroups.Contains(one.Group))
                    _ownGroups.Add(one.Group);
            }
        }

        /// <summary>How many groups the organized view lists: Sprocket's categories and the mods' own.</summary>
        int GroupCount => (int)Category.Other + 1 + _ownGroups.Count;

        /// <summary>The group the organized view lists at <paramref name="place" />: Sprocket's categories, then the mods' own groups, then Other.</summary>
        int GroupAt(int place)
        {
            if (place < (int)Category.Other)
                return place;

            var own = place - (int)Category.Other;
            return own < _ownGroups.Count ? Statuses + 1 + own : (int)Category.Other;
        }

        /// <summary>What the group <paramref name="group" /> is headed, in the view now showing.</summary>
        public string Heading(int group)
        {
            // What starts something lists its moments under no heading, and its numbers under one.
            if (_forEvents)
                return _editor.Texts.PickValues;

            if (_editor.ShowsTerminal)
                return group == Values ? _editor.Texts.PickValues : _editor.Texts.PickToolbarActions;

            if (group > Statuses)
                return _ownGroups[group - Statuses - 1];

            return group == Statuses ? _editor.Texts.PickStatus : CategoryText.Of(_editor.Texts, (Category)group);
        }
    }
}
