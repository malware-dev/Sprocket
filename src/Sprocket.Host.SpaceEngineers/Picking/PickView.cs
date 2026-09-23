using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Screen;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Words;

namespace Sprocket.Host.SpaceEngineers.Picking
{
    /// <summary>
    /// The stages of a pick, as columns, in the manner of a file browser's column view.
    /// </summary>
    /// <remarks>
    /// Columns are this target's decision, not core's. Core says there are stages and that each narrows
    /// the next; with every stage still in view the blocks already picked stay
    /// beside what is happening to them, and changing an earlier answer is a glance to the left rather than
    /// a retreat through screens.
    /// <para>
    /// Built once and thereafter changed in place. Anything torn down and made again loses what somebody
    /// was doing with it - the caret in the box they were typing into above all, since typing is what
    /// changes the list underneath. Only a change in how many stages there are rebuilds anything.
    /// </para>
    /// </remarks>
    public sealed class PickView : ComponentView
    {
        public const float ColumnWidth = Sizes.Column;

        readonly PickComponent _component;
        readonly Stack _columns = Widgets.RowOf(Space.Item);
        readonly Scroll _strip = new Scroll();

        /// <summary>What <see cref="_grabbed" /> says when the strip's own bar is the one being held.</summary>
        const int Strip = -2;

        const int None = -1;

        /// <summary>Which column's bar is being dragged, or <see cref="Strip" />, or nothing.</summary>
        int _grabbed = None;

        /// <summary>Where the pointer was when the strip last moved.</summary>
        float _held;
        readonly List<StageView> _stages = new List<StageView>();

        int _revision = -1;
        int _built = -1;

        /// <summary>The values the chosen thing takes, and the way to finish, where the pick is finished here.</summary>
        readonly Ask _given;

        /// <summary>
        /// The way to finish where there are no values to give, under the rows of the column the pick is
        /// finished from, as the values column carries its own under its fields.
        /// </summary>
        readonly Label _done;

        readonly Sized _givenRoot = new Sized(ColumnWidth);
        readonly Dock _givenColumn = new Dock();
        readonly Stack _givenHead = Widgets.Column(Space.Row);

        /// <summary>The heading on the left, and Done on the right of it while there are no fields.</summary>
        readonly Bar _givenTop = new Bar();
        readonly Label _givenTitle = Widgets.Label("", Palette.Ink, Lettering.Body);

        /// <summary>
        /// The line under the heading, where a stage says what it holds, kept so the first field lines
        /// up with the search boxes beside it.
        /// </summary>
        readonly Label _givenUnder = Widgets.Label("", Palette.Faint, Lettering.Tiny);

        /// <summary>The fields, and the line under them.</summary>
        readonly Stack _givenBody = Widgets.Column(Space.Region);

        /// <summary>Under the fields: why nothing can be finished yet, and Done on the right of it.</summary>
        readonly Bar _givenFoot = new Bar();

        /// <summary>
        /// What was entered for each chosen thing while the pick is open, by what it is for and then by
        /// field.
        /// </summary>
        /// <remarks>
        /// Kept by the view because what is typed is the view's until it is finished, and for as long as
        /// the view lives, which is as long as the pick is open. Each field as its own control spells it,
        /// so going back to something refills its controls the way opening on a step does.
        /// </remarks>
        readonly Dictionary<string, Dictionary<string, string>> _entered =
            new Dictionary<string, Dictionary<string, string>>();

        /// <summary>Which chosen thing the fields are showing, or null before they have shown anything.</summary>
        string _shown;

        int _restated = -1;

        /// <summary>Whether the fields have been filled for the first time, which is when they may take the caret on opening.</summary>
        bool _opened;

        readonly List<string> _filling = new List<string>();

        /// <summary>What core was last told the values hold.</summary>
        string _reported;

