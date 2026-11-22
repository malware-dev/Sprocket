namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    /// <summary>A part of an expression being built: an empty place, a value, or a box joining values.</summary>
    public abstract class DraftPiece
    {
        /// <summary>The box this stands in, or null where it is the whole expression.</summary>
        public DraftJoin Parent { get; set; }

        /// <summary>This again, with copies of everything it holds, standing in no box.</summary>
        public abstract DraftPiece Copied();
    }
}
