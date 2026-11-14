using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System;
using System.Collections.Generic;
using Lumel.Controls;
using Lumel.Layout;

namespace Sprocket.Host.SpaceEngineers.Features.States
{
    /// <summary>
    /// The state selector's form for a state made there: its name, its type, a new type's name and choices where it holds one,
    /// and the value it starts as. Nothing of it reaches the logic system until the pick is done.
    /// </summary>
    public partial class NewStateForm : VStack
    {
        readonly SprocketEditor _editor;
        readonly AutomationSystem _system;
        readonly Func<bool> _showing;
        readonly KeyedChildren<string, NamePlate> _choices;

        string _name = "";
        StateStart _start = StateValueQuestion.Off;
        DeclaredType _made;
        List<Field> _heading;
        string _headAsked;
        string _startsAsked;

        /// <summary>The form for a state made new in <paramref name="system" />, taking edits only while <paramref name="showing" /> says its page is the one shown.</summary>
        public NewStateForm(SprocketEditor editor, AutomationSystem system, Func<bool> showing)
        {
            _editor = editor;
            _system = system;
            _showing = showing;
            CreateView();

            var texts = editor.Texts;
            _head.Edited += HeadEdited;
            _starts.Edited += StartsEdited;
            _choices = new KeyedChildren<string, NamePlate>(_chips.Children, delegate { return new NamePlate(false); });
            _choicesLabel.Text = texts.StateCanBe;
            _addChoice.Text = StatusRow.Plus + texts.StateChoiceAdd;
            _addChoice.Command = editor.Act(CommandKind.AddChoice, delegate
            {
                _made.Choices.Add(NumberedName.Free(texts.StateChoiceNumbered, _made.Choices.Count, _made.Has));
                _editor.Invalidate();
            });
        }

        /// <summary>Raised when the state's type changes, with the type it holds now, empty for on or off.</summary>
        public event Action<string> TypeChanged;

        /// <summary>Raised when one of the new type's choices is renamed, from the name it had to the one it has.</summary>
        public event Action<string, string> ChoiceRenamed;

        /// <summary>Where the state's name and type, and a new type's name, are given.</summary>
        public FieldSet Head => _head;

        /// <summary>The choices of the type being made, one plate each by name, once one is.</summary>
        public KeyedChildren<string, NamePlate> Choices => _choices;

        /// <summary>The button that adds a choice to the type being made.</summary>
        public Button AddChoice => _addChoice;

        /// <summary>Where the value the state starts as is given.</summary>
        public FieldSet Starts => _starts;

        /// <summary>The state as its name, type and start now stand.</summary>
        public State State => new State(Name.Trim(), _start.Type, _start.Value);

        /// <summary>The type being made, where the state holds it, or null.</summary>
        public DeclaredType TypeMade => _made != null && _start.Type == _made.Name ? _made : null;

        /// <summary>Why what is being made cannot be added as it stands: a name taken or missing, for the state or its type; empty where it can.</summary>
        public string Why
        {
            get
            {
                var name = Name.Trim();
                if (name.Length == 0 || _system.TakesInAnyCase(name))
                    return _editor.Texts.PickStateNameTaken;

                var made = TypeMade;
                return made != null && (made.Name.Length == 0 || _system.TakesInAnyCase(made.Name) || string.Equals(made.Name, name, StringComparison.OrdinalIgnoreCase)) ? _editor.Texts.PickTypeNameTaken : "";
            }
        }

        /// <summary>The name of the state being made: what its field holds, once it is asked.</summary>
        string Name => _heading != null ? _head.Fields[0].Text ?? "" : _name;

        /// <summary>The word a new type is made with, at the end of the types.</summary>
        string NewTypeWord => StatusRow.Plus + _editor.Texts.PickNewType;