        public PickView(PickComponent component)
        {
            _component = component;
            _strip.Horizontal = true;
            Widgets.Barred(_strip);
            _strip.Content = _columns;

            if (!component.Finishes)
            {
                _done = Widgets.Label("Done", Palette.Ink, Lettering.Small);
                return;
            }

            _given = Ask.Column(ColumnWidth - Space.Region * 2f);

            _givenTitle.Wraps = false;
            _givenTitle.Trims = true;
            _givenTitle.Text = Widgets.Caps(SequenceText.ValuesTitle());
            _givenHead.Children.Add(_givenTitle);
            _givenHead.Children.Add(_givenUnder);
            _givenTop.Spacing = Space.Item;
            _givenTop.Fill = _givenHead;

            // The gap a stage keeps between its heading and its search box, so the first field lines
            // up with the search boxes beside it.
            _givenTop.Padding = new Thickness(0f, 0f, 0f, Space.Row);

            // The reason beside Done, wrapping downwards, so Done stays where it is whatever it says.
            _givenFoot.Spacing = Space.Item;
            _givenFoot.Tops = true;
            _givenFoot.Fill = _given.Why;

            _givenBody.Children.Add(_given.Form);
            _givenBody.Children.Add(_givenFoot);

            _givenColumn.Background = Palette.Panel;
            _givenColumn.Edge = Palette.Line;
            _givenColumn.Padding = new Thickness(Space.Region);
            _givenColumn.Top = _givenTop;
            _givenColumn.Fill = _givenBody;
            _givenRoot.Content = _givenColumn;
        }

        /// <summary>
        /// Tells core what the values hold, where that differs from what it was last told.
        /// </summary>
        /// <remarks>
        /// Asked after something that can change them - a key, a press or a drag in the column, or the
        /// column being filled again - and never on a tick.
        /// </remarks>
        public override void Entered()
        {
            if (_given == null || _component.EnteredCommand == 0 || Host == null)
                return;

            var answer = _given.Answer;
            if (answer == _reported)
                return;

            _reported = answer;
            Host.SetText(_component.EnteredCommand, answer);
        }

        public override bool Gives
        {
            get { return _given != null; }
        }

        public override Label Done
        {
            get { return _done; }
        }

        /// <summary>The values column, while it is on the screen.</summary>
        public override Ask Given
        {
            get { return _given != null && _givenRoot.Parent != null ? _given : null; }
        }

        /// <summary>
        /// Answered with the revision this view already keeps. The picker's stages are rebuilt whole
        /// rather than changed in place - a search narrows them, a choice re-derives them - so one
        /// number says everything a screenful of reading would have.
        /// </summary>
        protected override bool Stale
        {
            get { return _revision != _component.Revision || _built != _component.Stages.Count; }
        }

        public override Element Root
        {
            get { return _strip; }
        }

        /// <summary>The strip scrolls sideways and every column scrolls its own rows.</summary>
        public override bool ScrollsItself
        {
            get { return true; }
        }

        protected override void Refresh()
        {
            // Core says when it has changed something and says nothing the rest of the time, so an
            // ordinary tick costs one comparison.
            if (!Stale)
                return;

            _revision = _component.Revision;

            if (_built != _component.Stages.Count)
            {
                _built = _component.Stages.Count;
                Rebuild();
            }

            for (int i = 0; i < _stages.Count && i < _component.Stages.Count; i++)
                _stages[i].Update(_component.Stages[i], Host);

            if (_given != null && _restated != _component.Restated)
            {
                _restated = _component.Restated;
                Refill();
            }

            Show();
        }

        /// <summary>
        /// Puts the values column on after the stages once something to do is chosen, and takes it off
        /// when nothing is.
        /// </summary>
        void Show()
        {
            if (_given == null)
                return;

            var wanted = _component.Stages.Count > 1 && !string.IsNullOrEmpty(_component.Given);
            var shown = _givenRoot.Parent != null;
            if (wanted == shown)
                return;

            if (wanted)
                _columns.Children.Add(_givenRoot);
            else
                _columns.Children.Remove(_givenRoot);
        }

