namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>What a part says about itself.</summary>
    sealed class AnnotatePart : SystemEdit
    {
        readonly Part _part;
        readonly string _was;
        readonly string _now;
        readonly Change _change;

        public AnnotatePart(Part part, string now, Change change)
        {
            _part = part;
            _was = part.Annotation;
            _now = now;
            _change = change;
        }

        public override Change Change => _change;

        /// <summary>Only what the slot says changes, and its box already says it.</summary>
        public override bool ChangesShape => false;

        public override bool Changes => _was != _now;

        public override bool Undoable(AutomationSystem system)
        {
            return _part.Annotation == _now && system.FindPart(_part.Id) == _part;
        }

        public override bool Redoable(AutomationSystem system)
        {
            return _part.Annotation == _was && system.FindPart(_part.Id) == _part;
        }

        public override void Apply(AutomationSystem system)
        {
            _part.Annotation = _now;
        }

        public override void Undo(AutomationSystem system)
        {
            _part.Annotation = _was;
        }
    }
}
