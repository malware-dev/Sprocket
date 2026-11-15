using Sprocket.Host.SpaceEngineers.Features.Selector.Blocks;
using Sprocket.Host.SpaceEngineers.Features.Selector.Members;
using Sprocket.Host.SpaceEngineers.Features.Selector.Toolbar;
using Sprocket.Host.SpaceEngineers.Features.Triggers;
using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System;
using System.Collections.Generic;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Selector
{
    /// <summary>Picks blocks and then what happens to them, with both stages on screen at once.</summary>
    /// <remarks>
    /// The page keeps its own answer and hands it back only when the pick is done.
    /// </remarks>
    public sealed partial class PickPage : EditorPage
    {
        // What a pick of blocks has picked by name.
        static readonly IList<string> NoSlots = new string[0];

        readonly string _title;
        readonly bool _forEvents;
        readonly Action<PickAnswer> _done;
        readonly OpeningColumn _opens;
        readonly OutcomeColumn _aim;
        readonly int _base;
        readonly Func<GridEntity, bool> _fits;
        readonly bool _memberOnly;
        readonly PickChoices _picked = new PickChoices();
        readonly PickChoices _arrived;
        readonly BlockStage _blocks;
        readonly MemberStage _members;
        readonly PickArguments _arguments;
        readonly PickStrip _strip;

        bool _goesOn;
        bool _straight;
        bool _selectedTarget;

        // Where the pick names slots of a blueprint: its heading.
        SlotHeading _slotHeading;
        bool _retitled;

        StageColumn _does;
        ICommand _finish;

        /// <summary>Asks what to point at and what should happen to it, starting from whatever the answer is now.</summary>
        /// <remarks>
        /// <paramref name="after" /> is the strip of the pick this page goes on in, as its next page, or null for a pick of its
        /// own. <paramref name="admits" /> says which of the blocks able to do what is asked are listed, or null for all of
        /// them. <paramref name="signalsOnly" /> offers only what the construct does to start something, for a signal slot.
        /// <paramref name="forTests" /> offers what an if can test.
        /// <paramref name="facet" /> is the name of the facet the step acts on, where what it does is a member of a type the host
        /// provides.
        /// </remarks>
        public PickPage(SprocketEditor editor, string title, bool forEvents,
            IList<long> targets, string chosen, ReachKind chosenForm, Action<PickAnswer> done,
            bool asksAll = false, Quantifier quantifier = Quantifier.Any, IList<Literal> arguments = null,
            OpeningColumn opens = OpeningColumn.Blocks, OutcomeColumn aim = null, PickStrip after = null, Func<GridEntity, bool> admits = null,
            bool signalsOnly = false, bool forTests = false, string facet = "")
            : base(editor)
        {
            _picked.Facet = facet ?? "";
            _opens = opens;
            _aim = aim;
            if (aim != null)
            {
                aim.Onward = Primary;
                aim.MovesOn = MoveOn;
            }

            _strip = after ?? new PickStrip(editor.Texts);
            _base = after == null ? 0 : after.Stages.Count;

            _title = title;
            _forEvents = forEvents;
            _picked.Member = chosen ?? "";
            _picked.Form = chosenForm;
            _picked.Reading = quantifier;
            _done = done;

            var pressed = forEvents ? SlotOccupancy.Pressing(editor.World, targets, _picked.Member) : null;
            if (pressed != null)
            {
                _picked.Slot = ToolbarSlot.Of(_picked.Member);
                _picked.Member = pressed.Id;
            }

            for (var i = 0; i < targets.Count; i++)
                _picked.Blocks.Add(targets[i]);

            // A step that flips a switch was made by choosing toggle on the switch's value, so that is what it opens on.
            var flipped = !forEvents && !forTests && chosenForm == ReachKind.Action
                ? Vocabulary.Flipped(editor.World, _picked.Blocks, _picked.Member)
                : null;
            if (flipped != null)
            {
                _picked.Member = flipped;
                _picked.Form = ReachKind.Value;
            }

            _arrived = _picked.Copy();
            _arguments = new PickArguments(editor, this, _picked, _arrived, arguments, () => IsComplete, forEvents, forTests,
                opens == OpeningColumn.Arguments);
            if (flipped != null)
                _arguments.Arrive(Literal.Of(EntityAction.Toggle));

            _arguments.Target.SourceName = () => _slotHeading?.NewName ?? "";
            _members = new MemberStage(editor, _picked, forEvents, forTests, signalsOnly);
            _blocks = new BlockStage(editor, _picked, editor.Opener, admits, forEvents, forTests, asksAll, signalsOnly);
        }

        /// <summary>Asks which blocks fill a slot: the blocks alone, only those <paramref name="fits" /> allows, starting from <paramref name="targets" />.</summary>
        public PickPage(SprocketEditor editor, string title, IList<long> targets, Func<GridEntity, bool> fits,
            Action<PickAnswer> done)
            : this(editor, title, false, targets, "", ReachKind.Native, done)
        {
            _fits = fits;
        }

        /// <summary>
        /// Asks which blocks fill a slot of a type the host provides, and which of their facets: the member
        /// <paramref name="member" /> the slot's step does, reached as <paramref name="form" /> says, offered alone, asking
        /// for none of its values, starting from <paramref name="targets" /> and the facet called <paramref name="facet" />,
        /// and only blocks <paramref name="admits" /> allows listed.
        /// </summary>
        public PickPage(SprocketEditor editor, string title, IList<long> targets, string member, ReachKind form, string facet,
            Func<GridEntity, bool> admits, Action<PickAnswer> done)
            : this(editor, title, false, targets, member, form, done, admits: admits, facet: facet)
        {
            _memberOnly = true;
        }

        /// <summary>The slot the pick names where it names one, and otherwise the picker's own name.</summary>
        public override string Title => _slotHeading != null ? _slotHeading.Text : _title;

        /// <summary>
        /// The logic system whose slots the pick names, or null where it names none. For a blueprint the heading shows them: the
        /// slots already made that the picks fill, or the new slot they make, named in place; for any other system, and where
        /// this is null, the heading is the picker's own name. Set before the page is shown.
        /// </summary>
        public AutomationSystem Slotted
        {
            get { return _arguments.Target.System; }
            set { _arguments.Target.System = value; }
        }

        /// <summary>
        /// The signal slot of the trigger being changed, which a pick of what a toolbar sends changes where it matches no signal
        /// slot; empty for a new trigger. Set before the page is shown.
        /// </summary>
        public string Keeps { get; set; } = "";

        /// <summary>Whether the first column lists the slots <see cref="Slotted" /> already has, to be picked by name, rather than blocks. Set before the page is shown.</summary>
        public bool ListsSlots { get; set; }

        /// <summary>The list of slots the pick goes in place of, a step's or a trigger's, or null for a new one. Set before the page is shown.</summary>
        public IList<string> Replacing { get; set; }

        /// <summary>A name the slot the picks make is kept from, as another about to be made has it, or empty. Set before the page is shown.</summary>
        public string Avoiding { get; set; } = "";

        /// <inheritdoc />
        public override bool Asks => true;

        /// <inheritdoc />
        /// <remarks>Nothing where what the trigger does goes on to a page of its own.</remarks>
        public override string PrimaryText => _aim == null || _aim.Outcome != Outcome.One ? Editor.Texts.ChromeDone : "";

        /// <inheritdoc />
        public override bool CanPrimary => IsComplete && (_aim == null || _aim.Finishes);

        /// <inheritdoc />
        public override string PrimaryAbout => "";

        /// <inheritdoc />
        public override string PrimaryWaits => Editor.Texts.Codex.PickDoneFirst;

        /// <summary>
        /// What the one kind every block on offer is of is called, where the blocks are limited to it as a slot limits them, said
        /// where the list of kinds would say it offers any; null where any block is on offer.
        /// </summary>
        public string Limit { get; set; }

        /// <summary>What each value the chosen action takes wants, one field each, and empty where it takes none.</summary>
        public IList<Field> Fields => _arguments.Wanted();

        /// <summary>What the values open holding, where this opened on something that has values.</summary>
        /// <remarks>
        /// The step's own values only while its own action is chosen.
        /// </remarks>
        public IList<string> Filled => _arguments.Filled;

        /// <inheritdoc />
        public override bool ScrollsItself => true;

        /// <summary>The strip the pick is drawn in, shared with the pages before and after this one.</summary>
        public PickStrip Strip => _strip;

        /// <summary>
        /// The choice between this and a selector of the logic system's states, offered in the heading of the blocks column,
        /// or null where the pick is about blocks alone. Set before the page is shown.
        /// </summary>
        public SourceSwitch Source { get; set; }

        /// <summary>Whether anything here differs from what was chosen when it opened: the blocks, what is done to them, any or all, or the values.</summary>
        bool Moved
        {
            get
            {
                if (_picked.Blocks.Count != _arrived.Blocks.Count)
                    return true;

                foreach (var id in _picked.Blocks)
                {
                    if (!BlockListing.Contains(_arrived.Blocks, id))
                        return true;
                }

                if (_picked.Member != _arrived.Member || _picked.Form != _arrived.Form || _picked.Reading != _arrived.Reading
                    || !ToolbarSlot.Same(_picked.Slot, _arrived.Slot) || Facet != _arrived.Facet)
                    return true;

                if (_aim != null && _aim.Moved)
                    return true;

                return _arguments.Moved;
            }
        }

        bool IsComplete => _picked.Blocks.Count > 0 && (_fits != null || PicksSignal && (_aim == null || _aim.Chosen)
            || !string.IsNullOrEmpty(_picked.Member)
            && _members.ReadinessOf(_picked.Member, _picked.Form) == Readiness.Ready
            && (_aim == null || _aim.Chosen));

        /// <summary>The slots listed to be picked by name, or null where blocks are listed.</summary>
        SlotListing Slots => _blocks.Slots;

        /// <summary>Whether a signal slot is picked by name, which is all a trigger needs to know of what starts it.</summary>
        bool PicksSignal => Slots != null && Slots.PicksSignal;

        /// <summary>The type the host provides that a slot picked by name holds, whose members alone are offered; null for any other.</summary>
        HostType SlotType => Slots?.Type;

        /// <summary>The slots picked by name, none where blocks are picked.</summary>
        IList<string> PickedSlots => Slots != null ? Slots.Picked : NoSlots;

        /// <summary>The stages this page asks, which are the last in the pick while it is on top.</summary>
        int Own => (_blocks.Column == null ? 0 : 1) + (_members.Column == null ? 0 : 1) + (_does == null ? 0 : 1);

        /// <summary>The stage whose box takes the keyboard when its page is first shown, or minus one where none does.</summary>
        int Opens => _opens == OpeningColumn.Blocks ? _base : _opens == OpeningColumn.What ? _base + 1 : -1;

        /// <summary>What is chosen as the program names it: the one slot, where a toolbar a person presses is chosen.</summary>
        string AnswerId => _picked.Slot == null ? _picked.Member : _picked.Slot.EventId;

        /// <summary>The facet the chosen member acts on, or empty where it is no member of a type the host provides.</summary>
        string Facet => Vocabulary.TypeOf(Editor.World, _picked.Blocks, _picked.Member) == null ? "" : _picked.Facet;

        /// <inheritdoc />
        public override bool TakeRetitle()
        {
            var retitled = _retitled;
            _retitled = false;
            return retitled;
        }

        /// <summary>The heading: the slot the pick names, where it names the slots of a blueprint, and otherwise the picker's own name.</summary>
        public override void Heading(List<HeadingPart> parts)
        {
            if (Slotted == null || !Slotted.IsBlueprint)
            {
                base.Heading(parts);
                return;
            }

            if (_slotHeading == null)
                _slotHeading = new SlotHeading(Editor, Slotted, _title, _forEvents, Keeps, Replacing, SlotRenamed) { Avoiding = Avoiding };

            Restate();
            _slotHeading.Head(parts);
        }

        /// <inheritdoc />
        public override void RefreshHeading(List<HeadingPart> parts)
        {
            if (_slotHeading == null)
                base.RefreshHeading(parts);
            else
                _slotHeading.Reword();
        }

        /// <summary>Asks before throwing away what was picked here, where there is something to lose and something to do with it.</summary>
        /// <remarks>
        /// Both answers go back; the one Enter takes keeps the work, which finishes the pick as Done does.
        /// </remarks>
        public override bool Leaving()
        {
            if (_base > 0)
                return _straight ? LeavingStraight() : true;

            var after = _aim?.Following;
            if (!IsComplete || !Moved && (after == null || !after.Moved))
                return true;

            Editor.Ask(Editor.Texts.PickKeepQuestion);
            Editor.AnswerPrimary(Editor.Texts.PickKeepThem, true);
            Editor.Answer(Editor.Texts.PickDiscardThem, () => Editor.Pop());

            return false;
        }

        /// <summary>Finishes, with the values given in the column beside the stages where what is chosen takes any.</summary>
        /// <remarks>Every value is held to what it takes from here on, and where one fails the column says why and the keyboard goes to it.</remarks>
        public override void Primary()
        {
            var arguments = _strip.Arguments;
            if (_forEvents && _picked.Form != ReachKind.Value || !_strip.ShowsArguments || arguments == null || arguments.Form.Count == 0)
            {
                Finish(new PickAnswer(_picked.Blocks, AnswerId, _picked.Reading, null, _picked.Form, Facet));
                return;
            }

            var form = arguments.Form;
            form.Judge();
            arguments.Why = form.Wrong;
            if (form.Wrong.Length > 0 || form.Missing)
            {
                form.Take(Editor.Input);
                return;
            }

            Give(form.Answer);
        }

        /// <summary>Finishes with the values <paramref name="said" /> gives, written as a line of values.</summary>
        public void Give(string said)
        {
            var arguments = _arguments.Read(said);

            // Toggle on a switch is the game's toggle for it, which takes nothing.
            if (Vocabulary.Toggles(_picked.Form, arguments))
            {
                var toggle = Vocabulary.Toggle(Editor.World, _picked.Blocks, _picked.Member) ?? _picked.Member;
                Finish(new PickAnswer(_picked.Blocks, toggle, _picked.Reading, new List<Literal>(), ReachKind.Action, Facet));
                return;
            }

            Finish(new PickAnswer(_picked.Blocks, _picked.Member, _picked.Reading, arguments, _picked.Form, Facet));
        }

        /// <inheritdoc />
        public override void Build()
        {
            // A slot of a type the host provides, picked by name, is acted on at the facet it holds.
            if (SlotType != null)
                _picked.Facet = Slots.Facet;

            _blocks.Collect(_fits);

            if (_aim != null)
                _aim.Pick = _strip;

            // Back from a page after this one, whatever that page asked goes with it; and back here from the page before,
            // what this one asked is put back.
            _strip.Return(_base, _base + Own);
            if (_strip.Stages.Count == _base && _blocks.Column != null)
            {
                _strip.Add(_blocks.Column);
                if (_members.Column != null)
                    _strip.Add(_members.Column);

                if (_does != null)
                    _strip.Add(_does);
            }

            _strip.Returns = _base > 0 ? Editor.Act(CommandKind.PageBack, PageBack) : null;
            _strip.Onward = _aim != null && _aim.Outcome == Outcome.One ? Editor.Act(CommandKind.PageOn, Primary) : null;

            if (_finish == null)
                _finish = Editor.Act(CommandKind.Primary, Primary);

            if (_blocks.Column == null)
                _strip.Add(_blocks.Make(_strip.Width, Source, ListsSlots, Slotted, _fits != null, MoveOn));

            _blocks.Fill(Limit);

            // The second stage exists only once there is something for it to be about, and never where only blocks are asked.
            var wanted = _picked.Blocks.Count > 0 && _fits == null && !PicksSignal;
            if (wanted && _members.Column == null)
            {
                // The stages stand in order, so the column of what the trigger does is taken off and made again after this one.
                DropDoes();
                _strip.Add(_members.Make(_strip.Width, MoveOn));
            }
            else if (!wanted && _members.Column != null)
            {
                DropDoes();
                _strip.RemoveLast();
                _members.Drop();
            }

            if (_members.Column != null)
                _members.Fill(SlotType, _memberOnly ? _arrived.Member : null);

            // What the trigger does, beside what happens, once what happens is chosen or a signal slot is picked.
            var does = _aim != null && (PicksSignal || _members.Column != null && !string.IsNullOrEmpty(_picked.Member));
            if (does && _does == null)
            {
                _does = new StageColumn(_strip.Width, false, null, Editor.Texts);
                _does.Title = Editor.Texts.PickWhatItDoes;
                _strip.Add(_does);
            }
            else if (!does)
                DropDoes();

            if (_does != null)
                _does.Title = string.IsNullOrEmpty(_members.ChosenName) ? Editor.Texts.PickWhatItDoes : _members.ChosenName;

            // A moment is described and takes nothing; a number watched asks how it compares, and with what, over what the trigger does.
            if (_memberOnly)
                _strip.ShowsArguments = false;
            else
                _arguments.Fill(_strip, _members.Column, _does, _members.ChosenName, _members.ChosenAbout, () => Title, _finish);

            if (_does != null && _aim != null)
                _aim.Fill(_does);

            if (_slotHeading != null && Restate())
                _retitled = true;

            _strip.Show();
            _strip.Arrive(Opens, Editor.Input);

            if (_opens == OpeningColumn.Target)
                SelectTarget();

            if (!_goesOn)
                return;

            _goesOn = false;
            Primary();
        }

        /// <summary>Opens the selector of the target the page was opened for; back from it with nothing selected, this page goes too, having never been looked at.</summary>
        void SelectTarget()
        {
            if (!_selectedTarget)
            {
                _selectedTarget = _arguments.Target.SelectFirst(_strip, _finish);
                return;
            }

            if (_arguments.Target.IsChosen)
                return;

            Editor.Pop();
            if (_straight)
                Editor.Pop();
        }

        /// <summary>Moves the strip's page turning on a tick.</summary>
        public override void Refresh()
        {
            _strip.Tick();
        }

        /// <summary>Goes straight on to the page after this one as soon as this is built, for a part pressed that stands for something on that page.</summary>
        /// <remarks>Built first all the same, because the page after carries on from the stages this one asks.</remarks>
        public void GoOn()
        {
            _goesOn = true;
        }

        /// <summary>Says whether this page is being opened straight onto, rather than paged to.</summary>
        public void Arrived(bool straight)
        {
            _straight = straight;
        }

        /// <summary>
        /// Chooses the toolbar a person presses that has a slot read as <paramref name="words" />, "button 2 is pressed", and
        /// then that slot, as pressing the toolbar's line and then the slot under it does; false where no slot reads so.
        /// </summary>
        public bool ChooseSlot(string words) => _members.ChooseSlot(words);

        /// <summary>Gives the keyboard to the box of the column the pick opens on, now that there is a screen for it.</summary>
        protected override void ViewCreated()
        {
            _strip.Arrive(Opens, Editor.Input);
        }

        /// <summary>Takes the column of what the trigger does off the end of the strip, where it is there.</summary>
        void DropDoes()
        {
            if (_does == null)
                return;

            _strip.RemoveLast();
            _does = null;
        }

        /// <summary>Works out again which slots the picks name; whether the heading changes with it.</summary>
        /// <summary>Follows the slot called <paramref name="was" /> being renamed <paramref name="now" /> from the heading, in what this page holds of it.</summary>
        void SlotRenamed(string was, string now)
        {
            if (Keeps == was)
                Keeps = now;

            var picked = PickedSlots;
            for (var i = 0; i < picked.Count; i++)
            {
                if (picked[i] == was)
                    picked[i] = now;
            }
        }

        bool Restate() => _slotHeading.Restate(_picked.Blocks, _picked.Member, Facet, _picked.Slot ?? ToolbarSlot.Of(_picked.Member),
            PickedSlots, ListsSlots);

        /// <summary>Hands <paramref name="answer" /> back, with the name of the slot the heading says the picks make.</summary>
        void Finish(PickAnswer answer)
        {
            answer.SlotName = _slotHeading?.NewName ?? "";
            foreach (var slot in PickedSlots)
                answer.Slots.Add(slot);

            _done(answer);
        }

        /// <summary>
        /// Finishes the way Done does, as one gesture with a choice: through the values where there are any, which go to the
        /// first that needs giving unless every one already holds something it takes.
        /// </summary>
        void MoveOn()
        {
            if (PrimaryText.Length == 0 || !CanPrimary)
                return;

            var arguments = _strip.Arguments;
            if (_strip.ShowsArguments && arguments != null && arguments.Form.Count > 0 && !arguments.Form.Complete)
            {
                arguments.Form.Take(Editor.Input);
                return;
            }

            if (_finish != null)
                _finish.Execute();
        }

        /// <summary>Pages back to the page before, which from then on this page was paged to.</summary>
        void PageBack()
        {
            _straight = false;
            Editor.Pop();
        }

        /// <summary>Leaves the pick from a page opened straight onto, past the page before that was never looked at, asking first where anything has changed.</summary>
        bool LeavingStraight()
        {
            if (!IsComplete || !Moved)
            {
                Editor.Pop();
                return true;
            }

            Editor.Ask(Editor.Texts.PickKeepQuestion);
            Editor.AnswerPrimary(Editor.Texts.PickKeepThem, true);
            Editor.Answer(Editor.Texts.PickDiscardThem, delegate
            {
                Editor.Pop();
                Editor.Pop();
            });

            return false;
        }
    }
}
