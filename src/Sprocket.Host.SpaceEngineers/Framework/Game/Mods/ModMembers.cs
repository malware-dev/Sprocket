using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.ModApi;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Mods
{
    /// <summary>The members other mods have registered for their blocks through Sprocket's mod API, by block type.</summary>
    /// <remarks>One for the world, kept by its block kinds; a registration is checked as it arrives, and one that says too little is passed over.</remarks>
    public sealed class ModMembers
    {
        readonly Dictionary<string, List<Dictionary<string, object>>> _byType = new Dictionary<string, List<Dictionary<string, object>>>();
        readonly Dictionary<long, List<Action>> _listening = new Dictionary<long, List<Action>>();

        /// <summary>Raised with a block type whose members a registration has changed, so a description of that type made earlier is made again.</summary>
        public event Action<string> Registered;

        /// <summary>Takes <paramref name="said" />, a member as <see cref="ModMember.Said" /> words it, replacing one with the same id on the same type.</summary>
        /// <remarks>Called by another mod through the API, on the game's main thread.</remarks>
        public void Register(Dictionary<string, object> said)
        {
            if (said == null)
                return;

            var id = said.ContainsKey(ModMemberKeys.Id) ? said[ModMemberKeys.Id] as string : null;
            var type = said.ContainsKey(ModMemberKeys.BlockType) ? said[ModMemberKeys.BlockType] as string : null;
            var kind = said.ContainsKey(ModMemberKeys.Kind) ? said[ModMemberKeys.Kind] as string : null;
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(type) || string.IsNullOrEmpty(kind))
                return;

            List<Dictionary<string, object>> members;
            if (!_byType.TryGetValue(type, out members))
                _byType[type] = members = new List<Dictionary<string, object>>();

            members.RemoveAll(member => member[ModMemberKeys.Id] as string == id);
            members.Add(said);
            Registered?.Invoke(type);
        }

        /// <summary>The block types any member is registered for.</summary>
        public ICollection<string> Types => _byType.Keys;

        /// <summary>Whether any member is registered for <paramref name="block" />'s type and subtype.</summary>
        public bool Claims(IMyTerminalBlock block)
        {
            List<Dictionary<string, object>> members;
            if (!_byType.TryGetValue(block.BlockDefinition.TypeIdString, out members))
                return false;

            foreach (var member in members)
            {
                if (Covers(member, block))
                    return true;
            }

            return false;
        }

        /// <summary>Fills <paramref name="into" /> with the members registered for <paramref name="block" />'s type and subtype, in the order they were registered.</summary>
        public void For(IMyTerminalBlock block, List<Dictionary<string, object>> into)
        {
            into.Clear();
            List<Dictionary<string, object>> members;
            if (!_byType.TryGetValue(block.BlockDefinition.TypeIdString, out members))
                return;

            foreach (var member in members)
            {
                if (Covers(member, block))
                    into.Add(member);
            }
        }

        /// <summary>Has <paramref name="told" /> called whenever the mod says a member of <paramref name="block" /> may have changed, until the answer is disposed.</summary>
        public IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            List<Action> listeners;
            if (!_listening.TryGetValue(block.EntityId, out listeners))
                _listening[block.EntityId] = listeners = new List<Action>();

            listeners.Add(told);
            return new Listening(_listening, block.EntityId, told);
        }

        /// <summary>Tells everything listening to <paramref name="block" /> that its member <paramref name="id" /> may have changed.</summary>
        /// <remarks>Called by another mod through the API, on the game's main thread; a block nothing listens to costs one look-up.</remarks>
        public void Changed(IMyTerminalBlock block, string id)
        {
            List<Action> listeners;
            if (block == null || !_listening.TryGetValue(block.EntityId, out listeners))
                return;

            for (var i = 0; i < listeners.Count; i++)
                listeners[i]();
        }

        /// <summary>Whether <paramref name="member" /> is on <paramref name="block" />'s subtype: on every subtype where it names none.</summary>
        static bool Covers(Dictionary<string, object> member, IMyTerminalBlock block)
        {
            var subtypes = member.ContainsKey(ModMemberKeys.Subtypes) ? member[ModMemberKeys.Subtypes] as string[] : null;
            return subtypes == null || subtypes.Length == 0 || Array.IndexOf(subtypes, block.BlockDefinition.SubtypeName) >= 0;
        }
    }
}
