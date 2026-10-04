using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>What each kind of change is called after Undo and Redo.</summary>
    public static class ChangeText
    {
        /// <summary>The words for <paramref name="change" /> in <paramref name="texts" />, or empty for none.</summary>
        public static string Of(Texts texts, Change change)
        {
            switch (change)
            {
                case Change.Describe: return texts.ChangeDescribe;
                case Change.PickButton: return texts.ChangePickButton;
                case Change.Step: return texts.ChangeStep;
                case Change.When: return texts.ChangeWhen;
                case Change.ForgetTrigger: return texts.ChangeForgetTrigger;
                case Change.MoveStep: return texts.ChangeMoveStep;
                case Change.Target: return texts.ChangeTarget;
                case Change.RemoveStep: return texts.ChangeRemoveStep;
                case Change.Rename: return texts.ChangeRename;
                case Change.RenameSlot: return texts.ChangeRenameSlot;
                case Change.Trigger: return texts.ChangeTrigger;
                case Change.Cadence: return texts.ChangeCadence;
                case Change.Optional: return texts.ChangeOptional;
                case Change.Required: return texts.ChangeRequired;
                case Change.Wait: return texts.ChangeWait;
                case Change.PauseStep: return texts.ChangePauseStep;
                case Change.ResumeStep: return texts.ChangeResumeStep;
                case Change.TakeOver: return texts.ChangeTakeOver;
                case Change.AddStep: return texts.ChangeAddStep;
                case Change.AddWait: return texts.ChangeAddWait;
                case Change.DescribeSlot: return texts.ChangeDescribeSlot;
                case Change.AddIf: return texts.ChangeAddIf;
                case Change.AddBranch: return texts.ChangeAddBranch;
                case Change.RemoveIf: return texts.ChangeRemoveIf;
                case Change.Unwrap: return texts.ChangeUnwrap;
                case Change.RemoveBranch: return texts.ChangeRemoveBranch;
                case Change.MoveBranch: return texts.ChangeMoveBranch;
                default: return "";
            }
        }
    }
}
