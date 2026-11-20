using System;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
{
    /// <summary>Which presses the editor lets through.</summary>
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

        /// <summary>Whether a press of <paramref name="kind" /> would be the edit that replaces a program Sprocket could not read.</summary>
        public bool Replacing(CommandKind kind)
        {
            return !_editor.Program.Intact && !Told && !_editor.Screen.HasQuestion && Changes(kind);
        }

        /// <summary>Whether a command is refused because somebody else holds the Sprocket.</summary>
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
                case CommandKind.ShowScript:
                case CommandKind.CheckMembers:
                case CommandKind.OpenGuide:
                case CommandKind.Shelve:
                case CommandKind.Unshelve:
                case CommandKind.ForgetBackup:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>Asks whether to let the program that would not read go, and carries out <paramref name="go" /> only on the answer that does.</summary>
        public void AskBeforeReplacing(Action go)
        {
            _editor.Ask(_editor.Texts.SalvageQuestion);

            _editor.Answer(_editor.Texts.SalvageReplace, delegate
            {
                Replace();
                go();
            });

            // The one Enter takes, so a key pressed without reading keeps the program.
            _editor.Answer(_editor.Texts.SalvageKeep, () => { }, true);
        }

        /// <summary>Whether a command changes the program rather than where somebody is looking.</summary>
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
                case CommandKind.CheckMembers:
                case CommandKind.OpenGuide:
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
        void Replace()
        {
            Told = true;
            _editor.Program.OriginalIsSafe();
            _editor.Invalidate();
        }
    }
}
