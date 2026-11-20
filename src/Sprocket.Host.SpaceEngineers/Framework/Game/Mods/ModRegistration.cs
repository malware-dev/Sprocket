using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.ModSupport;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Mods
{
    /// <summary>What one mod registered through its mod support: who it is, its block scopes, its members and its types.</summary>
    /// <remarks>Read once as the mod registers; a mod's texts are asked again each time they are shown.</remarks>
    public sealed class ModRegistration
    {
        readonly List<ModScope> _scopes = new List<ModScope>();
        readonly List<KeyValuePair<ModScope, Dictionary<string, object>>> _members = new List<KeyValuePair<ModScope, Dictionary<string, object>>>();
        readonly Dictionary<string, Func<string>[]> _wordNames = new Dictionary<string, Func<string>[]>();

        /// <summary>The registration <paramref name="said" /> describes.</summary>
        public ModRegistration(Dictionary<string, object> said)
        {
            Namespace = ModWire.Part<string>(said, SprocketWire.Namespace) ?? "";
            Title = ModWire.Part<string>(said, SprocketWire.Name) ?? Namespace;
            Author = ModWire.Part<string>(said, SprocketWire.Author) ?? "";
            Owner = ModWire.Part<string>(said, SprocketWire.Owner) ?? Namespace;

            foreach (var scope in ModWire.Part<Dictionary<string, object>[]>(said, SprocketWire.Scopes) ?? new Dictionary<string, object>[0])
            {
                var narrows = ModWire.Part<int>(scope, SprocketWire.Narrows);
                var wider = narrows >= 0 && narrows < _scopes.Count ? _scopes[narrows] : null;
                _scopes.Add(new ModScope(scope, wider));
            }

            foreach (var member in ModWire.Part<Dictionary<string, object>[]>(said, SprocketWire.Members) ?? new Dictionary<string, object>[0])
            {
                var at = ModWire.Part<int>(member, SprocketWire.Scope);
                if (at >= 0 && at < _scopes.Count && !string.IsNullOrEmpty(ModWire.Part<string>(member, SprocketWire.Id)))
                    _members.Add(new KeyValuePair<ModScope, Dictionary<string, object>>(_scopes[at], member));
            }

            foreach (var type in ModWire.Part<Dictionary<string, object>[]>(said, SprocketWire.Types) ?? new Dictionary<string, object>[0])
            {
                var id = ModWire.Part<string>(type, SprocketWire.Id);
                var words = ModWire.Part<string[]>(type, SprocketWire.Words);
                if (string.IsNullOrEmpty(id) || words == null || words.Length == 0)
                    continue;

                Types.Add(new ChoiceType(Namespace, id, words));
                _wordNames[id] = ModWire.Part<Func<string>[]>(type, SprocketWire.WordNames) ?? new Func<string>[0];
            }
        }

        /// <summary>The mod's namespace.</summary>
        public string Namespace { get; }

        /// <summary>The mod's name as players read it.</summary>
        public string Title { get; }

        /// <summary>The mod's author as players read it.</summary>
        public string Author { get; }

        /// <summary>Who registered, unique to this registration.</summary>
        public string Owner { get; }

        /// <summary>The host types the mod defines.</summary>
        public List<ChoiceType> Types { get; } = new List<ChoiceType>();

        /// <summary>The names players read for the words of the mod's <paramref name="type" />, in its words' order; the words themselves where it names them otherwise.</summary>
        public string[] WordNames(ChoiceType type)
        {
            Func<string>[] names;
            var words = new string[type.Choices.Length];
            _wordNames.TryGetValue(type.Id, out names);
            for (var i = 0; i < words.Length; i++)
            {
                var said = names != null && i < names.Length ? ModWire.Said(names[i]) : "";
                words[i] = said.Length > 0 ? said : type.Choices[i];
            }

            return words;
        }

        /// <summary>Whether any of the mod's scopes takes <paramref name="block" />.</summary>
        public bool Claims(IMyTerminalBlock block)
        {
            foreach (var scope in _scopes)
            {
                if (scope.Claims(block))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Fills <paramref name="into" /> with the members <paramref name="block" /> has from this mod, each with what the
        /// scopes it is in say: its scope's group where it names none, and the details overridden by narrower scopes.
        /// </summary>
        public void For(IMyTerminalBlock block, List<Dictionary<string, object>> into)
        {
            var depth = -1;
            foreach (var member in _members)
            {
                if (ModWire.Part<string>(member.Value, SprocketWire.Kind) != SprocketWire.KindOverride && member.Key.Claims(block))
                    into.Add(Grouped(member.Key, member.Value));
            }

            // Overrides apply widest first, so the narrowest scope's details win.
            while (true)
            {
                var next = int.MaxValue;
                foreach (var member in _members)
                {
                    var at = member.Key.Depth;
                    if (at > depth && at < next && ModWire.Part<string>(member.Value, SprocketWire.Kind) == SprocketWire.KindOverride)
                        next = at;
                }

                if (next == int.MaxValue)
                    return;

                foreach (var member in _members)
                {
                    if (member.Key.Depth == next && ModWire.Part<string>(member.Value, SprocketWire.Kind) == SprocketWire.KindOverride && member.Key.Claims(block))
                        Override(into, member.Value);
                }

                depth = next;
            }
        }

        /// <summary>A copy of <paramref name="member" /> standing in its scope's group where it names none of its own.</summary>
        static Dictionary<string, object> Grouped(ModScope scope, Dictionary<string, object> member)
        {
            var made = new Dictionary<string, object>(member);
            if (made.ContainsKey(SprocketWire.Group) || made.ContainsKey(SprocketWire.SprocketsGroup))
                return made;

            for (var at = scope; at != null; at = at.Wider)
            {
                if (at.Group != null)
                {
                    made[SprocketWire.Group] = at.Group;
                    return made;
                }

                if (at.SprocketsGroup != null)
                {
                    made[SprocketWire.SprocketsGroup] = at.SprocketsGroup;
                    return made;
                }
            }

            return made;
        }

        /// <summary>Puts the details <paramref name="change" /> gives over the member of the same id in <paramref name="members" />, keeping its id, kind, type and name.</summary>
        static void Override(List<Dictionary<string, object>> members, Dictionary<string, object> change)
        {
            var id = ModWire.Part<string>(change, SprocketWire.Id);
            foreach (var member in members)
            {
                if (ModWire.Part<string>(member, SprocketWire.Id) != id)
                    continue;

                foreach (var detail in change)
                {
                    if (detail.Key != SprocketWire.Id && detail.Key != SprocketWire.Kind && detail.Key != SprocketWire.Type
                        && detail.Key != SprocketWire.Named && detail.Key != SprocketWire.Scope && detail.Key != SprocketWire.Of)
                        member[detail.Key] = detail.Value;
                }

                return;
            }
        }
    }
}
