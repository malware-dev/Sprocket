using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Sprocket.ModSupport
{
    /// <summary>
    /// A set of blocks a mod tells Sprocket about, and the members they have. A narrower set made from it has every member
    /// it has, and may add its own or override the details of those it inherits.
    /// </summary>
    public sealed class SprocketBlock : ISprocketBlockWiring, ISprocketWired
    {
        readonly ISprocketRegistry _registration;
        readonly Dictionary<string, object> _said = new Dictionary<string, object>();

        /// <summary>The scope numbered <paramref name="index" />, narrowing the scope <paramref name="narrows" /> or -1 for a base, registered through <paramref name="registration" />, which only the wrapper holds.</summary>
        public SprocketBlock(ISprocketRegistry registration, int index, int narrows)
        {
            _registration = registration;
            Index = index;
            _said[SprocketWire.Index] = index;
            _said[SprocketWire.Narrows] = narrows;
        }

        /// <summary>The scope's place among the mod's scopes.</summary>
        public int Index { get; }

        /// <summary>Stands every member of these blocks in a group of the mod's own, headed <paramref name="heading" />, unless a member says otherwise.</summary>
        public SprocketBlock InGroup(string heading)
        {
            _said[SprocketWire.Group] = SprocketText.Of(heading);
            return this;
        }

        /// <summary>Stands every member of these blocks in a group of the mod's own, headed by the text <paramref name="heading" /> names.</summary>
        public SprocketBlock InGroup(MyStringId heading)
        {
            _said[SprocketWire.Group] = SprocketText.Of(heading);
            return this;
        }

        /// <summary>Stands every member of these blocks in <paramref name="group" />, one of Sprocket's own, unless a member says otherwise.</summary>
        public SprocketBlock InGroup(SprocketGroup group)
        {
            _said[SprocketWire.SprocketsGroup] = group.Id;
            return this;
        }

        /// <summary>The narrower set of these blocks that are of <paramref name="subtypes" />.</summary>
        public SprocketBlock Subtypes(params string[] subtypes)
        {
            var narrower = _registration.Scope(Index);
            narrower._said[SprocketWire.Subtypes] = subtypes;
            return narrower;
        }

        /// <inheritdoc />
        SprocketBlock ISprocketBlockWiring.OfType(string blockType, string[] subtypes)
        {
            _said[SprocketWire.BlockType] = blockType;
            _said[SprocketWire.Subtypes] = subtypes;
            return this;
        }

        /// <summary>Whitelists the game's own terminal action or property <paramref name="gameId" /> for Sprocket's organized list, as it is.</summary>
        public SprocketMember Whitelist(string gameId) => _registration.Member(Index, SprocketWire.KindWhitelisted, gameId, false);

        /// <summary>Changes the details of the member <paramref name="id" /> these blocks inherit, here only.</summary>
        public SprocketMember Override(string id) => _registration.Member(Index, SprocketWire.KindOverride, id, false);

        /// <summary>Defines the signal <paramref name="id" />, which the mod raises.</summary>
        public SprocketSignal DefineSignal(string id) => new SprocketSignal(_registration, _registration.Member(Index, SprocketWire.KindSignal, id, true));

        /// <summary>A real number <paramref name="id" />, read by <paramref name="read" /> and set by <paramref name="write" /> where given.</summary>
        public SprocketMember Number(string id, Func<IMyTerminalBlock, double> read, Action<IMyTerminalBlock, double> write = null) =>
            Value(id, SprocketWire.TypeNumber, b => read(b), write == null ? null : (Action<IMyTerminalBlock, object>)((b, v) => write(b, (double)v)));

        /// <summary>A whole number <paramref name="id" />, read by <paramref name="read" /> and set by <paramref name="write" /> where given.</summary>
        public SprocketMember Whole(string id, Func<IMyTerminalBlock, long> read, Action<IMyTerminalBlock, long> write = null) =>
            Value(id, SprocketWire.TypeWhole, b => read(b), write == null ? null : (Action<IMyTerminalBlock, object>)((b, v) => write(b, (long)v)));

        /// <summary>An on or off <paramref name="id" />, read by <paramref name="read" /> and set by <paramref name="write" /> where given.</summary>
        public SprocketMember Flag(string id, Func<IMyTerminalBlock, bool> read, Action<IMyTerminalBlock, bool> write = null) =>
            Value(id, SprocketWire.TypeFlag, b => read(b), write == null ? null : (Action<IMyTerminalBlock, object>)((b, v) => write(b, (bool)v)));

        /// <summary>A text <paramref name="id" />, read by <paramref name="read" /> and set by <paramref name="write" /> where given.</summary>
        public SprocketMember Text(string id, Func<IMyTerminalBlock, string> read, Action<IMyTerminalBlock, string> write = null) =>
            Value(id, SprocketWire.TypeText, b => read(b), write == null ? null : (Action<IMyTerminalBlock, object>)((b, v) => write(b, (string)v)));

        /// <summary>A color <paramref name="id" />, read by <paramref name="read" /> and set by <paramref name="write" /> where given.</summary>
        public SprocketMember Colored(string id, Func<IMyTerminalBlock, Color> read, Action<IMyTerminalBlock, Color> write = null) =>
            Value(id, SprocketWire.TypeColor, b => read(b), write == null ? null : (Action<IMyTerminalBlock, object>)((b, v) => write(b, (Color)v)));

        /// <summary>A length of time <paramref name="id" />, read by <paramref name="read" /> and set by <paramref name="write" /> where given.</summary>
        public SprocketMember Duration(string id, Func<IMyTerminalBlock, TimeSpan> read, Action<IMyTerminalBlock, TimeSpan> write = null) =>
            Value(id, SprocketWire.TypeDuration, b => read(b), write == null ? null : (Action<IMyTerminalBlock, object>)((b, v) => write(b, (TimeSpan)v)));

        /// <summary>A word of <paramref name="type" /> <paramref name="id" />, read by <paramref name="read" /> and set by <paramref name="write" /> where given.</summary>
        public SprocketMember Choice(string id, SprocketType type, Func<IMyTerminalBlock, string> read, Action<IMyTerminalBlock, string> write = null) =>
            Value(id, SprocketWire.TypeChoice, b => read(b), write == null ? null : (Action<IMyTerminalBlock, object>)((b, v) => write(b, (string)v))).Choosing(type);

        /// <summary>A word of the enum type <paramref name="type" /> <paramref name="id" />, read and written as the enum.</summary>
        public SprocketMember Choice<T>(string id, SprocketEnumType<T> type, Func<IMyTerminalBlock, T> read, Action<IMyTerminalBlock, T> write = null) where T : struct =>
            Value(id, SprocketWire.TypeChoice, b => type.Word(read(b)), write == null ? null : (Action<IMyTerminalBlock, object>)((b, v) => write(b, type.Value((string)v)))).Choosing(type);

        /// <summary>A real number <paramref name="id" /> that needs <paramref name="o1" /> to go on, read by <paramref name="read" />.</summary>
        public SprocketMember Number<T1>(string id, SprocketOption<T1> o1, Func<IMyTerminalBlock, T1, double> read) =>
            Function(id, SprocketWire.TypeNumber, new ISprocketOption[] { o1 }, (b, g) => read(b, Read(o1, g[0])));

        /// <summary>A real number <paramref name="id" /> that needs <paramref name="o1" /> and <paramref name="o2" /> to go on, read by <paramref name="read" />.</summary>
        public SprocketMember Number<T1, T2>(string id, SprocketOption<T1> o1, SprocketOption<T2> o2, Func<IMyTerminalBlock, T1, T2, double> read) =>
            Function(id, SprocketWire.TypeNumber, new ISprocketOption[] { o1, o2 }, (b, g) => read(b, Read(o1, g[0]), Read(o2, g[1])));

        /// <summary>A whole number <paramref name="id" /> that needs <paramref name="o1" /> to go on, read by <paramref name="read" />.</summary>
        public SprocketMember Whole<T1>(string id, SprocketOption<T1> o1, Func<IMyTerminalBlock, T1, long> read) =>
            Function(id, SprocketWire.TypeWhole, new ISprocketOption[] { o1 }, (b, g) => read(b, Read(o1, g[0])));

        /// <summary>An on or off <paramref name="id" /> that needs <paramref name="o1" /> to go on, read by <paramref name="read" />.</summary>
        public SprocketMember Flag<T1>(string id, SprocketOption<T1> o1, Func<IMyTerminalBlock, T1, bool> read) =>
            Function(id, SprocketWire.TypeFlag, new ISprocketOption[] { o1 }, (b, g) => read(b, Read(o1, g[0])));

        /// <summary>A text <paramref name="id" /> that needs <paramref name="o1" /> to go on, read by <paramref name="read" />.</summary>
        public SprocketMember Text<T1>(string id, SprocketOption<T1> o1, Func<IMyTerminalBlock, T1, string> read) =>
            Function(id, SprocketWire.TypeText, new ISprocketOption[] { o1 }, (b, g) => read(b, Read(o1, g[0])));

        /// <summary>A word of <paramref name="type" /> <paramref name="id" /> that needs <paramref name="o1" /> to go on, read by <paramref name="read" />.</summary>
        public SprocketMember Choice<T1>(string id, SprocketType type, SprocketOption<T1> o1, Func<IMyTerminalBlock, T1, string> read) =>
            Function(id, SprocketWire.TypeChoice, new ISprocketOption[] { o1 }, (b, g) => read(b, Read(o1, g[0]))).Choosing(type);

        /// <summary>An action <paramref name="id" />, done by <paramref name="run" />.</summary>
        public SprocketMember Action(string id, Action<IMyTerminalBlock> run) =>
            Act(id, new ISprocketOption[0], (b, g) => run(b));

        /// <summary>An action <paramref name="id" /> taking <paramref name="o1" />, done by <paramref name="run" />.</summary>
        public SprocketMember Action<T1>(string id, SprocketOption<T1> o1, Action<IMyTerminalBlock, T1> run) =>
            Act(id, new ISprocketOption[] { o1 }, (b, g) => run(b, Read(o1, g[0])));

        /// <summary>An action <paramref name="id" /> taking <paramref name="o1" /> and <paramref name="o2" />, done by <paramref name="run" />.</summary>
        public SprocketMember Action<T1, T2>(string id, SprocketOption<T1> o1, SprocketOption<T2> o2, Action<IMyTerminalBlock, T1, T2> run) =>
            Act(id, new ISprocketOption[] { o1, o2 }, (b, g) => run(b, Read(o1, g[0]), Read(o2, g[1])));

        /// <summary>An action <paramref name="id" /> taking <paramref name="o1" />, <paramref name="o2" /> and <paramref name="o3" />, done by <paramref name="run" />.</summary>
        public SprocketMember Action<T1, T2, T3>(string id, SprocketOption<T1> o1, SprocketOption<T2> o2, SprocketOption<T3> o3, Action<IMyTerminalBlock, T1, T2, T3> run) =>
            Act(id, new ISprocketOption[] { o1, o2, o3 }, (b, g) => run(b, Read(o1, g[0]), Read(o2, g[1]), Read(o3, g[2])));

        /// <summary>An awaitable action <paramref name="id" />, started by <paramref name="start" />, which returns the task a step waits on.</summary>
        public SprocketMember Awaitable(string id, Func<IMyTerminalBlock, SprocketTask> start) =>
            Await(id, new ISprocketOption[0], (b, g) => start(b));

        /// <summary>An awaitable action <paramref name="id" /> taking <paramref name="o1" />, started by <paramref name="start" />, which returns the task a step waits on.</summary>
        public SprocketMember Awaitable<T1>(string id, SprocketOption<T1> o1, Func<IMyTerminalBlock, T1, SprocketTask> start) =>
            Await(id, new ISprocketOption[] { o1 }, (b, g) => start(b, Read(o1, g[0])));

        /// <summary>An awaitable action <paramref name="id" /> taking <paramref name="o1" /> and <paramref name="o2" />, started by <paramref name="start" />.</summary>
        public SprocketMember Awaitable<T1, T2>(string id, SprocketOption<T1> o1, SprocketOption<T2> o2, Func<IMyTerminalBlock, T1, T2, SprocketTask> start) =>
            Await(id, new ISprocketOption[] { o1, o2 }, (b, g) => start(b, Read(o1, g[0]), Read(o2, g[1])));

        /// <summary>An awaitable action <paramref name="id" /> taking three options, started by <paramref name="start" />.</summary>
        public SprocketMember Awaitable<T1, T2, T3>(string id, SprocketOption<T1> o1, SprocketOption<T2> o2, SprocketOption<T3> o3, Func<IMyTerminalBlock, T1, T2, T3, SprocketTask> start) =>
            Await(id, new ISprocketOption[] { o1, o2, o3 }, (b, g) => start(b, Read(o1, g[0]), Read(o2, g[1]), Read(o3, g[2])));

        /// <summary>An awaitable action <paramref name="id" /> started by <paramref name="kick" />; say when it is done with <see cref="SprocketMember.PolledDone" />.</summary>
        public SprocketMember Awaitable(string id, Action<IMyTerminalBlock> kick) =>
            Kick(id, new ISprocketOption[0], (b, g) => kick(b));

        /// <summary>An awaitable action <paramref name="id" /> taking <paramref name="o1" />, started by <paramref name="kick" />; say when it is done with <see cref="SprocketMember.PolledDone" />.</summary>
        public SprocketMember Awaitable<T1>(string id, SprocketOption<T1> o1, Action<IMyTerminalBlock, T1> kick) =>
            Kick(id, new ISprocketOption[] { o1 }, (b, g) => kick(b, Read(o1, g[0])));

        /// <summary>An awaitable action <paramref name="id" /> taking <paramref name="o1" /> and <paramref name="o2" />, started by <paramref name="kick" />.</summary>
        public SprocketMember Awaitable<T1, T2>(string id, SprocketOption<T1> o1, SprocketOption<T2> o2, Action<IMyTerminalBlock, T1, T2> kick) =>
            Kick(id, new ISprocketOption[] { o1, o2 }, (b, g) => kick(b, Read(o1, g[0]), Read(o2, g[1])));

        /// <inheritdoc />
        Dictionary<string, object> ISprocketWired.Wire() => _said;

        static T Read<T>(SprocketOption<T> option, object given) => ((ISprocketOptionReading<T>)option).Read(given);

        SprocketMember Value(string id, string type, Func<IMyTerminalBlock, object> read, Action<IMyTerminalBlock, object> write) =>
            ((ISprocketMemberWiring)_registration.Member(Index, SprocketWire.KindValue, id, true)).Valuing(type, read, write);

        SprocketMember Function(string id, string type, ISprocketOption[] options, Func<IMyTerminalBlock, object[], object> ask) =>
            ((ISprocketMemberWiring)_registration.Member(Index, SprocketWire.KindFunction, id, true)).Asking(type, options, ask);

        SprocketMember Act(string id, ISprocketOption[] options, Action<IMyTerminalBlock, object[]> run) =>
            ((ISprocketMemberWiring)_registration.Member(Index, SprocketWire.KindAction, id, true)).Running(options, run);

        SprocketMember Await(string id, ISprocketOption[] options, Func<IMyTerminalBlock, object[], SprocketTask> start) =>
            ((ISprocketMemberWiring)_registration.Member(Index, SprocketWire.KindAwaitable, id, true)).Starting(options, start);

        SprocketMember Kick(string id, ISprocketOption[] options, Action<IMyTerminalBlock, object[]> kick) =>
            ((ISprocketMemberWiring)_registration.Member(Index, SprocketWire.KindAwaitable, id, true)).Kicking(options, kick);
    }
}
