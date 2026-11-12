using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Sprocket.ModApi
{
    /// <summary>
    /// A mod's way to Sprocket: it finds Sprocket in the world, whichever of the two loads first, and hands it the mod's
    /// members, which Sprocket then offers players beside its own.
    /// </summary>
    /// <remarks>
    /// Made and loaded once per session, as the mod's session component loads its data, and unloaded with it. Members are
    /// registered in <c>ready</c>, which runs once Sprocket has answered; a world without Sprocket never calls it.
    /// </remarks>
    /// <example>
    /// <code>
    /// _sprocket = new SprocketApi(api => api.Register(ModMember.Value("hover.ride_height", "MyObjectBuilder_FunctionalBlock")
    ///     .Number(block => Height(block), (block, metres) => SetHeight(block, metres)).Measured("m", 1, 50)
    ///     .Named(() => Texts.RideHeight, () => Texts.RideHeightAbout).InGroup(() => Texts.Hover)));
    /// _sprocket.Load();
    /// </code>
    /// </example>
    public sealed class SprocketApi
    {
        /// <summary>The mod message channel Sprocket and every mod using its API talk on.</summary>
        public const long Channel = 38072347710001;

        /// <summary>The version of this API the mod was built with; Sprocket answers with the newest it speaks.</summary>
        public const int Version = 1;

        /// <summary>What a mod sends to ask Sprocket for its API.</summary>
        public const string Asking = "sprocket.api.ask";

        /// <summary>The key of Sprocket's register function in its answer: an Action&lt;Dictionary&lt;string, object&gt;&gt;.</summary>
        public const string RegisterKey = "register";

        /// <summary>The key of Sprocket's changed function in its answer: an Action&lt;IMyTerminalBlock, string&gt;.</summary>
        public const string ChangedKey = "changed";

        /// <summary>The key of the version Sprocket speaks in its answer: an int.</summary>
        public const string VersionKey = "version";

        readonly Action<SprocketApi> _ready;
        Action<Dictionary<string, object>> _register;
        Action<IMyTerminalBlock, string> _changed;

        /// <summary>A way to Sprocket that calls <paramref name="ready" /> once Sprocket has answered.</summary>
        public SprocketApi(Action<SprocketApi> ready)
        {
            _ready = ready;
        }

        /// <summary>Whether Sprocket has answered, so members can be registered.</summary>
        public bool IsReady => _register != null;

        /// <summary>The version of the API the Sprocket in this world speaks, or 0 before it has answered.</summary>
        public int SprocketVersion { get; private set; }

        /// <summary>Starts listening for Sprocket and asks for it, in case it loaded first.</summary>
        public void Load()
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(Channel, Heard);
            MyAPIGateway.Utilities.SendModMessage(Channel, Asking);
        }

        /// <summary>Stops listening, and forgets Sprocket.</summary>
        public void Unload()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(Channel, Heard);
            _register = null;
            _changed = null;
        }

        /// <summary>Hands <paramref name="member" /> to Sprocket; a member registered again with the same id replaces it.</summary>
        public void Register(ModMember member)
        {
            if (_register != null && member != null)
                _register(member.Said);
        }

        /// <summary>Tells Sprocket that the member <paramref name="id" /> of <paramref name="block" /> may have changed, for a member registered as announced.</summary>
        /// <remarks>Cheap where Sprocket does not watch the block; called on the game's main thread.</remarks>
        public void Changed(IMyTerminalBlock block, string id)
        {
            _changed?.Invoke(block, id);
        }

        /// <summary>Takes Sprocket's answer on the channel; anything else heard there, such as another mod asking, is passed over.</summary>
        void Heard(object message)
        {
            var answer = message as Dictionary<string, object>;
            if (answer == null || !answer.ContainsKey(RegisterKey))
                return;

            _register = answer[RegisterKey] as Action<Dictionary<string, object>>;
            _changed = answer.ContainsKey(ChangedKey) ? answer[ChangedKey] as Action<IMyTerminalBlock, string> : null;
            SprocketVersion = answer.ContainsKey(VersionKey) && answer[VersionKey] is int ? (int)answer[VersionKey] : 0;
            if (_register != null)
                _ready?.Invoke(this);
        }
    }
}
