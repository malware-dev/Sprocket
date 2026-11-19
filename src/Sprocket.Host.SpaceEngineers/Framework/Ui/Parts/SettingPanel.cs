using Sprocket.Host.SpaceEngineers.Features.Slots.Edits;
using Sprocket.Host.SpaceEngineers.Features.States;
using Sprocket.Host.SpaceEngineers.Framework.Editing;
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
    /// <summary>
    /// The panel a setting is made or opened in, asking its name, what it holds and its default, and the panel an installation's
    /// value of a setting is given in.
    /// </summary>
    /// <remarks>
    /// The name, what the setting holds and the default stand in every form of the panel in the same places, and what the setting holds
    /// is shown, and cannot be chosen, wherever it is already decided, so the panel keeps its size.
    /// </remarks>
    public sealed class SettingPanel
    {
        /// <summary>The height kept under the default for the line saying it may be left empty.</summary>
        const float HintHeight = 24f;

        /// <summary>The room above and below a switch, which a box or a list has by being taller, so every kind of default stands in the same room.</summary>
        const float Pad = 2f;

        static readonly string[] Kinds = { "flag", "integer", "real", "text", "duration" };

        readonly SprocketEditor _editor;
        readonly AutomationSystem _system;
        readonly bool _free;
        readonly Part _part;
        readonly Action<Part, DeclaredType> _made;

        ValuesQuestion _question;
        List<Field> _fields;
        string _type;
        DeclaredType _typeMade;

        SettingPanel(SprocketEditor editor, AutomationSystem system, Part part, bool free, string type, Action<Part, DeclaredType> made)
        {
            _editor = editor;
            _system = system;
            _part = part;
            _free = free;
            _type = type;
            _made = made;
        }

        /// <summary>The kind words the list of what a setting holds starts with, in the order of the language's built-in types.</summary>
        public static List<string> KindWords(Texts texts)
        {
            return new List<string> { texts.StateFlag, texts.SettingKindInteger, texts.SettingKindReal, texts.SettingKindText, texts.SettingKindDuration };
        }

        /// <summary>
        /// A panel asking for a new setting of <paramref name="system" />: what it holds is chosen in it where <paramref name="type" />
        /// is null, and is <paramref name="type" /> otherwise, shown but not changed. <paramref name="made" /> is given the setting, and the
        /// state type made for it where one was, once it is done, and <paramref name="closed" /> is called as the panel closes either way.
        /// </summary>
        public static ValuesQuestion New(SprocketEditor editor, AutomationSystem system, string type, Literal start, Func<Element> mark,
            Action<Part, DeclaredType> made, Action closed)
        {
            var panel = new SettingPanel(editor, system, null, type == null, type ?? "text", made);
            return panel.Open(mark, start, closed);
        }

        /// <summary>A panel opened on <paramref name="part" />, a setting of <paramref name="system" />, asking its name and its default.</summary>
        public static ValuesQuestion Edit(SprocketEditor editor, AutomationSystem system, Part part, Func<Element> mark, Action closed)
        {
            var panel = new SettingPanel(editor, system, part, false, part.Setting, null);
            return panel.Open(mark, part.HasDefault ? Shown(part.Setting, part.Default) : default(Literal), closed);
        }

        /// <summary>
        /// A panel giving <paramref name="instance" />'s value of <paramref name="part" />, a setting of <paramref name="system" />, which
        /// starts at the value it has now; where the setting has a default it offers using that.
        /// </summary>
        public static ValuesQuestion Value(SprocketEditor editor, AutomationSystem system, SystemInstance instance, Part part, Func<Element> mark)
        {
            var texts = editor.Texts;
            var type = part.Setting;
            Literal now;
            if (!instance.Gives(part.Id, out now))
                now = Initial(system, null, type);

            var field = DefaultField(texts, system, null, type, texts.SettingValue, false);
            var fields = new[] { field };
            var question = new ValuesQuestion(editor, null, fields, new[] { Shown(type, now) }, mark, (said, cancelled) =>
            {
                if (cancelled)
                    return;

                var values = FieldSet.Read(said, fields);
                if (values.Count > 0)
                    editor.Do(system, new AnswerSetting(instance, part, true, Held(type, values[0])));
            });
            if (part.HasDefault)
                question.Defaults(texts.SettingUseDefault, () => editor.Do(system, new AnswerSetting(instance, part, false, default(Literal))));

            question.Hang();
            return question;
        }

        ValuesQuestion Open(Func<Element> mark, Literal start, Action closed)
        {
            var texts = _editor.Texts;
            var name = Field.Called(texts.SettingName);
            name.Entry = FieldInput.Text;
            name.Needed = true;

            _fields = Fields(name);
            var given = new[]
            {
                Literal.Of(_part != null ? _part.Id : _system.Free(texts.SettingNumbered)),
                Literal.Name(Word(_type)),
                _part != null || !_free ? start : Initial(_system, _typeMade, _type)
            };

            var fields = _fields;
            _question = new ValuesQuestion(_editor, null, fields, given, mark, (said, cancelled) =>
            {
                closed();
                if (!cancelled)
                    Done(said);
            });
            _question.Form.Edited += Chosen;
            Decorate();
            _question.Hang();
            return _question;
        }

        /// <summary>The name, what the setting holds and the default, as fields.</summary>
        List<Field> Fields(Field name)
        {
            var texts = _editor.Texts;
            var words = Words();
            var holds = Field.Among(texts.SettingHolds, words, words);
            holds.Entry = FieldInput.Name;
            holds.Needed = true;
            holds.Searched = true;
            holds.Stacked = true;
            holds.About = texts.Codex.SettingHoldsAbout;
            var value = DefaultField(texts, _system, _typeMade, _type, texts.SettingDefault, true);
            value.Stacked = true;
            name.Stacked = true;
            return new List<Field> { name, holds, value };
        }

        /// <summary>The words what a setting holds is chosen among: the built-in kinds, the logic system's state types, and the way to make a new one; the one word shown where it is already decided.</summary>
        List<string> Words()
        {
            if (!_free)
                return new List<string> { Word(_type) };

            var texts = _editor.Texts;
            var words = KindWords(texts);
            foreach (var type in _system.Types)
                words.Add(type.Name);

            if (_typeMade != null && _system.FindType(_typeMade.Name) == null)
                words.Add(_typeMade.Name);

            words.Add(texts.SettingNewType);
            return words;
        }

        /// <summary>What <paramref name="type" />, a type a setting holds, is called in the list of what it holds.</summary>
        string Word(string type)
        {
            var at = Array.IndexOf(Kinds, type);
            return at < 0 ? type : KindWords(_editor.Texts)[at];
        }

        /// <summary>The type of the setting that <paramref name="word" /> stands for in the list of what it holds, or null for the way to make a new one.</summary>
        string TypeOf(string word)
        {
            var kinds = KindWords(_editor.Texts);
            var at = kinds.IndexOf(word);
            return word == _editor.Texts.SettingNewType ? null : at < 0 ? word : Kinds[at];
        }

        /// <summary>Draws the lines in the list of what the setting holds, shows that where it is decided as not to be changed, and says the default may be left empty where it may.</summary>
        void Decorate()
        {
            var holds = _question.Form.Fields[1] as ListField;
            if (holds != null)
            {
                var kinds = Kinds.Length;
                var words = Words();
                if (_free)
                {
                    if (words.Count > kinds + 1)
                        holds.Rule(kinds);

                    holds.Rule(words.Count - 1);
                }
                else
                    holds.IsEnabled = false;
            }

            var value = _question.Form.Fields[2] as SwitchField;
            if (value != null)
                value.Base.Padding = new Thickness(0f, Pad, 0f, Pad);

            var texts = _editor.Texts;
            var empty = _type != "flag" && _system.FindType(_type) == null && (_typeMade == null || _typeMade.Name != _type);
            _question.Hint(empty ? texts.SettingDefaultHint : "", HintHeight);
        }

        /// <summary>Asks the default again, and makes a new state type, where what the setting holds was changed.</summary>
        void Chosen()
        {
            if (!_free)
                return;

            var texts = _editor.Texts;
            var form = _question.Form;
            var word = form.Fields[1].Text;
            if (word == Word(_type))
                return;

            var type = TypeOf(word);
            if (type == null)
            {
                _typeMade = TypeEditing.New(texts, _system.Types.Count, Taken);
                type = _typeMade.Name;
            }

            _type = type;
            var name = form.Fields[0].Text;
            _fields = Fields(_fields[0]);
            var filled = new List<string> { name, Word(_type), Shown(_type, Initial(_system, _typeMade, _type)).Said };
            form.Show(_fields, filled, null, texts);
            Decorate();
        }

        bool Taken(string name)
        {
            return _system.HasTypeInAnyCase(name) || _system.HasStateInAnyCase(name) || _system.FindPartInAnyCase(name) != null
                   || StateReader.IsBuiltInInAnyCase(name);
        }

        /// <summary>Makes the setting, or changes the one opened, from what the panel says.</summary>
        void Done(string said)
        {
            var values = FieldSet.Read(said, _fields);
            var name = values.Count > 0 ? values[0].Text.Trim() : "";
            var hasDefault = values.Count > 2 && (values[2].Kind != ValueKind.Text || values[2].Text.Trim().Length > 0);
            var value = hasDefault ? Held(_type, values[2]) : default(Literal);

            if (_part != null)
            {
                Change(name, hasDefault, value);
                return;
            }

            var part = new Part(_system.Free(name.Length > 0 ? name : _editor.Texts.SettingNumbered))
            {
                Setting = _type,
                Optional = hasDefault,
                HasDefault = hasDefault,
                Default = value
            };
            _made(part, _typeMade != null && _typeMade.Name == _type && _system.FindType(_type) == null ? _typeMade : null);
        }

        /// <summary>Renames the setting opened, and changes its default, as one change.</summary>
        void Change(string name, bool hasDefault, Literal value)
        {
            SystemEdit edit = new SetSettingDefault(_part, hasDefault, value);
            if (name.Length > 0 && name != _part.Id)
            {
                var wanted = _system.FindPartInAnyCase(name) == _part ? name : _system.Free(name);
                if (wanted != _part.Id)
                    edit = new WithMade(new RenamePart(_part, wanted), edit);
            }

            if (edit.Changes || edit is WithMade)
                _editor.Do(_system, edit);
        }

        /// <summary>The field asking a setting holding <paramref name="type" /> for a value, called <paramref name="called" />: one of a declared type's choices, on or off, a length in seconds, a number or text; where <paramref name="mayBeEmpty" />, one that may be left empty.</summary>
        static Field DefaultField(Texts texts, AutomationSystem system, DeclaredType made, string type, string called, bool mayBeEmpty)
        {
            var declared = system.FindType(type) ?? (made != null && made.Name == type ? made : null);
            if (declared != null)
            {
                var among = Field.Among(called, declared.Choices, declared.Choices);
                among.Entry = FieldInput.Name;
                return among;
            }

            if (type == "flag")
            {
                var either = Field.OneOf(called, Literal.Of(true).Said, Literal.Of(false).Said);
                either.Edits = FieldEditor.Chosen;
                return either;
            }

            var box = Field.Called(called);
            if (mayBeEmpty)
                box.Default = "";
            else
                box.Needed = true;

            box.Entry = type == "integer" ? FieldInput.Whole : type == "real" || type == "duration" ? FieldInput.Decimal : FieldInput.Text;
            if (type == "duration")
                box.Unit = Template.Fill(texts.WaitPickSeconds, "count", "");

            return box;
        }

        /// <summary>The value a setting holding <paramref name="type" /> starts at in a panel that has none to show: off, the first choice, or nothing.</summary>
        static Literal Initial(AutomationSystem system, DeclaredType made, string type)
        {
            if (type == "flag")
                return Literal.Of(false);

            var declared = system.FindType(type) ?? (made != null && made.Name == type ? made : null);
            return declared != null && declared.Choices.Count > 0 ? Literal.Name(declared.Choices[0]) : Literal.Of("");
        }

        /// <summary><paramref name="start" /> as the default's field shows it: a length in seconds.</summary>
        static Literal Shown(string type, Literal start)
        {
            return type == "duration" && start.Kind == ValueKind.Duration ? Literal.Of(start.Length.Seconds) : start;
        }

        /// <summary>A value given in its field, <paramref name="given" />, as a value of <paramref name="type" />: a length from its seconds.</summary>
        static Literal Held(string type, Literal given)
        {
            return type == "duration" ? Literal.Of(Duration.OfSeconds(given.Number)) : given;
        }
    }
}
