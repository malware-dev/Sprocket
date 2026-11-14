using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Features.Triggers;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Tips;
using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.States
{
    /// <summary>
    /// One of a logic system's states and a value for it, side by side: the states, then what the state is set to or how it
    /// compares and with what, then, for a trigger, what it does. A new state can be made here, named and given the value it
    /// starts at as on the State tab, and a new type for it, named and given its choices as on the State tab.
    /// </summary>
    public sealed class StatePage : EditorPage
    {
        // The row that makes a new state, keyed apart from every state's name, which never holds this character.
        const string NewRow = "\u0000";

        readonly AutomationSystem _system;
        readonly string _lead;
        readonly bool _sets;
        readonly OutcomeColumn _aim;
        readonly Action<StateTest, NewState> _done;
        readonly List<string> _keys = new List<string>();

        string _state;
        bool _making;
        string _word;
        string _copy;
        string _seenWord;
        Relation _relation;
        string _valueAsked;
        bool _goesOn;
        PickStrip _strip;
        StageColumn _states;
        StageColumn _define;
        StageColumn _value;
        StageColumn _does;
        KeyedChildren<string, PickRow> _rows;
        NewStateForm _newState;
        FieldSet _form;
        Label _othersHeading;
        VStack _othersList;
        KeyedChildren<string, PickRow> _others;
        readonly List<string> _otherKeys = new List<string>();
        List<Field> _asking;
        ICommand _finish;

        /// <summary>
        /// The states of <paramref name="system" />, opened on <paramref name="start" /> where there is one, its sentence led by
        /// <paramref name="lead" />, asking what the state is set to where <paramref name="sets" /> says so and how it compares
        /// otherwise, then what a trigger does in <paramref name="aim" /> where it is not null; hands what is chosen to
        /// <paramref name="done" />, a set as the test it makes hold, with the state and type made here where they were, and null
        /// otherwise.
        /// </summary>
        public StatePage(SprocketEditor editor, AutomationSystem system, string lead, bool sets, StateTest start, OutcomeColumn aim,
            Action<StateTest, NewState> done) : base(editor)
        {
            _system = system;
            _lead = lead;
            _sets = sets;
            _aim = aim;
            _done = done;
            _state = start?.State ?? "";
            _relation = start?.Relation ?? Relation.Is;
            var state = system.FindState(_state);
            _copy = start?.Other ?? "";
            _word = start == null || state == null || _copy.Length != 0 ? "" : start.Value.Said;

            if (aim == null)
                return;

            aim.Onward = Primary;
            aim.MovesOn = delegate
            {
                if (_finish != null && CanPrimary)
                    _finish.Execute();
            };
        }

        /// <inheritdoc />
        public override string Title
        {
            get
            {
                var said = SentenceText.Capitalised(_lead);
                var state = Chosen;
                if (state == null)
                    return $"{said} {SentenceText.Empty}";

                var word = Copy.Length != 0 ? Copy : Word;
                var value = word.Length == 0 ? SentenceText.Empty : word;
                var name = state.Name.Length == 0 ? SentenceText.Empty : state.Name;
                if (_sets)
                    return $"{said} {name} to {value}";

                return $"{said} {name} {ComparisonQuestion.Said(Relating)} {value}";
            }
        }

        /// <inheritdoc />
        public override bool Asks => true;

        /// <inheritdoc />
        public override string PrimaryText => _aim == null || _aim.Outcome != Outcome.One ? Editor.Texts.ChromeDone : "";

        /// <inheritdoc />
        public override bool CanPrimary => Chosen != null && Why.Length == 0 && (Word.Length != 0 || Copy.Length != 0) && (_aim == null || _aim.Finishes);

        /// <inheritdoc />
        public override string PrimaryAbout => "";

        /// <inheritdoc />
        public override string PrimaryWaits => Editor.Texts.Codex.PickDoneFirst;

        /// <inheritdoc />
        public override bool ScrollsItself => true;

        /// <summary>The strip the pick is drawn in, which the page the single action is chosen on goes on in.</summary>
        public PickStrip Strip => _strip;

        /// <summary>
        /// The choice between this and a selector of blocks, offered in the heading of the column of states, or null where the
        /// pick is about states alone. Set before the page is shown.
        /// </summary>
        public SourceSwitch Source { get; set; }

        /// <summary>Where a new state's name and type, and a new type's name, are given, once the page is built.</summary>
        public FieldSet Head => _newState.Head;

        /// <summary>The choices of the type being made for a new state, one chip each by name, once one is.</summary>
        public KeyedChildren<string, NamePlate> Choices => _newState.Choices;

        /// <summary>The button that adds a choice to the type being made.</summary>
        public Button AddChoice => _newState.AddChoice;

        /// <summary>Where a new state's starting value is given, once the page is built.</summary>
        public FieldSet Starts => _newState.Starts;

        /// <summary>The other states whose value can be taken, one row each by name.</summary>
        public KeyedChildren<string, PickRow> Others => _others;

        /// <summary>Where the value of the state chosen is given, once the page is built.</summary>
        public FieldSet Form => _form;

        /// <summary>
        /// The state chosen, or null while none is: one of the system's, or the one being made, as its name and start now stand.
        /// </summary>
        State Chosen
        {
            get
            {
                return _making ? _newState.State : _system.FindState(_state);
            }
        }

        /// <summary>The type being made, where the state being made holds it, or null.</summary>
        DeclaredType TypeMade => _making ? _newState.TypeMade : null;

        /// <summary>Why what is being made cannot be added as it stands: a name taken or missing, for the state or its type; empty where it can.</summary>
        string Why => _making ? _newState.Why : "";

        /// <summary>Where the value's field stands among those asked: after how the state compares, where it is tested.</summary>
        int ValueAt => _sets ? 0 : 1;

        /// <summary>The word the value is given in: what its field holds, once it is asked.</summary>
        string Word => _asking == null ? _word : _form.Fields[ValueAt].Text ?? "";

        /// <summary>The other state whose value is taken, or empty where a value of its own is.</summary>
        string Copy => _copy;

        /// <summary>How the state is compared: what the first field holds, once it is asked.</summary>
        Relation Relating
        {
            get
            {
                if (_sets || _asking == null)
                    return _relation;

                return ComparisonQuestion.Read(Literal.Name(_asking[0].Meaning(_form.Fields[0].Text ?? "")));
            }
        }

        /// <inheritdoc />
        public override void Primary()
        {
            var state = Chosen;
            var copy = Copy;
            var value = default(Literal);
            if (state == null || Why.Length != 0 || copy.Length == 0 && !StateValueQuestion.Read(_system, state, Word, out value, TypeMade))
                return;

            var test = new StateTest { State = state.Name, Relation = Relating, Value = value, Other = copy };
            _done(test, _making ? new NewState(state, TypeMade) : null);
        }

        /// <inheritdoc />
        public override void Build()
        {
            var texts = Editor.Texts;
            if (_strip == null)
            {
                _strip = new PickStrip(texts);
                _finish = Editor.Act(CommandKind.Primary, Primary);

                _states = new StageColumn(_strip.Width, false, null, texts);
                _states.Title = Source == null ? texts.PickStates : texts.PickSource;
                _states.TitleTerm = Source == null ? CodexPages.LogicSystemStates : null;
                _states.Explain(Editor.Codex);
                if (Source != null)
                    _states.Heads(Source.Box(PickSource.States));

                _rows = new KeyedChildren<string, PickRow>(_states.Rows, Row);

                _define = new StageColumn(_strip.Width, false, null, texts);
                _define.Title = texts.PickNewState;
                _value = new StageColumn(_strip.Width, false, null, texts);
                _value.Title = _sets ? texts.PickStateSetTo : _aim == null ? texts.PickStateCheck : texts.PickStateWatch;
                _newState = new NewStateForm(Editor, _system, () => Editor.Current == this);
                _newState.TypeChanged += type =>
                {
                    // What the step sets belongs to the type before; an on or off state holds the off its switch shows.
                    _word = type.Length == 0 ? Literal.Of(false).Said : "";
                    _asking = null;
                };
                _newState.ChoiceRenamed += (was, now) =>
                {
                    if (_word == was)
                        _word = now;
                };
                _form = new FieldSet();
                _form.Edited += Edited;
                _form.Submitted += delegate
                {
                    if (_finish != null && CanPrimary)
                        _finish.Execute();
                };

                _define.Options.Add(_newState);
                // The other states whose value can be taken are a row of the form under the value, each as the States column
                // shows a state.
                _othersList = new VStack { IsVisible = false };
                _others = new KeyedChildren<string, PickRow>(_othersList.Children, Other);
                _othersHeading = new Label { Style = Styles.ValueLabel, Text = texts.PickStateValueOf, Tip = CodexTip.For(null, texts.Codex.PickStateValueOfAbout) };
                _value.Options.Add(_form);

                if (_aim != null)
                {
                    _does = new StageColumn(_strip.Width, false, null, texts);
                    _does.Title = texts.PickWhatItDoes;
                }
            }

            if (_aim != null)
                _aim.Pick = _strip;

            // The columns are laid again on every build: the new state's comes and goes with it, and whatever a page after this
            // one asked goes too.
            _strip.Return(0, 0);
            _strip.Add(_states);
            if (_making)
                _strip.Add(_define);

            _strip.Add(_value);
            if (_does != null)
                _strip.Add(_does);

            _strip.Returns = null;
            _strip.ShowsArguments = false;
            _strip.Onward = _aim != null && _aim.Outcome == Outcome.One ? Editor.Act(CommandKind.PageOn, Primary) : null;

            FillStates();
            FillValue();
            if (_aim != null)
                _aim.Fill(_does);

            _strip.Show();

            if (!_goesOn)
                return;

            _goesOn = false;
            Primary();
        }

        /// <summary>Goes straight on to the page after this one as soon as this is built.</summary>
        public void GoOn()
        {
            _goesOn = true;
        }

        /// <inheritdoc />
        public override void Refresh()
        {
            _strip.Tick();
        }

        /// <summary>The strip of the pick, which the page the single action is chosen on shares.</summary>
        protected override Element CreateView()
        {
            return _strip;
        }

        /// <summary>A row for the state <paramref name="key" /> names, or for a new one, choosing it when pressed and going on when pressed twice.</summary>
        PickRow Row(string key)
        {
            var row = new PickRow { Mark = Symbols.State };
            row.Picked += delegate(HeldModifier held, int presses)
            {
                var choose = Editor.Act(CommandKind.PickChoice, () => Choose(key));
                if (choose == null)
                    return;

                choose.Execute();
                if (presses > 1 && _finish != null && CanPrimary)
                    _finish.Execute();
            };
            return row;
        }

        /// <summary>Chooses the state <paramref name="key" /> names, or a new one, keeping the value where the state takes it too.</summary>
        void Choose(string key)
        {
            var making = key == NewRow;
            if (making == _making && (making || key == _state))
                return;

            Keep();
            _making = making;
            _state = making ? "" : key;
            if (making)
                _newState.Begin();

            var state = Chosen;
            if (state != null && !StateValueQuestion.Words(_system, state, TypeMade).Contains(_word))
                _word = "";

            if (state != null && !StateValueQuestion.Others(_system, state).Contains(_copy))
                _copy = "";

            // A switch always shows one of its two words, so an on or off state holds the one it will show: off.
            if (state != null && state.IsFlag && _word.Length == 0 && _copy.Length == 0)
                _word = Literal.Of(false).Said;

            // The value's fields belong to the state chosen before until the page is built again, so what they hold is let go.
            _asking = null;

            Editor.Invalidate();
        }

        /// <summary>Takes what the fields hold into the page's own, before they are asked again for another state.</summary>
        void Keep()
        {
            _word = Word;
            _copy = Copy;
            _relation = Relating;
            _newState.Keep();
        }

        /// <summary>Lists the system's states, the one chosen lit, each with what it holds under its name, then the row that makes a new one.</summary>
        void FillStates()
        {
            _keys.Clear();
            foreach (var state in _system.States)
                _keys.Add(state.Name);

            _keys.Add(NewRow);
            _rows.Show(_keys);
            foreach (var state in _system.States)
            {
                var row = _rows[state.Name];
                row.Text = state.Name;
                row.Secondary = state.IsFlag ? Editor.Texts.StateFlag : state.Type;
                row.IsChosen = !_making && state.Name == _state;
                row.Show();
            }

            var made = _rows[NewRow];
            made.Text = Editor.Texts.PickNewState;
            made.IsChosen = _making;
            made.Show();

            _states.ShowEmpty();
        }

        /// <summary>
        /// Asks for the value of the state chosen, headed by its name; for a new state its name and start first, and the type
        /// being made for it where it holds that. Each part is asked again only when what it asks for changes.
        /// </summary>
        void FillValue()
        {
            var state = Chosen;
            var texts = Editor.Texts;
            _define.Note = Why;
            _form.IsVisible = state != null;
            _define.ShowEmpty();
            _value.ShowEmpty();

            if (_making)
                _newState.Fill();

            FillOthers(state);

            var made = TypeMade;
            var asked = state == null ? "" : _making ? NewRow + state.Type + (made == null ? "" : made.Name + "|" + string.Join("|", made.Choices)) : state.Name;
            if (asked == _valueAsked)
                return;

            if (_valueAsked != null)
            {
                _word = Word;
                _copy = Copy;
                _relation = Relating;
            }

            _valueAsked = asked;
            _asking = null;
            if (state == null)
                return;

            var filled = new List<string>();
            if (!_sets)
                filled.Add(ComparisonQuestion.Said(_relation));

            filled.Add(StateValueQuestion.Words(_system, state, made).Contains(_word) && _copy.Length == 0 ? _word : "");
            var asking = StateValueQuestion.Fields(texts, _system, state, !_sets, made);

            // A switch always shows one of its two words, so it holds the one it shows: off, until it is flipped.
            if (asking[ValueAt].IsOnOff && filled[ValueAt].Length == 0)
                filled[ValueAt] = Literal.Of(false).Said;

            _asking = asking;
            _form.Show(_asking, filled, null, texts);
            _form.Rows.Add(_othersHeading, _othersList, FormLabelPlacement.Above);
            _seenWord = Word;
        }

        /// <summary>Says again what the pick has come to, as the value's fields change: a value chosen lets go of another state's.</summary>
        void Edited()
        {
            if (Editor.Current != this || _asking == null)
                return;

            var word = Word;
            if (word != _seenWord && word.Length != 0)
                _copy = "";

            _seenWord = word;
            Editor.Invalidate();
        }

        /// <summary>A row for the other state <paramref name="name" />, whose value is taken when it is pressed, letting go of a value.</summary>
        PickRow Other(string name)
        {
            var row = new PickRow { Mark = Symbols.State, About = Editor.Texts.Codex.PickStateValueOfAbout };
            row.Picked += delegate(HeldModifier held, int presses)
            {
                var take = Editor.Act(CommandKind.PickChoice, delegate
                {
                    _copy = name;
                    if (_asking != null && !_asking[ValueAt].IsOnOff)
                        _form.Fields[ValueAt].Text = "";

                    _seenWord = Word;
                    Editor.Invalidate();
                });
                if (take == null)
                    return;

                take.Execute();
                if (presses > 1 && _finish != null && CanPrimary)
                    _finish.Execute();
            };
            return row;
        }

        /// <summary>Lists the other states holding what <paramref name="state" /> holds, the one whose value is taken lit.</summary>
        void FillOthers(State state)
        {
            _otherKeys.Clear();
            if (state != null)
                _otherKeys.AddRange(StateValueQuestion.Others(_system, state));

            _others.Show(_otherKeys);
            _othersList.IsVisible = _otherKeys.Count != 0;
            foreach (var name in _otherKeys)
            {
                var row = _others[name];
                row.Text = name;
                row.IsChosen = name == _copy;
                row.Show();
            }
        }
    }
}
