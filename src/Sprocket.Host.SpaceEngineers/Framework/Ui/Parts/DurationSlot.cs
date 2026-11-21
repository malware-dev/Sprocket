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
        public DurationSlot(Duration length, string setting, ICommand opens) : base(opens)
        {
            Length = length;
            Setting = setting ?? "";
        }

        /// <summary>The length it says.</summary>
        public Duration Length { get; private set; }

        /// <summary>The setting whose length it says, or empty where it says <see cref="Length" /> itself.</summary>
        public string Setting { get; private set; }

        /// <summary>False, since a duration always has a value.</summary>
        public override bool IsEmpty => false;
    }
}
