namespace Sprocket.Host.SpaceEngineers.Screen
{
    /// <summary>
    /// The things a target must provide, which core cannot make it provide.
    /// </summary>
    /// <remarks>
    /// Core states capabilities and leaves presentation alone, which is right and leaves a
    /// hole: nothing stops a host simply never drawing one of them. That hole is not fully
    /// closeable - no test can tell whether a drag target is big enough to hit, or whether a
    /// controller can reach it. Those are review.
    ///
    /// What *is* closeable is forgetting. Each host carries one test that walks this list and
    /// names what it does about every entry, so adding a point here breaks every host until a
    /// person has looked at it. Self-reported, and worth having anyway: it converts "nobody
    /// thought about it" into "somebody decided", which is the same trade the round-trip tests
    /// make for undo.
    ///
    /// Nothing reads this at runtime. It is a handful of strings built once and touched only
    /// by tests, so it costs nothing in the update loop - which matters, because core runs
    /// inside a game's frame.
    /// </remarks>
    public static class Conformance
    {
        public static readonly ConformancePoint[] Points =
        {
            new ConformancePoint(
                "reorder-steps",
                "Some way to move a step to another position, using SentenceStep.MoveToCommand.",
                "A procedure written in the wrong order is the ordinary case, not the "
                + "exception. A host with no way to reorder makes somebody delete and retype "
                + "the rest of their sequence."),

            new ConformancePoint(
                "narrow-a-long-list",
                "Some way to narrow a stage that says it can be, using PickStage.NarrowCommand.",
                "A large construct has hundreds of blocks. Without narrowing, picking one is "
                + "scrolling, and the search is the only thing that makes the picker usable "
                + "at real scale."),

            new ConformancePoint(
                "a-way-back",
                "A way to leave the current screen, using EditorScreen.BackCommand.",
                "Every screen is reached from another one. A target without this can strand "
                + "somebody on a question they cannot decline to answer."),

            new ConformancePoint(
                "say-when-it-cannot-be-changed",
                "Show SequenceComponent.LockedText whenever Editable is false.",
                "The editing commands are withheld while a sequence can run, so the controls "
                + "genuinely do nothing. Somebody hunting for the add button concludes it was "
                + "removed rather than that it is waiting.")
        };
    }
}
