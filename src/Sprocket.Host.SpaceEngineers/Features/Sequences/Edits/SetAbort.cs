using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences.Edits
{
    /// <summary>What an abort step says, and the level it shows at.</summary>
    public sealed class SetAbort : SequenceEdit
    {
        readonly int _index;
        readonly string _wasMessage;
        readonly string _wasSetting;
        readonly Expression _wasExpression;
        readonly Expression _nowExpression;
        readonly AbortLevel _wasLevel;
        readonly string _nowMessage;
        readonly string _nowSetting;
        readonly AbortLevel _nowLevel;

        /// <summary>
        /// The abort <paramref name="step" />, at <paramref name="index" />, made to say <paramref name="message" /> at
        /// <paramref name="level" />; a message replaces the setting it said, and none keeps it.
        /// </summary>
        public SetAbort(int index, Abort step, string message, AbortLevel level)
            : this(index, step, message, string.IsNullOrEmpty(message) ? step.Setting : "", level)
        {
        }

        /// <summary>
        /// The abort <paramref name="step" />, at <paramref name="index" />, made to say the text of the setting
        /// <paramref name="setting" /> where it is not empty, or <paramref name="message" /> otherwise, at <paramref name="level" />.
        /// </summary>
        public SetAbort(int index, Abort step, string message, string setting, AbortLevel level)
            : this(index, step, message, setting, null, level)
        {
        }

        /// <summary>
        /// The abort <paramref name="step" />, at <paramref name="index" />, made to say what <paramref name="expression" /> works
        /// out to where it is given, or else the text of the setting <paramref name="setting" /> where it is not empty, or
        /// <paramref name="message" />, at <paramref name="level" />.
        /// </summary>
        public SetAbort(int index, Abort step, string message, string setting, Expression expression, AbortLevel level)
        {
            _index = index;
            _wasMessage = step.Message;
            _wasSetting = step.Setting;
            _wasExpression = step.Expression;
            _wasLevel = step.Level;
            _nowSetting = expression != null ? "" : setting ?? "";
            _nowMessage = expression != null || _nowSetting.Length > 0 ? "" : message ?? "";
            _nowLevel = level;

            // An expression stays where the panel was closed with no words of its own and no setting.
            _nowExpression = expression ?? (_nowSetting.Length == 0 && _nowMessage.Length == 0 ? step.Expression : null);
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
        public override bool Changes => _wasMessage != _nowMessage || _wasSetting != _nowSetting || _wasLevel != _nowLevel || _wasExpression != _nowExpression;

        /// <inheritdoc />
        public override void Apply(Sequence sequence)
        {
            Put(sequence, _nowMessage, _nowSetting, _nowExpression, _nowLevel);
        }

        /// <inheritdoc />
        public override void Undo(Sequence sequence)
        {
            Put(sequence, _wasMessage, _wasSetting, _wasExpression, _wasLevel);
        }

        void Put(Sequence sequence, string message, string setting, Expression expression, AbortLevel level)
        {
            var abort = sequence.Steps[_index] as Abort;
            if (abort == null)
                return;

            abort.Message = message;
            abort.Setting = setting;
            abort.Expression = expression;
            abort.Level = level;
        }
    }
}
