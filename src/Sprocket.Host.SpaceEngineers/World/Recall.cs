using System;
using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>What one reader of one block remembers of it between looks.</summary>
    /// <remarks>
    /// One per block a compiled step or trigger reads, made when it is compiled and let go when it is compiled again, so
    /// nothing it holds outlives what it is about and nothing finds it by a lookup. Kept in what the world remembers, so the
    /// first compile after a load takes up the one saved and the reader carries on as if nothing happened.
    /// <para>
    /// A class rather than a struct passed by reference, because a lambda taking a <c>ref</c> parameter has to spell out
    /// every parameter's type in C# 6.
    /// </para>
    /// <para>
    /// The same small shape for every member, so one list holds them all: a frame and a number covers a rotor's angle
    /// and the frame it was seen at, and a vent's last frame moving gas. A member that needs more is a question about
    /// the design, not a field to add.
    /// </para>
    /// </remarks>
    [ProtoContract]
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
        [ProtoMember(1, IsRequired = true)]
        public int Frame { get; set; }

        /// <summary>A reading taken then, where the frame alone is not the whole of it.</summary>
        [ProtoMember(2)]
        public double Number { get; set; }

        /// <summary>Whether anything has been seen.</summary>
        public bool Seen => Frame != Unseen;

        /// <summary>Sprocket's clock, which a reader stamps what it sees with; null where nothing gave it one.</summary>
        /// <remarks>Not kept: whatever takes the recall up after a load gives it the clock again.</remarks>
        public Func<int> Clock { get; set; }

        /// <summary>The tick a reader is reading on, or -1 where the recall has no clock.</summary>
        public int Now => Clock?.Invoke() ?? -1;
    }
}
