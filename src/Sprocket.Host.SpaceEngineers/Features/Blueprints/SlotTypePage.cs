using System.Collections.Generic;
using Lumel;
using Lumel.Input;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>
    /// The picker a block slot's type is chosen in, from its slot's page: the kinds selector the block picker narrows with, offering
    /// any block, each kind of block on the construct, and each family as the type its kinds share, each only where it offers
    /// everything the slot is used for.
    /// </summary>
    public sealed class SlotTypePage : EditorPage
    {
        readonly AutomationSystem _system;
        readonly Part _part;
        readonly SlotUses _uses;
        readonly KindFilter _kinds = new KindFilter();
        readonly Dictionary<string, string> _typeOf = new Dictionary<string, string>();
        readonly List<string> _offered = new List<string>();
        string _chosen;
        PickStrip _strip;
        StageColumn _column;
        KindRows _rows;

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
        public override string PrimaryAbout => "";

        /// <inheritdoc />
        public override bool ScrollsItself => true;

        /// <summary>The types that can be chosen, as a slot writes them, empty for any block, in the order their rows are listed.</summary>
        public IList<string> Offered => _offered;

        /// <summary>The key of the row that chooses <paramref name="type" />, or null where no row does.</summary>
        public string KeyOf(string type)
        {
            foreach (var pair in _typeOf)
            {
                if (pair.Value == type)
                    return pair.Key;
            }

            return null;
        }

        /// <summary>The kinds selector the types are listed in, once the page is built.</summary>
        public KindRows Kinds => _rows;

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
                _column = new StageColumn(_strip.Width, false, null, Editor.Texts);
                _column.Title = Editor.Texts.SlotExpects;
                _column.Narrowed += Narrow;
                _rows = new KindRows(_kinds, _column.Rows, Press);
                List();
            }

            if (_strip.Stages.Count == 0)
                _strip.Add(_column);

            _strip.Returns = null;
            _strip.ShowsArguments = false;

            Choose(_chosen);
            // Any block is offered only where any block can do what the slot is used for.
            _rows.Show(Editor.Texts, _typeOf.ContainsKey("") ? Editor.Texts.SlotAnyBlock : null, null);
            _column.NarrowText = _kinds.Search;
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

        /// <summary>What pressing the row kept under <paramref name="key" /> does: chooses its type, or nothing where the type does not offer what the slot is used for.</summary>
        ICommand Press(string key)
        {
            string type;
            if (!_typeOf.TryGetValue(key, out type))
                return null;

            return Editor.Act(CommandKind.PickChoice, delegate
            {
                _chosen = type;
                Editor.Invalidate();
            });
        }

        /// <summary>Narrows the kinds listed to those answering <paramref name="said" />.</summary>
        void Narrow(string said)
        {
            said = said ?? "";
            if (said == _kinds.Search)
                return;

            var narrow = Editor.Act(CommandKind.NarrowKinds, delegate
            {
                _kinds.Search = said;
                Editor.Invalidate();
            });

            if (narrow != null)
                narrow.Execute();
        }

        /// <summary>Marks the row that chooses <paramref name="type" /> as the one in force, or none where no row does.</summary>
        void Choose(string type)
        {
            var key = KeyOf(type);
            var family = key == null ? Family.None : KindFilter.FamilyOf(key);
            if (family != Family.None)
                _kinds.SetFamily(family);
            else
                _kinds.Set(key ?? "");
        }

        /// <summary>
        /// Offers the kinds of block on the construct the slot takes and that offer everything it is used for, each family
        /// meaning the type its kinds share, and works out which rows choose a type that does too; the slot's own always.
        /// </summary>
        void List()
        {
            foreach (var entity in Editor.World.Entities)
            {
                var kind = entity.Kind;
                if (!_kinds.Present.Contains(kind) && _part.Fits(kind.Lineage) && Takes(kind.Lineage[0]))
                    _kinds.Present.Add(kind);
            }

            Offer("", "");
            var families = new Dictionary<Family, List<IList<string>>>();
            foreach (var kind in _kinds.Present)
            {
                Offer(kind.Id, kind.Lineage[0]);
                if (kind.Family == Family.None)
                    continue;

                List<IList<string>> lineages;
                if (!families.TryGetValue(kind.Family, out lineages))
                    families.Add(kind.Family, lineages = new List<IList<string>>());

                lineages.Add(kind.Lineage);
            }

            foreach (var pair in families)
                Offer(_kinds.FamilyKey(pair.Key), Shared(pair.Value));

            _kinds.Fill(Editor.Texts);
            foreach (var key in _kinds.Keys)
            {
                string type;
                if (_typeOf.TryGetValue(key, out type))
                    _offered.Add(type);
            }
        }

        /// <summary>The type a family of kinds of <paramref name="lineages" /> stands for: the one they all share, above its kind where it has one, or null where there is none.</summary>
        static string Shared(List<IList<string>> lineages)
        {
            if (lineages.Count > 1)
                return Part.Nearest(lineages);

            var only = lineages[0];
            return only.Count > 1 ? only[1] : null;
        }

        /// <summary>Lets the row kept under <paramref name="key" /> choose <paramref name="type" />, where the slot can take it.</summary>
        void Offer(string key, string type)
        {
            if (type != null && Takes(type))
                _typeOf[key] = type;
        }

        /// <summary>Whether the slot can take <paramref name="type" />: it is the slot's own, or offers everything the slot is used for.</summary>
        bool Takes(string type)
        {
            return type == _part.Expects || _uses.Missing(_system, _part, type) == null;
        }

        /// <summary>Gives the slot <paramref name="type" /> as one step back, and goes back to its page.</summary>
        void Set(string type)
        {
            Editor.Do(_system, new SetExpects(_part, type));
            Editor.Pop();
        }
    }
}
