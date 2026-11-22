using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;

namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    /// <summary>A value in an expression being built: a fixed value, a setting, a state or a block value read.</summary>
    public sealed class DraftValue : DraftPiece
    {
        /// <summary>The value <paramref name="atom" /> is.</summary>
        public DraftValue(Expression atom)
        {
            Atom = atom;
        }

        /// <summary>The value, as the language holds it.</summary>
        public Expression Atom { get; set; }

        /// <inheritdoc />
        public override DraftPiece Copied() => new DraftValue(Atom.Copied());
    }
}