        /// <summary>The words the type is chosen among: on or off, each type, then the way to make a new one, which stands for the type being made once there is one.</summary>
        List<string> TypeWords
        {
            get
            {
                var words = new List<string> { _editor.Texts.StateFlag };
                foreach (var type in _system.Types)
                    words.Add(type.Name);

                words.Add(NewTypeWord);
                return words;
            }
        }

        /// <summary>Starts a state afresh: a numbered name, on or off, starting off; a type made before is kept to come back to.</summary>
        public void Begin()
        {
            _name = NumberedName.Free(_editor.Texts.StateStateNumbered, _system.States.Count, _system.TakesInAnyCase);
            _start = StateValueQuestion.Off;
        }

        /// <summary>Takes the name its field holds into the form's own, before the form is asked again.</summary>
        public void Keep()
        {
            _name = Name;
        }

        /// <summary>Asks for what has changed since it was last asked: the name and type, the new type's choices, the start.</summary>
        public void Fill()
        {
            FillHead();
            FillChoices();
            FillStarts();
        }

        /// <summary>Asks for the state's name and type, and the type's name where it is a new one, again only where the type chosen changed.</summary>
        void FillHead()
        {
            var made = TypeMade;
            var asked = (made != null ? NewTypeWord : _start.Type) + "#" + (_made != null);
            if (asked == _headAsked)
                return;

            if (_heading != null)
                _name = Name;

            _headAsked = asked;
            var texts = _editor.Texts;
            var name = Field.Called(texts.StateName);
            name.Entry = FieldInput.Text;
            name.About = texts.Codex.PickStateNameAbout;

            var types = TypeWords;
            var type = Field.Among(texts.PickStateType, types, types);
            type.Entry = FieldInput.Name;
            type.About = texts.Codex.PickStateTypeAbout;

            _heading = new List<Field> { name, type };
            var filled = new List<string> { _name, made != null ? NewTypeWord : _start.Type.Length == 0 ? texts.StateFlag : _start.Type };
            if (made != null)
            {
                var named = Field.Called(texts.PickTypeName);
                named.Entry = FieldInput.Text;
                named.About = texts.Codex.PickTypeNameAbout;
                _heading.Add(named);
                filled.Add(made.Name);
            }

            _head.Show(_heading, filled, null, texts);
        }

        /// <summary>Shows the choices of the type being made, where the state holds it.</summary>
        void FillChoices()
        {
            var made = TypeMade;
            _choicesRow.IsVisible = made != null;
            if (made == null)
                return;

            _choices.Show(made.Choices);
            var removes = made.Choices.Count > 2;
            foreach (var choice in made.Choices)
            {
                var chip = _choices[choice];
                var named = choice;
                chip.Text = choice;
                chip.Boxed = removes;
                chip.CanRename = !removes;
                chip.Command = removes
                    ? _editor.Act(CommandKind.ChangeChoice, () => ChoiceMenu(named, chip))
                    : _editor.Act(CommandKind.ChangeChoice, () => _editor.Rename(chip, ChoiceNaming(named)));
                chip.About = removes ? _editor.Texts.Codex.StateChoiceAbout : null;
            }
        }

        /// <summary>Asks for the value the state starts as, among those of its type, again only where they changed.</summary>
        void FillStarts()
        {
            var asked = _start.Type + "#" + Spelled(TypeMade);
            if (asked == _startsAsked)
                return;

            _startsAsked = asked;
            var texts = _editor.Texts;
            var words = new List<string>();
            foreach (var start in StateValueQuestion.Starts(texts, _system, _start.Type, _made))
                words.Add(start.Word);

            var starts = Field.Among(texts.StateStarts, words, words);
            starts.Entry = FieldInput.Name;
            starts.About = texts.Codex.StateStartsAbout;
            _starts.Show(new List<Field> { starts }, new[] { _start.Word }, null, texts);
        }

