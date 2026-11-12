using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.ModApi;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Mods
{
    /// <summary>What another mod has said its blocks are, through Sprocket's mod API: their values, actions and signals, offered as Sprocket's own are.</summary>
    /// <remarks>Describes a block type from the registrations standing when the type is first described; a later registration has the type described again.</remarks>
    public sealed class ModCapability : Capability
    {
        // Why a member a mod does not announce is looked at on a cadence: the mod raises no event Sprocket can hear.
        const string Unannounced = "the mod that adds it announces no change of it";

        readonly ModMembers _members;
        readonly List<Dictionary<string, object>> _found = new List<Dictionary<string, object>>();

        /// <summary>The capability of the blocks <paramref name="members" /> holds registrations for.</summary>
        public ModCapability(ModMembers members)
        {
            _members = members;
        }

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return _members.Claims(block);
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            _members.For(block, _found);
            foreach (var member in _found)
            {
                if (Registration.Part<string>(member, ModMemberKeys.Kind) == ModMemberKeys.KindValue)
                    Value(kind, member);
            }

            foreach (var member in _found)
            {
                if (Registration.Part<string>(member, ModMemberKeys.Kind) == ModMemberKeys.KindAction)
                    Action(kind, member);
            }

            foreach (var member in _found)
            {
                if (Registration.Part<string>(member, ModMemberKeys.Kind) == ModMemberKeys.KindSignal)
                    Signal(kind, member);
            }
        }

        /// <inheritdoc />
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            return _members.Listen(block, told);
        }

        /// <summary>Adds the value <paramref name="member" /> registers to <paramref name="kind" />, where it is of a type this Sprocket knows.</summary>
        static void Value(EntityKind kind, Dictionary<string, object> member)
        {
            var value = RegisteredValue.Of(member);
            if (value == null)
                return;

            kind.WithValue(Id(member), Name(member), value).In(GroupOf(member));
            Placed(kind, member);
            Watched(kind, member);
        }

        /// <summary>Adds the action <paramref name="member" /> registers to <paramref name="kind" />, where it names a function to run.</summary>
        static void Action(EntityKind kind, Dictionary<string, object> member)
        {
            var action = RegisteredAction.Of(member);
            if (action == null)
                return;

            kind.WithAction(Id(member), Name(member), action, RegisteredAction.Asked(member)).In(GroupOf(member));
            Placed(kind, member);
            if (!RegisteredAction.IsLasting(member))
                return;

            if (Registration.Part<bool>(member, ModMemberKeys.Announced))
                kind.LastingAnnounced();
            else
                kind.Lasting();
        }

        /// <summary>Adds the signal <paramref name="member" /> registers to <paramref name="kind" />, where the value it is a change of was registered and its state is one that value holds.</summary>
        void Signal(EntityKind kind, Dictionary<string, object> member)
        {
            var changes = Registration.Part<string>(member, ModMemberKeys.Changes);
            var value = Registered(changes);
            if (value == null || RegisteredValue.Of(value) == null)
                return;

            var becomes = RegisteredValue.State(value, Registration.Part<object>(member, ModMemberKeys.Becomes));
            if (becomes == null)
                return;

            kind.WithChange(Id(member), Name(member), changes, becomes.Value);
            var states = Registration.Part<Func<string[]>>(member, ModMemberKeys.States);
            var words = states == null ? null : states();
            if (words != null && words.Length == 2)
                kind.Reading(words[0], words[1]);

            Watched(kind, member);
        }

        /// <summary>The value registered for the block being described with the id <paramref name="id" />, or null where there is none.</summary>
        Dictionary<string, object> Registered(string id)
        {
            foreach (var member in _found)
            {
                if (Registration.Part<string>(member, ModMemberKeys.Kind) == ModMemberKeys.KindValue && Registration.Part<string>(member, ModMemberKeys.Id) == id)
                    return member;
            }

            return null;
        }

        /// <summary>Says how the value or signal <paramref name="member" /> registers, added to <paramref name="kind" /> last, is watched: heard where the mod announces it, looked at otherwise.</summary>
        static void Watched(EntityKind kind, Dictionary<string, object> member)
        {
            if (Registration.Part<bool>(member, ModMemberKeys.Announced))
                kind.Announced();
            else
                kind.Looked(Unannounced);
        }

        /// <summary>Stands the value or action <paramref name="member" /> registers, added to <paramref name="kind" /> last, in the mod's own group where it names one, and gives it the mod's description.</summary>
        static void Placed(EntityKind kind, Dictionary<string, object> member)
        {
            var group = Registration.Part<Func<string>>(member, ModMemberKeys.Group);
            if (group != null && !Registration.Part<bool>(member, ModMemberKeys.GroupIsSprockets))
                kind.InGroup(group());

            var about = Registration.Part<Func<string>>(member, ModMemberKeys.Description);
            if (about != null)
                kind.Telling(about());
        }

        /// <summary>The id <paramref name="member" /> is stored under.</summary>
        static string Id(Dictionary<string, object> member)
        {
            return Registration.Part<string>(member, ModMemberKeys.Id);
        }

        /// <summary>The name <paramref name="member" /> is said by in the player's language, or its id where the mod gives none.</summary>
        static string Name(Dictionary<string, object> member)
        {
            var name = Registration.Part<Func<string>>(member, ModMemberKeys.Name);
            return name == null ? Id(member) : name();
        }

        /// <summary>The group of Sprocket's that <paramref name="member" /> asks to stand in, or Other where it names none of them.</summary>
        static Category GroupOf(Dictionary<string, object> member)
        {
            var group = Registration.Part<Func<string>>(member, ModMemberKeys.Group);
            if (group == null || !Registration.Part<bool>(member, ModMemberKeys.GroupIsSprockets))
                return Category.Other;

            var name = group();
            foreach (Category category in Enum.GetValues(typeof(Category)))
            {
                if (string.Equals(category.ToString(), name, StringComparison.OrdinalIgnoreCase))
                    return category;
            }

            return Category.Other;
        }
    }
}
