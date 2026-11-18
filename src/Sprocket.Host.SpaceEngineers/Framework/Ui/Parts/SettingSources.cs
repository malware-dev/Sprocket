using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Parts
{
    /// <summary>Where a value comes from: the menu under its mark, and the panel a new setting for it is asked in.</summary>
    public static class SettingSources
    {
        /// <summary>
        /// Asks under the mark just pressed where a value comes from: a fixed value, a new setting, or, after a line, each
        /// setting of <paramref name="system" /> <paramref name="fits" /> takes, with its default; the one it comes from, <paramref name="now" />,
        /// marked. <paramref name="chosen" /> is given the setting chosen and its default as said, or two empty texts for a fixed
        /// value, and <paramref name="fresh" /> is carried out for a new one.
        /// </summary>
        public static void Offer(SprocketEditor editor, AutomationSystem system, Func<string, bool> fits, string now, Action<string, string> chosen,
            Action fresh)
        {
            var texts = editor.Texts;
            editor.AskHere("");
            editor.Answer(texts.ValueFixed, () => chosen("", ""), now.Length == 0);
            editor.Answer(texts.ValueNewSetting, fresh);

            var ruled = false;
            foreach (var part in system.Parts)
            {
                if (!part.IsSetting || !fits(part.Setting))
                    continue;

                if (!ruled)
                {
                    editor.AnswerRule();
                    ruled = true;
                }

                var name = part.Id;
                var said = Default(system, name);
                editor.Answer(said.Length == 0 ? name : $"{name}, {Template.Fill(texts.ValueByDefault, "value", said)}", () => chosen(name, said), name == now);
            }
        }

        /// <summary>
        /// Asks under <paramref name="mark" />, the mark of the value at <paramref name="at" /> in <paramref name="form" />, where the
        /// value comes from among <paramref name="system" />'s settings; a new setting's panel is handed to <paramref name="making" />,
        /// and null once it closes.
        /// </summary>
        public static void Source(SprocketEditor editor, AutomationSystem system, FieldSet form, int at, Element mark, Action<ValuesQuestion> making)
        {
            var field = form.Field(at);
            var type = SettingTypes.Of(field);
            Offer(editor, system, kind => SettingTypes.Fits(field, kind), form.SettingAt(at),
                (setting, said) => form.Choose(at, setting, said),
                () => making(New(editor, system, type, Read(type, form.ValueAt(at)), () => mark,
                    made => form.Choose(at, made, Default(system, made)), () => making(null))));
        }

        /// <summary>Shows in <paramref name="form" /> a chip in place of each of <paramref name="fields" /> that <paramref name="given" /> gives one of <paramref name="system" />'s settings.</summary>
        public static void Given(FieldSet form, AutomationSystem system, IList<Field> fields, IList<Literal> given)
        {
            var at = 0;
            for (var i = 0; i < fields.Count; i++)
            {
                if (at < given.Count && given[at].IsSetting)
                    form.Choose(i, given[at].Text, Default(system, given[at].Text));

                at += fields[i].Covers;
            }
        }

        /// <summary>
        /// A panel, hung from what <paramref name="mark" /> finds, asking for a new setting of <paramref name="system" /> holding
        /// <paramref name="type" />: its name and its default, starting at <paramref name="start" />, where a default
        /// left empty makes every installation choose it. Made as one change, its name is given to <paramref name="made" />; <paramref name="closed" />
        /// is called as the panel closes either way.
        /// </summary>
        /// <remarks>Its holder hangs it every tick, as any panel.</remarks>
        public static ValuesQuestion New(SprocketEditor editor, AutomationSystem system, string type, Literal start, Func<Element> mark,
            Action<string> made, Action closed)
        {
            var texts = editor.Texts;
            var name = Field.Called(texts.SettingName);
            name.Entry = FieldInput.Text;
            name.Needed = true;
            var fallback = Defaulted(texts, system, type);

            var fields = new[] { name, fallback };
            var given = new[] { Literal.Of(system.Free(texts.SettingNumbered)), Shown(type, start) };
            var question = new ValuesQuestion(editor, null, fields, given, mark, (said, cancelled) =>
            {
                closed();
                if (cancelled)
                    return;

                var values = FieldSet.Read(said, fields);
                var part = new Part(system.Free(values.Count > 0 && values[0].Text.Trim().Length > 0 ? values[0].Text.Trim() : texts.SettingNumbered))
                {
                    Setting = type,
                    Optional = values.Count > 1 && (values[1].Kind != ValueKind.Text || values[1].Text.Trim().Length > 0)
                };

                if (part.Optional)
                {
                    part.Default = Held(type, values[1]);
                    part.HasDefault = true;
                }

                editor.MakeSetting(system, part);
                made(part.Id);
            });
            question.Hang();
            return question;
        }

        /// <summary>The default of the setting <paramref name="setting" /> of <paramref name="system" />, as a person reads it, or empty where it has none.</summary>
        public static string Default(AutomationSystem system, string setting)
        {
            var part = system?.FindPart(setting);
            return part != null && part.HasDefault ? part.Default.Said : "";
        }

        /// <summary>What a value a person typed reads as, as one of <paramref name="type" />, or a value of its own where it reads as none.</summary>
        public static Literal Read(string type, string said)
        {
            if (type == "text")
                return Literal.Of(said);

            var values = new List<Literal>();
            ValueLine.Read(said, values);
            return values.Count == 1 ? values[0] : type == "flag" ? Literal.Of(false) : Literal.Of(0L);
        }

        /// <summary>The field a new setting of <paramref name="system" /> holding <paramref name="type" /> asks its default in: one of a declared type's choices, a length in seconds.</summary>
        static Field Defaulted(Texts texts, AutomationSystem system, string type)
        {
            var declared = system.FindType(type);
            if (declared != null)
            {
                var among = Field.Among(texts.SettingDefault, declared.Choices, declared.Choices);
                among.Entry = FieldInput.Name;
                return among;
            }

            if (type == "flag")
            {
                var either = Field.OneOf(texts.SettingDefault, Literal.Of(true).Said, Literal.Of(false).Said);
                either.Edits = FieldEditor.Chosen;
                return either;
            }

            var box = Field.Called(texts.SettingDefault);
            // May be left empty, which makes the setting one every installation chooses.
            box.Default = "";
            box.Entry = type == "integer" ? FieldInput.Whole : type == "real" || type == "duration" ? FieldInput.Decimal : FieldInput.Text;
            if (type == "duration")
                box.Unit = Template.Fill(texts.WaitPickSeconds, "count", "");

            return box;
        }

        /// <summary><paramref name="start" /> as the default's field shows it: a length in seconds.</summary>
        static Literal Shown(string type, Literal start)
        {
            return type == "duration" && start.Kind == ValueKind.Duration ? Literal.Of(start.Length.Seconds) : start;
        }

        /// <summary>A default given in its field, <paramref name="given" />, as a value of <paramref name="type" />: a length from its seconds.</summary>
        static Literal Held(string type, Literal given)
        {
            return type == "duration" ? Literal.Of(Duration.OfSeconds(given.Number)) : given;
        }
    }
}
