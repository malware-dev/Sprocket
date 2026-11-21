using Sprocket.Host.SpaceEngineers.Framework.Model;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Parts
{
    /// <summary>A length of time, in the unit the program writes it in.</summary>
    public sealed class DurationSlot : SentenceSlot
    {
        /// <summary>A part saying <paramref name="length" />, pressed by <paramref name="opens" />.</summary>
        public DurationSlot(Duration length, ICommand opens) : this(length, "", opens)
        {
        }

        /// <summary>A part saying the setting <paramref name="setting" />, or <paramref name="length" /> where it is empty, pressed by <paramref name="opens" />.</summary>
        public DurationSlot(Duration length, string setting, ICommand opens) : this(length, setting, "", opens)
        {
        }

        /// <summary>A part saying the expression written <paramref name="expression" />, or the setting <paramref name="setting" /> where that is empty, or <paramref name="length" /> where both are, pressed by <paramref name="opens" />.</summary>
        public DurationSlot(Duration length, string setting, string expression, ICommand opens) : base(opens)
        {
            Length = length;
            Setting = setting ?? "";
            Expression = expression ?? "";
        }

        /// <summary>The length it says.</summary>
        public Duration Length { get; private set; }

        /// <summary>The setting whose length it says, or empty where it says <see cref="Length" /> itself.</summary>
        public string Setting { get; private set; }

        /// <summary>The expression whose length it says, as the program writes it, or empty where it says a setting or <see cref="Length" />.</summary>
        public string Expression { get; private set; }

        /// <summary>False, since a duration always has a value.</summary>
        public override bool IsEmpty => false;
    }
}
