using System;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;

namespace Sprocket.Host.SpaceEngineers.Features.Library
{
    /// <summary>One row of the library's list, and the blueprint it stands for.</summary>
    public sealed class LibraryListing
    {
        /// <summary>The part of the library it stands in.</summary>
        public LibrarySection Section;

        /// <summary>The identity of the logic system or blueprint it stands for.</summary>
        public Guid Identity;

        /// <summary>The backup it stands for, or null where it is not one.</summary>
        public Backup Backup;

        /// <summary>The blueprint as read from its backup or the library, or null until it has been read, or where it would not read.</summary>
        public AutomationSystem Read;

        /// <summary>Why it would not read, or nothing.</summary>
        public string Trouble = "";

        /// <summary>Its row in the list.</summary>
        public StatusRow Row;

        /// <summary>Whether <paramref name="other" /> stands for the same thing in the same part of the library.</summary>
        public bool Same(LibraryListing other)
        {
            return other != null && other.Section == Section && other.Identity == Identity && other.Backup == Backup;
        }
    }
}
