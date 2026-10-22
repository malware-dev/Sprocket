using System;
using Lumel.Input;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    /// <summary>What the State tab and the state selector do alike with a type: make one, show it in a row, and rename in a chip.</summary>
    public static class TypeEditing
    {
        /// <summary>A new type, named and given two choices as numbered names, its name one <paramref name="taken" /> does not refuse, numbered on from <paramref name="count" /> types.</summary>
        public static DeclaredType New(Texts texts, int count, Func<string, bool> taken)
        {
            var type = new DeclaredType(NumberedName.Free(texts.StateTypeNumbered, count, taken));
            type.Choices.Add(NumberedName.Free(texts.StateChoiceNumbered, 0, type.Has));
            type.Choices.Add(NumberedName.Free(texts.StateChoiceNumbered, 1, type.Has));
            return type;
        }

        /// <summary>Shows <paramref name="type" /> in <paramref name="row" />: its name, the words before its choices, a chip per choice, and the way to add one.</summary>
        public static void Show(Texts texts, TypeRow row, DeclaredType type)
        {
            row.Name.Name = type.Name;
            row.Lead = texts.StateCanBe;
            row.LeadAbout = texts.StateCanBeAbout;
            row.ChoiceChips.Show(type.Choices);
            foreach (var choice in type.Choices)
                row.ChoiceChips[choice].Name = choice;

            row.AddChoice.Text = StatusRow.Plus + texts.StateChoiceAdd;
        }

        /// <summary>Makes <paramref name="chip" />'s box hand the keyboard back to <paramref name="input" /> on Enter, and on Escape put the name back first.</summary>
        public static NameChip Boxed(NameChip chip, Func<InputRoot> input)
        {
            chip.Box.Submit = new Relay(delegate { input()?.Focus(null); });
            chip.Box.Cancel = new Relay(delegate
            {
                chip.Box.Text = chip.Name;
                input()?.Focus(null);
            });
            return chip;
        }

        /// <summary>Shows the box in place of <paramref name="chip" />'s name and gives it the keyboard of <paramref name="input" />.</summary>
        public static void Rename(NameChip chip, InputRoot input)
        {
            chip.Rename();
            input?.Focus(chip.Box);
        }
    }
}
