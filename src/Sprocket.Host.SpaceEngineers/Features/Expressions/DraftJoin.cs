using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    /// <summary>A box of an expression being built, joining the values in its places as its kind says.</summary>
    public sealed class DraftJoin : DraftPiece
    {
        readonly List<DraftPiece> _parts = new List<DraftPiece>();

        /// <summary>A box of <paramref name="kind" /> with every place it has empty.</summary>
        public DraftJoin(JoinKind kind)
        {
            Kind = kind;
            for (var i = 0; i < Places(kind); i++)
                Add(new DraftHole());
        }

        /// <summary>A box of <paramref name="kind" /> holding <paramref name="parts" />, in order.</summary>
        public DraftJoin(JoinKind kind, IEnumerable<DraftPiece> parts)
        {
            Kind = kind;
            foreach (var part in parts)
                Add(part);
        }

        /// <summary>What the box does with its values.</summary>
        public JoinKind Kind { get; }

        /// <summary>What stands in the box's places, in order.</summary>
        public IReadOnlyList<DraftPiece> Parts => _parts;

        /// <summary>How many places a new box of <paramref name="kind" /> has: one for a sign, a rounding or a removed sign, else two.</summary>
        public static int Places(JoinKind kind)
        {
            return kind == JoinKind.Negate || kind == JoinKind.Rounded || kind == JoinKind.WithoutSign ? 1 : 2;
        }

        /// <summary>Puts <paramref name="piece" /> in the place <paramref name="at" />, in place of what stood there.</summary>
        public void Set(int at, DraftPiece piece)
        {
            _parts[at].Parent = null;
            piece.Parent = this;
            _parts[at] = piece;
        }

        /// <summary>The place <paramref name="piece" /> stands in, or -1 where it is not in this box.</summary>
        public int IndexOf(DraftPiece piece) => _parts.IndexOf(piece);

        /// <inheritdoc />
        public override DraftPiece Copied()
        {
            var parts = new List<DraftPiece>();
            foreach (var part in _parts)
                parts.Add(part.Copied());

            return new DraftJoin(Kind, parts);
        }

        void Add(DraftPiece piece)
        {
            piece.Parent = this;
            _parts.Add(piece);
        }
    }
}
