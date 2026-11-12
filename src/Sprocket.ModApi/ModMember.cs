using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRageMath;

namespace Sprocket.ModApi
{
    /// <summary>
    /// One member a mod gives its blocks in Sprocket, built up one setting at a time and then handed over with
    /// <see cref="SprocketApi.Register" />.
    /// </summary>
    /// <remarks>
    /// A member is a value, an action or a signal, as Sprocket's own are. Its id is stored in players' programs, so it is
    /// prefixed with the mod's name and never changed once released.
    /// </remarks>
    public sealed class ModMember
    {
        readonly Dictionary<string, object> _said = new Dictionary<string, object>();

        ModMember(string id, string kind, string blockType, string[] subtypes)
        {
            _said[ModMemberKeys.Id] = id;
            _said[ModMemberKeys.Kind] = kind;
            _said[ModMemberKeys.BlockType] = blockType;
            _said[ModMemberKeys.Subtypes] = subtypes ?? new string[0];
        }

        /// <summary>The registration as Sprocket reads it, the names of <see cref="ModMemberKeys" /> to their values.</summary>
        public Dictionary<string, object> Said => _said;

        /// <summary>A value called <paramref name="id" /> on blocks of <paramref name="blockType" />, of <paramref name="subtypes" /> where any are given.</summary>
        public static ModMember Value(string id, string blockType, params string[] subtypes)
        {
            return new ModMember(id, ModMemberKeys.KindValue, blockType, subtypes);
        }

        /// <summary>An action called <paramref name="id" /> on blocks of <paramref name="blockType" />, of <paramref name="subtypes" /> where any are given.</summary>
        public static ModMember Action(string id, string blockType, params string[] subtypes)
        {
            return new ModMember(id, ModMemberKeys.KindAction, blockType, subtypes);
        }

        /// <summary>A signal called <paramref name="id" /> on blocks of <paramref name="blockType" />, of <paramref name="subtypes" /> where any are given.</summary>
        public static ModMember Signal(string id, string blockType, params string[] subtypes)
        {
            return new ModMember(id, ModMemberKeys.KindSignal, blockType, subtypes);
        }

        /// <summary>Names the member <paramref name="name" /> and describes it with <paramref name="about" />, both said in the player's language each time they are asked.</summary>
        public ModMember Named(Func<string> name, Func<string> about)
        {
            _said[ModMemberKeys.Name] = name;
            _said[ModMemberKeys.Description] = about;
            return this;
        }

        /// <summary>Stands the member in the mod's own group, called <paramref name="group" /> in the player's language.</summary>
        public ModMember InGroup(Func<string> group)
        {
            _said[ModMemberKeys.Group] = group;
            _said[ModMemberKeys.GroupIsSprockets] = false;
            return this;
        }

        /// <summary>Stands the member in Sprocket's own group <paramref name="group" />, such as <c>behavior</c>.</summary>
        public ModMember InSprocketsGroup(string group)
        {
            _said[ModMemberKeys.Group] = (Func<string>)(() => group);
            _said[ModMemberKeys.GroupIsSprockets] = true;
            return this;
        }

        /// <summary>A value that is on or off, read by <paramref name="read" /> and set by <paramref name="write" /> where it can be.</summary>
        public ModMember Flag(Func<IMyTerminalBlock, bool> read, Action<IMyTerminalBlock, bool> write = null)
        {
            return Typed(ModMemberKeys.TypeFlag, block => read(block), write == null ? null : (Action<IMyTerminalBlock, object>)((block, value) => write(block, (bool)value)));
        }

        /// <summary>A value that is a number, read by <paramref name="read" /> and set by <paramref name="write" /> where it can be.</summary>
        public ModMember Number(Func<IMyTerminalBlock, double> read, Action<IMyTerminalBlock, double> write = null)
        {
            return Typed(ModMemberKeys.TypeNumber, block => read(block), write == null ? null : (Action<IMyTerminalBlock, object>)((block, value) => write(block, (double)value)));
        }

        /// <summary>A value that is a whole number, read by <paramref name="read" /> and set by <paramref name="write" /> where it can be.</summary>
        public ModMember Whole(Func<IMyTerminalBlock, long> read, Action<IMyTerminalBlock, long> write = null)
        {
            return Typed(ModMemberKeys.TypeWhole, block => read(block), write == null ? null : (Action<IMyTerminalBlock, object>)((block, value) => write(block, (long)value)));
        }

