
namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>What a sequence is doing right now.</summary>
    /// <remarks>
    /// A fact rather than a sentence. Which of these it is, is core's to work out; what it reads as,
    /// and whether it is worth saying at all, is the target's.
    /// </remarks>
    public enum SequenceStatus
    {
        /// <summary>Switched off, so nothing will start it.</summary>
        Off,

        /// <summary>Live, and has never run.</summary>
        NeverRun,

        /// <summary>Live, has run before, and is waiting for its trigger again.</summary>
        Waiting,

        /// <summary>Part way through its steps.</summary>
        Running,

        /// <summary>Part way through, and held on a step that has not finished.</summary>
        RunningWaiting
    }
}
