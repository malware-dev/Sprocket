using Sprocket.Host.SpaceEngineers.Framework.Game.Members;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>A step that ends the sequence it is in, at once, saying why where it has a message.</summary>
    public sealed class Abort : Step
    {
        /// <summary>The reason the sequence shows, as the player wrote it, or empty for none; where <see cref="Setting" /> is empty.</summary>
        public string Message { get; set; } = "";

        /// <summary>The setting whose text is the reason, each installation's own, or empty where the reason is <see cref="Message" />.</summary>
        public string Setting { get; set; } = "";

        /// <summary>How loudly the sequence says it ended.</summary>
        public AbortLevel Level { get; set; }

        /// <summary>The reason in <paramref name="through" />: <see cref="Message" />, or the text its setting has there; empty where that setting has no text.</summary>
        /// <remarks>Asked as the step is reached, never on a tick.</remarks>
        public string MessageIn(SystemInstance through)
        {
            if (Setting.Length == 0)
                return Message;

            Literal value;
            return through != null && through.Gives(Setting, out value) && value.Kind == ValueKind.Text ? value.Text ?? "" : "";
        }
    }
}
