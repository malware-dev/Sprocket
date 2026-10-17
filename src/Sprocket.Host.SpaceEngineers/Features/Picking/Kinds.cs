using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>The kinds of block a pick can be narrowed to: those on offer, grouped by family, and the kind or family in force.</summary>
    public sealed class Kinds
    {
        const string FamilyPrefix = "family:";

        readonly List<EntityKind> _present = new List<EntityKind>();
        readonly List<EntityKind> _shownKinds = new List<EntityKind>();
        readonly List<Family> _shownFamilies = new List<Family>();
        readonly List<string> _kindKeys = new List<string>();
        readonly Dictionary<Family, string> _familyKeys = new Dictionary<Family, string>();
        string _kind = "";
        Family _family;
        string _search = "";

        /// <summary>The kinds of the blocks on offer, filled as the blocks are worked out.</summary>
        public List<EntityKind> Present => _present;

        /// <summary>The kind the blocks are narrowed to, or a family's key, or empty for any.</summary>
        public string Kind => _kind;

        /// <summary>The family the blocks are narrowed to, or none.</summary>
        public Family Family => _family;

        /// <summary>What the box over the kinds holds.</summary>
        public string Search
        {
            get { return _search; }
            set { _search = value; }
        }

        /// <summary>The key of each row of the kinds listed, the row for any kind first.</summary>
        public List<string> Keys => _kindKeys;

        /// <summary>The kind of each row listed after the first, or null where the row is its family's.</summary>
        public List<EntityKind> Shown => _shownKinds;

        /// <summary>The family of each row listed after the first, or none.</summary>
        public List<Family> Families => _shownFamilies;

        /// <summary>The family a row's key stands for, or none where the key is a kind's.</summary>
        public static Family FamilyOf(string key)
        {
            return key.StartsWith(FamilyPrefix, StringComparison.Ordinal)
                ? (Family)Enum.Parse(typeof(Family), key.Substring(FamilyPrefix.Length))
                : Family.None;
        }

        /// <summary>Puts right a narrowing the blocks on offer no longer bear out; whether it changed.</summary>
        public bool Settle()
        {
            var changed = false;

            // A family left with one kind of block on the construct stands as that kind alone, and so does the narrowing to it.
            if (_family != Family.None && KindsOf(_family) == 1)
            {
                Set(OnlyKindOf(_family).Id);
                changed = true;
            }

            // A kind whose last block has gone from the construct would hide every block with nothing on screen to say why.
            if (_kind.Length > 0 && !HasKind(_kind))
            {
                Set("");
                changed = true;
            }

            return changed;
        }

        /// <summary>Lists the kinds on offer that answer the box over them, in <see cref="Shown" />, <see cref="Families" /> and <see cref="Keys" />, families named in <paramref name="texts" />.</summary>
        public void Fill(Texts texts)
        {
            _shownKinds.Clear();
            _shownFamilies.Clear();
            for (var i = 0; i < _present.Count; i++)
            {
                var family = _present[i].Family;
                if (family == Family.None || KindsOf(family) < 2)
                {
                    if (Offered(_present[i]))
                        Shows(_present[i], Family.None);

                    continue;
                }

                // A family is listed whole at its first kind, so its kinds stand together under it whatever their names.
                if (Listed(family, i))
                    continue;

                var named = EntityQuery.MatchesText(FamilyText.Of(texts, family), _search);
                var at = _shownKinds.Count;
                for (var j = i; j < _present.Count; j++)
                {
                    if (_present[j].Family == family && (named || Offered(_present[j])))
                        Shows(_present[j], family);
                }

                // The family in force is always offered, and a kind shown always has its family above it.
                if (named || _shownKinds.Count > at || _family == family)
                {
                    _shownKinds.Insert(at, null);
                    _shownFamilies.Insert(at, family);
                }
            }

            _kindKeys.Clear();
            _kindKeys.Add("");
            for (var i = 0; i < _shownKinds.Count; i++)
                _kindKeys.Add(_shownKinds[i] == null ? FamilyKey(_shownFamilies[i]) : _shownKinds[i].Id);
        }

        /// <summary>Narrows the blocks to the kind <paramref name="kindId" />, or to none where it is empty.</summary>
        public void Set(string kindId)
        {
            _kind = kindId ?? "";
            _family = Family.None;
        }

        /// <summary>Narrows the blocks to every kind of <paramref name="family" />.</summary>
        public void SetFamily(Family family)
        {
            _kind = FamilyKey(family);
            _family = family;
        }

        bool HasKind(string kindId)
        {
            foreach (var kind in _present)
            {
                if (kind.Id == kindId || (_family != Family.None && kind.Family == _family))
                    return true;
            }

            return false;
        }

        /// <summary>Whether <paramref name="kind" /> is offered on its own: it is the one in force, or its name answers the box.</summary>
        /// <remarks>The one in force is always offered, so what the line says is showing is always something the list says too.</remarks>
        bool Offered(EntityKind kind)
        {
            return kind.Id == _kind || EntityQuery.MatchesText(kind.DisplayName, _search);
        }

        void Shows(EntityKind kind, Family family)
        {
            _shownKinds.Add(kind);
            _shownFamilies.Add(family);
        }

        /// <summary>Whether a kind of <paramref name="family" /> comes before the one at <paramref name="index" />, which listed the family already.</summary>
        bool Listed(Family family, int index)
        {
            for (var i = 0; i < index; i++)
            {
                if (_present[i].Family == family)
                    return true;
            }

            return false;
        }

        /// <summary>How many kinds of block on offer are of <paramref name="family" />.</summary>
        int KindsOf(Family family)
        {
            var count = 0;
            foreach (var kind in _present)
            {
                if (kind.Family == family)
                    count++;
            }

            return count;
        }

        EntityKind OnlyKindOf(Family family)
        {
            foreach (var kind in _present)
            {
                if (kind.Family == family)
                    return kind;
            }

            return null;
        }

        /// <summary>The key the row of <paramref name="family" /> is kept under, made once for the pick.</summary>
        public string FamilyKey(Family family)
        {
            string key;
            if (!_familyKeys.TryGetValue(family, out key))
            {
                key = FamilyPrefix + family;
                _familyKeys.Add(family, key);
            }

            return key;
        }
    }
}
