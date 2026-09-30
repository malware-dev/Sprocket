
namespace Sprocket.Host.SpaceEngineers.Runtime
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

        /// <summary>It asks its blocks for something they do not offer.</summary>
        /// <remarks>
        /// The blocks are all here; what the step does is not among what they offer, because a block
        /// was replaced by one of another kind or the mod that taught it the act is gone. The step
        /// stays as it was written, so it can be read and re-picked rather than turned into something
        /// else.
        /// </remarks>
        Unoffered,

        /// <summary>
        /// One of its steps has not been given a value it cannot do without.
        /// </summary>
        /// <remarks>
        /// What an action needs is the action's to say: a rotor told to turn at no speed sets out for
        /// an angle it can never reach. A program written anywhere but the editor can hold one, and so
        /// can one the editor wrote before the action said it needed it.
        /// </remarks>
        Unfilled,

        /// <summary>One of its steps has been given a value its action does not take: another kind, or too large for its type.</summary>
        /// <remarks>
        /// Found when the program is bound rather than when the step runs, and the step does nothing to
        /// a block that would not take it. A program written anywhere but the editor can hold one.
        /// </remarks>
        Mistyped,

        /// <summary>One of its steps goes straight on without waiting for what it started, which takes time.</summary>
        /// <remarks>
        /// A program from anywhere but the editor can hold one, and the editor draws every step that takes
        /// time as one that waits, so what it would show is not what runs. Said rather than drawn as if it
        /// waited.
        /// </remarks>
        Unawaited,

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

        /// <summary>A slot it uses, and may not do without, is empty in the one of its system it runs on.</summary>
        EmptySlot,

        /// <summary>
        /// Everything it names is here, but at least one of them was found by a weaker key.
        /// </summary>
        /// <remarks>
        /// A door was rebuilt, or the ship was pasted from a blueprint. There is a candidate and it
        /// is not accepted, because accepting it silently is how the wrong door gets cycled. The
        /// sequence does not run until somebody says that is the right one.
        /// </remarks>
        Adrift,

        /// <summary>
        /// A step the sequence is waiting on that the blocks say will never finish.
        /// </summary>
        /// <remarks>
        /// Waiting is not a fault - an airlock waits for a room every time it cycles. This is the
        /// case where waiting can no longer end: a vent with nowhere to put the air, a room with no
        /// oxygen to fill it with. Only the block knows, so the words come from the block.
        /// </remarks>
        Stuck,

        /// <summary>
        /// The last run gave up because something else took a block out of its hands.
        /// </summary>
        /// <remarks>
        /// A warning rather than a fault: nothing is broken, two things wanted opposite things of
        /// one block and the later one won. It says what happened last time and is cleared the next
        /// time the sequence runs.
        /// </remarks>
        Conflicted,

        /// <summary>One of its steps runs a sequence that starts it again, directly or through the sequences that one runs.</summary>
        /// <remarks>The language makes it an error, so the system does not run. Stopping itself is not this: a sequence may.</remarks>
        Circular,

        /// <summary>A step asked a block that is switched off, or not working, for something a step waits for.</summary>
        /// <remarks>What it was asked would never happen, so the run ends there and says which block.</remarks>
        Unable
    }
}
