using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using System;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Fields
{
    /// <summary>
    /// How a setting of a built-in type is edited and shown beyond its type, named in its slot's <c>#@ editor</c> attribute: a
    /// percentage is a number from 0 to 100, set with a slider and said with its unit.
    /// </summary>
    public sealed class SettingEditor
    {
        /// <summary>A number from 0 to 100, said with " %", as a value of the game's that is a percentage is.</summary>
        public static readonly SettingEditor Percent = new SettingEditor("percent", "real", "0", "100", " %", texts => texts.SettingKindPercent);

        // Every editor a setting may name, in the order the list of what a setting holds offers them.
        static readonly SettingEditor[] All = { Percent };

        readonly Func<Texts, string> _word;

        SettingEditor(string name, string type, string least, string most, string unit, Func<Texts, string> word)
        {
            Name = name;
            Type = type;
            Least = least;
            Most = most;
            Unit = unit;
            _word = word;
        }

        /// <summary>The name a slot's <c>#@ editor</c> attribute gives it.</summary>
        /// <remarks>Persisted in every program with a setting edited by it.</remarks>
        public string Name { get; }

        /// <summary>The built-in type a setting edited by it holds.</summary>
        public string Type { get; }

        /// <summary>The least the value may be.</summary>
        public string Least { get; }

        /// <summary>The most the value may be.</summary>
        public string Most { get; }

        /// <summary>What is said straight after the value, with its space where it takes one.</summary>
        public string Unit { get; }

        /// <summary>The number of editors a setting may name.</summary>
        public static int Count => All.Length;

        /// <summary>The editor at <paramref name="index" />, in the order the list of what a setting holds offers them.</summary>
        public static SettingEditor At(int index)
        {
            return All[index];
        }

        /// <summary>Returns the editor called <paramref name="name" />, or null where it is empty or no editor this build knows.</summary>
        public static SettingEditor Find(string name)
        {
            foreach (var editor in All)
            {
                if (editor.Name == name)
                    return editor;
            }

            return null;
        }

        /// <summary>Returns the editor whose value is what <paramref name="field" /> asks for, by its type, bounds and unit, or null where none is.</summary>
        public static SettingEditor Of(Field field)
        {
            var type = SettingTypes.Of(field);
            foreach (var editor in All)
            {
                if (editor.Type == type && editor.Least == field.Least && editor.Most == field.Most && editor.Unit == (field.Unit ?? ""))
                    return editor;
            }

            return null;
        }

        /// <summary>What it is called in the list of what a setting holds, in the words of <paramref name="texts" />.</summary>
        public string Word(Texts texts)
        {
            return _word(texts);
        }

        /// <summary>Returns <paramref name="field" />, a box asking for a value of <see cref="Type" />, with its bounds and its unit.</summary>
        public Field Shaped(Field field)
        {
            field.Least = Least;
            field.Most = Most;
            field.Unit = Unit;
            return field;
        }

        /// <summary>Returns <paramref name="value" /> as a person reads it, with its unit.</summary>
        public string Said(Literal value)
        {
            return value.Said + Unit;
        }

        /// <summary>Returns <paramref name="value" />, a value of the setting <paramref name="part" />, as a person reads it: with its editor's unit where it has one.</summary>
        public static string Said(Part part, Literal value)
        {
            var editor = Find(part.Editor);
            return editor == null ? value.Said : editor.Said(value);
        }
    }
}
