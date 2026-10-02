
namespace Sprocket.Host.SpaceEngineers.Features.Shell
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

        /// <summary>Switches a step off, or back on.</summary>
        SwitchStep,

        /// <summary>Puts the program a text says in place of the one there, having read the text first.</summary>
        Mend,

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
        MakeOptional,
        StartWhen,
        StartEvery,
        OpenSequence,
        NewSequence,

        // Triggers.
        OpenTrigger,
        TriggerTarget,
        DeleteTrigger,

        // A logic system's switch, and one of a blueprint's.
        EnableSystem,
        EnableInstance,

        // A sequence.
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

        // The library. Keeping a blueprint for every world and forgetting a lost construct change no program, so a copy
        // lent to look at may do both.
        Shelve,
        Unshelve,
        PutOn,
        TakeOff,
        ForgetBackup,

        // A list of answers.
        Choice,
        ChoiceTaken,

        /// <summary>Tools for working on Sprocket itself. Only ever registered in developer mode.</summary>
        ShowScript
    }
}
