namespace Sprocket.Host.SpaceEngineers.Editing
{
    /// <summary>
    /// Why a sequence cannot work.
    /// </summary>
    /// <remarks>
    /// A sequence pointing at a block that got shot off must never simply do nothing: silence is
    /// indistinguishable from working, and that is precisely the failure a non-programmer has no way
    /// to diagnose. So core states which of these it is, and a target says so however it says things.
    /// </remarks>
    public enum SequenceProblem
    {
        None,

        /// <summary>It listens to something that is no longer here.</summary>
        MissingSource,

        /// <summary>It acts on something that is no longer here.</summary>
        MissingTarget,

        /// <summary>It waits on something that is no longer here.</summary>
        MissingGate,

        /// <summary>
        /// Nothing is missing and nothing will ever start it.
        /// </summary>
        /// <remarks>
        /// A different kind of wrong from the rest of these. Every other member says something the
        /// sequence names is gone; this one says the sequence is complete, correct, and unreachable -
        /// so the fix is not to mend a reference but to give it something that fires it.
        /// <para>
        /// A button whose toolbar slot is no longer Sprocket's, because somebody cleared it from the
        /// terminal - which is a legitimate way to say "stop doing that", so it is reported rather than
        /// undone. Or a trigger left with nothing in it on a sequence that has steps, which cannot run
        /// however it got that way.
        /// </para>
        /// </remarks>
        NeverTriggered,

        /// <summary>
        /// Nothing has been given the job of starting it: no trigger, and no sequence that calls it.
        /// </summary>
        /// <remarks>
        /// <b>A warning rather than a fault.</b> Nothing is broken and nothing is wired wrongly - there
        /// is simply nobody to start this yet, which is an ordinary state for work in progress. So it is
        /// said quietly: a line of words, no red, and Sprocket does not park it, because parking
        /// something somebody is about to call would make the call fail for a second reason.
        /// <para>
        /// It could not be judged at all until a step could say <c>run</c>. Before that, a sequence with
        /// no trigger might have been waiting to be called by something the model had no way to write
        /// down, so saying "nothing starts this" would have been a guess.
        /// </para>
        /// </remarks>
        NothingStarts,

        /// <summary>
        /// It is switched off, so nothing starts it however right the rest of it is.
        /// </summary>
        /// <remarks>
        /// <b>A warning rather than a fault</b>, and the one that is deliberate: switching a sequence
        /// off is how somebody stops it happening while they work on it. It is still the likeliest
        /// reason a button does nothing, and the switch is on a different screen from the button that
        /// was pressed, so a row that only reported its state in the column where every row reports
        /// something said it too quietly to be found by somebody who did not already suspect it.
        /// </remarks>
        SwitchedOff,

        /// <summary>
        /// Everything it names is here, but at least one of them was found by a weaker key.
        /// </summary>
        /// <remarks>
        /// A door was rebuilt, or the ship was pasted from a blueprint. There is a candidate and it
        /// is not accepted, because accepting it silently is how the wrong door gets cycled. The
        /// sequence does not run until somebody says that is the right one.
        /// </remarks>
        Adrift
    }
}
