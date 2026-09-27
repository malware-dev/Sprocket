
namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>What a trigger does, as chosen in the column beside what starts it.</summary>
    enum Outcome
    {
        /// <summary>Nothing chosen yet.</summary>
        None,

        /// <summary>Run a sequence made for it, and open that sequence.</summary>
        New,

        /// <summary>Run, or stop, a sequence the system already has.</summary>
        Existing,

        /// <summary>Do one thing to some blocks, chosen next.</summary>
        One
    }
}
