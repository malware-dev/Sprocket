namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>What an edit did, as it is offered back after Undo and Redo.</summary>
    /// <remarks>Consecutive edits of one kind fold into one step back.</remarks>
    public enum ChangeKind
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

        /// <summary>Giving a signal slot its short name.</summary>
        ShortenSlot,

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
        /// <summary>Deleting a sequence, with the triggers that run it.</summary>
        DeleteSequence,
        /// <summary>Making a new sequence.</summary>
        NewSequence,
        /// <summary>Copying a sequence.</summary>
        CopySequence,
        /// <summary>Renaming a logic system.</summary>
        RenameSystem,
        /// <summary>Renaming an installation.</summary>
        RenameInstallation,
        /// <summary>Filling a slot of an installation.</summary>
        FillSlot,
        /// <summary>Adding a trigger, with what it starts.</summary>
        AddTrigger,
        /// <summary>Putting a logic system on the construct from the library.</summary>
        PutOnSystem,
        /// <summary>Copying a blueprint as a new logic system.</summary>
        CopyBlueprint,
        /// <summary>Renaming the construct.</summary>
        RenameConstruct,
        /// <summary>Deleting a logic system.</summary>
        DeleteSystem,
        /// <summary>Taking a logic system off the construct.</summary>
        TakeOffSystem,
        /// <summary>Deleting an installation.</summary>
        DeleteInstallation,
        /// <summary>Adding a logic system.</summary>
        AddSystem,
        /// <summary>Adding an installation.</summary>
        AddInstallation,
        /// <summary>Putting a logic system's blueprint in the library in the place of the logic system.</summary>
        UpdateFromLibrary,
        /// <summary>Putting a logic system's blueprint in the library back in the place of the logic system, which changed since.</summary>
        ResetToBlueprint,
        /// <summary>Copying a step.</summary>
        CopyStep,
        /// <summary>Copying a trigger.</summary>
        CopyTrigger,
        /// <summary>Keeping one of two versions of a logic system.</summary>
        ChooseVersion,
        /// <summary>Moving a slot to another place on the installation panel.</summary>
        MoveSlot,
        /// <summary>Moving a category of slots, with its slots, to another place on the installation panel.</summary>
        MoveCategory,
        /// <summary>Adding a category of slots.</summary>
        AddCategory,
        /// <summary>Renaming a category of slots.</summary>
        RenameCategory,
        /// <summary>Removing a category of slots, which leaves its slots under none.</summary>
        RemoveCategory,

        /// <summary>Adding a type to a logic system.</summary>
        AddType,

        /// <summary>Renaming a type of a logic system.</summary>
        RenameType,

        /// <summary>Removing a type no state holds from a logic system.</summary>
        RemoveType,

        /// <summary>Adding a choice to a type.</summary>
        AddChoice,

        /// <summary>Renaming a choice of a type, everywhere it is used.</summary>
        RenameChoice,

        /// <summary>Removing a choice nothing uses from a type.</summary>
        RemoveChoice,

        /// <summary>Adding a state to a logic system.</summary>
        AddState,

        /// <summary>Renaming a state, everywhere it is used.</summary>
        RenameState,

        /// <summary>Removing a state nothing uses from a logic system.</summary>
        RemoveState,

        /// <summary>Changing the value a state starts at.</summary>
        ChangeStart
    }
}
