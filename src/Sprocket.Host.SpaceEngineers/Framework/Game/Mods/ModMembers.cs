using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Mods
{
    /// <summary>The registrations of every mod supporting Sprocket in the world, the members they give blocks, and who listens to those blocks.</summary>
    /// <remarks>One for the world, held by its block kinds; registrations arrive and are withdrawn on the game's main thread.</remarks>
    public sealed class ModMembers
    {
        readonly List<ModRegistration> _registrations = new List<ModRegistration>();
        readonly Dictionary<long, List<Action>> _listening = new Dictionary<long, List<Action>>();
        readonly List<Dictionary<string, object>> _found = new List<Dictionary<string, object>>();

        /// <summary>Raised when a registration arrives or is withdrawn, so every block is described again.</summary>
        public event Action Registered;

        /// <summary>Where a mod's task Sprocket stopped is looked at again a second later, or null where nothing looks.</summary>
        public ModStopChecks Stops { get; set; }

        /// <summary>The registrations standing now.</summary>
        public IList<ModRegistration> Registrations => _registrations;

        /// <summary>Takes <paramref name="registration" />, replacing one by the same owner.</summary>
        public void Add(ModRegistration registration)
        {
            _registrations.RemoveAll(had => had.Owner == registration.Owner);
            _registrations.Add(registration);
            Registered?.Invoke();
        }

        /// <summary>Withdraws the registration made by <paramref name="owner" />.</summary>
        public void Remove(string owner)
        {
            if (_registrations.RemoveAll(had => had.Owner == owner) > 0)
                Registered?.Invoke();
        }

        /// <summary>Whether any mod gives <paramref name="block" /> members.</summary>
        public bool Claims(IMyTerminalBlock block)
        {
            foreach (var registration in _registrations)
            {
                if (registration.Claims(block))
                    return true;
            }

            return false;
        }

        /// <summary>Fills <paramref name="into" /> with each mod's members of <paramref name="block" />, paired with the registration giving them.</summary>
        public void For(IMyTerminalBlock block, List<KeyValuePair<ModRegistration, Dictionary<string, object>>> into)
        {
            into.Clear();
            foreach (var registration in _registrations)
            {
                _found.Clear();
                registration.For(block, _found);
                foreach (var member in _found)
                    into.Add(new KeyValuePair<ModRegistration, Dictionary<string, object>>(registration, member));
            }
        }

        /// <summary>The host type of the full name <paramref name="fullName" />: Sprocket's own or a registered mod's; null where none is.</summary>
        public ChoiceType FindType(string fullName, out ModRegistration from)
        {
            from = null;
            if (string.IsNullOrEmpty(fullName))
                return null;

            var dot = fullName.LastIndexOf('.');
            if (dot <= 0)
                return null;

            var space = fullName.Substring(0, dot);
            var id = fullName.Substring(dot + 1);
            foreach (var registration in _registrations)
            {
                if (registration.Namespace != space)
                    continue;

                foreach (var type in registration.Types)
                {
                    if (type.Id == id)
                    {
                        from = registration;
                        return type;
                    }
                }
            }

            return space == BlockKinds.Own ? ChoiceTypes.Named(space, id) : null;
        }

        /// <summary>Has <paramref name="told" /> called whenever a mod says a member of <paramref name="block" /> may have changed, until the answer is disposed.</summary>
        public IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            List<Action> listeners;
            if (!_listening.TryGetValue(block.EntityId, out listeners))
                _listening[block.EntityId] = listeners = new List<Action>();

            listeners.Add(told);
            return new Listening(_listening, block.EntityId, told);
        }

        /// <summary>Tells everything listening to <paramref name="block" /> that one of its mod members may have changed.</summary>
        /// <remarks>Called by a mod, on the game's main thread; a block nothing listens to costs one look-up.</remarks>
        public void Changed(IMyTerminalBlock block)
        {
            List<Action> listeners;
            if (block == null || !_listening.TryGetValue(block.EntityId, out listeners))
                return;

            // Last first: a listener told may stop listening, which takes it out of the list.
            for (var i = listeners.Count - 1; i >= 0; i--)
                listeners[i]();
        }
    }
}
