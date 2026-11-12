using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.ModApi;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Mods
{
    /// <summary>Makes the actions other mods register through Sprocket's mod API into actions of Sprocket's own, options and all.</summary>
    public static class RegisteredAction
    {
        /// <summary>The action <paramref name="member" /> registers, run through the mod's function and waited on until its done says so; null where it names no function to run.</summary>
        public static OwnAction Of(Dictionary<string, object> member)
        {
            var run = Registration.Part<Action<IMyTerminalBlock, object[]>>(member, ModMemberKeys.Run);
            if (run == null)
                return null;

            var options = Registration.Part<Dictionary<string, object>[]>(member, ModMemberKeys.Options) ?? new Dictionary<string, object>[0];
            var done = Registration.Part<Func<IMyTerminalBlock, bool>>(member, ModMemberKeys.Done);
            return OwnAction.Of<IMyTerminalBlock, object[]>(
                arguments => Read(options, arguments),
                (block, given, seen) => run(block, given),
                done == null ? null : Reading.Of<IMyTerminalBlock>(block => done(block)));
        }

        /// <summary>Whether a step running the action <paramref name="member" /> registers waits until it is done.</summary>
        public static bool IsLasting(Dictionary<string, object> member)
        {
            return Registration.Part<Func<IMyTerminalBlock, bool>>(member, ModMemberKeys.Done) != null;
        }

        /// <summary>The fields a step asks the options of the action <paramref name="member" /> registers by, in the order the mod declared them.</summary>
        public static Field[] Asked(Dictionary<string, object> member)
        {
            var options = Registration.Part<Dictionary<string, object>[]>(member, ModMemberKeys.Options);
            if (options == null)
                return new Field[0];

            var fields = new Field[options.Length];
            for (var i = 0; i < options.Length; i++)
                fields[i] = Asking(options[i]);

            return fields;
        }

        /// <summary>The field a step asks <paramref name="option" /> by.</summary>
        static Field Asking(Dictionary<string, object> option)
        {
            var named = Registration.Part<Func<string>>(option, ModMemberKeys.Name);
            var name = named == null ? "" : named();
            Field field;
            switch (Registration.Part<string>(option, ModMemberKeys.Type))
            {
                case ModMemberKeys.TypeFlag:
                    field = Field.OneOf(name, Literal.Of(true).Said, Literal.Of(false).Said);
                    field.Edits = FieldEditor.Chosen;
                    break;
                case ModMemberKeys.TypeNumber:
                    field = Field.Between(name, Registration.Bound(option, ModMemberKeys.Least), Registration.Bound(option, ModMemberKeys.Most));
                    field.Entry = FieldInput.Decimal;
                    break;
                case ModMemberKeys.TypeWhole:
                    field = Field.Between(name, Registration.Bound(option, ModMemberKeys.Least), Registration.Bound(option, ModMemberKeys.Most));
                    field.Entry = FieldInput.Whole;
                    break;
                case ModMemberKeys.TypeChoice:
                    field = Among(name, option);
                    break;
                default:
                    field = Field.Called(name);
                    field.Entry = FieldInput.Text;
                    break;
            }

            var unit = Registration.Part<string>(option, ModMemberKeys.Unit);
            if (!string.IsNullOrEmpty(unit))
                field.Unit = unit;

            field.Needed = true;
            return field;
        }

        /// <summary>A field called <paramref name="name" /> choosing one of the choices <paramref name="option" /> declares, said in the player's language and standing for its id, which a stored step keeps.</summary>
        static Field Among(string name, Dictionary<string, object> option)
        {
            var ids = Registration.Choices(option);
            var words = Registration.ChoiceWords(option);
            var means = new string[ids.Length];
            for (var i = 0; i < ids.Length; i++)
                means[i] = Literal.Name(ids[i]).Said;

            var field = Field.Among(name, words, means);
            if (words.Length > 0)
                field.Default = words[0];

            return field;
        }

        /// <summary>The values a step gave the action's <paramref name="options" />, in the mod's types, each option missing from <paramref name="arguments" /> at its type's default.</summary>
        static object[] Read(Dictionary<string, object>[] options, IList<Literal> arguments)
        {
            var values = new object[options.Length];
            for (var i = 0; i < options.Length; i++)
            {
                var type = Registration.Part<string>(options[i], ModMemberKeys.Type);
                values[i] = arguments != null && i < arguments.Count ? Read(options[i], arguments[i]) : Default(type);
            }

            return values;
        }

        /// <summary>The value <paramref name="given" /> holds as <paramref name="option" /> takes it: a choice as its place among the option's ids, -1 where it names none of them.</summary>
        static object Read(Dictionary<string, object> option, Literal given)
        {
            switch (Registration.Part<string>(option, ModMemberKeys.Type))
            {
                case ModMemberKeys.TypeFlag:
                    return given.Flag;
                case ModMemberKeys.TypeNumber:
                    return given.Number;
                case ModMemberKeys.TypeWhole:
                    return given.Whole;
                case ModMemberKeys.TypeChoice:
                    return Array.IndexOf(Registration.Choices(option), given.Text);
                default:
                    return given.Text ?? "";
            }
        }

        /// <summary>The value an option of <paramref name="type" /> takes where a step gives it none.</summary>
        static object Default(string type)
        {
            switch (type)
            {
                case ModMemberKeys.TypeFlag:
                    return false;
                case ModMemberKeys.TypeNumber:
                    return 0.0;
                case ModMemberKeys.TypeWhole:
                    return 0L;
                case ModMemberKeys.TypeChoice:
                    return 0;
                default:
                    return "";
            }
        }
    }
}
