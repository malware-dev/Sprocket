using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems.Edits
{
    /// <summary>What a system says about itself: a blueprint's description.</summary>
    public sealed class AnnotateSystem : SystemEdit
    {
        readonly string _was;
        readonly string _now;

        /// <summary>Changes what a system says about itself from <paramref name="was" /> to <paramref name="now" />.</summary>
        public AnnotateSystem(string was, string now)
        {
            _was = was;
            _now = now;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.Describe;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = system;
        }

        /// <inheritdoc />
        public override bool Changes => _was != _now;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return system.Annotation == _now;
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return system.Annotation == _was;
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            system.Annotation = _now;
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            system.Annotation = _was;
        }
    }
}
