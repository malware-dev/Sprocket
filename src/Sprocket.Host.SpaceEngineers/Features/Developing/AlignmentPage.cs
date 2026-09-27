using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Developing
{
    /// <summary>Glyphs and lines of text in boxes, with each box's middle drawn over them.</summary>
    /// <remarks>
    /// A developer tool and not part of the product, reached only where <c>developerMode</c> is on. It answers where a
    /// glyph lands in the room it is given by drawing it, in the game, beside a mark that cannot be wrong. The boxes are
    /// declared in <c>AlignmentPage.View.cs</c>.
    /// </remarks>
    sealed partial class AlignmentPage : EditorPage
    {
        /// <summary>What it is called, on its own heading and on the way in to it.</summary>
        public const string Called = "Alignment";

        /// <summary>The page of boxes, for the editor <paramref name="editor" />.</summary>
        public AlignmentPage(SprocketEditor editor) : base(editor)
        {
        }

        /// <inheritdoc />
        public override string Title => Called;

        /// <summary>Nothing to fill: every box is drawn from what it holds.</summary>
        public override void Build()
        {
        }
    }
}
