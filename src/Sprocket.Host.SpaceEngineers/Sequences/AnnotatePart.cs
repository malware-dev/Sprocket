namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>What a part says about itself: its label, and what it is for after it.</summary>
    /// <remarks>Both are the part's one annotation, so renaming a slot and describing it are the same change in different words.</remarks>
    sealed class AnnotatePart : SystemEdit
    {
        readonly Part _part;
        readonly string _was;
        readonly string _now;
        readonly string _label;

        public AnnotatePart(Part part, string now, string label)
        {
            _part = part;
            _was = part.Annotation;
            _now = now;
            _label = label;
        }

        public override string Label => _label;

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
