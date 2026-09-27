
namespace Sprocket.Host.SpaceEngineers.Ui.Text
{
    /// <summary>What a block of a document is.</summary>
    public enum BlockKind
    {
        /// <summary>Running words.</summary>
        Paragraph,

        /// <summary>A heading, at the level the block carries.</summary>
        Heading,

        /// <summary>An item of a list whose items are marked alike.</summary>
        Bullet,

        /// <summary>An item of a list whose items are counted, at the number the block carries.</summary>
        Numbered,

        /// <summary>A line across, dividing what is above from what is below.</summary>
        Rule
    }
}
