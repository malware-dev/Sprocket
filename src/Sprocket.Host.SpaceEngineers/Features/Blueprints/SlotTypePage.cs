using System.Collections.Generic;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>
    /// The picker a block slot's type is chosen in, from its slot's page: the types the slot can take, wider and narrower than
    /// it takes now and down to one kind of block, each one only where it offers everything the slot is used for.
    /// </summary>
    public sealed class SlotTypePage : EditorPage
    {
        readonly AutomationSystem _system;
        readonly Part _part;
        readonly SlotUses _uses;
        readonly List<string> _types = new List<string>();
        readonly List<string> _words = new List<string>();
        string _chosen;
        PickStrip _strip;
        StageColumn _column;
        KeyedChildren<string, PickRow> _rows;
        ICommand _finish;

        /// <summary>The type of the block slot <paramref name="part" /> of <paramref name="system" />, chosen in <paramref name="editor" />.</summary>
        public SlotTypePage(SprocketEditor editor, AutomationSystem system, Part part) : base(editor)
        {
            _system = system;
            _part = part;
            _uses = new SlotUses(editor);
            _chosen = part.Expects;
        }

        /// <inheritdoc />
        public override string Title => _part.Id;

        /// <inheritdoc />
        public override bool Asks => true;

        /// <inheritdoc />
        public override string PrimaryText => Editor.Texts.ChromeDone;

        /// <inheritdoc />
        public override string PrimaryAbout => Editor.Texts.Codex.PickDoneAbout;

        /// <inheritdoc />
        public override bool ScrollsItself => true;

        /// <summary>The types offered, as a slot writes them, empty for any block, in the order they are listed.</summary>
        public IList<string> Offered => _types;

        /// <summary>The strip the types are listed in, once the page is built.</summary>
        public PickStrip Strip => _strip;

        /// <summary>Gives the slot the type chosen, asking first where it takes fewer kinds of block than the slot takes now.</summary>
        public override void Primary()
        {
            var chosen = _chosen;
            if (chosen == _part.Expects)
            {
                Editor.Pop();
                return;
            }

            if (!TypeTree.Narrows(Editor.World, _part, chosen))
            {
                Set(chosen);
                return;
            }

            var texts = Editor.Texts;
            Editor.Ask(texts.SlotNarrowQuestion);
            Editor.Answer(texts.SlotNarrowYes, () => Set(chosen));
            Editor.Answer(texts.SlotNarrowCancel, delegate { }, true);
        }

        /// <inheritdoc />
        public override void Build()
        {
            if (_strip == null)
            {
                _strip = new PickStrip(Editor.Texts);
                _finish = Editor.Act(CommandKind.Primary, Primary);
                _column = new StageColumn(_strip.Width, false, null, Editor.Texts);
                _column.Title = Editor.Texts.SlotExpects;
                _rows = new KeyedChildren<string, PickRow>(_column.Rows, Row);
                List();
            }

            if (_strip.Stages.Count == 0)
                _strip.Add(_column);

            _strip.Returns = null;
            _strip.ShowsArguments = false;

            _rows.Show(_types);
            for (var i = 0; i < _types.Count; i++)
            {
                var row = _rows[_types[i]];
                row.Text = _words[i];
                row.Many = false;
                row.IsChosen = _types[i] == _chosen;
                row.Show();
            }

            _column.ShowEmpty();
            _strip.Show();
        }

        /// <inheritdoc />
        public override void Refresh()
        {
            _strip.Tick();
        }

        /// <inheritdoc />
        protected override Element CreateView()
        {
            return _strip;
        }

        PickRow Row(string type)
        {
            var row = new PickRow();
            row.Picked += (held, presses) =>
            {
                var choose = Editor.Act(CommandKind.PickChoice, delegate
                {
                    _chosen = type;
                    Editor.Invalidate();
                });
                if (choose == null)
                    return;

                choose.Execute();
                if (presses > 1 && _finish != null)
                    _finish.Execute();
            };

            return row;
        }

        /// <summary>
        /// Lists the types: any block, then every type and kind of block of the construct that the slot's type takes or that
        /// takes it, widest first, each only where it offers everything the slot is used for; the slot's own always.
        /// </summary>
        void List()
        {
            var counts = new Dictionary<string, int>();
            var words = new Dictionary<string, string>();
            var texts = Editor.Texts;
            counts[""] = int.MaxValue;
            words[""] = texts.SlotAnyBlock;

            var kinds = new List<World.EntityKind>();
            foreach (var entity in Editor.World.Entities)
            {
                if (!kinds.Contains(entity.Kind))
                    kinds.Add(entity.Kind);
            }

            foreach (var kind in kinds)
            {
                var lineage = kind.Lineage;
                if (!_part.Fits(lineage))
                    continue;

                for (var i = 0; i < lineage.Count; i++)
                {
                    var type = lineage[i];
                    if (words.ContainsKey(type))
                        continue;

                    words[type] = i == 0 ? kind.DisplayName : TypeText.Of(texts, type);
                    counts[type] = Covered(kinds, type);
                }
            }

            if (!words.ContainsKey(_part.Expects))
            {
                words[_part.Expects] = TypeText.Of(texts, _part.Expects);
                counts[_part.Expects] = 0;
            }

            var order = new List<string>(words.Keys);
            order.Sort((one, other) =>
            {
                var wider = counts[other].CompareTo(counts[one]);
                return wider != 0 ? wider : string.CompareOrdinal(words[one], words[other]);
            });

            foreach (var type in order)
            {
                if (type != _part.Expects && _uses.Missing(_system, _part, type) != null)
                    continue;

                _types.Add(type);
                _words.Add(words[type]);
            }
        }

        /// <summary>How many of <paramref name="kinds" /> a slot of <paramref name="type" /> takes.</summary>
        static int Covered(List<World.EntityKind> kinds, string type)
        {
            var count = 0;
            foreach (var kind in kinds)
            {
                if (Part.Takes(type, kind.Lineage))
                    count++;
            }

            return count;
        }

        /// <summary>Gives the slot <paramref name="type" /> as one step back, and goes back to its page.</summary>
        void Set(string type)
        {
            Editor.Do(_system, new SetExpects(_part, type));
            Editor.Pop();
        }
    }
}
