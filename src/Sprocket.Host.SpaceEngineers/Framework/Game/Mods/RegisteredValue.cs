using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.ModApi;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Mods
{
    /// <summary>Makes the values other mods register through Sprocket's mod API into values of Sprocket's own.</summary>
    public static class RegisteredValue
    {
        /// <summary>The value <paramref name="member" /> registers, reading and writing through the mod's functions; null for a type this Sprocket does not know.</summary>
        public static OwnValue Of(Dictionary<string, object> member)
        {
            var read = Registration.Part<Func<IMyTerminalBlock, object>>(member, ModMemberKeys.Read);
            if (read == null)
                return null;

            var write = Registration.Part<Action<IMyTerminalBlock, object>>(member, ModMemberKeys.Write);
            OwnValue value;
            switch (Registration.Part<string>(member, ModMemberKeys.Type))
            {
                case ModMemberKeys.TypeFlag:
                    value = OwnValue.Flag(Reading.Of<IMyTerminalBlock>(block => (bool)read(block)));
                    return write == null ? value : value.Writing<IMyTerminalBlock>((block, given) => write(block, given.Flag));
                case ModMemberKeys.TypeNumber:
                    value = OwnValue.Number<IMyTerminalBlock>(block => (double)read(block));
                    return write == null ? value : value.Writing<IMyTerminalBlock>((block, given) => write(block, given.Number));
                case ModMemberKeys.TypeWhole:
                    value = OwnValue.Whole<IMyTerminalBlock>(block => (long)read(block));
                    return write == null ? value : value.Writing<IMyTerminalBlock>((block, given) => write(block, given.Whole));
                case ModMemberKeys.TypeChoice:
                    var choices = Registration.Choices(member);
                    if (choices.Length == 0)
                        return null;

                    value = OwnValue.OneOf<IMyTerminalBlock>(choices, block => (int)read(block)).Saying(Registration.ChoiceWords(member));
                    return write == null ? value : value.Writing<IMyTerminalBlock>((block, given) => write(block, (int)given.Whole));
                case ModMemberKeys.TypeText:
                    value = OwnValue.Text<IMyTerminalBlock>(block => (string)read(block));
                    return write == null ? value : value.Writing<IMyTerminalBlock>((block, given) => write(block, given.Text));
                case ModMemberKeys.TypeColor:
                    value = OwnValue.Color<IMyTerminalBlock>(block => (Color)read(block));
                    return write == null ? value : value.Writing<IMyTerminalBlock>((block, given) => write(block, new Color(given.Red, given.Green, given.Blue, given.Alpha)));
                default:
                    return null;
            }
        }

        /// <summary>The state <paramref name="becomes" /> of the value <paramref name="value" /> registers, as a literal a signal compares it with; null for a state of no type the value holds.</summary>
        public static Literal? State(Dictionary<string, object> value, object becomes)
        {
            if (Registration.Part<string>(value, ModMemberKeys.Type) == ModMemberKeys.TypeChoice)
            {
                var choices = Registration.Choices(value);
                if (becomes is int)
                    return (int)becomes >= 0 && (int)becomes < choices.Length ? Literal.Name(choices[(int)becomes]) : (Literal?)null;

                var id = becomes as string;
                return id != null && Array.IndexOf(choices, id) >= 0 ? Literal.Name(id) : (Literal?)null;
            }

            if (becomes is bool)
                return Literal.Of((bool)becomes);
            if (becomes is int)
                return Literal.Of((int)becomes);
            if (becomes is long)
                return Literal.Of((long)becomes);
            if (becomes is double)
                return Literal.Of((double)becomes);

            var text = becomes as string;
            return text == null ? (Literal?)null : Literal.Of(text);
        }
    }
}
