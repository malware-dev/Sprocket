using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.ModApi;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Mods
{
    /// <summary>
    /// Sprocket's end of its mod API: answers every mod that asks on the API's channel with the functions it registers its
    /// members through, and announces itself once as it starts, for the mods that asked before it was listening.
    /// </summary>
    /// <remarks>Started with the session and stopped with it, on the server and on every client alike, since each describes its blocks.</remarks>
    public sealed class ModApiHost
    {
        readonly Dictionary<string, object> _answer;

        /// <summary>The host handing mods <paramref name="members" /> to register into.</summary>
        public ModApiHost(ModMembers members)
        {
            _answer = new Dictionary<string, object>
            {
                { SprocketApi.RegisterKey, (Action<Dictionary<string, object>>)members.Register },
                { SprocketApi.ChangedKey, (Action<IMyTerminalBlock, string>)members.Changed },
                { SprocketApi.VersionKey, SprocketApi.Version }
            };
        }

        /// <summary>Starts answering on the channel, and announces Sprocket to the mods already listening.</summary>
        public void Start()
        {
            if (MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.RegisterMessageHandler(SprocketApi.Channel, Heard);
            MyAPIGateway.Utilities.SendModMessage(SprocketApi.Channel, _answer);
        }

        /// <summary>Stops answering.</summary>
        public void Stop()
        {
            MyAPIGateway.Utilities?.UnregisterMessageHandler(SprocketApi.Channel, Heard);
        }

        /// <summary>Answers a mod asking for the API; anything else heard on the channel, Sprocket's own answer among it, is passed over.</summary>
        void Heard(object message)
        {
            if (message as string == SprocketApi.Asking)
                MyAPIGateway.Utilities.SendModMessage(SprocketApi.Channel, _answer);
        }
    }
}
