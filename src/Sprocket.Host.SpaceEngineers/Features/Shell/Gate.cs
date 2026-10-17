using System;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>Which presses the editor lets through: none that would change a Sprocket lent to look at, and the first edit of a program that would not read only once its loss is agreed to.</summary>
    public sealed class Gate
    {
        readonly SprocketEditor _editor;

        /// <summary>A gate on the presses of <paramref name="editor" />.</summary>
        public Gate(SprocketEditor editor)
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
        public bool Replacing(Cmd kind)
        {
            return !_editor.Program.Intact && !Told && !_editor.Screen.HasQuestion && Changes(kind);
        }

        /// <summary>Whether a command is refused because somebody else holds the Sprocket.</summary>
        /// <remarks>Moving around is all a copy lent to look at may do.</remarks>
        public bool Refuses(Cmd kind)
        {
            if (_editor.Holder == null)
                return false;

            switch (kind)
            {
                case Cmd.Back:
                case Cmd.Expand:
                case Cmd.OpenSystem:
                case Cmd.OpenSequence:
                case Cmd.OpenSlot:
                case Cmd.PageOn:
                case Cmd.PageBack:
                case Cmd.ShowScript:
                case Cmd.OpenCodex:
                case Cmd.Shelve:
                case Cmd.Unshelve:
                case Cmd.ForgetBackup:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Says what the first edit costs, and does it only if they say so.
        /// </summary>
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
        /// showing and nothing in the program; the trip ends at <see cref="Cmd.Primary" />, which is
        /// where the edit is, and that is not listed.
        /// </para>
        /// <para>
        /// The same line undo draws: what is shown, switches, and the library's files change no program here.
        /// </para>
        /// </remarks>
        static bool Changes(Cmd kind)
        {
            switch (kind)
            {
                case Cmd.None:
                case Cmd.Back:
                case Cmd.Answer:
                case Cmd.Expand:
                case Cmd.Resolve:
                case Cmd.Mend:
                case Cmd.Choice:
                case Cmd.OpenSystem:
                case Cmd.OpenSequence:
                case Cmd.OpenTrigger:
                case Cmd.LoadSystem:
                case Cmd.ShowScript:
                case Cmd.OpenCodex:
                case Cmd.RunNow:
                case Cmd.StopNow:
                case Cmd.TriggerTarget:
                case Cmd.StartWhen:
                case Cmd.StartEvery:
                case Cmd.AddAction:
                case Cmd.AddWait:
                case Cmd.StepSlot:
                case Cmd.PickEntity:
                case Cmd.PickChoice:
                case Cmd.NarrowEntities:
                case Cmd.NarrowChoices:
                case Cmd.NarrowKinds:
                case Cmd.AnyKind:
                case Cmd.OneKind:
                case Cmd.ReadBlocks:
                case Cmd.TriggerStops:
                case Cmd.TriggerDoes:
                case Cmd.PageOn:
                case Cmd.PageBack:
                case Cmd.ShowOrganized:
                case Cmd.ShowTerminal:
                case Cmd.HiddenBlocks:
                case Cmd.ChooseInstance:
                case Cmd.OpenSlot:
                case Cmd.EnableSystem:
                case Cmd.EnableInstance:
                case Cmd.Shelve:
                case Cmd.Unshelve:
                case Cmd.ForgetBackup:
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Takes the decision the banner and the question both ask for.
        /// </summary>
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
