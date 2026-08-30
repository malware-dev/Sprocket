using System.Collections.Generic;

namespace Reactor.Sequences
{
    /// <summary>
    /// Started by name from elsewhere, rather than by anything happening.
    /// </summary>
    /// <remarks>
    /// What lets two buttons run the same sequence, and what lets a sequence be a
    /// thing the ship does rather than a thing a particular block causes.
    /// </remarks>
    public sealed class CalledTrigger : Trigger
    {
    }
}
