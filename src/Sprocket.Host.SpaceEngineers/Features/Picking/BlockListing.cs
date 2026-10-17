using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>The blocks a pick lists: every block on offer by name, the ones listed ahead first, and those the narrowing leaves.</summary>
    public sealed class BlockListing
    {
        static readonly NameOrder ByName = new NameOrder();

        readonly SprocketEditor _editor;
        readonly Func<GridEntity, bool> _admits;
        readonly bool _forEvents;
        readonly bool _forTests;
        readonly List<GridEntity> _candidates = new List<GridEntity>();
        readonly List<GridEntity> _filtered = new List<GridEntity>();
        readonly List<long> _filteredIds = new List<long>();
        readonly HashSet<long> _wasShown = new HashSet<long>();
        readonly Dictionary<long, string> _ahead = new Dictionary<long, string>();
        string _narrowedSearch;
        string _narrowedKind;

        /// <summary>
        /// The blocks listed by a pick in <paramref name="editor" />, of those <paramref name="admits" /> allows, or all where it
        /// is null: able to start something where <paramref name="forEvents" />, to be checked where <paramref name="forTests" />,
        /// and otherwise to be acted on.
        /// </summary>
        public BlockListing(SprocketEditor editor, Func<GridEntity, bool> admits, bool forEvents, bool forTests)
        {
            _editor = editor;
            _admits = admits;
            _forEvents = forEvents;
            _forTests = forTests;
        }

        /// <summary>Every block on offer, by name, the ones listed ahead first.</summary>
        public List<GridEntity> Candidates => _candidates;

        /// <summary>The blocks the narrowing leaves listed, in order.</summary>
        public List<GridEntity> Filtered => _filtered;

        /// <summary>The ids of <see cref="Filtered" />, in the same order.</summary>
        public List<long> FilteredIds => _filteredIds;

        /// <summary>Blocks listed ahead of the rest, by entity id, each with what is said of it in place of its kind.</summary>
        public Dictionary<long, string> Ahead => _ahead;

        /// <summary>
        /// Works out the blocks on offer, which does not depend on what has been picked, and the kinds of them into
        /// <paramref name="kinds" />: those <paramref name="fits" /> allows where it is given, those able to do what is
        /// asked otherwise, narrowed by <paramref name="search" /> and by <paramref name="kind" /> or
        /// <paramref name="family" />, keeping <paramref name="selection" /> listed, and the blocks hidden from toolbars only
        /// where <paramref name="showsHidden" />.
        /// </summary>
        /// <remarks>
        /// Ticking a block narrows what can be said about it in the next stage and nothing else, so nobody watches the list of
        /// their own blocks shrink underneath them. Narrowing is the one thing that can hide a row, and a picked block is
        /// exempt: a tick nobody can find is a tick nobody can clear. A row stays on the list until the narrowing changes, so a
        /// block kept only because it was picked is still there after somebody picks another in its place. Sorted by name,
        /// because the order the world hands them over is whatever walking the grid produced.
        /// <para>
        /// A block hidden from toolbars is left out until the eye is opened, as the game's toolbar list leaves it out, unless
        /// it is picked.
        /// </para>
        /// </remarks>
        public void Collect(List<long> selection, string search, string kind, Family family, bool showsHidden,
            Func<GridEntity, bool> fits, List<EntityKind> kinds)
        {
            _candidates.Clear();

            if (fits != null)
            {
                var entities = _editor.World.Entities;
                for (var i = 0; i < entities.Count; i++)
                {
                    if (fits(entities[i]) && Listed(entities[i], selection, showsHidden))
                        _candidates.Add(entities[i]);
                }
            }
            else
            {
                var capable = _forTests ? Vocabulary.Testable(_editor.World) : Vocabulary.Capable(_editor.World, _forEvents);
                foreach (var entity in capable)
                {
                    if ((_admits == null || _admits(entity)) && Listed(entity, selection, showsHidden))
                        _candidates.Add(entity);
                }
            }

            _candidates.Sort(ByName);
            Lead();

            kinds.Clear();
            EntityQuery.KindsPresent(_candidates, kinds);

            _wasShown.Clear();
            if (search == _narrowedSearch && kind == _narrowedKind)
            {
                foreach (var id in _filteredIds)
                    _wasShown.Add(id);
            }

            _narrowedSearch = search;
            _narrowedKind = kind;

            _filtered.Clear();
            _filteredIds.Clear();
            foreach (var entity in _candidates)
            {
                if (!_wasShown.Contains(entity.Id) && !Contains(selection, entity.Id)
                    && !EntityQuery.Matches(entity, search, kind, family))
                    continue;

                _filtered.Add(entity);
                _filteredIds.Add(entity.Id);
            }
        }

        /// <summary>Moves the blocks listed ahead of the rest to the front, keeping their order among themselves and the rest's among theirs.</summary>
        void Lead()
        {
            if (_ahead.Count == 0)
                return;

            var at = 0;
            for (var i = 0; i < _candidates.Count; i++)
            {
                if (!_ahead.ContainsKey(_candidates[i].Id))
                    continue;

                var entity = _candidates[i];
                _candidates.RemoveAt(i);
                _candidates.Insert(at++, entity);
            }
        }

        /// <summary>Whether <paramref name="entity" /> is listed: shown in toolbars, or the eye open by <paramref name="showsHidden" />, or among <paramref name="selection" /> already.</summary>
        static bool Listed(GridEntity entity, List<long> selection, bool showsHidden)
        {
            return showsHidden || entity.Block == null || entity.Block.ShowInToolbarConfig || Contains(selection, entity.Id);
        }

        /// <summary>Where the block <paramref name="entityId" /> is among the blocks listed, or -1 where it is not listed.</summary>
        public int Index(long entityId)
        {
            for (var i = 0; i < _filtered.Count; i++)
            {
                if (_filtered[i].Id == entityId)
                    return i;
            }

            return -1;
        }

        /// <summary>How many of the blocks on offer are of <paramref name="family" />.</summary>
        public int CountOfFamily(Family family)
        {
            var count = 0;
            foreach (var candidate in _candidates)
            {
                if (candidate.Kind.Family == family)
                    count++;
            }

            return count;
        }

        /// <summary>How many of the blocks on offer are of the kind <paramref name="kindId" />.</summary>
        public int CountOfKind(string kindId)
        {
            var count = 0;
            foreach (var candidate in _candidates)
            {
                if (candidate.Kind.Id == kindId)
                    count++;
            }

            return count;
        }

        /// <summary>Whether <paramref name="ids" /> holds <paramref name="id" />.</summary>
        public static bool Contains(IList<long> ids, long id)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                if (ids[i] == id)
                    return true;
            }

            return false;
        }
    }
}
