using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Game.Wiring;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.ModSupport;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Mods
{
    /// <summary>What mods supporting Sprocket say their blocks have: values, functions and actions of their own, and the game's members they whitelist.</summary>
    /// <remarks>Describes a block type from the registrations standing when it is first described; a registration arriving or leaving has every type described again.</remarks>
    public sealed class ModCapability : Capability
    {
        // Why a mod's value it does not announce is looked at on a cadence: the mod raises no event Sprocket can hear.
        const string Unannounced = "the mod that adds it announces no change of it";

        readonly ModMembers _members;
        readonly List<KeyValuePair<ModRegistration, Dictionary<string, object>>> _found = new List<KeyValuePair<ModRegistration, Dictionary<string, object>>>();

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
            foreach (var found in _found)
            {
                var member = found.Value;
                switch (ModWire.Part<string>(member, SprocketWire.Kind))
                {
                    case SprocketWire.KindValue:
                        Value(kind, found.Key, member);
                        break;
                    case SprocketWire.KindFunction:
                        Function(kind, block, found.Key, member);
                        break;
                    case SprocketWire.KindAction:
                        Action(kind, block, found.Key, member);
                        break;
                    case SprocketWire.KindWhitelisted:
                        Whitelisted(kind, block, member);
                        break;
                    case SprocketWire.KindAwaitable:
                        Awaitable(kind, block, found.Key, member);
                        break;
                    case SprocketWire.KindSignal:
                        kind.WithRaised(Id(member), Name(member), found.Key.Namespace);
                        break;
                }
            }
        }

        /// <inheritdoc />
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            return _members.Listen(block, told);
        }

        /// <summary>Adds the value <paramref name="member" /> describes to <paramref name="kind" />, where it is of a type this Sprocket knows.</summary>
        void Value(EntityKind kind, ModRegistration from, Dictionary<string, object> member)
        {
            var read = ModWire.Part<Func<IMyTerminalBlock, object>>(member, SprocketWire.Read);
            if (read == null)
                return;

            var value = Being(ModWire.Part<string>(member, SprocketWire.Type), member, read);
            if (value == null)
                return;

            var write = ModWire.Part<Action<IMyTerminalBlock, object>>(member, SprocketWire.Write);
            if (write != null)
                value.Writing<IMyTerminalBlock>((block, given) => Written(member, block, given, write));

            kind.WithValue(Id(member), Name(member), value);
            Placed(kind, from, member);
            Measured(kind, member, null);
            if (ModWire.Part<bool>(member, SprocketWire.Announced))
                kind.Announced();
            else
                kind.Looked(Unannounced);
        }

        /// <summary>Adds the function <paramref name="member" /> describes to <paramref name="kind" />.</summary>
        void Function(EntityKind kind, IMyTerminalBlock block, ModRegistration from, Dictionary<string, object> member)
        {
            var ask = ModWire.Part<Func<IMyTerminalBlock, object[], object>>(member, SprocketWire.Ask);
            var options = ModWire.Part<Dictionary<string, object>[]>(member, SprocketWire.Options) ?? new Dictionary<string, object>[0];
            if (ask == null)
                return;

            var value = Asking(ModWire.Part<string>(member, SprocketWire.Type), (on, given) => Asked(ask, on, ModFields.Read(options, given)));
            if (value == null)
                return;

            kind.WithValue(Id(member), Name(member), value.Taking(ModFields.Asked(options, block, _members)));
            Placed(kind, from, member);
            Measured(kind, member, block);
            kind.Looked(Unannounced);
        }

        /// <summary>Adds the action <paramref name="member" /> describes to <paramref name="kind" />, done by the mod.</summary>
        void Action(EntityKind kind, IMyTerminalBlock block, ModRegistration from, Dictionary<string, object> member)
        {
            var run = ModWire.Part<Action<IMyTerminalBlock, object[]>>(member, SprocketWire.Run);
            if (run == null)
                return;

            var options = ModWire.Part<Dictionary<string, object>[]>(member, SprocketWire.Options) ?? new Dictionary<string, object>[0];
            var action = OwnAction.Of<IMyTerminalBlock, object[]>(
                given => ModFields.Read(options, given),
                (on, given, seen) => Ran(run, on, given));
            kind.WithAction(Id(member), Name(member), action, ModFields.Asked(options, block, _members));
            Placed(kind, from, member);
        }

        /// <summary>Adds the awaitable action <paramref name="member" /> describes to <paramref name="kind" />: a step waits on the task the mod starts until it ends.</summary>
        void Awaitable(EntityKind kind, IMyTerminalBlock block, ModRegistration from, Dictionary<string, object> member)
        {
            var start = ModWire.Part<Func<IMyTerminalBlock, object[], Dictionary<string, object>>>(member, SprocketWire.Start);
            if (start == null)
                return;

            var options = ModWire.Part<Dictionary<string, object>[]>(member, SprocketWire.Options) ?? new Dictionary<string, object>[0];
            var tasks = new ModTasks($"{from.Namespace}.{Id(member)}", start, _members);
            var action = OwnAction.Told<IMyTerminalBlock, object[]>(
                given => ModFields.Read(options, given),
                (on, given, seen) => tasks.Start(on, given),
                Reading.Of<IMyTerminalBlock>(tasks.Completed),
                (on, given, seen) => tasks.Cancelled(on),
                (on, given, seen) => tasks.Trouble(on),
                tasks.Stop);
            kind.WithAction(Id(member), Name(member), action, ModFields.Asked(options, block, _members));
            Placed(kind, from, member);
            kind.LastingAnnounced();
        }

        /// <summary>Puts the game's own member <paramref name="member" /> names in the organized list, by the name and details the mod gives it, where the block has it.</summary>
        static void Whitelisted(EntityKind kind, IMyTerminalBlock block, Dictionary<string, object> member)
        {
            var gameId = Id(member);
            var name = ModWire.Said(ModWire.Part<Func<string>>(member, SprocketWire.Named));
            var sets = Toolbars.Gauge(block, gameId) != null;
            var acts = Toolbars.Offers(block, gameId);
            if (!sets && !acts)
            {
                GameLog.Say($"A mod whitelists \"{gameId}\" on {block.BlockDefinition}, which has no terminal action or property by that id.");
                return;
            }

            foreach (var asValue in new[] { true, false })
            {
                if (asValue ? !sets : !acts)
                    continue;

                kind.Vetting(asValue, gameId);
                if (name.Length > 0)
                    kind.Renaming(gameId, asValue, name);
            }

            var unit = ModWire.Part<bool>(member, SprocketWire.Percent) ? " %" : Spaced(ModWire.Part<string>(member, SprocketWire.Unit));
            if (sets && unit != null)
                kind.Measuring(gameId, unit);
        }

        /// <summary>A value of Sprocket's own of the type word <paramref name="type" />, read through <paramref name="read" />; null for a type this Sprocket does not know.</summary>
        OwnValue Being(string type, Dictionary<string, object> member, Func<IMyTerminalBlock, object> read)
        {
            switch (type)
            {
                case SprocketWire.TypeFlag:
                    return OwnValue.Flag(Reading.Of<IMyTerminalBlock>(block => As(Read(read, block), false)));
                case SprocketWire.TypeReal:
                    return OwnValue.Real<IMyTerminalBlock>(block => Number(Read(read, block)));
                case SprocketWire.TypeInteger:
                    return OwnValue.Integer<IMyTerminalBlock>(block => As(Read(read, block), 0L));
                case SprocketWire.TypeText:
                    return OwnValue.Text<IMyTerminalBlock>(block => Read(read, block) as string ?? "");
                case SprocketWire.TypeColor:
                    return OwnValue.Color<IMyTerminalBlock>(block => As(Read(read, block), Color.White));
                case SprocketWire.TypeDuration:
                    return OwnValue.Length<IMyTerminalBlock>(block => Duration.OfSeconds(As(Read(read, block), TimeSpan.Zero).TotalSeconds));
                case SprocketWire.TypeChoice:
                    ModRegistration owner;
                    var choices = _members.FindType(ModWire.Part<string>(member, SprocketWire.Of), out owner);
                    if (choices == null)
                        return null;

                    var words = owner != null ? owner.WordNames(choices) : choices.Choices;
                    return OwnValue.OneOf<IMyTerminalBlock>(choices, block => Array.IndexOf(choices.Choices, Read(read, block) as string)).Saying(words);
                default:
                    return null;
            }
        }

        /// <summary>A function of Sprocket's own of the type word <paramref name="type" />, read through <paramref name="ask" /> with a step's values; null for a type this Sprocket does not know.</summary>
        static OwnValue Asking(string type, Func<IMyTerminalBlock, IList<Literal>, object> ask)
        {
            Func<IList<Literal>, IList<Literal>> given = arguments => arguments;
            switch (type)
            {
                case SprocketWire.TypeFlag:
                    return OwnValue.Flag<IMyTerminalBlock, IList<Literal>>(given, (block, arguments) => As(ask(block, arguments), false));
                case SprocketWire.TypeReal:
                    return OwnValue.Real<IMyTerminalBlock, IList<Literal>>(given, (block, arguments) => Number(ask(block, arguments)));
                case SprocketWire.TypeInteger:
                    return OwnValue.Integer<IMyTerminalBlock, IList<Literal>>(given, (block, arguments) => As(ask(block, arguments), 0L));
                case SprocketWire.TypeText:
                    return OwnValue.Text<IMyTerminalBlock, IList<Literal>>(given, (block, arguments) => ask(block, arguments) as string ?? "");
                case SprocketWire.TypeColor:
                    return OwnValue.Color<IMyTerminalBlock, IList<Literal>>(given, (block, arguments) => As(ask(block, arguments), Color.White));
                default:
                    return null;
            }
        }

        /// <summary>Gives the member added last to <paramref name="kind" /> the mod's namespace, group and description.</summary>
        static void Placed(EntityKind kind, ModRegistration from, Dictionary<string, object> member)
        {
            kind.InNamespace(from.Namespace);
            var group = ModWire.Part<Func<string>>(member, SprocketWire.Group);
            if (group != null)
                kind.InGroup(ModWire.Said(group));
            else
                kind.In(GroupOf(ModWire.Part<string>(member, SprocketWire.SprocketsGroup)));

            var about = ModWire.Said(ModWire.Part<Func<string>>(member, SprocketWire.Described));
            if (about.Length > 0)
                kind.Telling(about);

            var join = ModWire.Part<string>(member, SprocketWire.Joined);
            if (!string.IsNullOrEmpty(join))
                kind.Joining(join);
        }

        /// <summary>Gives the value added last to <paramref name="kind" /> the unit, percentage or range <paramref name="member" /> says, its range as asked of <paramref name="block" /> where one is given.</summary>
        static void Measured(EntityKind kind, Dictionary<string, object> member, IMyTerminalBlock block)
        {
            var percent = ModWire.Part<bool>(member, SprocketWire.Percent);
            if (percent)
                kind.Percent();
            else
            {
                var unit = Spaced(ModWire.Part<string>(member, SprocketWire.Unit));
                if (unit != null)
                    kind.Measured(unit);
            }

            var least = ModWire.Part<Func<IMyTerminalBlock, double>>(member, SprocketWire.Least);
            var most = ModWire.Part<Func<IMyTerminalBlock, double>>(member, SprocketWire.Most);
            if (block != null && (least != null || most != null))
                kind.Between(ModFields.Bound(least, block, percent), ModFields.Bound(most, block, percent));
        }

        /// <summary>Gives the mod <paramref name="given" /> for the value <paramref name="member" /> describes, in the mod's type, a share where it is a percentage.</summary>
        void Written(Dictionary<string, object> member, IMyTerminalBlock block, Literal given, Action<IMyTerminalBlock, object> write)
        {
            var type = ModWire.Part<string>(member, SprocketWire.Type);
            // A choice is set by its place among the type's words, as Sprocket's own are.
            object value;
            if (type == SprocketWire.TypeChoice)
            {
                ModRegistration owner;
                var choices = _members.FindType(ModWire.Part<string>(member, SprocketWire.Of), out owner);
                var at = (int)given.Integer;
                if (choices == null || at < 0 || at >= choices.Choices.Length)
                    return;

                value = choices.Choices[at];
            }
            else
                value = ModFields.Read(member, type, given);

            Safely(() => write(block, value), member);
        }

        /// <summary>The group of Sprocket's the id <paramref name="id" /> names, as <see cref="SprocketGroup" /> gives it; Other where it names none.</summary>
        static Category GroupOf(string id)
        {
            if (string.IsNullOrEmpty(id))
                return Category.Other;

            var plain = id.Replace("_", "");
            foreach (Category category in Enum.GetValues(typeof(Category)))
            {
                var name = category.ToString();
                if (string.Equals(name, plain, StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Behaviour", StringComparison.Ordinal) && plain == "behavior")
                    return category;
            }

            return Category.Other;
        }

        /// <summary><paramref name="unit" /> as a sentence says it after a number: a space before it, but none before a degree.</summary>
        static string Spaced(string unit)
        {
            if (string.IsNullOrEmpty(unit))
                return null;

            return unit.StartsWith("°", StringComparison.Ordinal) ? unit : " " + unit;
        }

        /// <summary>What <paramref name="read" /> says of <paramref name="block" />, or null where it fails.</summary>
        static object Read(Func<IMyTerminalBlock, object> read, IMyTerminalBlock block)
        {
            try
            {
                return read(block);
            }
            catch (Exception)
            {
                return null;
            }
        }

        static object Asked(Func<IMyTerminalBlock, object[], object> ask, IMyTerminalBlock block, object[] given)
        {
            try
            {
                return ask(block, given);
            }
            catch (Exception)
            {
                return null;
            }
        }

        static void Ran(Action<IMyTerminalBlock, object[]> run, IMyTerminalBlock block, object[] given)
        {
            try
            {
                run(block, given);
            }
            catch (Exception e)
            {
                GameLog.Say($"A mod's action on {block.CustomName} failed: {e.Message}");
            }
        }

        static void Safely(Action act, Dictionary<string, object> member)
        {
            try
            {
                act();
            }
            catch (Exception e)
            {
                GameLog.Say($"A mod failed setting {Id(member)}: {e.Message}");
            }
        }

        /// <summary>Returns <paramref name="said" /> as a <typeparamref name="T" />, or <paramref name="otherwise" /> where it is another type or nothing.</summary>
        static T As<T>(object said, T otherwise) where T : struct
        {
            return said is T ? (T)said : otherwise;
        }

        static double Number(object read)
        {
            return read is double ? (double)read : read is float ? (float)read : read is long ? (long)read : read is int ? (int)read : 0.0;
        }

        static string Id(Dictionary<string, object> member) => ModWire.Part<string>(member, SprocketWire.Id) ?? "";

        static string Name(Dictionary<string, object> member)
        {
            var name = ModWire.Said(ModWire.Part<Func<string>>(member, SprocketWire.Named));
            return name.Length > 0 ? name : Id(member);
        }
    }
}
