using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A slot given another block in one installation, in place of the block it had.</summary>
    /// <remarks>
    /// Carried by the edit that traded one block for another in a step, so the slot keeps its name, its description and what
    /// every other installation filled it with, and taking the edit back gives the slot its block back.
    /// </remarks>
    public sealed class Swap
    {
        readonly SystemInstance _instance;
        readonly List<string> _had;
        readonly List<string> _now;

        /// <summary>Slot <paramref name="slot" /> of <paramref name="instance" />, to be filled with <paramref name="now" /> in place of what it holds.</summary>
        public Swap(SystemInstance instance, string slot, IList<string> now)
        {
            _instance = instance;
            Slot = slot;
            _had = new List<string>(instance.FilledWith(slot) ?? new List<string>());
            _now = new List<string>(now);
        }

        /// <summary>The slot that keeps its place.</summary>
        public string Slot { get; private set; }

        /// <summary>Fills the slot with the new block.</summary>
        public void Apply()
        {
            _instance.Fill(Slot, _now);
        }

        /// <summary>Gives the slot back what it held before <see cref="Apply" />.</summary>
        public void Undo()
        {
            if (_had.Count > 0)
                _instance.Fill(Slot, _had);
            else
                _instance.Clear(Slot);
        }
    }
}
