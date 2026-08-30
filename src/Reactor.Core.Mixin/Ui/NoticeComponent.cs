namespace Reactor.Ui
{
    /// <summary>
    /// Something the user needs told.
    /// </summary>
    /// <remarks>
    /// Used above all for broken references. A sequence quietly doing nothing is
    /// indistinguishable from one that works, which is the exact shape of failure a
    /// non-programmer cannot diagnose - so it is never silent.
    /// </remarks>
    public sealed class NoticeComponent : EditorComponent
    {
        string _text;
        NoticeLevel _level;

        public NoticeComponent(string text, NoticeLevel level)
        {
            _text = text;
            _level = level;
        }

        public string Text
        {
            get { return _text; }
            set { Set(ref _text, value, "Text"); }
        }

        public NoticeLevel Level
        {
            get { return _level; }
            set { Set(ref _level, value, "Level"); }
        }
    }
}
