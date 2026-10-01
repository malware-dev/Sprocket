
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
    enum Keyword
    {
        None = 0,

        Sprocket,
        Block,
        System,
        Instance,
        Of,
        Sequence,
        When,
        Every,
        Then,
        Trigger,
        To,
        Run,
        Set,

        On,
        Off,
        All,
        Any,
        Wait,
        Await,
        Required,
        Expects,
        Equals,
        Changed,
        As,
        Last,

        /// <summary>A run that was under way when the world was written out.</summary>
        Running,

        /// <summary>Which step of it, in the same line.</summary>
        Step,

        /// <summary>Abandoning a run: a step, as a sequence or a trigger does it.</summary>
        Stop,

        /// <summary>A step calling an action on some blocks.</summary>
        Call,

        /// <summary>The values such an action has to be given.</summary>
        With,

        /// <summary>Which draft of an unfinished format a program was written against.</summary>
        Alpha,

        /// <summary>A slot an instance may leave unanswered.</summary>
        Optional,

        /// <summary>A variable: a name for something on the program's construct.</summary>
        Let,

        /// <summary>A system written against slots, to be used by instances.</summary>
        Blueprint,

        /// <summary>Something a system blueprint needs a construct to provide.</summary>
        Slot
    }
}
