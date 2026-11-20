using System;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage
{
    /// <summary>One blueprint in a player's library, stored in a file of its own.</summary>
    public sealed class LibraryEntry
    {
        /// <summary>The blueprint <paramref name="identity" />, by <paramref name="space" /> and called <paramref name="name" />, stored in <paramref name="file" /> as <paramref name="text" />.</summary>
        public LibraryEntry(string file, Guid identity, string space, string name, DateTime? changed, string text)
        {
            File = file;
            Identity = identity;
            Namespace = space;
            Name = name;
            Changed = changed;
            Text = text;
            State = LibraryEntryState.Read;
            Trouble = "";
        }

        /// <summary>A line of the index naming <paramref name="file" />, which did not give a blueprint, for the reason <paramref name="trouble" />.</summary>
        public LibraryEntry(string file, LibraryEntryState state, string trouble)
        {
            File = file;
            Namespace = "";
            Name = "";
            Text = "";
            State = state;
            Trouble = trouble;
        }

        /// <summary>The name of the file it is stored in, as the index gives it.</summary>
        public string File { get; private set; }

        /// <summary>The blueprint's identity, which a copy of it on a construct shares; empty where it did not read.</summary>
        public Guid Identity { get; private set; }

        /// <summary>Who the blueprint is by, or empty.</summary>
        public string Namespace { get; private set; }

        /// <summary>What the blueprint is called; empty where it did not read.</summary>
        public string Name { get; private set; }

        /// <summary>When the blueprint was last changed, as its own text says, or null where it says nothing.</summary>
        public DateTime? Changed { get; private set; }

        /// <summary>The blueprint, as <c>ProgramWriter.Blueprint</c> writes it.</summary>
        public string Text { get; private set; }

        /// <summary>Whether the file gave a blueprint, and why not where it did not.</summary>
        public LibraryEntryState State { get; private set; }

        /// <summary>What was wrong with the file, in the reader's words, or empty.</summary>
        public string Trouble { get; private set; }

        /// <summary>Puts this copy in place of what the library held.</summary>
        public void Replace(string space, string name, DateTime? changed, string text)
        {
            Namespace = space;
            Name = name;
            Changed = changed;
            Text = text;
        }

        /// <summary>Moves this copy to the file <paramref name="file" />.</summary>
        public void Move(string file)
        {
            File = file;
        }
    }
}
