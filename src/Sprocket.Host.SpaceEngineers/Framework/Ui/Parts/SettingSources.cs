using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using System;
using System.Collections.Generic;
using Lumel;

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
                editor.Answer(name, () => chosen(name, said), name == now);
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
            var shaped = SettingEditor.Of(field);
            Offer(editor, system, kind => SettingTypes.Fits(field, kind), form.SettingAt(at),
                (setting, said) => form.Choose(at, setting, said),
                () => making(New(editor, system, shaped != null ? shaped.Name : type, Read(type, form.ValueAt(at)), () => mark,
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
                else if (at < given.Count && given[at].IsExpression)
                    form.Hold(i, given[at].Tree.Written);

                at += fields[i].Covers;
            }
        }

        /// <summary>
        /// A panel, hung from what <paramref name="mark" /> finds, asking for a new setting of <paramref name="system" /> holding
        /// <paramref name="kind" />, a type or an editor's name: its name and its default, starting at <paramref name="start" />, where
        /// a default left empty makes every installation choose it. Made as one change, its name is given to <paramref name="made" />;
        /// <paramref name="closed" /> is called as the panel closes either way.
        /// </summary>
        /// <remarks>Its holder hangs it every tick, as any panel.</remarks>
        public static ValuesQuestion New(SprocketEditor editor, AutomationSystem system, string kind, Literal start, Func<Element> mark,
            Action<string> made, Action closed)
        {
            return SettingPanel.New(editor, system, kind, start, mark, (part, none) =>
            {
                editor.MakeSetting(system, part);
                made(part.Id);
            }, closed);
        }

        /// <summary>The default of the setting <paramref name="setting" /> of <paramref name="system" />, as a person reads it, or empty where it has none.</summary>
        public static string Default(AutomationSystem system, string setting)
        {
            var part = system?.FindPart(setting);
            return part != null && part.HasDefault ? SettingEditor.Said(part, part.Default) : "";
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
    }
}
