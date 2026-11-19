using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Viewing;

namespace Sprocket.Host.SpaceEngineers.Features.Selector
{
    /// <summary>The stages of a pick as columns side by side, in the manner of a file browser's column view, and the arguments the last of them takes.</summary>
    public class PickStrip : HStack
    {
        /// <summary>How wide a column is in a pick.</summary>
        public const float ColumnWidth = Sizes.Column;

        const float Gap = 8f;

        // Where more columns are shown than fit, each gives up this much, so the edge of the next one shows.
        const float CrowdedWidth = ColumnWidth - 16f;
        const int SlideTicks = 20;

        readonly List<StageColumn> _stages = new List<StageColumn>();
        readonly Dictionary<string, Dictionary<string, string>> _entered = new Dictionary<string, Dictionary<string, string>>();
        readonly Texts _texts;

        ArgumentsColumn _arguments;
        ScrollView _scroller;
        bool _crowded;
        int _toEnd;
        int _glide = -1;
        float _glideFrom;
        float _glideTo;
        StageColumn _holder;
        string _chosen;
        bool _arrivedAt;

        /// <summary>An empty strip saying its fixed words in <paramref name="texts" />.</summary>
        public PickStrip(Texts texts)
        {
            _texts = texts;
            Base.Spacing = Gap;
        }

        /// <summary>Every stage of the pick in order, each narrowing the next.</summary>
        public IReadOnlyList<StageColumn> Stages => _stages;

        /// <summary>The column asking what the chosen thing is given, made when the pick first finishes with one.</summary>
        public ArgumentsColumn Arguments => _arguments;

        /// <summary>Whether the column of what is given stands after the stages.</summary>
        public bool ShowsArguments { get; set; }

        /// <summary>
        /// The stage what is given is asked in, over its rows and beside what was chosen there, or null for a column of its own
        /// after the stages.
        /// </summary>
        public StageColumn ArgumentsIn { get; set; }

        /// <summary>The stage the values stand before as a column of their own, or null for a column after the stages.</summary>
        public StageColumn ArgumentsBefore { get; set; }

        /// <summary>What shows the strip: a view scrolling sideways, where columns past the screen's width are scrolled to.</summary>
        public Element View => _scroller ?? (_scroller = new ScrollView { ScrollsHorizontally = true, ScrollsVertically = false, Content = this });

        /// <summary>How wide a column is: narrower where more columns are shown than fit, so the start of the next one shows at the edge.</summary>
        public float Width => _crowded ? CrowdedWidth : ColumnWidth;

        /// <inheritdoc />
        protected override bool Clips => true;

        /// <summary>Adds a stage after the last.</summary>
        public void Add(StageColumn stage)
        {
            _stages.Add(stage);
        }

        /// <summary>Takes every stage away, for a page that lays its columns again.</summary>
        public void Clear()
        {
            _stages.Clear();
        }

        /// <summary>Drops the last stage, when there is no longer anything for it to be about.</summary>
        public void RemoveLast()
        {
            if (_stages.Count > 0)
                _stages.RemoveAt(_stages.Count - 1);
        }

        /// <summary>Which of the arguments may come from a setting, with a mark beside each to choose one, or null for none.</summary>
        public Func<Field, bool> Sourced { get; set; }

        /// <summary>
        /// Asks for the arguments what is chosen takes, known as <paramref name="chosen" /> and headed by its name
        /// <paramref name="named" />: <paramref name="fields" />, holding what was given for it earlier in this pick where anything
        /// was and <paramref name="filled" /> otherwise.
        /// </summary>
        /// <remarks>What was given for what was chosen before is kept, so choosing it again gives it back.</remarks>
        /// <returns>The column the arguments are asked for in.</returns>
        public ArgumentsColumn Restate(string chosen, string named, IList<Field> fields, IList<string> filled, ColorOffer colors)
        {
            if (_arguments == null)
            {
                _arguments = new ArgumentsColumn(Width, named);
            }

            _arguments.Title = named;

            if (!string.IsNullOrEmpty(_chosen))
            {
                var was = Kept(_chosen);
                was.Clear();
                _arguments.Form.Spell(was);
            }

            _chosen = chosen ?? "";

            Dictionary<string, string> kept = null;
            if (_chosen.Length > 0)
                _entered.TryGetValue(_chosen, out kept);

            var filling = new List<string>();
            for (var i = 0; i < fields.Count; i++)
            {
                string said;
                if (kept != null)
                    filling.Add(kept.TryGetValue(FieldSet.Key(fields[i], i), out said) ? said : "");
                else
                    filling.Add(i < filled.Count ? filled[i] : "");
            }

            _arguments.Form.Sourced = Sourced;
            _arguments.Form.Show(fields, filling, colors, _texts);
            _arguments.Why = "";
            return _arguments;
        }

