using System;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>Which presses the editor lets through: none that would change a Sprocket lent to look at, and the first edit of a program that would not read only once its loss is agreed to.</summary>
    public sealed class PressFilter
    {
        readonly SprocketEditor _editor;

        /// <summary>A gate on the presses of <paramref name="editor" />.</summary>
        public PressFilter(SprocketEditor editor)
        {
            _editor = editor;
        }

        /// <summary>Whether the loss of the program that would not read has been agreed to, so no edit asks again.</summary>
        public bool Told { get; set; }

        /// <summary>
        /// Whether a press of <paramref name="kind" /> would be the edit that replaces a program Sprocket could not read.
        /// </summary>
        /// <remarks>
        /// Only ever true on a block whose program failed to load, and only until it is answered once.
        /// A failed load leaves what the block holds untouched, so the first edit is the moment that
        /// stops being reversible, and it is the moment to say so.
        /// </remarks>
        public bool Replacing(CommandKind kind)
        {
            return !_editor.Program.Intact && !Told && !_editor.Screen.HasQuestion && Changes(kind);
        }

        /// <summary>Whether a command is refused because somebody else holds the Sprocket.</summary>
        /// <remarks>Moving around is all a copy lent to look at may do.</remarks>
        public bool Refuses(CommandKind kind)
        {
            if (_editor.Holder == null)
                return false;

            switch (kind)
            {
                case CommandKind.Back:
                case CommandKind.Expand:
                case CommandKind.OpenSystem:
                case CommandKind.OpenSequence:
                case CommandKind.OpenSlot:
                case CommandKind.PageOn:
                case CommandKind.PageBack:
                case CommandKind.ShowScript:
                case CommandKind.OpenCodex:
                case CommandKind.Shelve:
                case CommandKind.Unshelve:
                case CommandKind.ForgetBackup:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>Asks whether to let the program that would not read go, and carries out <paramref name="go" /> only on the answer that does.</summary>
        /// <remarks>
        /// The screen has been saying all along that what it shows is not what the block holds. This is
        /// the other half: the press that makes it so, held until somebody has been told that the one
        /// that would not read goes with it. There is no undo for that - it is not an edit to a program,
        /// it is which program the block has.
        /// </remarks>
        public void AskBeforeReplacing(Action go)
        {
            _editor.Ask(_editor.Texts.SalvageQuestion);

            // Said here rather than at the save, because this press is the decision and everything
            // after it is ordinary editing. It also settles the banner: what it was warning about has
            // just stopped being true, and nothing else would have told it.
            _editor.Answer(_editor.Texts.SalvageReplace, delegate
            {
                Replace();
                go();
            });

            // The one Enter takes: a key pressed without reading must not throw a program away.
            _editor.Answer(_editor.Texts.SalvageKeep, () => { }, true);
        }

        /// <summary>
        /// Whether a command changes the program rather than where somebody is looking.
        /// </summary>
        /// <remarks>
        /// <b>Anything not listed here changes something.</b> The list is the navigating half because
        /// that is the half that can be enumerated safely: a command added later and forgotten about is
        /// then treated as an edit, which costs one question, where the other default would cost
        /// somebody the only copy of what their block held.
        /// <para>
        /// Picking is navigation. Choosing a block or narrowing a list changes what the picker is
        /// showing and nothing in the program; the trip ends at <see cref="CommandKind.Primary" />, which is
        /// where the edit is, and that is not listed.
        /// </para>
        /// <para>
        /// The same line undo draws: what is shown, switches, and the library's files change no program here.
        /// </para>
        /// </remarks>
        static bool Changes(CommandKind kind)
        {
            switch (kind)
            {
                case CommandKind.None:
                case CommandKind.Back:
                case CommandKind.Answer:
                case CommandKind.Expand:
                case CommandKind.Resolve:
                case CommandKind.Mend:
                case CommandKind.Choice:
                case CommandKind.OpenSystem:
                case CommandKind.OpenSequence:
                case CommandKind.OpenTrigger:
                case CommandKind.LoadSystem:
                case CommandKind.ShowScript:
                case CommandKind.OpenCodex:
                case CommandKind.RunNow:
                case CommandKind.StopNow:
                case CommandKind.TriggerTarget:
                case CommandKind.StartWhen:
                case CommandKind.StartEvery:
                case CommandKind.AddAction:
                case CommandKind.AddWait:
                case CommandKind.StepSlot:
                case CommandKind.PickEntity:
                case CommandKind.PickChoice:
                case CommandKind.NarrowEntities:
                case CommandKind.NarrowChoices:
                case CommandKind.NarrowKinds:
                case CommandKind.AnyKind:
                case CommandKind.OneKind:
                case CommandKind.ReadBlocks:
                case CommandKind.TriggerStops:
                case CommandKind.TriggerDoes:
                case CommandKind.PageOn:
                case CommandKind.PageBack:
                case CommandKind.ShowOrganized:
                case CommandKind.ShowTerminal:
                case CommandKind.HiddenBlocks:
                case CommandKind.ChooseInstance:
                case CommandKind.OpenSlot:
                case CommandKind.EnableSystem:
                case CommandKind.EnableInstance:
                case CommandKind.Shelve:
                case CommandKind.Unshelve:
                case CommandKind.ForgetBackup:
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>Lets the program that would not read go: from here on the block keeps the program being edited.</summary>
        /// <remarks>
        /// One place, two ways in: the question the first edit raises, and the operation on the banner.
        /// Both mean "the program on this block is not coming back", and after it every edit saves like
        /// any other - so the warning has nothing left to say and leaves.
        /// </remarks>
        void Replace()
        {
            Told = true;
            _editor.Program.Kept();
            _editor.Invalidate();
        }
    }
}
