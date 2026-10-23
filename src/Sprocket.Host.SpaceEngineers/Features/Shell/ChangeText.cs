using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>What each kind of change is called after Undo and Redo.</summary>
    public static class ChangeText
    {
        /// <summary>The words for <paramref name="change" /> in <paramref name="texts" />, or empty for none.</summary>
        public static string Of(Texts texts, ChangeKind change)
        {
            switch (change)
            {
                case ChangeKind.Describe: return texts.Codex.ChangeDescribe;
                case ChangeKind.PickButton: return texts.Codex.ChangePickButton;
                case ChangeKind.Step: return texts.Codex.ChangeStep;
                case ChangeKind.When: return texts.Codex.ChangeWhen;
                case ChangeKind.ForgetTrigger: return texts.Codex.ChangeForgetTrigger;
                case ChangeKind.MoveStep: return texts.Codex.ChangeMoveStep;
                case ChangeKind.Target: return texts.Codex.ChangeTarget;
                case ChangeKind.RemoveStep: return texts.Codex.ChangeRemoveStep;
                case ChangeKind.Rename: return texts.Codex.ChangeRename;
                case ChangeKind.RenameSlot: return texts.Codex.ChangeRenameSlot;
                case ChangeKind.Trigger: return texts.Codex.ChangeTrigger;
                case ChangeKind.Cadence: return texts.Codex.ChangeCadence;
                case ChangeKind.Optional: return texts.Codex.ChangeOptional;
                case ChangeKind.Required: return texts.Codex.ChangeRequired;
                case ChangeKind.SlotType: return texts.Codex.ChangeSlotType;
                case ChangeKind.Wait: return texts.Codex.ChangeWait;
                case ChangeKind.PauseStep: return texts.Codex.ChangePauseStep;
                case ChangeKind.ResumeStep: return texts.Codex.ChangeResumeStep;
                case ChangeKind.TakeOver: return texts.Codex.ChangeTakeOver;
                case ChangeKind.AddStep: return texts.Codex.ChangeAddStep;
                case ChangeKind.AddWait: return texts.Codex.ChangeAddWait;
                case ChangeKind.DescribeSlot: return texts.Codex.ChangeDescribeSlot;
                case ChangeKind.ShortenSlot: return texts.Codex.ChangeShortenSlot;
                case ChangeKind.AddIf: return texts.Codex.ChangeAddIf;
                case ChangeKind.AddBranch: return texts.Codex.ChangeAddBranch;
                case ChangeKind.RemoveIf: return texts.Codex.ChangeRemoveIf;
                case ChangeKind.Unwrap: return texts.Codex.ChangeUnwrap;
                case ChangeKind.RemoveBranch: return texts.Codex.ChangeRemoveBranch;
                case ChangeKind.MoveBranch: return texts.Codex.ChangeMoveBranch;
                case ChangeKind.DeleteSequence: return texts.Codex.ChangeDeleteSequence;
                case ChangeKind.NewSequence: return texts.Codex.ChangeNewSequence;
                case ChangeKind.CopySequence: return texts.Codex.ChangeCopySequence;
                case ChangeKind.CopyStep: return texts.Codex.ChangeCopyStep;
                case ChangeKind.CopyTrigger: return texts.Codex.ChangeCopyTrigger;
                case ChangeKind.RenameSystem: return texts.Codex.ChangeRenameSystem;
                case ChangeKind.RenameInstallation: return texts.Codex.ChangeRenameInstallation;
                case ChangeKind.FillSlot: return texts.Codex.ChangeFillSlot;
                case ChangeKind.AddTrigger: return texts.Codex.ChangeAddTrigger;
                case ChangeKind.PutOnSystem: return texts.Codex.ChangePutOnSystem;
                case ChangeKind.CopyBlueprint: return texts.Codex.ChangeCopyBlueprint;
                case ChangeKind.RenameConstruct: return texts.Codex.ChangeRenameConstruct;
                case ChangeKind.DeleteSystem: return texts.Codex.ChangeDeleteSystem;
                case ChangeKind.TakeOffSystem: return texts.Codex.ChangeTakeOffSystem;
                case ChangeKind.DeleteInstallation: return texts.Codex.ChangeDeleteInstallation;
                case ChangeKind.AddSystem: return texts.Codex.ChangeAddSystem;
                case ChangeKind.AddInstallation: return texts.Codex.ChangeAddInstallation;
                case ChangeKind.UpdateFromLibrary: return texts.Codex.ChangeUpdateFromLibrary;
                case ChangeKind.ResetToBlueprint: return texts.Codex.ChangeResetToBlueprint;
                case ChangeKind.ChooseVersion: return texts.Codex.ChangeChooseVersion;
                case ChangeKind.MoveSlot: return texts.Codex.ChangeMoveSlot;
                case ChangeKind.MoveCategory: return texts.Codex.ChangeMoveCategory;
                case ChangeKind.AddCategory: return texts.Codex.ChangeAddCategory;
                case ChangeKind.RenameCategory: return texts.Codex.ChangeRenameCategory;
                case ChangeKind.RemoveCategory: return texts.Codex.ChangeRemoveCategory;
                case ChangeKind.AddType: return texts.Codex.ChangeAddType;
                case ChangeKind.RenameType: return texts.Codex.ChangeRenameType;
                case ChangeKind.RemoveType: return texts.Codex.ChangeRemoveType;
                case ChangeKind.MoveType: return texts.Codex.ChangeMoveType;
                case ChangeKind.AddChoice: return texts.Codex.ChangeAddChoice;
                case ChangeKind.RenameChoice: return texts.Codex.ChangeRenameChoice;
                case ChangeKind.RemoveChoice: return texts.Codex.ChangeRemoveChoice;
                case ChangeKind.AddState: return texts.Codex.ChangeAddState;
                case ChangeKind.RenameState: return texts.Codex.ChangeRenameState;
                case ChangeKind.RemoveState: return texts.Codex.ChangeRemoveState;
                case ChangeKind.MoveState: return texts.Codex.ChangeMoveState;
                case ChangeKind.ChangeStart: return texts.Codex.ChangeChangeStart;
                default: return "";
            }
        }
    }
}
