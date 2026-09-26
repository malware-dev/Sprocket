using System;

namespace Sprocket.Host.SpaceEngineers.Ui.Text
{
    /// <summary>What sets a run apart from the words around it; strong, emphasis and code combine.</summary>
    [Flags]
    public enum RunKind
    {
        /// <summary>Ordinary words.</summary>
        Plain = 0,

        /// <summary>Words given strong importance.</summary>
        Strong = 1,

        /// <summary>Words given emphasis, a lighter stress than strong.</summary>
        Emphasis = 2,

        /// <summary>Words that are code.</summary>
        Code = 4,

        /// <summary>A line break, carrying no words.</summary>
        Break = 8
    }
}
