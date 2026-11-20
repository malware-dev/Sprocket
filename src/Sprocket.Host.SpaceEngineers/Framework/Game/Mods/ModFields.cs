using System;
using System.Collections.Generic;
using System.Globalization;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.ModSupport;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Mods
{
    /// <summary>Makes a mod's options into the fields a step asks them by, and a step's values into what the mod's callbacks take.</summary>
    public static class ModFields
    {
        /// <summary>The fields a step asks <paramref name="options" /> by on <paramref name="block" />, in order.</summary>
        public static Field[] Asked(Dictionary<string, object>[] options, IMyTerminalBlock block, ModMembers members)
        {
            var fields = new Field[options.Length];
            for (var i = 0; i < options.Length; i++)
                fields[i] = Asking(options[i], block, members);
            return fields;
        }

        /// <summary>The values a step gave <paramref name="options" />, in the mod's types, each one missing from <paramref name="arguments" /> at its type's default.</summary>
        public static object[] Read(Dictionary<string, object>[] options, IList<Literal> arguments)
        {
            var values = new object[options.Length];
            for (var i = 0; i < options.Length; i++)
            {
                var type = ModWire.Part<string>(options[i], SprocketWire.Type);
                values[i] = arguments != null && i < arguments.Count ? Read(options[i], type, arguments[i]) : Default(type);
            }

            return values;
        }

        /// <summary>The value <paramref name="given" /> holds as the mod takes the type word <paramref name="type" />, a share where <paramref name="option" /> is a percentage.</summary>
        public static object Read(Dictionary<string, object> option, string type, Literal given)
        {
            switch (type)
            {
                case SprocketWire.TypeFlag:
                    return given.Flag;
                case SprocketWire.TypeNumber:
                    return ModWire.Part<bool>(option, SprocketWire.Percent) ? given.Number / 100 : given.Number;
                case SprocketWire.TypeWhole:
                    return given.Whole;
                case SprocketWire.TypeColor:
                    return new Color(given.Red, given.Green, given.Blue, given.Alpha);
                case SprocketWire.TypeDuration:
                    return TimeSpan.FromSeconds(given.Length.Seconds);
                default:
                    return given.Text ?? "";
            }
        }

        /// <summary>The value an option of the type word <paramref name="type" /> takes where a step gives it none.</summary>
        public static object Default(string type)
        {
            switch (type)
            {
                case SprocketWire.TypeFlag:
                    return false;
                case SprocketWire.TypeNumber:
                    return 0.0;
                case SprocketWire.TypeWhole:
                    return 0L;
                case SprocketWire.TypeColor:
                    return Color.White;
                case SprocketWire.TypeDuration:
                    return TimeSpan.Zero;
                default:
                    return "";
            }
        }

        /// <summary>The bound <paramref name="bound" /> says for <paramref name="block" />, as a field writes it, in percent where <paramref name="percent" /> says so; empty where none is given or it fails.</summary>
        public static string Bound(Func<IMyTerminalBlock, double> bound, IMyTerminalBlock block, bool percent)
        {
            if (bound == null)
                return "";

            try
            {
                var value = bound(block) * (percent ? 100 : 1);
                return value.ToString("0.######", CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>The field a step asks <paramref name="option" /> by.</summary>
        static Field Asking(Dictionary<string, object> option, IMyTerminalBlock block, ModMembers members)
        {
            var name = ModWire.Said(ModWire.Part<Func<string>>(option, SprocketWire.Named));
            var percent = ModWire.Part<bool>(option, SprocketWire.Percent);
            Field field;
            switch (ModWire.Part<string>(option, SprocketWire.Type))
            {
                case SprocketWire.TypeFlag:
                    field = Field.OneOf(name, Literal.Of(true).Said, Literal.Of(false).Said);
                    field.Edits = FieldEditor.Chosen;
                    break;
                case SprocketWire.TypeNumber:
                case SprocketWire.TypeWhole:
                    field = Field.Between(name,
                        Bound(ModWire.Part<Func<IMyTerminalBlock, double>>(option, SprocketWire.Least), block, percent),
                        Bound(ModWire.Part<Func<IMyTerminalBlock, double>>(option, SprocketWire.Most), block, percent));
                    field.Entry = ModWire.Part<string>(option, SprocketWire.Type) == SprocketWire.TypeWhole ? FieldInput.Whole : FieldInput.Decimal;
                    break;
                case SprocketWire.TypeColor:
                    field = Field.Holding(name, FieldEditor.Color, 1);
                    break;
                case SprocketWire.TypeDuration:
                    field = Field.Called(name);
                    field.Edits = FieldEditor.Duration;
                    field.Entry = FieldInput.Any;
                    break;
                case SprocketWire.TypeChoice:
                    field = Choosing(name, option, members);
                    break;
                default:
                    field = Field.Called(name);
                    field.Entry = FieldInput.Text;
                    break;
            }

            var unit = percent ? " %" : ModWire.Part<string>(option, SprocketWire.Unit);
            if (!string.IsNullOrEmpty(unit))
                field.Unit = unit;
            field.Needed = true;
            return field;
        }

        /// <summary>A field called <paramref name="name" /> choosing a word of the type <paramref name="option" /> names, said as players read it.</summary>
        static Field Choosing(string name, Dictionary<string, object> option, ModMembers members)
        {
            ModRegistration from;
            var type = members.FindType(ModWire.Part<string>(option, SprocketWire.Of), out from);
            if (type == null)
                return Field.Called(name);

            var words = from != null ? from.WordNames(type) : type.Choices;
            var means = new string[type.Choices.Length];
            for (var i = 0; i < means.Length; i++)
                means[i] = Literal.Name(type.Choices[i]).Said;

            var field = Field.Among(name, words, means);
            field.Choices = type;
            if (words.Length > 0)
                field.Default = words[0];
            return field;
        }
    }
}
