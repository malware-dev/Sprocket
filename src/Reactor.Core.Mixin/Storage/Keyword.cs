namespace Reactor.Storage
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

        Reactor,
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
        /// <summary>Reserved before anything writes it, so a save written now still reads when it does.</summary>
        Run,

        /// <summary>Reserved before anything writes it. See <see cref="Run" />.</summary>
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
        Last
    }
}
