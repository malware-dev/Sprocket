using System;

namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    /// <summary>The broad kinds of value a part of an expression being built can still come to.</summary>
    [Flags]
    public enum KindClass
    {
        /// <summary>No kind: the part can never work.</summary>
        None = 0,

        /// <summary>An integer or a real.</summary>
        Number = 1,

        /// <summary>A text.</summary>
        Text = 2,

        /// <summary>A length of time.</summary>
        Duration = 4,

        /// <summary>On or off.</summary>
        Flag = 8,

        /// <summary>One of a type's choices.</summary>
        Choice = 16,

        /// <summary>Any kind, as an empty place can still be filled with.</summary>
        Any = Number | Text | Duration | Flag | Choice,
    }
}
