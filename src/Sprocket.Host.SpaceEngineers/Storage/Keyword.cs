
namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>
    /// One of the format's own words, as a number rather than as text.
    /// </summary>
    /// <remarks>
    /// A bare word is recognised while the text is being read, so a keyword never becomes a string at
    /// all: no allocation to make it, and every later question about it is an integer comparison instead
    /// of a character-by-character one. A step asks five of those - wait, now, after, once, onlyif - on
    /// every line.
    /// <para>
    /// <see cref="None" /> is zero, so an identifier that is nobody's keyword is the default answer.
    /// </para>
    /// </remarks>
    public enum Keyword
    {
        /// <summary>No keyword: a word the format gives no meaning to.</summary>
        None = 0,

        /// <summary>The first statement of a program, saying which format it is written in.</summary>
        Sprocket,
        /// <summary>One actual block on the program's construct.</summary>
        Block,
        /// <summary>A logic system, or a system blueprint after <c>blueprint</c>.</summary>
        System,
        /// <summary>One use of a system blueprint on the construct.</summary>
        Instance,
        /// <summary>Which system blueprint an instance uses.</summary>
        Of,
        /// <summary>A sequence of steps.</summary>
        Sequence,
        /// <summary>A trigger started by a moment a block reaches.</summary>
        When,
        /// <summary>A trigger started by a clock, or how often a watched moment is looked for.</summary>
        Every,
        /// <summary>What a trigger does, after what starts it.</summary>
        Then,
        /// <summary>Which trigger a saved run belongs to.</summary>
        Trigger,
        /// <summary>The value a step sets.</summary>
        To,
        /// <summary>A step or trigger that runs a sequence.</summary>
        Run,
        /// <summary>A step that sets a value on some blocks.</summary>
        Set,

        /// <summary>The blocks a member is on, or a flag that holds.</summary>
        On,
        /// <summary>A flag that does not hold, such as a block's switch.</summary>
        Off,
        /// <summary>A test that has to hold on every one of the blocks named.</summary>
        All,
        /// <summary>A test that holds where it holds on any one of the blocks named.</summary>
        Any,
        /// <summary>A step that waits a length of time.</summary>
        Wait,
        /// <summary>A step that waits for a sequence it started to finish.</summary>
        Await,
        /// <summary>A slot every instance has to fill.</summary>
        Required,
        /// <summary>What may fill a slot.</summary>
        Expects,
        /// <summary>What a variable holds, or what answers a slot in an instance.</summary>
        Equals,
        /// <summary>When a logic system was last changed.</summary>
        Changed,
        /// <summary>The name something is called by.</summary>
        As,
        /// <summary>The highest sequence number a logic system has given out, kept where that sequence is gone.</summary>
        Last,

        /// <summary>A run that was under way when the world was written out.</summary>
        Running,

        /// <summary>Which step of it, in the same line.</summary>
        Step,

        /// <summary>Abandoning a run: a step, as a sequence or a trigger does it.</summary>
        Stop,

        /// <summary>A step calling an action on some blocks.</summary>
        Call,

        /// <summary>Which draft of an unfinished format a program was written against.</summary>
        Alpha,

        /// <summary>A slot an instance may leave unanswered.</summary>
        Optional,

        /// <summary>A variable: a name for something on the program's construct.</summary>
        Let,

        /// <summary>A system written against slots, to be used by instances.</summary>
        Blueprint,

        /// <summary>Something a system blueprint needs a construct to provide.</summary>
        Slot,

        /// <summary>A step that runs its block only where a test holds.</summary>
        If,

        /// <summary>What follows an <c>if</c>: another test, or what runs where none held.</summary>
        Else,

        /// <summary>The test that a member equals a value.</summary>
        EqualTo,

        /// <summary>What a slot expects where a signal answers it: a signal, named by the place it is sent from.</summary>
        Signal,

        /// <summary>A test on the total of a number read on every one of the blocks named.</summary>
        Sum,

        /// <summary>The test that a member differs from a value.</summary>
        NotEqualTo,

        /// <summary>The test that a number is below a value.</summary>
        Below,

        /// <summary>The test that a number is above a value.</summary>
        Above,

        /// <summary>The test that a number is at most a value.</summary>
        AtMost,

        /// <summary>The test that a number is at least a value.</summary>
        AtLeast,

        /// <summary>A trigger started each time a signal is sent.</summary>
        Whenever,

        /// <summary>The stored blueprint a logic system was made from, and the version of it.</summary>
        From,

        /// <summary>A test on the average of a number read on every one of the blocks named.</summary>
        Average
    }
}
