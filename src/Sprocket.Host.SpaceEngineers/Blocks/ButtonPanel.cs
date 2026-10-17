using System.Collections.Generic;
using System.Globalization;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A button panel, with one signal for each button and the label a person reads on each.</summary>
    /// <remarks>
    /// A button raises nothing while its toolbar slot is empty (<c>MyButtonPanel.ActivateButton</c>), so Sprocket fills the
    /// slot of each button its program listens to.
    /// </remarks>
    public sealed class ButtonPanel : Capability
    {
        /// <summary>Writing a label on a button, which is Sprocket's own.</summary>
        public const string Label = "label-button";

        /// <summary>What a button's label reads, which is Sprocket's own and takes the button.</summary>
        public const string LabelOf = "label-of-button";

        /// <summary>Whether a button has anything on its slot, which is Sprocket's own and takes the button.</summary>
        public const string Assigned = "button-has-an-action";

        const string Unheard = "A button panel raises no event when a label or a slot changes.";

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyButtonPanel;
        }

        /// <summary>Describes the panel's buttons as one signal, "button 2 is pressed", with the button chosen beside it.</summary>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Hiding("ButtonName")
                .Renaming("AnyoneCanUse", true, "anyone can use")
                .Renaming("AnyoneCanUse", false, "toggle anyone can use");

            IMyToolbar toolbar = Toolbars.Of(block, 1);
            if (toolbar == null || toolbar.SlotCount < 1)
                return;

            // The game gives a panel one page more than its buttons fill, so every page but the last is buttons.
            var pages = toolbar.PageCount <= 1 ? 1 : toolbar.PageCount - 1;
            var buttons = toolbar.SlotCount * pages;
            var slots = toolbar.SlotCount;
            kind.WithPressed(1, "button is pressed", pages, slots, at => $"button {((at.Page - 1) * slots + at.Slot)} is pressed");

            var labels = OwnAction.Of<IMyButtonPanel, Labelled>(Labelled.Read, (panel, labelled, seen) =>
            {
                // An action's button is not held to the panel's when a program is checked, and the game keeps a label for any button it is handed.
                if (!Within(labelled, buttons))
                    return;

                if (labelled.Text.Length == 0)
                    panel.ClearCustomButtonName(labelled.Button);
                else
                    panel.SetCustomButtonName(labelled.Button, labelled.Text);
            });

            var label = Field.Called("label");
            label.Entry = FieldInput.Text;

            kind.WithAction(Label, "label button", labels, Asked(buttons), label).In(Category.Screen)
                .WithValue(LabelOf, "label of button", OwnValue.Text<IMyButtonPanel, Labelled>(Labelled.Read,
                    (panel, labelled, seen) => panel.GetButtonName(labelled.Button))
                    .Taking(Asked(buttons))).Looked(Unheard).In(Category.Screen)
                .WithValue(Assigned, "button has an action", OwnValue.Flag<IMyButtonPanel, Labelled>(Labelled.Read,
                    (panel, labelled, seen) => panel.IsButtonAssigned(labelled.Button))
                    .Taking(Asked(buttons))).Looked(Unheard).In(Category.Automation);
        }

        /// <summary>Not wired where none of its buttons is.</summary>
        public override Status StatusOf(IMyTerminalBlock block)
        {
            var panel = block as IMyButtonPanel;
            if (panel == null)
                return Status.None;

            var buttons = Buttons(block);
            for (int i = 0; i < buttons; i++)
            {
                if (panel.IsButtonAssigned(i))
                    return Status.None;
            }

            return Status.Of(Condition.NotWired);
        }

        /// <summary>How many buttons a panel has.</summary>
        static int Buttons(IMyTerminalBlock block)
        {
            IMyToolbar toolbar = Toolbars.Of(block);
            return toolbar?.SlotCount ?? 0;
        }

        /// <summary>The button a member is given, by its number from 1 to <paramref name="buttons" />.</summary>
        static Field Asked(int buttons)
        {
            var button = Field.Between("button", "1", buttons.ToString(CultureInfo.InvariantCulture));
            button.Entry = FieldInput.Whole;
            button.Needed = true;
            return button;
        }

        /// <summary>Whether the button a member was given is one of the panel's <paramref name="buttons" />.</summary>
        static bool Within(Labelled labelled, int buttons)
        {
            return labelled.Button >= 0 && labelled.Button < buttons;
        }

        /// <summary>The button a member was given, counted from 0 as the game counts it, and the label where one was given.</summary>
        sealed class Labelled
        {
            /// <summary>The button, counted from 0, or -1 where none was given.</summary>
            public int Button = -1;

            /// <summary>The label, or empty to give the button back the game's own.</summary>
            public string Text = "";

            /// <summary>What a member taking a button, and perhaps a label, was given.</summary>
            public static Labelled Read(IList<Literal> arguments)
            {
                var labelled = new Labelled();
                if (arguments == null || arguments.Count == 0 || arguments[0].Kind != ValueKind.Integer
                    || arguments[0].Whole < 1 || arguments[0].Whole > int.MaxValue)
                    return labelled;

                labelled.Button = (int)arguments[0].Whole - 1;
                if (arguments.Count > 1)
                    labelled.Text = arguments[1].Text ?? "";

                return labelled;
            }
        }
    }
}
