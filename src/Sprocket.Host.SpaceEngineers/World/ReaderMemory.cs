using System;
using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>What one reader of one block remembers of it between looks.</summary>
    /// <remarks>
    /// Made when a step or trigger is compiled, and saved with the world, so the first compile after a load takes up the one
    /// saved.
    /// </remarks>
    [ProtoContract]
    public sealed class ReaderMemory
    {
        /// <summary>The frame of a recall that has seen nothing.</summary>
        public const int Unseen = int.MinValue;

        /// <summary>A recall with nothing seen yet.</summary>
        public ReaderMemory()
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
