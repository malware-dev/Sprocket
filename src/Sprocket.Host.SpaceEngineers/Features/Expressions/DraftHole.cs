namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    /// <summary>An empty place in an expression being built, waiting for a value or a box.</summary>
    public sealed class DraftHole : DraftPiece
    {
        /// <inheritdoc />
        public override DraftPiece Copied() => new DraftHole();
    }
}
