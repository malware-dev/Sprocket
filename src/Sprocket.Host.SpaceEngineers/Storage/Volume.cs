using System;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>One blueprint in a player's library, written out alone.</summary>
    public sealed class Volume
    {
        /// <summary>The blueprint <paramref name="identity" />, by <paramref name="space" /> and called <paramref name="name" />, written as <paramref name="text" />.</summary>
        public Volume(Guid identity, string space, string name, DateTime saved, string text)
        {
            Identity = identity;
            Namespace = space;
            Name = name;
            Saved = saved;
            Text = text;
        }

        /// <summary>The blueprint's identity, which a copy of it on a construct shares.</summary>
        public Guid Identity { get; private set; }

        /// <summary>Who the blueprint is by, or empty.</summary>
        public string Namespace { get; private set; }

        /// <summary>What the blueprint is called.</summary>
        public string Name { get; private set; }

        /// <summary>When it was put in the library, in UTC.</summary>
        public DateTime Saved { get; private set; }

        /// <summary>The blueprint, as <c>ProgramText.Blueprint</c> writes it.</summary>
        public string Text { get; private set; }

        /// <summary>Puts this copy in place of what the library held.</summary>
        public void Replace(string space, string name, DateTime saved, string text)
        {
            Namespace = space;
            Name = name;
            Saved = saved;
            Text = text;
        }
    }
}
