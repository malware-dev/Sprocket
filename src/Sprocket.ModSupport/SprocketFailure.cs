using System;
using VRage.Utils;

namespace Sprocket.ModSupport
{
    /// <summary>One way a mod's awaitable action can end badly: an id programs know it by, and the message players read.</summary>
    /// <remarks>Best defined once, as a <c>static readonly</c> field, so every action meeting it fails the same way.</remarks>
    public struct SprocketFailure
    {
        readonly Func<string> _message;

        /// <summary>The failure <paramref name="id" />, read by players as <paramref name="message" />.</summary>
        /// <remarks>Throws where the id breaks the naming convention. The id takes the mod's namespace and is never changed once released.</remarks>
        public SprocketFailure(string id, string message)
        {
            SprocketIds.Check(id, "Failure id");
            Id = id;
            _message = SprocketText.Of(message);
        }

        /// <summary>The failure <paramref name="id" />, read by players as the text <paramref name="message" /> names in the mod's own texts.</summary>
        public SprocketFailure(string id, MyStringId message)
        {
            SprocketIds.Check(id, "Failure id");
            Id = id;
            _message = SprocketText.Of(message);
        }

        /// <summary>The id programs know the failure by, in the mod's namespace.</summary>
        public string Id { get; }

        /// <summary>The message players read, in their language where the mod gives it one.</summary>
        public string Message => _message?.Invoke() ?? "";
    }
}
