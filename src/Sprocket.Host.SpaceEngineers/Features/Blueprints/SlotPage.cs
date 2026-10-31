using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Lumel.Controls;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Writing;
using Sprocket.Host.SpaceEngineers.Sequences;
using Lumel;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>One slot of a blueprint: its name on the heading, what it is for, and the kinds of block it expects.</summary>
    public sealed partial class SlotPage : EditorPage
    {
        readonly AutomationSystem _system;
        readonly Part _part;

        Element _focused;

        /// <summary>How long a short name may be: the most the game's own text boxes in the terminal take.</summary>
        const int ShortLongest = 512;

        /// <summary>The page of the slot <paramref name="part" /> of the blueprint <paramref name="system" />, in the editor <paramref name="editor" />.</summary>
        public SlotPage(SprocketEditor editor, AutomationSystem system, Part part) : base(editor)
        {
            _system = system;
            _part = part;
        }

        /// <inheritdoc />
        public override string Title => _part.Id;

        /// <inheritdoc />
        public override string CustomName => _part.Id;

        /// <inheritdoc />
        public override bool Edits => true;

        /// <summary>A slot is renamed on its heading while its logic system may be changed.</summary>
        public override bool CanRename => Editable;

        /// <inheritdoc />
        public override string NameAbout => Editor.Texts.Codex.BlueprintNameBoxAbout;

        /// <summary>A slot has to be called something, since its name is how a sentence names it.</summary>
        public override bool NameRequired => true;

        /// <summary>The slot's way back is its logic system's.</summary>
        public override bool HasHistory => true;

        /// <inheritdoc />
        public override bool CanUndo => Editor.Holder == null && Editor.CanUndo(_system, _part);

        /// <inheritdoc />
        public override string UndoText => Editor.UndoLabel(_system, _part);

        /// <inheritdoc />
        public override bool CanRedo => Editor.Holder == null && Editor.CanRedo(_system, _part);

        /// <inheritdoc />
        public override string RedoText => Editor.RedoLabel(_system, _part);

        /// <summary>The box what the slot is for is written in.</summary>
        public Composer About => _composer;

        /// <summary>The box a signal slot's short name is written in.</summary>
        public TextBox Short => _short;

        /// <summary>The part saying the type of block a block slot takes, pressed to choose another.</summary>
        public SlotChip Expects => _expectsPart;

        /// <summary>The switch that says whether the slot may be left without a block, on for required.</summary>
        public Switch Required => _required;

        /// <summary>Whether the slot may be changed now: not while its logic system may run, and not while somebody else holds the editor.</summary>
        bool Editable => !_system.Enabled && Editor.Holder == null;

        /// <inheritdoc />
        public override bool IsAbout(AutomationSystem system)
        {
            return system == _system;
        }

        /// <summary>Renames the slot wherever its logic system names it, as one step back.</summary>
        public override void Rename(string name)
        {
            var wanted = NameRules.Clean(name);
            if (wanted.Length == 0 || !AcceptsName(wanted) || wanted == _part.Id)
                return;

            Make(new RenamePart(_part, wanted));
        }

        /// <summary>Whether no other slot, no state and no type has <paramref name="name" />, in any case.</summary>
        public override bool AcceptsName(string name)
        {
            var wanted = NameRules.Clean(name);
            return _system.FindPartInAnyCase(wanted) == _part || !_system.TakesInAnyCase(wanted);
        }

        /// <summary>Writes what is still being typed into the slot, and where anything was changed here asks whether to keep it.</summary>
        public override bool Leaving()
        {
            Written(_focused);
            _focused = null;
            return base.Leaving();
        }

        /// <summary>Keeps every change made here, what is still being typed included, and finishes.</summary>
        public override void Primary()
        {
            Written(_focused);
            _focused = null;
            base.Primary();
        }

        /// <inheritdoc />
        public override void Undo()
        {
            Settle();
            Editor.Undo(_system, _part);
        }

        /// <inheritdoc />
        public override void Redo()
        {
            Settle();
            Editor.Redo(_system, _part);
        }

        /// <summary>Brings the view up to date, or goes back where a step back has taken the slot away.</summary>
        public override void Build()
        {
            // Before Show, which would otherwise put back what the box replaced.
            Refocused();
            Show();
        }

        /// <summary>Writes a box the keyboard has left, and goes back once the slot is gone.</summary>
        /// <remarks>Every tick; one comparison for the box, and one walk of the slots, of which there are a handful.</remarks>
        public override void Refresh()
        {
            Refocused();

            if (_system.FindPart(_part.Id) != _part)
                Editor.Pop();
        }

        /// <inheritdoc />
        protected override void ViewCreated()
        {
            Show();
        }

        void Show()
        {
            if (_composer == null)
                return;

            var texts = Editor.Texts;
            var editable = Editable;

            _locked.IsVisible = !editable;
            _locked.Text = editable
                ? ""
                : Editor.Holder != null
                    ? Template.Fill(texts.SequenceHeld, "holder", Editor.Holder)
                    : Template.Fill(texts.SystemLocked, "name", _system.Name);

            _purposeHeading.Text = texts.SlotPurpose.ToUpperInvariant();
            _composer.IsVisible = editable;
            _composer.Show(texts);
            _composer.Guides = Editor.Guides;
            _composer.Hint = texts.BlueprintAboutHint;
            if (!_composer.Box.IsFocused)
                _composer.Text = _part.Annotation;

            _purpose.IsVisible = !editable && _part.Annotation.Length > 0;
            _purpose.Markdown = _part.Annotation;

            // The few words its buttons say, for a slot answered by toolbar slots.
            _naming.IsVisible = _part.IsSignal;
            _shortHeading.Text = texts.SlotShort.ToUpperInvariant();
            _short.IsVisible = _part.IsSignal && editable;
            _short.Hint = texts.SlotShortHint;
            _short.Tip = CodexTip.For(_short.Tip, texts.Codex.SlotShortAbout);
            if (!_short.IsFocused)
                _short.Text = _part.Short;

            _shownShort.IsVisible = _part.IsSignal && !editable;
            _shownShort.Text = _part.Short.Length > 0 ? _part.Short : _part.Id;

            _expectsHeading.Text = texts.SlotExpects.ToUpperInvariant();

            // A block slot's type is chosen in a picker of its own; a signal or a screen has no type of block to choose.
            var typed = !_part.IsSignal && _part.Holds.Length == 0;
            _expects.IsVisible = !typed;
            _expects.Text = Expected(texts);
            _expectsPart.IsVisible = typed;
            _expectsPart.Say(Expected(texts));
            _expectsPart.Tip = editable ? CodexTip.For(_expectsPart.Tip, texts.Codex.SlotExpectsAbout) : KeptTip.For(_expectsPart.Tip, _locked.Text);
            _expectsPart.Command = typed && editable
                ? Editor.Act(CommandKind.SlotType, () => Editor.Push(new SlotTypePage(Editor, _system, _part)))
                : null;

            _required.OnText = texts.BlueprintRequired;
            _required.OffText = texts.BlueprintOptional;
            _required.IsOn = !_part.Optional;
            _required.Tip = CodexTip.For(_required.Tip, texts.Codex.BlueprintRequiredAbout);
            _required.Command = editable
                ? Editor.Act(CommandKind.MakeOptional, () => Make(new SetOptional(_part, !_part.Optional)))
                : null;
        }

        /// <summary>The type of block the slot expects, in its name, that it takes any block, or for a signal slot any signal.</summary>
        string Expected(Localization.Texts texts)
        {
            if (_part.IsSignal)
                return texts.SlotAnySignal;

            return TypeText.Expected(texts, _part, Editor.World.Entities) ?? texts.SlotAnyBlock;
        }

        /// <summary>Writes the box into the slot once the keyboard has left it, read every tick because it is one comparison.</summary>
        void Refocused()
        {
            var now = Editor.Input?.Focused;
            if (ReferenceEquals(now, _focused))
                return;

            var left = _focused;
            _focused = now;
            Written(left);
        }

        /// <summary>Writes the box that holds the keyboard into the slot and lets the keyboard go, so what comes next starts from what was typed.</summary>
        /// <remarks>Called before a step back.</remarks>
        void Settle()
        {
            Written(_focused);
            _focused = null;
            if (Editor.Input != null)
                Editor.Input.Focus(null);
        }

        /// <summary>Writes what was typed in <paramref name="box" /> into the slot, where it is one of this page's boxes.</summary>
        /// <remarks>Only once the keyboard has left it, so a description being typed is one step back rather than one a letter.</remarks>
        void Written(Element box)
        {
            if (box == null || _composer == null || !Editable)
                return;

            if (box == _composer.Box && _composer.Text != (_part.Annotation ?? ""))
                Make(new AnnotatePart(_part, _composer.Text, ChangeKind.DescribeSlot));
            else if (box == _short && _short.Text.Trim() != _part.Short)
                Make(new ShortenPart(_part, _short.Text.Trim()));
        }

        /// <summary>Makes <paramref name="change" /> to the slot as one step back.</summary>
        void Make(SystemEdit change)
        {
            Editor.Do(_system, change);
        }
    }
}
