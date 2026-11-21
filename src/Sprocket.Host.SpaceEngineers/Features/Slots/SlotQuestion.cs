using Sprocket.Host.SpaceEngineers.Features.Slots.Edits;
using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Writing;
using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Slots
{
    /// <summary>
    /// The panel a slot is opened in, hung from its name, as a setting's is: its name, the few words a signal slot's buttons
    /// say, what it is for, and what it expects.
    /// </summary>
    public sealed class SlotQuestion
    {
        readonly SprocketEditor _editor;
        readonly AutomationSystem _system;
        readonly Part _part;
        readonly List<Field> _fields = new List<Field>();
        readonly Composer _about;
        ValuesQuestion _question;

        /// <summary>How long a short name may be: the most the game's own text boxes in the terminal take.</summary>
        const int ShortLongest = 512;

        SlotQuestion(SprocketEditor editor, AutomationSystem system, Part part)
        {
            _editor = editor;
            _system = system;
            _part = part;
            _about = PartAbout.Composer(editor, editor.Texts.BlueprintAboutHint, part.Annotation);
        }

        /// <summary>A panel opened on <paramref name="part" />, a slot of <paramref name="system" />, hung from what <paramref name="mark" /> finds; <paramref name="closed" /> is called as it closes.</summary>
        public static ValuesQuestion Edit(SprocketEditor editor, AutomationSystem system, Part part, Func<Element> mark, Action closed)
        {
            return new SlotQuestion(editor, system, part).Open(mark, closed);
        }

        ValuesQuestion Open(Func<Element> mark, Action closed)
        {
            var texts = _editor.Texts;
            var name = Field.Called(texts.SettingName);
            name.Entry = FieldInput.Text;
            name.Needed = true;
            name.Stacked = true;
            name.Refusing = said =>
            {
                var other = _system.FindPartInAnyCase(NameRules.Clean(said));
                return other != null && other != _part ? texts.RenameTaken : "";
            };
            _fields.Add(name);
            var given = new List<Literal> { Literal.Of(_part.Id) };

            // The few words its buttons say, for a slot answered by toolbar slots.
            if (_part.IsSignal)
            {
                var shortName = Field.Called(texts.SlotShort);
                shortName.Entry = FieldInput.Text;
                shortName.Default = "";
                shortName.Stacked = true;
                shortName.Longest = ShortLongest;
                shortName.Hint = texts.SlotShortHint;
                shortName.About = texts.Codex.SlotShortAbout;
                _fields.Add(shortName);
                given.Add(Literal.Of(_part.Short));
            }

            var expects = Expects(texts);
            _question = new ValuesQuestion(_editor, null, _fields, given, mark, (said, cancelled) =>
            {
                closed();
                if (!cancelled)
                    Done(said);
            }, form =>
            {
                form.After(0, texts.SlotPurpose, _about);
                form.After(_fields.Count - 1, texts.SlotExpects, expects);
            });
            _question.Hang();
            return _question;
        }

        /// <summary>
        /// What the slot expects: for a block slot, a part naming the type of block, pressed to choose another after what the panel
        /// says is kept; for any other, the words alone.
        /// </summary>
        Element Expects(Texts texts)
        {
            var said = Expected(texts);
            if (_part.IsSignal || _part.Holds.Length > 0)
                return new Label { Style = Styles.Phrase, TextWrapping = TextWrapping.Wrap, Text = said };

            var chip = new SlotChip { Align = Align.Left };
            chip.Say(said);
            chip.Tip = CodexTip.For(chip.Tip, texts.Codex.SlotExpectsAbout);
            chip.Command = _editor.Act(CommandKind.SlotType, () =>
            {
                _question?.Done();
                _editor.Push(new SlotTypePage(_editor, _system, _part));
            });
            return chip;
        }

        /// <summary>The type of block the slot expects, in its name, that it takes any block, or for a signal slot any signal.</summary>
        string Expected(Texts texts)
        {
            if (_part.IsSignal)
                return texts.SlotAnySignal;

            return TypeText.Expected(texts, _part, _editor.World.Entities) ?? texts.SlotAnyBlock;
        }

        /// <summary>Renames the slot, and changes the few words its buttons say and what it is for, as one change.</summary>
        void Done(string said)
        {
            var values = FieldSet.Read(said, _fields);
            var name = values.Count > 0 ? NameRules.Clean(values[0].Text) : "";
            var about = _about.Text ?? "";

            SystemEdit edit = null;
            if (about != (_part.Annotation ?? ""))
                edit = new AnnotatePart(_part, about, ChangeKind.DescribeSlot);

            if (_part.IsSignal)
            {
                var shortened = values.Count > 1 ? values[1].Text.Trim() : "";
                if (shortened != _part.Short)
                    edit = Then(new ShortenPart(_part, shortened), edit);
            }

            if (name.Length > 0 && name != _part.Id)
            {
                var wanted = _system.FindPartInAnyCase(name) == _part ? name : _system.Free(name);
                if (wanted != _part.Id)
                    edit = Then(new RenamePart(_part, wanted), edit);
            }

            if (edit != null)
                _editor.Do(_system, edit);
        }

        /// <summary><paramref name="first" />, then <paramref name="then" /> where there is one, as one change.</summary>
        static SystemEdit Then(SystemEdit first, SystemEdit then)
        {
            return then == null ? first : new WithMade(first, then);
        }
    }
}
