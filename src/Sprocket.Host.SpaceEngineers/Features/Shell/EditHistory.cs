using System.Collections.Generic;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>
    /// Every change made in the editor, kept to be walked back and forward: each logic system's own and the program's as a
    /// whole, with the words the undo and redo arrows name them in.
    /// </summary>
    public sealed class EditHistory
    {
        readonly Automation _program;
        readonly AutomationRunner _runner;
        readonly Texts _texts;

        readonly History<AutomationSystem> _systems = new History<AutomationSystem>();
        readonly History<Automation> _construct = new History<Automation>();
        readonly List<object> _also = new List<object>();

        readonly ChangeWording _undoWords = new ChangeWording();
        readonly ChangeWording _redoWords = new ChangeWording();

        // What each change taken back was said as when it was taken back: what it named, such as a slot made with it, may be
        // gone with it. A new change forgets them all, and a change still taken back elsewhere is worded afresh.
        readonly Dictionary<object, string> _undoneSaid = new Dictionary<object, string>();

        // Counts the blocks renamed in the game, which the arrows' words name.
        int _renames;

        /// <summary>A history of <paramref name="program" />'s changes, worded in <paramref name="texts" /> with the names <paramref name="runner" /> binds.</summary>
        public EditHistory(Automation program, AutomationRunner runner, Texts texts)
        {
            _program = program;
            _runner = runner;
            _texts = texts;
        }

        /// <summary>How many times the undo and redo arrows' words have been put together.</summary>
        public int Worded { get; private set; }

        /// <summary>Makes <paramref name="edit" /> to <paramref name="system" /> as the next step of its history, made on the page numbered <paramref name="maker" />, and says whether it changed anything.</summary>
        public bool Did(AutomationSystem system, Edit<AutomationSystem> edit, int maker)
        {
            if (!_systems.Did(system, edit, maker))
                return false;

            _undoneSaid.Clear();
            return true;
        }

        /// <summary>Makes <paramref name="edit" /> to the program as a whole as the next step of its history, made on the page numbered <paramref name="maker" />, and says whether it changed anything.</summary>
        public bool Did(ProgramEdit edit, int maker)
        {
            if (!_construct.Did(_program, edit, maker))
                return false;

            _undoneSaid.Clear();
            return true;
        }

        /// <summary>Whether a change made on the page numbered <paramref name="maker" /> still stands.</summary>
        public bool MadeBy(int maker)
        {
            return _systems.MadeBy(maker) || _construct.MadeBy(maker);
        }

        /// <summary>Keeps every change made on the page numbered <paramref name="maker" /> as any other change.</summary>
        public void Disown(int maker)
        {
            _systems.Disown(maker);
            _construct.Disown(maker);
        }

        /// <summary>Takes back every change made on the page numbered <paramref name="maker" />, adding those to logic systems to <paramref name="systems" /> and those to the program as a whole to <paramref name="construct" />.</summary>
        public void Discard(int maker, IList<Edit<AutomationSystem>> systems, IList<Edit<Automation>> construct)
        {
            _systems.Discard(maker, systems);
            _construct.Discard(maker, construct);
        }

        /// <summary>Whether there is a change to <paramref name="system" /> on its screen <paramref name="screen" /> to take back now.</summary>
        public bool CanUndo(AutomationSystem system, object screen)
        {
            return _systems.CanUndo(system, screen);
        }

        /// <summary>Whether there is a change to <paramref name="system" /> on its screen <paramref name="screen" /> taken back to make again.</summary>
        public bool CanRedo(AutomationSystem system, object screen)
        {
            return _systems.CanRedo(system, screen);
        }

        /// <summary>What taking back the last change on <paramref name="screen" /> would undo, in words as seen through <paramref name="through" />, with the other screens it takes back changes on.</summary>
        public string UndoLabel(AutomationSystem system, object screen, SystemInstance through)
        {
            return Label(_systems.Undoes(system, screen), system, screen, through, true);
        }

        /// <summary>What making the last change taken back on <paramref name="screen" /> again would redo, in words as seen through <paramref name="through" />, with the other screens it makes changes on.</summary>
        public string RedoLabel(AutomationSystem system, object screen, SystemInstance through)
        {
            return Label(_systems.Redoes(system, screen), system, screen, through, false);
        }

        /// <summary>Takes back the last change to <paramref name="system" /> on <paramref name="screen" />, with every later change it takes along, remembering it in words as seen through <paramref name="through" />, and returns it, or null for none.</summary>
        public Edit<AutomationSystem> Undo(AutomationSystem system, object screen, SystemInstance through)
        {
            var said = ChangeText.Of(_texts, _systems.Undoes(system, screen), system, true, _program, _runner.Bindings, through);
            return Undone(_systems.Undo(system, screen), said);
        }

        /// <summary>Makes the last change to <paramref name="system" /> taken back on <paramref name="screen" /> again, with whatever taken back it leans on, and returns it, or null for none.</summary>
        public Edit<AutomationSystem> Redo(AutomationSystem system, object screen)
        {
            return Redone(_systems.Redo(system, screen));
        }

        /// <summary>Whether there is a change to the program as a whole to walk back.</summary>
        public bool CanUndoConstruct()
        {
            return _construct.CanUndo(_program, null);
        }

        /// <summary>Whether there is a change to the program as a whole to walk forward again.</summary>
        public bool CanRedoConstruct()
        {
            return _construct.CanRedo(_program, null);
        }

        /// <summary>What walking back the last change to the program as a whole undoes, as the undo arrow names it.</summary>
        public string UndoLabelConstruct()
        {
            return ConstructLabel(_undoWords, _construct.Undoes(_program, null), true);
        }

        /// <summary>What walking forward again the last change to the program as a whole that was walked back redoes, as the redo arrow names it.</summary>
        public string RedoLabelConstruct()
        {
            return ConstructLabel(_redoWords, _construct.Redoes(_program, null), false);
        }

        /// <summary>Walks back the last change to the program as a whole and returns it, or null for none.</summary>
        public Edit<Automation> UndoConstruct()
        {
            var said = ChangeText.Of(_texts, _construct.Undoes(_program, null), _program, true, _program, _runner.Bindings, null);
            return Undone(_construct.Undo(_program, null), said);
        }

        /// <summary>Walks forward again the last change to the program as a whole that was walked back and returns it, or null for none.</summary>
        public Edit<Automation> RedoConstruct()
        {
            return Redone(_construct.Redo(_program, null));
        }

        /// <summary>The last change to <paramref name="system" />, or null for none.</summary>
        public Edit<AutomationSystem> Last(AutomationSystem system)
        {
            return _systems.Last(system);
        }

        /// <summary>The last change to the program as a whole, or null for none.</summary>
        public Edit<Automation> LastConstruct()
        {
            return _construct.Last(_program);
        }

        /// <summary>Takes <paramref name="edit" />, the last change to <paramref name="system" />, back as though it was never made, and says whether it could.</summary>
        public bool Withdraw(AutomationSystem system, Edit<AutomationSystem> edit)
        {
            return _systems.Withdraw(system, edit);
        }

        /// <summary>Takes <paramref name="edit" />, the last change to the program as a whole, back as though it was never made, and says whether it could.</summary>
        public bool WithdrawConstruct(Edit<Automation> edit)
        {
            return _construct.Withdraw(_program, edit);
        }

        /// <summary>Lets go of every change to <paramref name="system" />.</summary>
        public void Forget(AutomationSystem system)
        {
            _systems.Forget(system);
        }

        /// <summary>Lets go of every change, to any logic system and to the program as a whole.</summary>
        public void Clear()
        {
            _systems.Clear();
            _construct.Clear();
        }

        /// <summary>Has the arrows' words put together again, a block having been renamed in the game.</summary>
        public void Renamed()
        {
            _renames++;
        }

        /// <summary>
        /// <paramref name="change" /> in words, and where walking it takes changes on other screens along, back where
        /// <paramref name="back" /> or forward, what those screens are called.
        /// </summary>
        string Label(Edit<AutomationSystem> change, AutomationSystem system, object screen, SystemInstance through, bool back)
        {
            // Asked every tick, and the words name steps and blocks, so they are put together again only for another change,
            // an edit, or a block renamed in the game.
            var wording = back ? _undoWords : _redoWords;
            if (wording.Says(change, screen, _program.Edits, _runner.Bindings.Revision, _renames))
                return wording.Text;

            Worded++;
            wording.Text = Labelled(change, system, screen, through, back);
            return wording.Text;
        }

        /// <summary><paramref name="change" /> in words, with the other screens walking it takes changes on along, back where <paramref name="back" />.</summary>
        string Labelled(Edit<AutomationSystem> change, AutomationSystem system, object screen, SystemInstance through, bool back)
        {
            var label = Said(change, system, back, through);
            _also.Clear();
            _systems.Also(system, screen, back, _also);
            if (_also.Count == 0)
                return label;

            var names = new List<string>();
            for (var i = 0; i < _also.Count; i++)
            {
                var name = ScreenName(_also[i]);
                if (name.Length > 0 && !names.Contains(name))
                    names.Add(name);
            }

            return names.Count == 0 ? label : Template.Fill(Template.Fill(_texts.UndoAlso, "screens", string.Join(", ", names.ToArray())), "change", label);
        }

        /// <summary><paramref name="change" /> to the program as a whole in the words <paramref name="wording" /> keeps, put together again only where they are out of date.</summary>
        string ConstructLabel(ChangeWording wording, Edit<Automation> change, bool back)
        {
            if (wording.Says(change, null, _program.Edits, _runner.Bindings.Revision, _renames))
                return wording.Text;

            Worded++;
            wording.Text = Said(change, _program, back, null);
            return wording.Text;
        }

        /// <summary>What the screen of <paramref name="screen" /> is called: the sequence's, slot's, installation's or logic system's name.</summary>
        string ScreenName(object screen)
        {
            var sequence = screen as Sequence;
            if (sequence != null)
                return SequenceText.Called(_texts, sequence);

            var part = screen as Part;
            if (part != null)
                return part.Id;

            var instance = screen as SystemInstance;
            if (instance != null)
                return instance.Name ?? "";

            var system = screen as AutomationSystem;
            return system != null ? SystemSummary.Describe(_texts, _runner.Bindings, system) : "";
        }

        /// <summary>
        /// <paramref name="change" /> in words as <paramref name="subject" /> stands, made where <paramref name="back" />; a change
        /// taken back is said as it was when it was taken back.
        /// </summary>
        string Said<T>(Edit<T> change, T subject, bool back, SystemInstance through)
        {
            string said;
            if (!back && change != null && _undoneSaid.TryGetValue(change, out said))
                return said;

            return ChangeText.Of(_texts, change, subject, back, _program, _runner.Bindings, through);
        }

        /// <summary><paramref name="edit" />, just taken back, remembered as said in <paramref name="said" />.</summary>
        Edit<T> Undone<T>(Edit<T> edit, string said)
        {
            if (edit != null)
                _undoneSaid[edit] = said;

            return edit;
        }

        /// <summary><paramref name="edit" />, just made again, with the words it was taken back in forgotten.</summary>
        Edit<T> Redone<T>(Edit<T> edit)
        {
            if (edit != null)
                _undoneSaid.Remove(edit);

            return edit;
        }
    }
}
