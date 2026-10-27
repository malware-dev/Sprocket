using Lumel.Styling;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>How a member's name looks: the face, size and color of its words.</summary>
    public class MemberNameStyle : Style<MemberName, TextLook>
    {
        /// <summary>A style with nothing set, which applies as the neutral values.</summary>
        public MemberNameStyle()
        {
        }

        /// <summary>A style that is everything <paramref name="basedOn" /> is, with whatever this sets laid over it.</summary>
        public MemberNameStyle(IStyle<MemberName> basedOn) : base(basedOn)
        {
        }
    }
}
