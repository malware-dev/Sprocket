namespace Reactor.Host.SpaceEngineers.Editing
{
    /// <summary>
    /// What a command is for. Half of a command's identity; the rest is which one it is.
    /// </summary>
    /// <remarks>
    /// An id is a reference and never a position. A counter reset on every rebuild would mean "the
    /// seventh thing this page registered this time round", which is stable only by coincidence: every
    /// id a target had captured would go dead the moment anything moved, and
    /// <see cref="ReactorEditor.Invoke"/> ignores an id it does not know, so it would fail invisibly.
    /// R47 says the same thing everywhere else - references are by identity, and an edit carries what
    /// it points at.
    /// </remarks>
    enum Cmd
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
        StartWhen,
        StartEvery,
        OpenSequence,
        NewSequence,

        // Triggers.
        OpenTrigger,
        TriggerTarget,
        ClearTarget,
        DeleteTrigger,

        // A sequence.
        Enable,
        EnableInstance,
        DeleteSequence,
        DuplicateSequence,
        RunNow,
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
        AnyKind,
        OneKind,
        RequireAny,
        RequireAll,

        // A list of answers.
        Choice
    }
}
