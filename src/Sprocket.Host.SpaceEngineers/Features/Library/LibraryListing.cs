using Sprocket.Host.SpaceEngineers.Features.Sharing.Messages;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System;

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

        /// <summary>The share it stands for, or null where it is not one.</summary>
        public ShareLine Share;

        /// <summary>The blueprint as read from its backup or the library, or null until it has been read, or where it would not read.</summary>
        public AutomationSystem Read;

        /// <summary>Why it would not read, or nothing.</summary>
        public string Trouble = "";

        /// <summary>Its row in the list.</summary>
        public StatusRow Row;

        /// <summary>
        /// The logic system on <paramref name="program" /> it is of, or null: itself, the one its backup stores, or the one made from
        /// its blueprint in the library.
        /// </summary>
        public AutomationSystem LiveIn(Automation program)
        {
            var system = Section == LibrarySection.Saved ? program.MadeFrom(Identity) : program.FindSystem(Identity);
            return system != null && system.IsBlueprint ? system : null;
        }

        /// <summary>Whether <paramref name="other" /> stands for the same thing in the same part of the library.</summary>
        public bool Same(LibraryListing other)
        {
            return other != null && other.Section == Section && other.Identity == Identity && other.Backup == Backup;
        }
    }
}
