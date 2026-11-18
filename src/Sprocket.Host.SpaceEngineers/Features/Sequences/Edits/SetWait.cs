using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences.Edits
{
    /// <summary>How long a wait step waits: a fixed length, in place of a setting it took.</summary>
    public sealed class SetWait : SequenceEdit
    {
        readonly int _index;
        readonly Duration _was;
        readonly string _wasSetting;
        readonly Duration _now;

        /// <summary>The wait at <paramref name="index" /> made <paramref name="now" /> long from <paramref name="was" />, or from the setting <paramref name="wasSetting" /> where it took one.</summary>
        public SetWait(int index, Duration was, string wasSetting, Duration now)
        {
            _index = index;
            _was = was;
            _wasSetting = wasSetting;
            _now = now;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.Wait;

        /// <inheritdoc />
        public override void Name(Sequence sequence, bool done, ChangeName name)
        {
            name.Thing = sequence.Steps[_index];
            name.Number = sequence.NumberOf(_index);
            name.Was = _was;
            name.Now = _now;
        }

        /// <inheritdoc />
        public override bool Changes => _was != _now || _wasSetting.Length > 0;

        /// <inheritdoc />
        public override void Apply(Sequence sequence)
        {
            Put(sequence, _now, "");
        }

        /// <inheritdoc />
        public override void Undo(Sequence sequence)
        {
            Put(sequence, _was, _wasSetting);
        }

        void Put(Sequence sequence, Duration length, string setting)
        {
            var wait = sequence.Steps[_index] as Wait;
            if (wait == null)
                return;

            wait.Length = length;
            wait.Setting = setting;
        }
    }
}
