using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Gives the construct another name, which is the game's own name for its grid.</summary>
    public sealed class RenameConstruct : ProgramEdit
    {
        readonly GridWorld _world;
        readonly string _was;
        readonly string _now;

        /// <summary>Renames the construct of <paramref name="world" />, called <paramref name="was" />, to <paramref name="now" />.</summary>
        public RenameConstruct(GridWorld world, string was, string now)
        {
            _world = world;
            _was = was;
            _now = now;
        }

        /// <inheritdoc />
        public override Change Change => Change.RenameConstruct;

        /// <inheritdoc />
        public override bool Changes => _was != _now;

        /// <inheritdoc />
        public override bool ChangesShape => false;

        /// <summary>Only while the grid is still called what this named it; a name given in the game since is the player's.</summary>
        public override bool Undoable(Automation program)
        {
            return _world.Name == _now;
        }

        /// <inheritdoc />
        public override bool Redoable(Automation program)
        {
            return _world.Name == _was;
        }

        /// <inheritdoc />
        public override void Apply(Automation program)
        {
            _world.Rename(_now);
        }

        /// <inheritdoc />
        public override void Undo(Automation program)
        {
            _world.Rename(_was);
        }
    }
}
