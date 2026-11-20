using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Mal.MdkModMixin.Network;
using Sprocket.ModSupport;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Mods
{
    /// <summary>
    /// Sprocket's end of its mod support: answers every mod that asks on the channel with the function it registers
    /// through, and announces itself once as it starts, for the mods that asked before it was listening.
    /// </summary>
    /// <remarks>Started with the session and stopped with it, on the server and on every client alike, since each describes its blocks.</remarks>
    public sealed class ModSupportHost
    {
        readonly ModMembers _members;
        readonly Func<IMyTerminalBlock, SprocketUnit> _raise;
        readonly Dictionary<string, object> _answer;

        /// <summary>The host taking registrations into <paramref name="members" />, raising moments on the construct <paramref name="unitOf" /> finds for a block, and looking at stopped tasks on <paramref name="rota" />.</summary>
        public ModSupportHost(ModMembers members, Func<IMyTerminalBlock, SprocketUnit> unitOf, Rota rota)
        {
            _members = members;
            members.Stops = new ModStopChecks(rota);
            _raise = unitOf;
            _answer = new Dictionary<string, object>
            {
                { SprocketWire.Register, (Func<Dictionary<string, object>, Dictionary<string, object>>)Register },
                { SprocketWire.Spoken, SprocketWire.Version }
            };
        }

        /// <summary>Starts answering on the channel, and announces Sprocket to the mods already listening.</summary>
        public void Start()
        {
            if (MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.RegisterMessageHandler(SprocketWire.Channel, Heard);
            Answer();
        }

        /// <summary>Stops answering.</summary>
        public void Stop()
        {
            MyAPIGateway.Utilities?.UnregisterMessageHandler(SprocketWire.Channel, Heard);
        }

        /// <summary>Answers a mod asking; anything else heard on the channel, Sprocket's own answer among it, is passed over.</summary>
        void Heard(object message)
        {
            if (message as string == SprocketWire.Asking)
                Answer();
        }

        /// <summary>Sends the answer; a mod's registration runs inside this call, so what it throws is caught and logged here.</summary>
        void Answer()
        {
            try
            {
                MyAPIGateway.Utilities.SendModMessage(SprocketWire.Channel, _answer);
            }
            catch (Exception e)
            {
                GameLog.Say($"A mod's registration with Sprocket failed: {e.Message}");
            }
        }

        /// <summary>Starts what waits on the moment <paramref name="key" /> a mod raised on <paramref name="block" />, on the server, where automation runs.</summary>
        /// <remarks>A block on no construct Sprocket runs costs one look-up.</remarks>
        void Raise(IMyTerminalBlock block, MemberKey key)
        {
            if (block == null || !NetworkChannel.Network.IsServer)
                return;

            _raise(block)?.Raised(block, key);
        }

        /// <summary>Takes the registration <paramref name="said" />, answering with the functions the mod tells Sprocket through, or why it was refused.</summary>
        Dictionary<string, object> Register(Dictionary<string, object> said)
        {
            var registration = new ModRegistration(said);

            // The wrapper checks the convention as a mod registers; a mod sending the registration otherwise is held to it here.
            try
            {
                SprocketIds.CheckNamespace(registration.Namespace);
            }
            catch (ArgumentException e)
            {
                GameLog.Say(e.Message);
                return new Dictionary<string, object> { { SprocketWire.Refused, e.Message } };
            }

            if (!HostNamespaces.Register(registration.Namespace, registration.Owner, registration.Title, registration.Author))
            {
                var holder = HostNamespaces.Find(registration.Namespace);
                var why = $"The namespace {registration.Namespace} is already registered by {holder?.Title} ({holder?.Author}); {registration.Title} ({registration.Author}) is not.";
                GameLog.Say(why);
                return new Dictionary<string, object> { { SprocketWire.Refused, why } };
            }

            _members.Add(registration);
            var owner = registration.Owner;
            var space = registration.Namespace;
            return new Dictionary<string, object>
            {
                { SprocketWire.Changed, (Action<IMyTerminalBlock, string>)((block, id) => _members.Changed(block)) },
                { SprocketWire.Raise, (Action<IMyTerminalBlock, string>)((block, id) => Raise(block, new MemberKey(space, id))) },
                { SprocketWire.Withdraw, (Action)(() =>
                    {
                        _members.Remove(owner);
                        HostNamespaces.Unregister(space, owner);
                    }) }
            };
        }
    }
}
