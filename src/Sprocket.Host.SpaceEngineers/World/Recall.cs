namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>What one reader of one block remembers of it between looks.</summary>
    /// <remarks>
    /// One per block a compiled step or trigger reads, made when it is compiled and gone when it is compiled again, so
    /// nothing it holds outlives what it is about and nothing finds it by a lookup. A class rather than a struct passed
    /// by reference, because a lambda taking a <c>ref</c> parameter has to spell out every parameter's type in C# 6.
    /// <para>
    /// The same small shape for every member, so one list holds them all: a frame and a number covers a rotor's angle
    /// and the frame it was seen at, and a vent's last frame moving gas. A member that needs more is a question about
    /// the design, not a field to add.
    /// </para>
    /// </remarks>
    public sealed class Recall
    {
        /// <summary>The frame of a recall that has seen nothing.</summary>
        public const int Unseen = int.MinValue;

        /// <summary>A recall with nothing seen yet.</summary>
        public Recall()
        {
            Frame = Unseen;
        }

        /// <summary>The frame something was last seen on, or <see cref="Unseen" />.</summary>
        public int Frame { get; set; }

        /// <summary>A reading taken then, where the frame alone is not the whole of it.</summary>
        public double Number { get; set; }

        /// <summary>Whether anything has been seen.</summary>
        public bool Seen => Frame != Unseen;
    }
}
