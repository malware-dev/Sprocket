using Lumel.Layout;
using Lumel.Styling;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>What a member's name looks like in one state, on top of what any stack does: the face, size and color of its words.</summary>
    public class MemberNameLook : StackLook
    {
        Color? _color;
        string _font;
        float? _fontSize;

        /// <summary>Which face the words are drawn in, or null to leave it to the looks beneath.</summary>
        public string Font
        {
            get { return _font; }
            set
            {
                _font = value;
                OnChanged();
            }
        }

        /// <summary>How tall a line of the words is, in UI units, or null to leave it to the looks beneath.</summary>
        public float? FontSize
        {
            get { return _fontSize; }
            set
            {
                _fontSize = value;
                OnChanged();
            }
        }

        /// <summary>The color of the words, or null to leave it to the looks beneath.</summary>
        public Color? Color
        {
            get { return _color; }
            set
            {
                _color = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as MemberNameLook;
            if (look == null)
                return;

            _font = look._font ?? _font;
            _fontSize = look._fontSize ?? _fontSize;
            _color = look._color ?? _color;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _font = null;
            _fontSize = null;
            _color = null;
        }
    }
}
