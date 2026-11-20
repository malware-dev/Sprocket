using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>How one installation answers one slot, held so the answer can be put back once the slot is taken out.</summary>
    public sealed class SlotAnswer
    {
        readonly List<string> _blocks;
        readonly List<int> _facets;
        readonly List<ToolbarSlot> _positions;
        readonly string _note;
        readonly UnusedAttributes _unused;
        readonly bool _answered;
        readonly Literal _setting;

        SlotAnswer(SystemInstance instance, string slot)
        {
            Instance = instance;
            var blocks = instance.FilledWith(slot);
            var facets = instance.Facets(slot);
            var positions = instance.Positions(slot);
            _blocks = blocks == null ? null : new List<string>(blocks);
            _facets = facets == null ? null : new List<int>(facets);
            _positions = positions == null ? null : new List<ToolbarSlot>(positions);
            _note = instance.NoteOn(slot);
            _unused = instance.UnusedOn(slot);
            _answered = instance.AnswerTo(slot, out _setting);
        }

        /// <summary>The installation that answers the slot.</summary>
        public SystemInstance Instance { get; }

        /// <summary>How <paramref name="instance" /> answers <paramref name="slot" /> now, or null where it says nothing about it.</summary>
        public static SlotAnswer Of(SystemInstance instance, string slot)
        {
            var answer = new SlotAnswer(instance, slot);
            return answer._blocks == null && !answer._answered && answer._note.Length == 0 && answer._unused == null ? null : answer;
        }

        /// <summary>Has the installation answer <paramref name="slot" /> as it did when this was taken.</summary>
        public void Put(string slot)
        {
            if (_answered)
                Instance.Answer(slot, _setting);

            if (_positions != null)
                Instance.Fill(slot, _blocks ?? new List<string>(), _positions);
            else if (_facets != null)
                Instance.Fill(slot, _blocks ?? new List<string>(), _facets);
            else if (_blocks != null)
                Instance.Fill(slot, _blocks);

            Instance.Note(slot, _note);
            Instance.SetUnused(slot, _unused);
        }
    }
}
