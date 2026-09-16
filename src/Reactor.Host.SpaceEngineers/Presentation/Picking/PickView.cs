using System.Collections.Generic;
using Reactor.Ui;
using VRageMath;
using Reactor.Host.SpaceEngineers.Presentation.Ui;
using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Look;

namespace Reactor.Host.SpaceEngineers.Presentation.Picking
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

        public PickView(PickComponent component)
        {
            _component = component;
            _strip.Horizontal = true;
            Widgets.Barred(_strip);
            _strip.Content = _columns;
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

        public override void Update(int tick)
        {
            // Core says when it has changed something and says nothing the rest of the time, so an
            // ordinary tick costs one comparison.
            if (_revision == _component.Revision && _built == _component.Stages.Count)
                return;

            _revision = _component.Revision;

            if (_built != _component.Stages.Count)
            {
                _built = _component.Stages.Count;
                Rebuild();
            }

            for (int i = 0; i < _stages.Count && i < _component.Stages.Count; i++)
                _stages[i].Update(_component.Stages[i], Host);
        }

        public override void Collect(List<Hit> hits)
        {
            for (int i = 0; i < _stages.Count && i < _component.Stages.Count; i++)
                _stages[i].Collect(hits, _component.Stages[i], i, Host);
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
            if (!EditorView.Contains(_strip.Bounds, point))
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

            if (!_strip.HasBar || !EditorView.Contains(_strip.Track, point))
                return false;

            if (!EditorView.Contains(_strip.Thumb, point))
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
            _stages.Clear();
            _columns.Children.Clear();

            for (int i = 0; i < _component.Stages.Count; i++)
            {
                var stage = new StageView(_component, i);
                _stages.Add(stage);
                _columns.Children.Add(stage.Root);
            }
        }
    }
}
