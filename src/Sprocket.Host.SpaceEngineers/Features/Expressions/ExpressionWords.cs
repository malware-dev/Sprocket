using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    /// <summary>The words the expression builder shows: its calculations' symbols and tips, its kinds of value, and the kinds a value can be.</summary>
    public static class ExpressionWords
    {
        /// <summary>The language's name for a value of <paramref name="kind" />, or null where no setting type matches it.</summary>
        public static string TypeWord(ValueKind kind)
        {
            switch (kind)
            {
                case ValueKind.Flag: return "flag";
                case ValueKind.Integer: return "integer";
                case ValueKind.Real: return "real";
                case ValueKind.Text: return "text";
                case ValueKind.Duration: return "duration";
            }

            return null;
        }

        /// <summary>
        /// How a player reads the type <paramref name="type" />, in <paramref name="texts" />: a type the language names in its
        /// words, a state type by its own name.
        /// </summary>
        public static string KindSaid(Texts texts, string type)
        {
            switch (type)
            {
                case "flag": return texts.StateFlag;
                case "integer": return texts.SettingKindInteger;
                case "real": return texts.SettingKindReal;
                case "duration": return texts.SettingKindDuration;
                case "text": return texts.SettingKindText;
            }

            return type;
        }

        /// <summary>What a box of <paramref name="kind" /> and its piece in the tray are written with.</summary>
        public static string Symbol(Texts texts, JoinKind kind)
        {
            switch (kind)
            {
                case JoinKind.Add: return "+";
                case JoinKind.Subtract: return "-";
                case JoinKind.Multiply: return "×";
                case JoinKind.Divide: return "÷";
                case JoinKind.Negate: return "-x";
                case JoinKind.Lowest: return texts.ExpressionLowest;
                case JoinKind.Highest: return texts.ExpressionHighest;
                case JoinKind.Rounded: return texts.ExpressionRounded;
                default: return texts.ExpressionWithoutSign;
            }
        }

        /// <summary>The tip of the tray's piece for the calculation <paramref name="kind" />.</summary>
        public static string About(Texts texts, JoinKind kind)
        {
            var codex = texts.Codex;
            switch (kind)
            {
                case JoinKind.Add: return codex.ExpressionAddAbout;
                case JoinKind.Subtract: return codex.ExpressionSubtractAbout;
                case JoinKind.Multiply: return codex.ExpressionMultiplyAbout;
                case JoinKind.Divide: return codex.ExpressionDivideAbout;
                case JoinKind.Negate: return codex.ExpressionNegateAbout;
                case JoinKind.Lowest: return codex.ExpressionLowestAbout;
                case JoinKind.Highest: return codex.ExpressionHighestAbout;
                case JoinKind.Rounded: return codex.ExpressionRoundedAbout;
                default: return codex.ExpressionWithoutSignAbout;
            }
        }

        /// <summary>The words of the tray's piece for the kind of value <paramref name="source" />.</summary>
        public static string Said(Texts texts, ValueSource source)
        {
            switch (source)
            {
                case ValueSource.Reading: return texts.ExpressionReading;
                case ValueSource.Setting: return texts.ExpressionSetting;
                default: return texts.ExpressionState;
            }
        }

        /// <summary>The words of the tray's piece for a value of <paramref name="kind" /> typed in.</summary>
        public static string Said(Texts texts, KindClass kind)
        {
            switch (kind)
            {
                case KindClass.Duration: return texts.ExpressionTypedLength;
                case KindClass.Text: return texts.ExpressionTypedText;
                case KindClass.Flag: return texts.ExpressionTypedFlag;
                default: return texts.ExpressionTypedNumber;
            }
        }

        /// <summary>The tip of the tray's piece for the kind of value <paramref name="source" />.</summary>
        public static string About(Texts texts, ValueSource source)
        {
            var codex = texts.Codex;
            switch (source)
            {
                case ValueSource.Reading: return codex.ExpressionReadingAbout;
                case ValueSource.Setting: return codex.ExpressionSettingAbout;
                default: return codex.ExpressionStateAbout;
            }
        }
    }
}
