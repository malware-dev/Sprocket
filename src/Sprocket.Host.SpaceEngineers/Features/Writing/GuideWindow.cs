using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;
using Lumel.Text;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Writing
{
    /// <summary>The guide to rich text, read in a window that floats beside the screen while the text is written.</summary>
    public partial class GuideWindow : FloatingWindow
    {
        const float Wide = 700f;
        const float High = 860f;
        const float Gap = 16f;

        /// <summary>The guide in the editor <paramref name="editor" />, in its words, where its window was last moved to.</summary>
        public GuideWindow(SprocketEditor editor)
        {
            CreateView();
            Show(editor.Texts);
            Position = editor.Habits.GuideAt;
            Moved += () => editor.Habits.GuideAt = Position;
        }

        /// <summary>Reads the guide again from <paramref name="texts" />, in the language they now say it in.</summary>
        /// <remarks>The page's title, a heading of the first level it opens with, is the window's title.</remarks>
        public void Show(Texts texts)
        {
            Document title;
            _body.Document = Markdown.Parse(texts.GuideRichText).Untitled(out title);
            _title.Document = title;
            _close.Tip = Lumel.Tip.For(_close.Tip, texts.Codex.ChromeClose);
        }

        /// <inheritdoc />
        protected override Vector2 Home(RectangleF area, Vector2 size)
        {
            var right = area.X + area.Width;
            var room = Screen.Width > 0f ? Screen.X + Screen.Width - right : 0f;
            var x = room >= size.X + 2f * Gap ? right + Gap : right - size.X - Gap;
            return new Vector2(x, area.Y + (area.Height - size.Y) * 0.5f);
        }
    }
}