        /// <summary>
        /// Fills the fields for what is chosen now, keeping what was entered for what was chosen before.
        /// </summary>
        /// <remarks>
        /// What something opens holding is what was entered for it earlier in this pick, where anything
        /// was, and what the pick says otherwise: a step's own values, for what the step does.
        /// </remarks>
        void Refill()
        {
            if (!string.IsNullOrEmpty(_shown))
            {
                var was = Kept(_shown);
                was.Clear();
                _given.Spell(was);
            }

            var given = _component.Given ?? "";
            if (given != _shown)
                _given.Unjudge();

            _shown = given;

            Dictionary<string, string> kept = null;
            if (given.Length > 0)
                _entered.TryGetValue(given, out kept);

            List<Field> fields = _component.Fields;
            _filling.Clear();
            for (var i = 0; i < fields.Count; i++)
            {
                string said;
                if (kept != null)
                    _filling.Add(kept.TryGetValue(Ask.Key(fields[i], i), out said) ? said : "");
                else
                    _filling.Add(i < _component.Filled.Count ? _component.Filled[i] : "");
            }

            // The fields say what is wanted by being there, and nothing is said over them.
            _given.Values("", 0, fields, _filling);

            Place(_given.Count > 0);

            // Nowhere to type is nowhere to keep the keyboard. Somebody who came for the values gets
            // the caret in them, once, when they first arrive.
            if (_given.Count == 0)
                _given.Release();
            else if (_component.OpensOnValues && !_opened)
                _given.Take();

            _opened = true;
            Entered();
        }

        /// <summary>
        /// Puts Done under the fields where there are any, and beside the heading where there are none.
        /// </summary>
        /// <remarks>Only when what is chosen changes, never while it is being filled in.</remarks>
        void Place(bool underFields)
        {
            if (underFields)
            {
                _givenTop.Right = null;
                _givenFoot.Right = _given.Done;
            }
            else
            {
                _givenFoot.Right = null;
                _givenTop.Right = _given.Done;
            }
        }

        Dictionary<string, string> Kept(string given)
        {
            Dictionary<string, string> kept;
            if (!_entered.TryGetValue(given, out kept))
            {
                kept = new Dictionary<string, string>();
                _entered[given] = kept;
            }

            return kept;
        }

        /// <summary>
        /// Moves the column being pointed at, or the whole strip when that column has nowhere to go.
        /// </summary>
        /// <remarks>
        /// The fallback is not a flourish. With several stages open the strip runs off the side, and a
        /// wheel that did nothing because this particular column happened to fit would read as the
        /// screen being broken rather than as the column being full.
        /// </remarks>
        public override bool ScrollBy(Vector2 point, float amount)
        {
            if (!_strip.Reaches(point))
                return false;

            for (int i = 0; i < _stages.Count; i++)
            {
                if (_stages[i].ScrollBy(point, amount))
                    return true;
            }

            if (!_strip.CanScroll)
                return false;

            _strip.ScrollBy(amount);
            return true;
        }

        /// <summary>
        /// Takes hold of a scrollbar - a column's, or the strip's own.
        /// </summary>
        /// <remarks>
        /// Through the drag path rather than the press path, because a bar is the one control here that
        /// wants every move of the pointer rather than the fact of a click. It costs nothing: this view
        /// had no other use for grabbing, and the screen already offers a grab before it offers a press.
        /// </remarks>
        public override bool TryGrab(Vector2 point)
        {
            for (int i = 0; i < _stages.Count; i++)
            {
                if (!_stages[i].TryGrabBar(point))
                    continue;

                _grabbed = i;
                return true;
            }

            if (!_strip.OnBar(point))
                return false;

            if (!_strip.OnThumb(point))
                _strip.ThumbTo(point.X);

            _strip.BarHeld = true;
            _held = point.X;
            _grabbed = Strip;
            return true;
        }

        public override void Carry(Vector2 point)
        {
            if (_grabbed == Strip)
            {
                _strip.DragBy(point.X - _held);
                _held = point.X;
                return;
            }

            if (_grabbed >= 0 && _grabbed < _stages.Count)
                _stages[_grabbed].CarryBar(point);
        }

        public override void Drop()
        {
            _strip.BarHeld = false;

            for (int i = 0; i < _stages.Count; i++)
                _stages[i].DropBar();

            _grabbed = None;
        }

        void Rebuild()
        {
            for (int i = 0; i < _stages.Count; i++)
                _stages[i].Finishing(null);

            _stages.Clear();
            _columns.Children.Clear();

            for (int i = 0; i < _component.Stages.Count; i++)
            {
                var stage = new StageView(_component, i);
                if (_done != null && _component.Stages[i].Finishes)
                    stage.Finishing(_done);

                _stages.Add(stage);
                _columns.Children.Add(stage.Root);
            }

            Show();
        }
    }
}
