namespace Reactor.Host.SpaceEngineers.Core.Ui
{
    /// <summary>A label introducing the section under it.</summary>
    public sealed class HeadingComponent : EditorComponent
    {
        string _text;

        public HeadingComponent(string text)
        {
            _text = text;
        }

        public string Text
        {
            get { return _text; }
            set { Set(ref _text, value, "Text"); }
        }
    }
}
