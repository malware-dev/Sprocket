using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>A blueprint's configuration panel: one blockset's slots filled with its blocks, or, in edit mode, the panel itself set up.</summary>
    /// <remarks>
    /// Filling, each slot says what it is for in the words its author gave it, and pressing its blocks chooses others. In edit
    /// mode the panel's words and slots are written, for everyone who fills a blockset made from the blueprint. The elements it
    /// is drawn with are declared in <c>BlueprintPage.View.cs</c>.
    /// </remarks>
    sealed partial class BlueprintPage : EditorPage
    {
        readonly AutomationSystem _system;
        readonly List<Part> _parts = new List<Part>();
        readonly List<string> _used = new List<string>();
        readonly List<Mention> _gone = new List<Mention>();
        readonly bool _editing;

        SystemInstance _made;
        string _madeName;
        bool _madeOn;
        int _shape = -1;
        string _described;
        Element _focused;
        Keyed<Part, PartRow> _partRows;

        /// <summary>The configuration panel of the blueprint <paramref name="system" />, in the editor <paramref name="editor" />, filling a blockset's slots or, where <paramref name="editing" />, setting up the panel.</summary>
        public BlueprintPage(SprocketEditor editor, AutomationSystem system, bool editing) : base(editor)
        {
            _system = system;
            _editing = editing;
        }

        /// <summary>The blockset whose blocks fill the slots, or, in edit mode, the panel itself.</summary>
        public override string Title => _editing ? Editor.Texts.BlueprintPanel : Blockset == null ? _system.Name : Blockset.Name;

        /// <inheritdoc />
        public override bool Edits => true;

        /// <inheritdoc />
        public override string CustomName => Title;

        /// <summary>The blockset can be renamed here; the panel is named for its blueprint, which is renamed on its logic system screen.</summary>
        public override bool CanRename => !_editing && Blockset != null;

        /// <summary>In edit mode, who reads what is written here.</summary>
        public override string Subtitle => _editing ? Editor.Texts.BlueprintPanelAbout : "";

        /// <summary>The blockset has a switch, filling; the panel itself does not.</summary>
        public override bool CanSwitch => !_editing && Blockset != null;

        /// <inheritdoc />
        public override bool SwitchIsOn => Blockset != null && _system.Enabled && Blockset.Enabled;

        /// <summary>The blockset's switch stops that one and nothing else made from the blueprint.</summary>
        public override string SwitchAbout => StatusRow.Switching(Editor.Texts, false, SwitchIsOn);

        /// <summary>The blockset just made is as it was made: its name, its switch, and not one slot filled.</summary>
        public override bool Untouched
        {
            get
            {
                // Switched off by Sprocket for its empty slots is not somebody touching it.
                return _made != null && _made.Name == _madeName && (_made.Enabled == _madeOn || _made.Halt != null)
                    && _made.IsEmpty;
            }
        }

        /// <inheritdoc />
        public override string NameAbout => Editor.Texts.BlueprintNameAbout;

        /// <summary>A blueprint has to be called something, since it is told from the others by its name.</summary>
        public override bool NameRequired => true;

        /// <summary>While the blueprint is being written, whose way back is the system's own, since a blueprint is a system.</summary>
        public override bool HasHistory => _editing;

        /// <inheritdoc />
        public override bool CanUndo => _editing && Editor.CanUndo(_system);

        /// <inheritdoc />
        public override string UndoText => Editor.UndoLabel(_system);

        /// <inheritdoc />
        public override bool CanRedo => _editing && Editor.CanRedo(_system);

        /// <inheritdoc />
        public override string RedoText => Editor.RedoLabel(_system);

        /// <summary>The blueprint's parts, one row each, with the chosen logic system's blocks for each.</summary>
        public Element.Collection Parts => _partList.Children;

        /// <summary>The logic system whose blocks fill the slots, or null before there is one.</summary>
        SystemInstance Blockset => Editor.StandIn(_system);

        /// <summary>Whether the panel is being set up rather than its slots filled.</summary>
        public bool IsEditing => _editing;

        /// <inheritdoc />
        public override bool IsAbout(AutomationSystem system)
        {
            return system == _system;
        }

        /// <summary>Switches the blockset being filled on or off, and nothing else made from the blueprint; on while the blueprint is off, it asks about the blueprint.</summary>
        public override void Switch(bool on)
        {
            var chosen = Blockset;
            if (chosen != null)
                Editor.SwitchInstance(_system, chosen, on);
        }

        /// <inheritdoc />
        public override bool AcceptsName(string name)
        {
            return Naming.Clean(name).Length > 0;
        }

        /// <inheritdoc />
        public override void Rename(string name)
        {
            var wanted = Naming.Clean(name);
            if (!AcceptsName(wanted))
                return;

            if (Blockset != null)
                Blockset.Name = wanted;
        }

        /// <summary>Names the blueprint after the panel's own name: as words on the panel, which is where the blueprint is changed from, and as a part of its own that opens the blueprint on a logic system made from it.</summary>
        public override void Heading(List<HeadingPart> parts)
        {
            base.Heading(parts);
            if (_editing)
            {
                parts[0].After = Template.Fill(Editor.Texts.BlueprintPanelFor, "name", _system.Name);
                return;
            }

            // Pushed rather than swapped in, so Back comes back to the blocks being filled.
            var from = new HeadingPart(_system.Name);
            from.Lead = Editor.Texts.BlueprintFromLead;
            from.After = Editor.Texts.BlueprintFromAfter;
            from.About = Template.Fill(Editor.Texts.SystemsEditBlueprint, "name", _system.Name);
            from.Opens = Editor.Act(Cmd.OpenSystem, delegate { Editor.Push(new LogicSystemPage(Editor, _system)); });
            parts.Add(from);
        }

        /// <summary>Writes what is still being typed into the blueprint before going.</summary>
        public override bool Leaving()
        {
            Written(_focused);
            _focused = null;
            return true;
        }

        /// <inheritdoc />
        public override void Unmake()
        {
            if (_made != null)
                Editor.TakeOut(_system, _made);
        }

        /// <summary>Notes the blockset just made, as it was made.</summary>
        protected override void Remember()
        {
            _made = Blockset;
            if (_made == null)
                return;

            _madeName = _made.Name;
            _madeOn = _made.Enabled;
        }

        /// <inheritdoc />
        public override void Undo()
        {
            Settle();
            Editor.Undo(_system);
        }

        /// <inheritdoc />
        public override void Redo()
        {
            Settle();
            Editor.Redo(_system);
        }

        /// <summary>Brings the view up to date.</summary>
        public override void Build()
        {
            // A box just left is written first: the rows filled below would otherwise put back what it replaced, and the
            // write that follows would read that back.
            Refocused();
            _shape = -1;
            Show();
        }

        /// <summary>Writes a box the keyboard has left, and builds the screen again where how many blocksets there are or which is chosen has changed.</summary>
        /// <remarks>
        /// Every tick, and it walks nothing: the parts change only through this page, which rebuilds itself when they do, and a
        /// sentence changes only on the screens under it, which rebuild it on the way back.
        /// </remarks>
        public override void Refresh()
        {
            Refocused();

            if (Shape(Editor.StandIn(_system)) != _shape)
            {
                Editor.Invalidate();
                return;
            }

            if (SecondPassed())
                Troubled(Blockset);
        }

        /// <summary>Keeps the rows by slot, and fills the view.</summary>
        protected override void ViewCreated()
        {
            _partRows = new Keyed<Part, PartRow>(_partList.Children, PartRow);
            Show();
        }

        void Show()
        {
            if (_partRows == null)
                return;

            var texts = Editor.Texts;
            var chosen = Blockset;
            _shape = Shape(chosen);

            _partsHeading.Text = texts.BlueprintParts.ToUpperInvariant();
            Describe();
            Write();

            Collect();
            _partRows.Show(_parts);

            var unfilled = 0;
            for (var i = 0; i < _parts.Count; i++)
            {
                if (!Part(_partRows[_parts[i]], _parts[i], chosen) && !_parts[i].Optional)
                    unfilled++;
            }

            Needs(chosen, unfilled);
            Troubled(chosen);
            _rule.IsVisible = _description.IsVisible || _troubled.IsVisible || _writing.IsVisible;
        }

        /// <summary>Says what is wrong with the blockset being filled, and offers the sequence at fault where that is where it is mended.</summary>
        /// <remarks>Why Sprocket switched it off where it did, and otherwise what is wrong now; nothing while setting up the panel.</remarks>
        void Troubled(SystemInstance chosen)
        {
            Sequence sequence = null;
            var said = "";
            var mended = false;
            var empty = false;

            if (!_editing && chosen != null && !chosen.Enabled && chosen.Halt != null)
            {
                said = SequenceText.Stopped(Editor.Texts, chosen.Halt, _system);
                sequence = _system.FindSequence(chosen.Halt.Sequence);
                empty = chosen.Halt.Problem == Runtime.SequenceProblem.EmptySlot;
                mended = true;
            }
            else if (!_editing && chosen != null)
            {
                Runtime.SequenceFault fault;
                sequence = SystemSummary.Failing(Editor.Runner, _system, chosen, out fault);
                said = sequence == null ? "" : SequenceText.Failing(Editor.Texts, fault, sequence);
                empty = fault.Problem == Runtime.SequenceProblem.EmptySlot;
            }

            _troubled.IsVisible = said.Length > 0;
            _trouble.Text = said;
            _trouble.Style = mended ? Styles.Caution : Styles.Problem;

            // An empty slot is filled on this page, so there is nowhere else to go.
            var opens = sequence != null && !empty;
            _openTrouble.IsVisible = opens;
            _openTrouble.Text = opens ? Template.Fill(Editor.Texts.BlueprintOpenSequence, "name", sequence.Name) : "";
            _openTrouble.Command = opens
                ? Editor.Act(Cmd.OpenSequence, delegate
                {
                    Editor.Choose(_system, chosen);
                    Editor.Push(new SequencePage(Editor, sequence));
                })
                : null;
        }

        /// <summary>Shows what the blueprint's author wrote about it, parsed again only when it has changed.</summary>
        void Describe()
        {
            var said = _system.Annotation;
            _description.IsVisible = said.Length > 0 && !_editing;
            if (ReferenceEquals(said, _described))
                return;

            _described = said;
            _description.Document = Markdown.Parse(said);
        }

        /// <summary>Puts the description in its box while the blueprint is being written, leaving alone a box being typed in.</summary>
        void Write()
        {
            var texts = Editor.Texts;
            _writing.IsVisible = _editing;
            if (!_editing)
                return;

            _writingHeading.Text = texts.BlueprintDescription.ToUpperInvariant();
            _composer.Show(texts);
            _composer.Hint = texts.BlueprintDescriptionHint;
            if (!_composer.Box.IsFocused)
                _composer.Text = _system.Annotation;
        }

        /// <summary>Writes a box into the blueprint once the keyboard has left it, read every tick because it is one comparison.</summary>
        void Refocused()
        {
            var now = Editor.Input == null ? null : Editor.Input.Focused;
            if (ReferenceEquals(now, _focused))
                return;

            var left = _focused;
            _focused = now;
            Written(left);
        }

        /// <summary>Writes the box that holds the keyboard into the blueprint and lets the keyboard go, so what comes next starts from what was typed.</summary>
        /// <remarks>Before a step back, so it takes back what was typed and the box then shows what the step leaves.</remarks>
        void Settle()
        {
            Written(_focused);
            _focused = null;
            if (Editor.Input != null)
                Editor.Input.Focus(null);
        }

        /// <summary>Writes what was typed in <paramref name="box" /> into the blueprint, where it is one of this page's boxes.</summary>
        /// <remarks>Only once the keyboard has left it, so a description being typed is one step back rather than one a letter.</remarks>
        void Written(Element box)
        {
            if (box == null || !_editing)
                return;

            if (box == _composer.Box)
            {
                Editor.Do(_system, new Annotate(_system.Annotation, _composer.Text));
                return;
            }

            for (var i = 0; i < _parts.Count; i++)
            {
                var part = _parts[i];
                var row = _partRows[part];
                if (box == row.NameBox)
                {
                    // Refused where another slot has the name, since the name is what tells the two apart.
                    var name = Naming.Clean(row.NameBox.Text);
                    var other = _system.FindPart(name);
                    if (name.Length > 0 && (other == null || other == part))
                        Editor.Do(_system, new RenamePart(part, name));
                    return;
                }

                if (box == row.About.Box)
                {
                    Editor.Do(_system, new AnnotatePart(part, row.About.Text, Change.DescribeSlot));
                    return;
                }
            }
        }

        /// <summary>Beside the slots' heading, how many of those that need a block still have none, while filling; nothing once every one has.</summary>
        void Needs(SystemInstance chosen, int unfilled)
        {
            var texts = Editor.Texts;
            _needs.IsVisible = !_editing && chosen != null && unfilled > 0;
            _needs.Text = unfilled == 1
                ? texts.BlueprintStillOne
                : Template.Fill(texts.BlueprintStillMany, "count", unfilled.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        PartRow PartRow(Part part)
        {
            return new PartRow(Windows);
        }

        WindowLayer Windows()
        {
            return Editor.Windows;
        }

        /// <summary>A slot, with the blocks <paramref name="chosen" /> fills it with and a press on them that chooses others; false where no block fills it.</summary>
        /// <remarks>While the blueprint is being written, its name and what it is for in their boxes instead, and whether it needs a block.</remarks>
        bool Part(PartRow row, Part part, SystemInstance chosen)
        {
            row.Editing = _editing;
            if (_editing)
                Authored(row, part);

            row.Name = part.Id;
            row.Described = part.Annotation;
            row.Optional = part.Optional ? Editor.Texts.BlueprintOptional : null;
            row.Blocks.Clear();
            _gone.Clear();

            var filled = chosen == null ? null : chosen.FilledWith(part.Id);
            if (filled != null)
            {
                for (var i = 0; i < filled.Count; i++)
                {
                    var block = Editor.Runner.Bindings.Entity(filled[i]);
                    if (block != null)
                        row.Blocks.Add(block.Id);
                    else if (_gone.Count == 0)
                        _gone.Add(new Mention(Template.Fill(Editor.Texts.BlueprintGone, "name", Called(filled[i])), null));
                }
            }

            row.Fills = chosen == null ? null : Editor.Act(Cmd.FillPart, delegate { Fill(part, chosen); });

            row.Problem = row.Blocks.Count == 0 && _gone.Count > 0 ? _gone[0].Text : null;

            if (row.Blocks.Count > 0)
                row.Show(Editor.World);
            else if (part.Optional)
                row.Say(SentenceText.Empty, false);
            else
                row.Say(SentenceText.Empty, true);

            return row.Blocks.Count > 0;
        }

        /// <summary>Puts a slot's name and what it is for in their boxes, leaving alone a box being typed in, and gives its switch its words and its press.</summary>
        void Authored(PartRow row, Part part)
        {
            var texts = Editor.Texts;
            if (!row.NameBox.IsFocused)
                row.NameBox.Text = part.Id;

            row.NameBox.Submit = new Relay(delegate { Editor.Input.Focus(null); });
            row.NameBox.Tip = Tip.For(row.NameBox.Tip, texts.BlueprintNameBoxAbout);

            row.About.Show(texts);
            row.About.Hint = texts.BlueprintAboutHint;
            if (!row.About.Box.IsFocused)
                row.About.Text = part.Annotation;
            row.Required.Says(texts.BlueprintRequired, texts.BlueprintOptional);
            row.Required.IsOn = !part.Optional;
            row.Required.Tip = Tip.For(row.Required.Tip, texts.BlueprintRequiredAbout);
            row.Required.Command = Editor.Act(Cmd.MakeOptional, delegate
            {
                Editor.Do(_system, new SetOptional(part, !part.Optional));
            });
        }

        /// <summary>What the block a reference names was called in the game, where the reference remembers, or its name in the program.</summary>
        string Called(string reference)
        {
            return Editor.Program.Called(reference, Editor.World.Values);
        }

        /// <summary>Asks which of the construct's blocks fill <paramref name="part" /> for <paramref name="chosen" />, in the picker.</summary>
        /// <remarks>
        /// Only blocks of the types the part expects, where it says: a door's part is offered doors. The blocks it holds now are
        /// chosen already, so choosing is also seeing what is there.
        /// </remarks>
        void Fill(Part part, SystemInstance chosen)
        {
            var targets = new List<long>();
            var filled = chosen.FilledWith(part.Id);
            for (var i = 0; filled != null && i < filled.Count; i++)
            {
                var block = Editor.Runner.Bindings.Entity(filled[i]);
                if (block != null)
                    targets.Add(block.Id);
            }

            if (part.IsTrigger)
            {
                // A trigger slot is a block and one of its toolbar slots, picked as a trigger's are.
                var positions = chosen.Positions(part.Id);
                var pressed = positions != null && positions.Count > 0 ? positions[0].EventId : "";
                Editor.Push(new PickPage(Editor, part.Id, true, targets, pressed, Form.Native,
                    delegate(PickAnswer answer) { Answered(part, chosen, answer); },
                    admits: Triggering, triggersOnly: true));
                return;
            }

            Editor.Push(new PickPage(Editor, part.Id, targets, delegate(GridEntity block) { return Fits(part, block); },
                delegate(PickAnswer answer) { Filled(part, chosen, answer.Targets); }));
        }

        /// <summary>Answers trigger slot <paramref name="part" /> for <paramref name="chosen" /> with the trigger <paramref name="answer" /> picked on each of its blocks, and goes back to the blueprint.</summary>
        /// <remarks>Only a trigger answers a trigger slot; anything else picked leaves the picker open.</remarks>
        void Answered(Part part, SystemInstance chosen, PickAnswer answer)
        {
            if (ToolbarSlot.Of(answer.Chosen) == null)
                return;

            Editor.Filling(_system, part, chosen, answer.Targets, Editor.Pressed(answer), delegate
            {
                var names = new List<string>();
                var kept = new List<ToolbarSlot>();
                Editor.Picked(answer, names, kept);
                chosen.Fill(part.Id, names, kept);
                Editor.Program.Edited();
                Editor.Program.Rewire();
                Editor.Pop();
                Editor.Invalidate();
            });
        }

        /// <summary>Whether <paramref name="block" /> has a trigger: something the construct itself does to start a step.</summary>
        static bool Triggering(GridEntity block)
        {
            var events = block.Kind.Events;
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].Answered)
                    return true;
            }

            return false;
        }

        /// <summary>Whether <paramref name="block" /> is of a type <paramref name="part" /> may be filled with.</summary>

        static bool Fits(Part part, GridEntity block)
        {
            if (part.Expects.Count == 0)
                return true;

            var sort = BlockKinds.Sort(block.Kind.Id);
            for (var i = 0; i < part.Expects.Count; i++)
            {
                var expects = part.Expects[i];
                if (string.Equals(sort, expects, StringComparison.Ordinal)
                    || sort.StartsWith(expects + "/", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        /// <summary>Fills <paramref name="part" /> for <paramref name="chosen" /> with the blocks <paramref name="ids" />, and goes back to the blueprint.</summary>
        /// <remarks>
        /// Asked about first where a button among them already starts something, and rewired as any edit is, because what a
        /// trigger's button is may have been this part.
        /// </remarks>
        void Filled(Part part, SystemInstance chosen, IList<long> ids)
        {
            Editor.Filling(_system, part, chosen, ids, null, delegate
            {
                var names = new List<string>();
                for (var i = 0; i < ids.Count; i++)
                {
                    var block = Editor.World.FindEntity(ids[i]);
                    if (block != null)
                        names.Add(Editor.Program.Declare(Editor.World, block).Name);
                }

                chosen.Fill(part.Id, names);
                Editor.Program.Edited();
                Editor.Program.Rewire();
                Editor.Pop();
                Editor.Invalidate();
            });
        }

        /// <summary>The parts the system's sentences use, in the order the system declares them.</summary>
        void Collect()
        {
            _parts.Clear();
            _system.Acted(_used);

            var parts = _system.Parts;
            for (var i = 0; i < parts.Count; i++)
            {
                if (_used.Contains(parts[i].Id))
                    _parts.Add(parts[i]);
            }
        }

        /// <summary>What the page is built from that can change under it, as one number: how many of it, and which is chosen.</summary>
        int Shape(SystemInstance chosen)
        {
            return _system.Instances.Count * 31 + (chosen == null ? 0 : chosen.Number);
        }
    }
}
