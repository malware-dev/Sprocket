using Sprocket.Host.SpaceEngineers.Screen;

namespace Sprocket.Host.SpaceEngineers.Developing
{
    /// <summary>The program as the script it is saved as.</summary>
    /// <remarks>
    /// One long run of text and nothing else. What a target does about text longer than a screen is
    /// the target's business, and this says nothing about it.
    /// </remarks>
    public sealed class ScriptComponent : EditorComponent
    {
        string _text = "";

        public string Text
        {
            get { return _text; }
            set { Set(ref _text, value ?? "", nameof(Text)); }
        }
    }
}
