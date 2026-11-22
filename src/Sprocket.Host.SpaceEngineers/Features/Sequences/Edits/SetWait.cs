using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences.Edits
{
    /// <summary>How long a wait step waits: a fixed length, a setting, or an expression.</summary>
    public sealed class SetWait : SequenceEdit
    {
        readonly int _index;
        readonly Duration _was;
        readonly string _wasSetting;
        readonly Expression _wasExpression;
        readonly Duration _now;
        readonly string _nowSetting;
        readonly Expression _nowExpression;

        /// <summary>
        /// The wait at <paramref name="index" /> made <paramref name="now" /> long, or as long as the setting <paramref name="nowSetting" />
        /// where it is not empty, from <paramref name="was" />, or the setting <paramref name="wasSetting" /> where it took one, or the expression
        /// <paramref name="wasExpression" /> where it took that, which a length or a setting takes the place of; or as long as <paramref name="nowExpression" /> works out to, where it is given.
        /// </summary>
        public SetWait(int index, Duration was, string wasSetting, Duration now, string nowSetting = "", Expression wasExpression = null,
            Expression nowExpression = null)
        {
            _nowExpression = nowExpression;
            _index = index;
            _was = was;
            _wasSetting = wasSetting;
            _wasExpression = wasExpression;
            _now = now;
            _nowSetting = nowSetting ?? "";
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.Wait;

        /// <inheritdoc />
        public override void Name(Sequence sequence, bool done, ChangeName name)
        {
            name.Thing = sequence.Steps[_index];
            name.Number = sequence.NumberOf(_index);
            name.Was = _wasExpression ?? (!string.IsNullOrEmpty(_wasSetting) ? (object)_wasSetting : _was);
            name.Now = _nowExpression ?? (!string.IsNullOrEmpty(_nowSetting) ? (object)_nowSetting : _now);
        }

        /// <inheritdoc />
        public override bool Changes => _was != _now || _wasSetting != _nowSetting || _wasExpression != _nowExpression;

        /// <inheritdoc />
        public override void Apply(Sequence sequence)
        {
            Put(sequence, _now, _nowSetting, _nowExpression);
        }

        /// <inheritdoc />
        public override void Undo(Sequence sequence)
        {
            Put(sequence, _was, _wasSetting, _wasExpression);
        }

        void Put(Sequence sequence, Duration length, string setting, Expression expression)
        {
            var wait = sequence.Steps[_index] as Wait;
            if (wait == null)
                return;

            wait.Length = length;
            wait.Setting = setting;
            wait.Expression = expression;
        }
    }
}