        /// <summary>
        /// A value that is one of <paramref name="choices" />, ids a stored program keeps, said as <paramref name="names" /> in the
        /// player's language; read as its place among them by <paramref name="read" /> and set by <paramref name="write" /> where it can be.
        /// </summary>
        public ModMember OneOf(string[] choices, Func<string[]> names, Func<IMyTerminalBlock, int> read, Action<IMyTerminalBlock, int> write = null)
        {
            _said[ModMemberKeys.Choices] = choices;
            _said[ModMemberKeys.ChoiceNames] = names;
            return Typed(ModMemberKeys.TypeChoice, block => read(block), write == null ? null : (Action<IMyTerminalBlock, object>)((block, value) => write(block, (int)value)));
        }

        /// <summary>A value that is text, read by <paramref name="read" /> and set by <paramref name="write" /> where it can be.</summary>
        public ModMember Text(Func<IMyTerminalBlock, string> read, Action<IMyTerminalBlock, string> write = null)
        {
            return Typed(ModMemberKeys.TypeText, block => read(block), write == null ? null : (Action<IMyTerminalBlock, object>)((block, value) => write(block, (string)value)));
        }

        /// <summary>A value that is a color, read by <paramref name="read" /> and set by <paramref name="write" /> where it can be.</summary>
        public ModMember Colored(Func<IMyTerminalBlock, Color> read, Action<IMyTerminalBlock, Color> write = null)
        {
            return Typed(ModMemberKeys.TypeColor, block => read(block), write == null ? null : (Action<IMyTerminalBlock, object>)((block, value) => write(block, (Color)value)));
        }

        /// <summary>Gives a number its <paramref name="unit" />, and the <paramref name="least" /> and <paramref name="most" /> it may be set to.</summary>
        public ModMember Measured(string unit, double least, double most)
        {
            _said[ModMemberKeys.Unit] = unit;
            _said[ModMemberKeys.Least] = least;
            _said[ModMemberKeys.Most] = most;
            return this;
        }

        /// <summary>Has the action run by <paramref name="run" />, given its options' values in the order <see cref="Taking" /> declared them.</summary>
        public ModMember Running(Action<IMyTerminalBlock, object[]> run)
        {
            _said[ModMemberKeys.Run] = run;
            return this;
        }

        /// <summary>Declares the options the action takes, in order, each made by <see cref="Option" />.</summary>
        public ModMember Taking(params Dictionary<string, object>[] options)
        {
            _said[ModMemberKeys.Options] = options;
            return this;
        }

        /// <summary>
        /// An option of an action: called <paramref name="name" />, holding <paramref name="type" /> (one of the type words in
        /// <see cref="ModMemberKeys" />); a choice is one of the ids <paramref name="choices" />, said as <paramref name="choiceNames" />.
        /// </summary>
        public static Dictionary<string, object> Option(Func<string> name, string type, string unit = null, double least = double.NaN, double most = double.NaN,
            string[] choices = null, Func<string[]> choiceNames = null)
        {
            var option = new Dictionary<string, object> { { ModMemberKeys.Name, name }, { ModMemberKeys.Type, type } };
            if (unit != null)
                option[ModMemberKeys.Unit] = unit;
            if (!double.IsNaN(least))
                option[ModMemberKeys.Least] = least;
            if (!double.IsNaN(most))
                option[ModMemberKeys.Most] = most;
            if (choices != null)
                option[ModMemberKeys.Choices] = choices;
            if (choiceNames != null)
                option[ModMemberKeys.ChoiceNames] = choiceNames;

            return option;
        }

        /// <summary>Makes the action one a step waits on, until <paramref name="done" /> says the block got there.</summary>
        public ModMember DoneWhen(Func<IMyTerminalBlock, bool> done)
        {
            _said[ModMemberKeys.Done] = done;
            return this;
        }

        /// <summary>Makes the signal fire as the value <paramref name="value" />, registered before it, comes to be <paramref name="becomes" />, saying <paramref name="states" />: the words for being in that state and not.</summary>
        public ModMember Becoming(string value, object becomes, Func<string[]> states)
        {
            _said[ModMemberKeys.Changes] = value;
            _said[ModMemberKeys.Becomes] = becomes;
            _said[ModMemberKeys.States] = states;
            return this;
        }

        /// <summary>Promises that the mod calls <see cref="SprocketApi.Changed" /> whenever this member may have changed, so Sprocket listens instead of looking.</summary>
        public ModMember Announced()
        {
            _said[ModMemberKeys.Announced] = true;
            return this;
        }

        ModMember Typed(string type, Func<IMyTerminalBlock, object> read, Action<IMyTerminalBlock, object> write)
        {
            _said[ModMemberKeys.Type] = type;
            _said[ModMemberKeys.Read] = read;
            if (write != null)
                _said[ModMemberKeys.Write] = write;

            return this;
        }
    }
}
