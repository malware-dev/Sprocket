namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>What an edit did, as it is offered back after Undo and Redo.</summary>
    /// <remarks>Also what a run of edits is folded by: typing a name is many edits of one kind, and one step back.</remarks>
    public enum Change
    {
        /// <summary>Nothing.</summary>
        None,

        /// <summary>Changing the description.</summary>
        Describe,

        /// <summary>Picking a button.</summary>
        PickButton,

        /// <summary>Changing a step.</summary>
        Step,

        /// <summary>Changing when it runs.</summary>
        When,

        /// <summary>Deleting a trigger.</summary>
        ForgetTrigger,

        /// <summary>Moving a step.</summary>
        MoveStep,

        /// <summary>Changing what it does.</summary>
        Target,

        /// <summary>Deleting a step.</summary>
        RemoveStep,

        /// <summary>Renaming.</summary>
        Rename,

        /// <summary>Renaming a slot.</summary>
        RenameSlot,

        /// <summary>Changing the trigger.</summary>
        Trigger,

        /// <summary>Changing how often it runs.</summary>
        Cadence,

        /// <summary>Making a slot optional.</summary>
        Optional,

        /// <summary>Making a slot required.</summary>
        Required,

        /// <summary>Changing a wait.</summary>
        Wait,

        /// <summary>Switching a step off.</summary>
        PauseStep,

        /// <summary>Switching a step on.</summary>
        ResumeStep,

        /// <summary>Taking a button over.</summary>
        TakeOver,

        /// <summary>Adding a step.</summary>
        AddStep,

        /// <summary>Adding a wait.</summary>
        AddWait,

        /// <summary>Describing a slot.</summary>
        DescribeSlot,

        /// <summary>Adding an if.</summary>
        AddIf,

        /// <summary>Adding a branch to an if.</summary>
        AddBranch,

        /// <summary>Deleting an if and the steps in it.</summary>
        RemoveIf,

        /// <summary>Taking an if away and keeping the steps in it.</summary>
        Unwrap,

        /// <summary>Deleting a branch of an if.</summary>
        RemoveBranch,

        /// <summary>Moving a branch of an if to another place in it.</summary>
        MoveBranch,
        /// <summary>Deleting a logic system.</summary>
        DeleteSystem,
        /// <summary>Taking a logic system off the construct.</summary>
        TakeOffSystem,
        /// <summary>Deleting an installation.</summary>
        DeleteInstallation,
        /// <summary>Adding a logic system.</summary>
        AddSystem,
        /// <summary>Adding an installation.</summary>
        AddInstallation
    }
}
