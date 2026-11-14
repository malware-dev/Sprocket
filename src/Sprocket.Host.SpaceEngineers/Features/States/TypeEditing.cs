using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System;

namespace Sprocket.Host.SpaceEngineers.Features.States
{
    /// <summary>What the State tab and the state selector do alike with a type: make one, show it in a row, and name its choices.</summary>
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
            row.Name.Text = type.Name;
            row.Lead = texts.StateCanBe;
            row.ChoiceChips.Show(type.Choices);
            foreach (var choice in type.Choices)
            {
                var chip = row.ChoiceChips[choice];
                chip.Text = choice;
                chip.Boxed = type.Choices.Count > 2;
            }

            row.AddChoice.Text = StatusRow.Plus + texts.StateChoiceAdd;
        }

        /// <summary>The name of <paramref name="choice" /> of <paramref name="type" />, given through <paramref name="renames" />, trimmed, where it changed and the type has no other choice by it.</summary>
        public static Naming ChoiceNaming(DeclaredType type, string choice, Action<string> renames)
        {
            return new Naming
            {
                Name = choice,
                NameRequired = true,
                Accepts = wanted => wanted.Trim() == choice || !type.Has(wanted.Trim()),
                Renames = wanted =>
                {
                    if (wanted.Trim() != choice)
                        renames(wanted.Trim());
                }
            };
        }
    }
}
