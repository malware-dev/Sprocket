using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Versioning;
using System;

namespace Sprocket.Host.SpaceEngineers.Features.Callouts
{
    /// <summary>A bubble pointing out something that arrived in a release: what it says, and the places it can hang from.</summary>
    public sealed class Callout
    {
        /// <summary>A callout recorded as <paramref name="id" />, made for release <paramref name="release" />, saying what <paramref name="said" /> gives, hung from the first of <paramref name="places" /> the player meets.</summary>
        public Callout(string id, ModRelease release, Func<Texts, string> said, params CalloutPlace[] places)
        {
            Id = id;
            Release = release;
            Said = said;
            Places = places;
        }

        /// <summary>The name a player's file records it by once it is dismissed.</summary>
        /// <remarks>Persisted in the player's global file.</remarks>
        public string Id { get; }

        /// <summary>The release it arrived in.</summary>
        public ModRelease Release { get; }

        /// <summary>The words of the callout, for the texts of the player's language.</summary>
        public Func<Texts, string> Said { get; }

        /// <summary>Where it can hang from, in the order they are looked for.</summary>
        public CalloutPlace[] Places { get; }
    }
}
