using Sprocket.Host.SpaceEngineers.Features.States.Edits;
using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Tips;
using System;
using System.Collections.Generic;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.States
{
    /// <summary>
    /// The work of a logic system's State tab: showing its types and states, adding, renaming, moving and removing them,
    /// choosing what a state starts at, and saying what the installation shown holds now.
    /// </summary>
    public sealed class StateTab
    {
        readonly SprocketEditor _editor;
        readonly AutomationSystem _system;
        readonly StatePanel _panel;
        readonly Func<CommandKind, Action, ICommand> _changes;
        readonly Func<string> _locked;
        readonly KeyedChildren<DeclaredType, TypeRow> _typeRows;
        readonly KeyedChildren<State, StateRow> _stateRows;
        readonly List<DeclaredType> _types = new List<DeclaredType>();
        readonly List<State> _states = new List<State>();

        // Whether the values shown now can be changed by hand, as last offered; null where the rows were just made again.
        bool? _holdable;

        /// <summary>
        /// The State tab of <paramref name="system" />, shown in <paramref name="panel" />, its changes made through commands
        /// <paramref name="changes" /> makes, and <paramref name="locked" /> saying why nothing can be changed where it cannot.
        /// </summary>
        public StateTab(SprocketEditor editor, AutomationSystem system, StatePanel panel, Func<CommandKind, Action, ICommand> changes,
            Func<string> locked)
        {
            _editor = editor;
            _system = system;
            _panel = panel;
            _changes = changes;
            _locked = locked;
            _typeRows = new KeyedChildren<DeclaredType, TypeRow>(panel.Types.Children, TypeRowFor);
            _stateRows = new KeyedChildren<State, StateRow>(panel.StateList.Children, StateRowFor);
            editor.Codex.Explain(panel.TypesHeading);
            editor.Codex.Explain(panel.StatesHeading);

            panel.Types.Reordered += moved => Move(CommandKind.MoveType, () => _editor.Do(_system, new MoveType(_types[moved.From], moved.From, moved.To)));
            panel.StateList.Reordered += moved => Move(CommandKind.MoveState, () => _editor.Do(_system, new MoveState(_states[moved.From], moved.From, moved.To)));
        }

        /// <summary>Lays the tab out again from the system's types and states.</summary>
        /// <remarks>Walks every mention of each type, choice and state to say what may be removed, so it runs only when the page is built.</remarks>
        public void Arrange(Texts texts)
        {
            var locked = _locked();
            var editable = locked.Length == 0;

            _types.Clear();
            _types.AddRange(_system.Types);
            _states.Clear();
            _states.AddRange(_system.States);

            // Where types and states can be added, the ways to add them say it all.
            _panel.Empty = _types.Count == 0 && _states.Count == 0 && !editable ? texts.SystemNoState : "";
            _panel.TypesHeading.Markdown = CodexPages.Term(texts.StateTypes.ToUpperInvariant(), CodexPages.Type);
            _panel.StatesHeading.Markdown = CodexPages.Term(texts.StateStates.ToUpperInvariant(), CodexPages.LogicSystemStates);
            _panel.TypesHeading.IsVisible = _types.Count != 0 || editable;
            _panel.StatesHeading.IsVisible = _states.Count != 0 || editable;
            _panel.Types.IsVisible = _types.Count != 0;
            _panel.StateList.IsVisible = _states.Count != 0;
            _panel.Adds = editable;

            _panel.AddType.Text = StatusRow.Plus + texts.StateTypeAdd;
            _panel.AddType.Command = _changes(CommandKind.AddType, AddType);
            _panel.AddState.Text = StatusRow.Plus + texts.StateStateAdd;
            _panel.AddState.Command = _changes(CommandKind.AddState, AddState);

            _typeRows.Show(_types);
            foreach (var type in _types)
                ArrangeType(texts, _typeRows[type], type, editable, locked);

            _stateRows.Show(_states);
            _holdable = null;
            foreach (var state in _states)
                ArrangeState(texts, _stateRows[state], state, editable, locked);

            Live();
        }

        /// <summary>
        /// Says what the installation shown holds now in each state, changing only the words that changed, each pressed to
        /// change it by hand while the installation or its logic system is switched off.
        /// </summary>
        /// <remarks>Called every tick while the tab is showing: a lookup per state, and the presses made again only as a switch turns.</remarks>
        public void Live()
        {
            var shown = _editor.StandIn(_system);
            var holdable = shown != null && (!_system.Enabled || !shown.Enabled);
            var turned = _holdable != holdable;
            _holdable = holdable;
            foreach (var state in _states)
            {
                var now = _editor.Runner.States.Read(_system, shown, state).Said;
                var row = _stateRows[state];
                if (row.Now != now)
                    row.Now = now;

                if (!turned)
                    continue;

                var held = state;
                row.Current.Command = holdable ? _editor.Act(CommandKind.ChangeNow, () => Hold(held)) : null;
                row.Current.Set(LookStates.Still, row.Current.Command == null);
            }
        }

        /// <summary>
        /// Offers the values <paramref name="state" /> can hold in a menu under what the installation shown holds now, that one
        /// marked, and gives it the one chosen, by hand, as one edit.
        /// </summary>
        void Hold(State state)
        {
            var shown = _editor.StandIn(_system);
            if (shown == null)
                return;

            Func<Literal> read = () => _editor.Runner.States.Read(_system, shown, state);
            Action<Literal> write = value => _editor.Hold(_system, shown, state, value);
            var now = read();
            _editor.AskHere("");
            foreach (var word in StateValueQuestion.Words(_system, state))
            {
                Literal value;
                StateValueQuestion.Read(_system, state, word, out value);
                var marked = StateMentions.Same(now, value);
                _editor.Answer(word, delegate
                {
                    if (!marked)
                        _editor.Do(_system, new ChangeNow(shown, state, value, read, write));
                }, marked);
            }
        }

        void ArrangeType(Texts texts, TypeRow row, DeclaredType type, bool editable, string locked)
        {
            TypeEditing.Show(texts, row, type);
            row.Name.Command = _changes(CommandKind.RenameType, () => _editor.Rename(row.Name, TypeNaming(type)));
            row.Name.CanRename = row.Name.Command != null;
            row.Name.About = editable ? texts.Codex.StateTypeRenameAbout : locked;

            // A type keeps two choices at least, so two can only be renamed; past two, a choice may also be removed.
            var removable = type.Choices.Count > 2;
            foreach (var choice in type.Choices)
            {
                var chip = row.ChoiceChips[choice];
                var named = choice;
                chip.Command = removable
                    ? _changes(CommandKind.ChangeChoice, () => Choice(type, named, chip))
                    : _changes(CommandKind.ChangeChoice, () => _editor.Rename(chip, ChoiceNaming(type, named)));
                chip.CanRename = !removable && chip.Command != null;
                chip.About = !editable ? locked : removable ? texts.Codex.StateChoiceAbout : null;
            }

            row.AddChoice.Command = _changes(CommandKind.AddChoice,
                () => _editor.Do(_system, new AddChoice(type, NumberedName.Free(texts.StateChoiceNumbered, type.Choices.Count, type.Has), type.Choices.Count)));
            row.AddChoice.IsVisible = editable;

            var removes = new RemoveType(_system, type).Changes;
            row.Removes = removes ? _changes(CommandKind.RemoveType, () => _editor.Do(_system, new RemoveType(_system, type))) : null;
            row.RemovesAbout = !editable ? locked : removes ? texts.Codex.StateTypeRemove : texts.Codex.StateTypeKept;
            row.Grips(editable, editable ? texts.Codex.StateTypeDrag : locked);
        }

        void ArrangeState(Texts texts, StateRow row, State state, bool editable, string locked)
        {
            row.Name.Text = state.Name;
            row.Name.Command = _changes(CommandKind.RenameState, () => _editor.Rename(row.Name, StateNaming(state)));
            row.Name.CanRename = row.Name.Command != null;
            row.Name.About = editable ? texts.Codex.StateStateRenameAbout : locked;
            row.Kind = Template.Fill(texts.StateHolds, "type", state.IsFlag ? texts.StateFlag : state.Type);

            var held = StateMentions.Mentions(_system, state.Name);
            row.KindOpens = held ? null : _changes(CommandKind.ChangeStart, () => Types(state));
            row.KindAbout = !editable ? locked : held ? texts.Codex.StateKindKept : texts.Codex.StateHoldsAbout;
            row.StartsLead = texts.StateStarts;
            row.StartsAbout = texts.Codex.StateStartsAbout;
            row.Start.Text = state.Start.Said;
            row.Start.Command = _changes(CommandKind.ChangeStart, () => Starts(state));
            row.Start.Set(LookStates.Still, row.Start.Command == null);
            row.Start.Tip = KeptTip.For(row.Start.Tip, editable ? null : locked);
            row.NowLead = texts.StateNow;
            row.NowAbout = texts.Codex.StateNowAbout;

            var removes = new RemoveState(_system, state).Changes;
            row.Removes = removes ? _changes(CommandKind.RemoveState, () => _editor.Do(_system, new RemoveState(_system, state))) : null;
            row.RemovesAbout = !editable ? locked : removes ? texts.Codex.StateStateRemove : texts.Codex.StateStateKept;
            row.Grips(editable, editable ? texts.Codex.StateStateDrag : locked);
        }

        /// <summary>Offers renaming the choice <paramref name="choice" /> of <paramref name="type" />, and removing it where nothing holds it there.</summary>
        void Choice(DeclaredType type, string choice, NamePlate chip)
        {
            var texts = _editor.Texts;
            var removes = new RemoveChoice(_system, type, choice).Changes;
            _editor.AskHere(removes ? "" : texts.StateChoiceKept);
            _editor.Answer(texts.StateRename, () => _editor.Rename(chip, ChoiceNaming(type, choice)));
            if (removes)
                _editor.Answer(texts.StateRemoveChoice, () => _editor.Do(_system, new RemoveChoice(_system, type, choice)));
        }

        /// <summary>Offers the values of the type <paramref name="state" /> holds to start as, the one it starts as marked.</summary>
        void Starts(State state)
        {
            _editor.AskHere("");
            foreach (var start in StateValueQuestion.Starts(_editor.Texts, _system, state.Type))
            {
                var chosen = start;
                _editor.Answer(start.Word, () => _editor.Do(_system, new ChangeStart(_system, state, chosen.Value, chosen.Type)),
                    state.Type == start.Type && StateMentions.Same(state.Start, start.Value));
            }
        }

        /// <summary>Offers on or off, each type and a new type for <paramref name="state" /> to hold, the one it holds marked; another starts the state as its first value, or off.</summary>
        void Types(State state)
        {
            var texts = _editor.Texts;
            _editor.AskHere("");
            _editor.Answer(texts.StateFlag, () => Retype(state, ""), state.IsFlag);
            foreach (var type in _system.Types)
            {
                var named = type.Name;
                _editor.Answer(named, () => Retype(state, named), state.Type == named);
            }

            _editor.Answer(StatusRow.Plus + texts.PickNewType, () => RetypeToNew(state), false);
        }

        /// <summary>Makes <paramref name="state" /> hold the type <paramref name="type" />, empty for on or off, starting as its first value, or off.</summary>
        void Retype(State state, string type)
        {
            if (state.Type == type)
                return;

            var start = type.Length == 0 ? StateValueQuestion.Off : StateValueQuestion.Starts(_editor.Texts, _system, type)[0];
            _editor.Do(_system, new ChangeStart(_system, state, start.Value, start.Type));
        }

        /// <summary>Makes a new type and makes <paramref name="state" /> hold it, starting as its first choice, as one change.</summary>
        void RetypeToNew(State state)
        {
            var texts = _editor.Texts;
            var type = TypeEditing.New(texts, _system.Types.Count, taken => _system.HasTypeInAnyCase(taken) || StateReader.IsBuiltInInAnyCase(taken));
            var start = StateValueQuestion.Starts(texts, _system, type.Name, type)[0];
            _editor.Do(_system, new WithMade(new AddType(type, _system.Types.Count), new ChangeStart(_system, state, start.Value, start.Type)));
        }

        void AddType()
        {
            var type = TypeEditing.New(_editor.Texts, _system.Types.Count, taken => _system.HasTypeInAnyCase(taken) || StateReader.IsBuiltInInAnyCase(taken));
            _editor.Do(_system, new AddType(type, _system.Types.Count));
        }

        void AddState()
        {
            var name = NumberedName.Free(_editor.Texts.StateStateNumbered, _system.States.Count, _system.HasStateInAnyCase);
            _editor.Do(_system, new AddState(new State(name, "", Literal.Of(false)), _system.States.Count));
        }

        void Move(CommandKind kind, Action move)
        {
            var command = _editor.Act(kind, move);
            if (command != null)
                command.Execute();
        }

        TypeRow TypeRowFor(DeclaredType type)
        {
            return new TypeRow();
        }

        StateRow StateRowFor(State state)
        {
            return new StateRow();
        }

        /// <summary>The name of <paramref name="choice" /> of <paramref name="type" />, given where the type has no other choice by it.</summary>
        Naming ChoiceNaming(DeclaredType type, string choice)
        {
            return TypeEditing.ChoiceNaming(type, choice, wanted => _editor.Do(_system, new RenameChoice(type, choice, wanted)));
        }

        /// <summary>The name of <paramref name="type" />, given where no other type of the system has it, nor a built-in type.</summary>
        Naming TypeNaming(DeclaredType type)
        {
            var was = type.Name;
            return new Naming
            {
                Name = was,
                NameRequired = true,
                Accepts = wanted => Free(wanted, was, taken => _system.HasTypeInAnyCase(taken) || StateReader.IsBuiltInInAnyCase(taken)),
                Renames = wanted =>
                {
                    if (wanted.Trim() != was)
                        _editor.Do(_system, new RenameType(type, wanted.Trim()));
                }
            };
        }

        /// <summary>The name of <paramref name="state" />, given where no other state of the system has it.</summary>
        Naming StateNaming(State state)
        {
            var was = state.Name;
            return new Naming
            {
                Name = was,
                NameRequired = true,
                Accepts = wanted => Free(wanted, was, _system.HasStateInAnyCase),
                Renames = wanted =>
                {
                    if (wanted.Trim() != was)
                        _editor.Do(_system, new RenameState(state, wanted.Trim()));
                }
            };
        }

        /// <summary>Whether <paramref name="wanted" />, trimmed, can stand in place of <paramref name="was" />: the same in any case, or a name <paramref name="taken" /> does not refuse.</summary>
        static bool Free(string wanted, string was, Func<string, bool> taken)
        {
            var name = wanted.Trim();
            return string.Equals(name, was, StringComparison.OrdinalIgnoreCase) || !taken(name);
        }
    }
}
