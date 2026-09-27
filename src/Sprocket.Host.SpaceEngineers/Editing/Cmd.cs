namespace Sprocket.Host.SpaceEngineers.Editing
{
    /// <summary>What a press is for, which decides whether a copy lent to look at may make it and whether it changes the program.</summary>
    public enum Cmd
    {
        None = 0,

        // Screen furniture. One of each per screen.
        Back,
        Answer,
        Primary,
        Rename,
        Refresh,
        Switch,
        Expand,
        Resolve,

        // The list screens.
        NewSystem,
        LoadSystem,
        OpenSystem,
        AddInstance,
        MakeBlueprint,
        ChooseInstance,
        DeleteSystem,
        DeleteInstance,
        EditBlueprint,
        FillPart,
        RenamePart,
        StartWhen,
        StartEvery,
        OpenSequence,
        NewSequence,

        // Triggers.
        OpenTrigger,
        TriggerTarget,
        DeleteTrigger,

        // A sequence.
        Enable,
        EnableInstance,
        DeleteSequence,
        DuplicateSequence,
        RunNow,

        StopNow,
        Undo,
        Redo,
        AddAction,
        AddWait,
        StepSlot,
        MoveStep,
        DeleteStep,

        // Picking.
        PickEntity,
        PickChoice,
        NarrowEntities,
        NarrowChoices,
        NarrowKinds,
        ShowOrganized,
        ShowTerminal,
        AnyKind,
        OneKind,
        RequireAny,
        RequireAll,
        TriggerRuns,
        TriggerStops,
        TriggerDoes,
        PageOn,
        PageBack,

        // A list of answers.
        Choice,
        ChoiceTaken,

        /// <summary>Tools for working on Sprocket itself. Only ever registered in developer mode.</summary>
        ShowScript
    }
}
