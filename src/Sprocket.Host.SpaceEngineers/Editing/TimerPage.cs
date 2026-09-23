using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Picking;
using Sprocket.Host.SpaceEngineers.Screen;

namespace Sprocket.Host.SpaceEngineers.Editing
{
    /// <summary>
    /// How often a trigger on a timer fires, and what it then does, side by side.
    /// </summary>
    /// <remarks>
    /// The timer's own picker. A trigger on a block picks the block and what happens to it; this picks
    /// the rate, and the column saying what the trigger does is the same one, so it is chosen the same
    /// way whichever kind of trigger it is.
    /// </remarks>
    sealed class TimerPage : EditorPage
    {
        readonly Aim _aim;
        readonly Action<int> _done;

        /// <summary>The rate chosen so far, in ticks.</summary>
        int _ticks;

        PickComponent _pick;
        PickStage _rates;
        PickStage _does;

        public TimerPage(SprocketEditor editor, int ticks, Aim aim, Action<int> done) : base(editor)
        {
            _ticks = ticks;
            _aim = aim;
            _done = done;
        }

        public override string Title => "How often?";

        public override string PrimaryText => _aim.Chosen ? "Done" : "";

        public override void Primary()
        {
            _done(_ticks);
        }

        public override void Build(List<EditorComponent> components)
        {
            if (_pick == null)
            {
                _pick = new PickComponent();
                _pick.Fills = true;

                _rates = new PickStage("How often");
                _does = new PickStage("What it does");
                _does.Finishes = true;
                _pick.Add(_rates);
                _pick.Add(_does);
            }

            var changed = FillRates();
            changed |= _aim.Fill(_does);
            if (changed)
                _pick.Changed();

            components.Add(_pick);
        }

        /// <summary>Every rate there is, the one chosen marked, and what the costly ones cost on the pointer.</summary>
        bool FillRates()
        {
            var cadences = Cadences.All;
            var changed = PickPage.Resize(_rates.Entries, cadences.Count);

            for (var i = 0; i < cadences.Count; i++)
            {
                var ticks = cadences[i].Ticks;
                var label = ticks == 1 ? "every tick" : cadences[i].Name + " - every " + ticks + " ticks";

                changed |= PickPage.Apply(_rates.Entries[i], label, "", "", false, ticks == _ticks,
                    Editor.Register(Cmd.Choice, i, delegate
                    {
                        _ticks = ticks;
                        Editor.Invalidate();
                    }));

                var warning = string.IsNullOrEmpty(cadences[i].Warning) ? null : cadences[i].Warning;
                if (_rates.Entries[i].Tip != warning)
                {
                    _rates.Entries[i].Tip = warning;
                    changed = true;
                }
            }

            return changed;
        }
    }
}
