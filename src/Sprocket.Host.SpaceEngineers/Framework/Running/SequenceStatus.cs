
namespace Sprocket.Host.SpaceEngineers.Framework.Running
{
    /// <summary>What a sequence is doing right now.</summary>
    public enum SequenceStatus
    {
        /// <summary>Switched off, so nothing will start it.</summary>
        Off,

        /// <summary>Live, and has never run.</summary>
        NeverRun,

        /// <summary>Live, has run before, and is waiting for its trigger again.</summary>
        Waiting,

        /// <summary>Live, and its last run was stopped from outside before it finished; see <see cref="SequenceRunner.StoppedBy" />.</summary>
        Stopped,

        /// <summary>Part way through its steps.</summary>
        Running,

        /// <summary>Part way through, and held on a step that has not finished.</summary>
        RunningWaiting
    }
}
