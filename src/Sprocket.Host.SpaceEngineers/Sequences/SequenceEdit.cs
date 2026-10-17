
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// One change to a sequence: its steps, its name, what it does.
    /// </summary>
    public abstract class SequenceEdit : Edit<Sequence>
    {
        /// <summary>Waits for the switch of the logic system the sequence is in, as making it did.</summary>
        public override bool Held(Sequence sequence, bool back)
        {
            return sequence.System != null && sequence.System.Enabled;
        }
    }
}