        /// <summary>Offers renaming the choice <paramref name="choice" /> of the type being made, and removing it.</summary>
        void ChoiceMenu(string choice, NamePlate chip)
        {
            var texts = _editor.Texts;
            _editor.AskHere("");
            _editor.Answer(texts.StateRename, () => _editor.Rename(chip, ChoiceNaming(choice)));
            _editor.Answer(texts.StateRemoveChoice, delegate
            {
                _made.Choices.Remove(choice);
                if (_start.Type == _made.Name && _start.Value.Text == choice)
                    _start = Start(_made.Name, _made.Choices[0]);

                _editor.Invalidate();
            });
        }

        /// <summary>The name of the new type's choice <paramref name="choice" />, given where the type has no other choice by it.</summary>
        Naming ChoiceNaming(string choice)
        {
            return TypeEditing.ChoiceNaming(_made, choice, wanted => Renamed(choice, wanted));
        }

        /// <summary>The way <paramref name="type" /> is spelled, its name and its choices, to tell whether what is asked about it has changed.</summary>
        static string Spelled(DeclaredType type)
        {
            return type == null ? "" : type.Name + "|" + string.Join("|", type.Choices);
        }

        /// <summary>The start that is <paramref name="choice" /> of the type called <paramref name="type" />, among those offered now.</summary>
        StateStart Start(string type, string choice)
        {
            foreach (var start in StateValueQuestion.Starts(_editor.Texts, _system, null, _made))
            {
                if (start.Type == type && start.Value.Text == choice)
                    return start;
            }

            return StateValueQuestion.Off;
        }

        /// <summary>
        /// Takes the type chosen, and the new type's name where it is one: another type starts as its first value, or off for on
        /// or off; the way to a new type makes one, two choices named as the State tab names them.
        /// </summary>
        void HeadEdited()
        {
            if (!_showing() || _heading == null)
                return;

            var texts = _editor.Texts;
            var typeWord = _head.Fields[1].Text ?? "";
            if (typeWord == NewTypeWord && _made == null)
            {
                var name = Name.Trim();
                _made = TypeEditing.New(texts, _system.Types.Count, taken => _system.TakesInAnyCase(taken) || string.Equals(taken, name, StringComparison.OrdinalIgnoreCase));
            }

            var type = typeWord == texts.StateFlag ? "" : typeWord == NewTypeWord ? _made.Name : typeWord;
            if (_made != null && type == _made.Name && _head.Fields.Count > 2)
            {
                var wanted = (_head.Fields[2].Text ?? "").Trim();
                if (wanted.Length != 0 && wanted != _made.Name)
                {
                    var starts = _start.Type == _made.Name;
                    _made.Name = wanted;
                    type = wanted;
                    if (starts)
                        _start = Start(wanted, _start.Value.Text);
                }
            }

            _name = Name;
            if (type != _start.Type)
            {
                var starts = StateValueQuestion.Starts(texts, _system, type, _made);
                _start = type.Length == 0 ? StateValueQuestion.Off : starts[0];
                TypeChanged?.Invoke(type);
            }

            _editor.Invalidate();
        }

        /// <summary>Takes the value the state starts as.</summary>
        void StartsEdited()
        {
            if (!_showing())
                return;

            var word = _starts.Fields[0].Text ?? "";
            foreach (var start in StateValueQuestion.Starts(_editor.Texts, _system, _start.Type, _made))
            {
                if (start.Word == word)
                    _start = start;
            }

            _editor.Invalidate();
        }

        /// <summary>Calls the new type's choice <paramref name="was" /> <paramref name="wanted" />, which the type has no other choice by.</summary>
        void Renamed(string was, string wanted)
        {
            var at = _made.Choices.IndexOf(was);
            if (at < 0)
                return;

            _made.Choices[at] = wanted;
            if (_start.Type == _made.Name)
                _start = Start(_made.Name, _start.Value.Text == was ? wanted : _start.Value.Text);

            ChoiceRenamed?.Invoke(was, wanted);
            _editor.Invalidate();
        }
    }
}
