namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Whether a construct may leave a part unfilled.</summary>
    sealed class SetOptional : SystemEdit
    {
        readonly Part _part;
        readonly bool _now;

        public SetOptional(Part part, bool now)
        {
            _part = part;
            _now = now;
        }

        public override Change Change => _now ? Change.Optional : Change.Required;

        public override bool Changes => _part.Optional != _now;

        public override bool Undoable(AutomationSystem system)
        {
            return _part.Optional == _now && system.FindPart(_part.Id) == _part;
        }

        public override bool Redoable(AutomationSystem system)
        {
            return _part.Optional != _now && system.FindPart(_part.Id) == _part;
        }

        public override void Apply(AutomationSystem system)
        {
            _part.Optional = _now;
        }

        public override void Undo(AutomationSystem system)
        {
            _part.Optional = !_now;
        }
    }
}
