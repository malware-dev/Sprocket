using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences.Edits
{
    /// <summary>What an abort step says, and the level it shows at.</summary>
    public sealed class SetAbort : SequenceEdit
    {
        readonly int _index;
        readonly string _wasMessage;
        readonly string _wasSetting;
        readonly AbortLevel _wasLevel;
        readonly string _nowMessage;
        readonly string _nowSetting;
        readonly AbortLevel _nowLevel;

        /// <summary>
        /// The abort <paramref name="step" />, at <paramref name="index" />, made to say <paramref name="message" /> at
        /// <paramref name="level" />; a message replaces the setting it said, and none keeps it.
        /// </summary>
        public SetAbort(int index, Abort step, string message, AbortLevel level)
        {
            _index = index;
            _wasMessage = step.Message;
            _wasSetting = step.Setting;
            _wasLevel = step.Level;
            _nowMessage = message ?? "";
            _nowSetting = _nowMessage.Length > 0 ? "" : step.Setting;
            _nowLevel = level;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.Step;

        /// <inheritdoc />
        public override void Name(Sequence sequence, bool done, ChangeName name)
        {
            name.Thing = sequence.Steps[_index];
            name.Number = sequence.NumberOf(_index);
        }

        /// <inheritdoc />
        public override bool Changes => _wasMessage != _nowMessage || _wasSetting != _nowSetting || _wasLevel != _nowLevel;

        /// <inheritdoc />
        public override void Apply(Sequence sequence)
        {
            Put(sequence, _nowMessage, _nowSetting, _nowLevel);
        }

        /// <inheritdoc />
        public override void Undo(Sequence sequence)
        {
            Put(sequence, _wasMessage, _wasSetting, _wasLevel);
        }

        void Put(Sequence sequence, string message, string setting, AbortLevel level)
        {
            var abort = sequence.Steps[_index] as Abort;
            if (abort == null)
                return;

            abort.Message = message;
            abort.Setting = setting;
            abort.Level = level;
        }
    }
}
