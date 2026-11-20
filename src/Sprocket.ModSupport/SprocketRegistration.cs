using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Utils;

namespace Sprocket.ModSupport
{
    /// <summary>
    /// One mod's registration with Sprocket: it finds Sprocket in the world, whichever loads first, has the mod describe its
    /// blocks once Sprocket answers, and hands that over. Disposing it withdraws the registration.
    /// </summary>
    /// <remarks>Made by <see cref="SprocketModSupport.Register" /> in the mod's <c>LoadData</c> and disposed in its <c>UnloadData</c>.</remarks>
    public sealed class SprocketRegistration : IDisposable, ISprocketRegistry
    {
        readonly SprocketMod _mod;
        readonly Action<SprocketModBuilder> _build;
        readonly string _owner = Guid.NewGuid().ToString();
        readonly List<SprocketBlock> _scopes = new List<SprocketBlock>();
        readonly List<SprocketMember> _members = new List<SprocketMember>();
        readonly List<SprocketType> _types = new List<SprocketType>();
        readonly HashSet<string> _ids = new HashSet<string>();
        Action<IMyTerminalBlock, string> _changed;
        Action<IMyTerminalBlock, string> _raise;
        Action _withdraw;
        bool _listening;

        /// <summary>The registration of <paramref name="mod" />, made with the wrapper's key <paramref name="wiring" />, whose blocks <paramref name="build" /> describes once Sprocket answers.</summary>
        // The key parameter is the gate itself: a caller has to hold one to make this, and nothing is read from it.
        // ReSharper disable once UnusedParameter.Local
        public SprocketRegistration(ISprocketWiring wiring, SprocketMod mod, Action<SprocketModBuilder> build)
        {
            SprocketIds.CheckNamespace(mod.Namespace);
            if (string.IsNullOrEmpty(mod.Name))
                throw new ArgumentException("SprocketMod.Name is missing. Give your mod's name as players should read it.");
            if (string.IsNullOrEmpty(mod.Author))
                throw new ArgumentException("SprocketMod.Author is missing. Give your name as players should read it.");
            if (build == null)
                throw new ArgumentNullException(nameof(build));

            _mod = mod;
            _build = build;
        }

        /// <inheritdoc />
        public string Namespace => _mod.Namespace;

        /// <summary>Whether Sprocket took the registration.</summary>
        public bool IsRegistered => _withdraw != null;

        /// <summary>Why Sprocket refused the registration, or null where it has not.</summary>
        public string Refusal { get; private set; }

        /// <inheritdoc />
        void ISprocketRegistry.Listen()
        {
            if (_listening)
                return;

            _listening = true;
            MyAPIGateway.Utilities.RegisterMessageHandler(SprocketWire.Channel, Heard);
            MyAPIGateway.Utilities.SendModMessage(SprocketWire.Channel, SprocketWire.Asking);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_listening)
                MyAPIGateway.Utilities?.UnregisterMessageHandler(SprocketWire.Channel, Heard);
            _listening = false;
            _withdraw?.Invoke();
            _withdraw = null;
            _changed = null;
            _raise = null;
        }

        /// <inheritdoc />
        SprocketBlock ISprocketRegistry.Scope(int narrows)
        {
            var scope = new SprocketBlock(this, _scopes.Count, narrows);
            _scopes.Add(scope);
            return scope;
        }

        /// <inheritdoc />
        SprocketMember ISprocketRegistry.Member(int scope, string kind, string id, bool made)
        {
            if (made)
            {
                SprocketIds.Check(id, "Member id");
                if (!_ids.Add(id))
                    throw new ArgumentException($"Member id \"{id}\" is used twice in {Namespace}. Each member needs an id of its own.");
            }
            else if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Whitelist(...) and Override(...) need the id of the member they name.");

            var member = new SprocketMember(this, scope, kind, id);
            _members.Add(member);
            return member;
        }

        /// <inheritdoc />
        T ISprocketRegistry.Define<T>(T type)
        {
            foreach (var had in _types)
            {
                if (had.Id == type.Id)
                    throw new ArgumentException($"Type id \"{type.Id}\" is defined twice in {Namespace}.");
            }

            _types.Add(type);
            return type;
        }

        /// <inheritdoc />
        void ISprocketRegistry.Changed(IMyTerminalBlock block, string id)
        {
            _changed?.Invoke(block, id);
        }

        /// <inheritdoc />
        void ISprocketRegistry.Raise(IMyTerminalBlock block, string id)
        {
            _raise?.Invoke(block, id);
        }

        /// <summary>Takes Sprocket's answer on the channel and registers with it, once; anything else heard there is passed over.</summary>
        void Heard(object message)
        {
            var answer = message as Dictionary<string, object>;
            if (IsRegistered || answer == null || !answer.ContainsKey(SprocketWire.Register))
                return;

            var register = answer[SprocketWire.Register] as Func<Dictionary<string, object>, Dictionary<string, object>>;
            if (register == null)
                return;

            _scopes.Clear();
            _members.Clear();
            _types.Clear();
            _ids.Clear();

            // This runs inside the game's mod message to every mod listening, which stops at a handler that throws: a
            // registration that fails is refused here, so the mods after it still hear Sprocket.
            Dictionary<string, object> said;
            try
            {
                _build(new SprocketModBuilder(this));
                said = Said();
            }
            catch (Exception e)
            {
                Refusal = e.Message;
                MyLog.Default?.WriteLineAndConsole($"[Sprocket] {_mod.Name}'s registration with Sprocket failed: {e.Message}");
                return;
            }

            var reply = register(said);
            object refused = null;
            if (reply == null || reply.TryGetValue(SprocketWire.Refused, out refused))
            {
                Refusal = refused as string ?? "Sprocket gave no answer.";
                return;
            }

            _changed = Part<Action<IMyTerminalBlock, string>>(reply, SprocketWire.Changed);
            _raise = Part<Action<IMyTerminalBlock, string>>(reply, SprocketWire.Raise);
            _withdraw = Part<Action>(reply, SprocketWire.Withdraw) ?? delegate { };
        }

        /// <summary>The registration as Sprocket reads it.</summary>
        Dictionary<string, object> Said()
        {
            var scopes = new Dictionary<string, object>[_scopes.Count];
            for (var i = 0; i < scopes.Length; i++)
                scopes[i] = ((ISprocketWired)_scopes[i]).Wire();

            var members = new Dictionary<string, object>[_members.Count];
            for (var i = 0; i < members.Length; i++)
                members[i] = ((ISprocketWired)_members[i]).Wire();

            var types = new List<Dictionary<string, object>>();
            foreach (var type in _types)
            {
                if (type.IsDefined)
                    types.Add(((ISprocketWired)type).Wire());
            }

            return new Dictionary<string, object>
            {
                { SprocketWire.Spoken, SprocketWire.Version },
                { SprocketWire.Namespace, _mod.Namespace },
                { SprocketWire.Name, _mod.Name },
                { SprocketWire.Author, _mod.Author },
                { SprocketWire.Owner, _owner },
                { SprocketWire.Scopes, scopes },
                { SprocketWire.Members, members },
                { SprocketWire.Types, types.ToArray() }
            };
        }

        static T Part<T>(Dictionary<string, object> said, string key) where T : class
        {
            object part;
            return said.TryGetValue(key, out part) ? part as T : null;
        }
    }
}
