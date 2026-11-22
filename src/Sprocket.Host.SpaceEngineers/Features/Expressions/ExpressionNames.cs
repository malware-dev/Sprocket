using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using Sprocket.Host.SpaceEngineers.Framework.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    /// <summary>How the expression builder names a value in its chip: a small word for its kind, and what it is.</summary>
    public static class ExpressionNames
    {
        /// <summary>
        /// What <paramref name="atom" />, a fixed value, setting, state or reading of <paramref name="system" />, is called in
        /// its chip as <paramref name="editor" /> names it, and in <paramref name="kind" /> the small word saying what kind it is.
        /// </summary>
        public static string Of(SprocketEditor editor, AutomationSystem system, Expression atom, out string kind)
        {
            var texts = editor.Texts;
            var state = atom as StateOperand;
            if (state != null)
            {
                kind = texts.ExpressionKindState;
                return state.Name;
            }

            var reading = atom as BlockReading;
            if (reading != null)
            {
                kind = Combined(texts, reading.Combine);
                var value = EntityAction.Value(Vocabulary.ActionsOf(editor.World, editor.Blocks(system, reading.Targets)), reading.Key);
                var join = value != null && value.Join.Length > 0 ? value.Join : "of";
                return $"{value?.Named ?? reading.Key.Id} {join} {string.Join(", ", reading.Targets)}";
            }

            var operand = atom as Operand;
            if (operand != null && operand.Value.IsSetting)
            {
                kind = texts.ExpressionKindSetting;
                return operand.Value.Text;
            }

            kind = texts.ExpressionKindValue;
            return operand != null ? operand.Value.Said : atom.Written;
        }

        // The small word for a reading combined as combine says.
        static string Combined(Texts texts, Combination combine)
        {
            switch (combine)
            {
                case Combination.Sum: return texts.ExpressionKindSum;
                case Combination.Average: return texts.ExpressionKindAverage;
                case Combination.Min: return texts.ExpressionKindLowest;
                case Combination.Max: return texts.ExpressionKindHighest;
            }

            return texts.ExpressionKindReading;
        }
    }
}
