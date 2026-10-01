namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>What a system says about itself: a blueprint's description.</summary>
    sealed class Annotate : SystemEdit
    {
        readonly string _was;
        readonly string _now;

        public Annotate(string was, string now)
        {
            _was = was;
            _now = now;
        }

        public override Change Change => Change.Describe;

        public override bool Changes => _was != _now;

        public override bool Undoable(AutomationSystem system)
        {
            return system.Annotation == _now;
        }

        public override bool Redoable(AutomationSystem system)
        {
            return system.Annotation == _was;
        }

        public override void Apply(AutomationSystem system)
        {
            system.Annotation = _now;
        }

        public override void Undo(AutomationSystem system)
        {
            system.Annotation = _was;
        }
    }
}
