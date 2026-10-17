using System.Collections.Generic;
using System.Globalization;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// A button panel: one signal for each button, which runs its own slot when it is pressed, and the label a person reads
    /// on each button.
    /// </summary>
    /// <remarks>
    /// There is no "was pressed" to sample - a press is a moment the game reports and then forgets - so the
    /// slot runs Sprocket's own action, which is how the press arrives. The game runs the slot at the button's
    /// own place in the toolbar, across pages where a panel has more buttons than a page has slots.
    /// <para>
    /// <b>The button's toolbar slot has to have something in it.</b> <c>MyButtonPanel.ActivateButton</c>
    /// returns before raising anything when the slot is empty. Sprocket fills the slot of each button its
    /// program listens to with <see cref="Features.Terminal.SignalAction" />, and <c>IsButtonAssigned</c> says
    /// which buttons hold anything, so an empty one can be explained.
    /// </para>
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

        /// <summary>"button 2 is pressed": the panel's buttons as one signal, the button chosen beside it, said by its number on a panel with one button as on a panel with several.</summary>
        /// <remarks>
        /// The same signal reads the same whatever panel it is on, so installations filled from different panels agree. The
        /// terminal's own label box edits whichever button is picked in its list, so it is left to the terminal view, and a
        /// step labels the button it names.
        /// </remarks>
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
                // A function's button is held to the panel's when a program is checked; an action's is not, so a button the
                // panel lacks is never handed to the game, which would keep a label for it.
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
                .WithValue(LabelOf, "label of button", Being.Text<IMyButtonPanel, Labelled>(Labelled.Read,
                    (panel, labelled, seen) => panel.GetButtonName(labelled.Button))
                    .Taking(Asked(buttons))).Looked(Unheard).In(Category.Screen)
                .WithValue(Assigned, "button has an action", Being.Flag<IMyButtonPanel, Labelled>(Labelled.Read,
                    (panel, labelled, seen) => panel.IsButtonAssigned(labelled.Button))
                    .Taking(Asked(buttons))).Looked(Unheard).In(Category.Automation);
        }

        /// <summary>Not wired where none of its buttons is, because an unwired button cannot report a press.</summary>
        /// <remarks>
        /// A panel's buttons raise nothing while their toolbar slots are empty, and Sprocket fills a slot only for a
        /// button its program listens to.
        /// </remarks>
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
        /// <remarks>Its toolbar's slot count, which is the game's own answer to the same question.</remarks>
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
