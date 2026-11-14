using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Installations.Edits
{
    /// <summary>Gives an installation another name.</summary>
    public sealed class RenameInstallation : SystemEdit, IInstallationEdit
    {
        readonly string _was;
        readonly string _now;

        /// <summary>Renames <paramref name="instance" />, called <paramref name="was" />, to <paramref name="now" />.</summary>
        public RenameInstallation(SystemInstance instance, string was, string now)
        {
            Instance = instance;
            _was = was;
            _now = now;
        }

        /// <inheritdoc />
        public SystemInstance Instance { get; }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.RenameInstallation;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = Instance;
            name.Was = _was;
            name.Now = _now;
        }

        /// <inheritdoc />
        public override void Touches(AutomationSystem system, IList<object> screens)
        {
            Touch(screens, Instance);
        }

        /// <inheritdoc />
        public override bool Changes => _was != _now;

        /// <inheritdoc />
        public override bool ChangesShape => false;

        /// <summary>An installation is renamed whether or not it runs, so taking the name back waits for nothing either.</summary>
        public override bool Held(AutomationSystem system, bool back)
        {
            return false;
        }

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return Has(system, Instance) && Instance.Name == _now;
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return Has(system, Instance) && Instance.Name == _was;
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            Instance.Name = _now;
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            Instance.Name = _was;
        }
    }
}