        /// <summary>Lays out the columns, and what is given after them.</summary>
        public void Show()
        {
            var own = ShowsArguments && _arguments != null && ArgumentsIn == null ? 1 : 0;
            _crowded = _stages.Count + own > 3;

            Children.Clear();
            Columns();
            Given();

            // The column furthest on is the one being answered, so the view glides to the end once this is laid out.
            _toEnd = _scroller != null ? 2 : 0;
        }

        /// <summary>Starts the view gliding to <paramref name="offset" /> across the strip, held within it.</summary>
        void Glide(float offset)
        {
            _glideFrom = _scroller.HorizontalOffset;

            // The view holds an offset within what it shows at once, so the farthest is found by asking it for the end.
            _scroller.ScrollToHorizontalOffset(float.MaxValue);
            var farthest = _scroller.HorizontalOffset;
            _scroller.ScrollToHorizontalOffset(_glideFrom);

            _glideTo = Math.Max(0f, Math.Min(offset, farthest));
            _glide = _glideFrom == _glideTo ? -1 : 0;
        }

        /// <summary>Moves the view a tick along its glide, where one is under way.</summary>
        void Gliding()
        {
            // Once the tick that showed the columns has laid them out, so the strip's width is known.
            if (_toEnd > 0 && --_toEnd == 0)
                Glide(float.MaxValue);

            if (_glide >= 0)
            {
                _glide++;
                _scroller.ScrollToHorizontalOffset(_glide >= SlideTicks ? _glideTo : _glideFrom + (_glideTo - _glideFrom) * Eased(_glide));
                if (_glide >= SlideTicks)
                    _glide = -1;
            }
        }

        /// <summary>Moves the view's glide on by a tick.</summary>
        /// <remarks>Called every tick; it does work only while a glide is under way, a third of a second.</remarks>
        public void Tick()
        {
            if (_scroller != null)
                Gliding();
        }

        /// <summary>Gives the keyboard to the box of the column at <paramref name="opens" />, the first time the strip is shown.</summary>
        public void Arrive(int opens, InputRoot input)
        {
            if (_arrivedAt)
                return;

            _arrivedAt = true;
            if (input == null || opens < 0 || opens >= _stages.Count || !_stages[opens].CanNarrow)
                return;

            input.Focus(_stages[opens].Narrow);
        }

        /// <summary>The columns, each as wide as the strip has them.</summary>
        void Columns()
        {
            var width = Width;
            for (var i = 0; i < _stages.Count; i++)
            {
                _stages[i].Width = width;
                Children.Add(_stages[i]);
            }
        }

        /// <summary>What is given, where it is showing: in the stage it is asked in, or as a column after the stages.</summary>
        void Given()
        {
            var shown = ShowsArguments && _arguments != null;
            var holder = shown ? ArgumentsIn : null;
            if (_holder != null && _holder != holder)
                _holder.Ask(null);

            _holder = holder;
            if (!shown)
                return;

            _arguments.Nested = holder != null;
            if (holder != null)
            {
                holder.Ask(_arguments);
                return;
            }

            _arguments.Width = Width;
            var before = ArgumentsBefore == null ? -1 : Children.IndexOf(ArgumentsBefore);
            if (before >= 0)
                Children.Insert(before, _arguments);
            else
                Children.Add(_arguments);
        }

        /// <summary>Quick to start and slow to arrive, so the eye has where it is going before it stops.</summary>
        static float Eased(int step)
        {
            var t = (float)step / SlideTicks;
            return 1f - (1f - t) * (1f - t) * (1f - t);
        }

        Dictionary<string, string> Kept(string chosen)
        {
            Dictionary<string, string> kept;
            if (!_entered.TryGetValue(chosen, out kept))
            {
                kept = new Dictionary<string, string>();
                _entered[chosen] = kept;
            }

            return kept;
        }
    }
}
