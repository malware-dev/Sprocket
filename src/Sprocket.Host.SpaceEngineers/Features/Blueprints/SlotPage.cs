using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Codex;
using Lumel.Controls;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Writing;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Lumel;
using Lumel.Input;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>One slot of a blueprint: its name on the heading, what it is for, and the kinds of block it expects.</summary>
    /// <remarks>
    /// What is written here is what everyone filling an installation of the blueprint reads. The elements it is drawn with are
    /// declared in <c>SlotPage.View.cs</c>.
    /// </remarks>
    public sealed partial class SlotPage : EditorPage
    {
        readonly AutomationSystem _system;
        readonly Part _part;
        readonly ICommand _guide;
        readonly List<string> _used = new List<string>();

        Element _focused;

        /// <summary>The page of the slot <paramref name="part" /> of the blueprint <paramref name="system" />, in the editor <paramref name="editor" />.</summary>
        public SlotPage(SprocketEditor editor, AutomationSystem system, Part part) : base(editor)
        {
            _system = system;
            _part = part;
            _guide = new Relay(delegate
            {
                var term = Editor.Codex.Find(Terms.Writing);
                if (term != null)
                    Editor.OpenCodex(term);
            });
        }

        /// <inheritdoc />
        public override string Title => _part.Id;

        /// <inheritdoc />
        public override string CustomName => _part.Id;

        /// <inheritdoc />
        public override bool Edits => true;

        /// <inheritdoc />
        public override string Help => Terms.Slot;

        /// <summary>A slot is renamed on its heading while its logic system may be changed.</summary>
        public override bool CanRename => Editable;

        /// <inheritdoc />
        public override string NameAbout => Editor.Texts.BlueprintNameBoxAbout;

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
            var wanted = Naming.Clean(name);
            if (wanted.Length == 0 || !AcceptsName(wanted) || wanted == _part.Id)
                return;

            // A slot nothing uses and no installation holds blocks in is not saved, so its name is free, and it goes.
            var other = _system.FindPart(wanted);
            if (other != null && other != _part)
                _system.DropPart(other);

            Editor.Do(_system, new RenamePart(_part, wanted));
        }

        /// <summary>Whether no other slot that is used, or that an installation holds blocks in, has <paramref name="name" />.</summary>
        public override bool AcceptsName(string name)
        {
            var other = _system.FindPart(Naming.Clean(name));
            if (other == null || other == _part)
                return true;

            _system.Acted(_used);
            return !_used.Contains(other.Id) && !Held(other);
        }

        /// <summary>Writes what is still being typed into the slot before going.</summary>
        public override bool Leaving()
        {
            Written(_focused);
            _focused = null;
            return true;
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
            // A box just left is written first: the box filled below would otherwise put back what it replaced.
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
            _composer.Guides = _guide;
            _composer.Hint = texts.BlueprintAboutHint;
            if (!_composer.Box.IsFocused)
                _composer.Text = _part.Annotation;

            _purpose.IsVisible = !editable && _part.Annotation.Length > 0;
            _purpose.Markdown = _part.Annotation;

            _expectsHeading.Text = texts.SlotExpects.ToUpperInvariant();
            _expects.Text = Expected(texts);

            _required.Says(texts.BlueprintRequired, texts.BlueprintOptional);
            _required.IsOn = !_part.Optional;
            _required.Tip = Tip.For(_required.Tip, texts.BlueprintRequiredAbout);
            _required.Command = editable
                ? Editor.Act(Cmd.MakeOptional, delegate { Editor.Do(_system, new SetOptional(_part, !_part.Optional)); })
                : null;
        }

        /// <summary>The type of block the slot expects, in its name, or that it takes any block.</summary>
        /// <remarks>
        /// One exact block is named as the game names it on this construct, and a type in a name of Sprocket's own. Either is said
        /// as the slot writes it where there is no name for it: a block no block on the construct is, or a modded type.
        /// </remarks>
        string Expected(Localization.Texts texts)
        {
            var expects = _part.Expects;
            if (expects.Length == 0)
                return texts.SlotAnyBlock;

            if (expects.IndexOf('/') < 0)
                return TypeText.Of(texts, expects);

            var entities = Editor.World.Entities;
            for (var i = 0; i < entities.Count; i++)
            {
                var kind = entities[i].Kind;
                if (string.Equals(BlockKinds.Sort(kind.Id), expects, System.StringComparison.Ordinal))
                    return kind.DisplayName;
            }

            return expects;
        }

        /// <summary>Whether an installation holds blocks in <paramref name="part" />.</summary>
        bool Held(Part part)
        {
            var instances = _system.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                var filled = instances[i].FilledWith(part.Id);
                if (filled != null && filled.Count > 0)
                    return true;
            }

            return false;
        }

        /// <summary>Writes the box into the slot once the keyboard has left it, read every tick because it is one comparison.</summary>
        void Refocused()
        {
            var now = Editor.Input == null ? null : Editor.Input.Focused;
            if (ReferenceEquals(now, _focused))
                return;

            var left = _focused;
            _focused = now;
            Written(left);
        }

        /// <summary>Writes the box that holds the keyboard into the slot and lets the keyboard go, so what comes next starts from what was typed.</summary>
        /// <remarks>Before a step back, so it takes back what was typed and the box then shows what the step leaves.</remarks>
        void Settle()
        {
            Written(_focused);
            _focused = null;
            if (Editor.Input != null)
                Editor.Input.Focus(null);
        }

        /// <summary>Writes what was typed in <paramref name="box" /> into the slot, where it is this page's box.</summary>
        /// <remarks>Only once the keyboard has left it, so a description being typed is one step back rather than one a letter.</remarks>
        void Written(Element box)
        {
            if (box == null || _composer == null || box != _composer.Box || !Editable)
                return;

            Editor.Do(_system, new AnnotatePart(_part, _composer.Text, Change.DescribeSlot));
        }
    }
}
