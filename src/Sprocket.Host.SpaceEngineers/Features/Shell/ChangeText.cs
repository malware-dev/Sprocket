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
                case ChangeKind.Describe: return texts.ChangeDescribe;
                case ChangeKind.PickButton: return texts.ChangePickButton;
                case ChangeKind.Step: return texts.ChangeStep;
                case ChangeKind.When: return texts.ChangeWhen;
                case ChangeKind.ForgetTrigger: return texts.ChangeForgetTrigger;
                case ChangeKind.MoveStep: return texts.ChangeMoveStep;
                case ChangeKind.Target: return texts.ChangeTarget;
                case ChangeKind.RemoveStep: return texts.ChangeRemoveStep;
                case ChangeKind.Rename: return texts.ChangeRename;
                case ChangeKind.RenameSlot: return texts.ChangeRenameSlot;
                case ChangeKind.Trigger: return texts.ChangeTrigger;
                case ChangeKind.Cadence: return texts.ChangeCadence;
                case ChangeKind.Optional: return texts.ChangeOptional;
                case ChangeKind.Required: return texts.ChangeRequired;
                case ChangeKind.Wait: return texts.ChangeWait;
                case ChangeKind.PauseStep: return texts.ChangePauseStep;
                case ChangeKind.ResumeStep: return texts.ChangeResumeStep;
                case ChangeKind.TakeOver: return texts.ChangeTakeOver;
                case ChangeKind.AddStep: return texts.ChangeAddStep;
                case ChangeKind.AddWait: return texts.ChangeAddWait;
                case ChangeKind.DescribeSlot: return texts.ChangeDescribeSlot;
                case ChangeKind.ShortenSlot: return texts.ChangeShortenSlot;
                case ChangeKind.AddIf: return texts.ChangeAddIf;
                case ChangeKind.AddBranch: return texts.ChangeAddBranch;
                case ChangeKind.RemoveIf: return texts.ChangeRemoveIf;
                case ChangeKind.Unwrap: return texts.ChangeUnwrap;
                case ChangeKind.RemoveBranch: return texts.ChangeRemoveBranch;
                case ChangeKind.MoveBranch: return texts.ChangeMoveBranch;
                case ChangeKind.DeleteSequence: return texts.ChangeDeleteSequence;
                case ChangeKind.NewSequence: return texts.ChangeNewSequence;
                case ChangeKind.CopySequence: return texts.ChangeCopySequence;
                case ChangeKind.CopyStep: return texts.ChangeCopyStep;
                case ChangeKind.CopyTrigger: return texts.ChangeCopyTrigger;
                case ChangeKind.RenameSystem: return texts.ChangeRenameSystem;
                case ChangeKind.RenameInstallation: return texts.ChangeRenameInstallation;
                case ChangeKind.FillSlot: return texts.ChangeFillSlot;
                case ChangeKind.AddTrigger: return texts.ChangeAddTrigger;
                case ChangeKind.PutOnSystem: return texts.ChangePutOnSystem;
                case ChangeKind.CopyBlueprint: return texts.ChangeCopyBlueprint;
                case ChangeKind.RenameConstruct: return texts.ChangeRenameConstruct;
                case ChangeKind.DeleteSystem: return texts.ChangeDeleteSystem;
                case ChangeKind.TakeOffSystem: return texts.ChangeTakeOffSystem;
                case ChangeKind.DeleteInstallation: return texts.ChangeDeleteInstallation;
                case ChangeKind.AddSystem: return texts.ChangeAddSystem;
                case ChangeKind.AddInstallation: return texts.ChangeAddInstallation;
                case ChangeKind.UpdateFromLibrary: return texts.ChangeUpdateFromLibrary;
                case ChangeKind.ResetToBlueprint: return texts.ChangeResetToBlueprint;
                case ChangeKind.ChooseVersion: return texts.ChangeChooseVersion;
                case ChangeKind.MoveSlot: return texts.ChangeMoveSlot;
                case ChangeKind.MoveCategory: return texts.ChangeMoveCategory;
                case ChangeKind.AddCategory: return texts.ChangeAddCategory;
                case ChangeKind.RenameCategory: return texts.ChangeRenameCategory;
                case ChangeKind.RemoveCategory: return texts.ChangeRemoveCategory;
                case ChangeKind.AddType: return texts.ChangeAddType;
                case ChangeKind.RenameType: return texts.ChangeRenameType;
                case ChangeKind.RemoveType: return texts.ChangeRemoveType;
                case ChangeKind.MoveType: return texts.ChangeMoveType;
                case ChangeKind.AddChoice: return texts.ChangeAddChoice;
                case ChangeKind.RenameChoice: return texts.ChangeRenameChoice;
                case ChangeKind.RemoveChoice: return texts.ChangeRemoveChoice;
                case ChangeKind.AddState: return texts.ChangeAddState;
                case ChangeKind.RenameState: return texts.ChangeRenameState;
                case ChangeKind.RemoveState: return texts.ChangeRemoveState;
                case ChangeKind.MoveState: return texts.ChangeMoveState;
                case ChangeKind.ChangeStart: return texts.ChangeChangeStart;
                default: return "";
            }
        }
    }
}
