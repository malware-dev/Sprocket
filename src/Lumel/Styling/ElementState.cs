using VRageMath;

namespace Lumel.Styling
{
    /// <summary>What an element looks like in one state: only what that state sets, the rest left to the layers beneath.</summary>
    public class ElementState
    {
        /// <summary>What is painted behind the element, or null to leave it to the layers beneath.</summary>
        public Color? Background { get; set; }

        /// <summary>What is laid over the element's background, or null to leave it to the layers beneath.</summary>
        public Drawing.Pattern Pattern { get; set; }

        /// <summary>The hairline round the element, or null to leave it to the layers beneath.</summary>
        public Color? Edge { get; set; }

        /// <summary>The room held clear inside the element, or null to leave it to the layers beneath.</summary>
        public Thickness? Padding { get; set; }

        /// <summary>Writes the neutral value of each property this sets onto <paramref name="element" />, and nothing else.</summary>
        public virtual void Neutral(Element element)
        {
            if (Background.HasValue)
                element.Background = Color.Transparent;

            if (Pattern != null)
                element.Pattern = null;

            if (Edge.HasValue)
                element.Edge = Color.Transparent;

            var padded = element as IPadded;
            if (padded != null && Padding.HasValue)
                padded.Padding = new Thickness(0f);
        }

        /// <summary>Sets nothing, leaving every property to the layers beneath.</summary>
        public virtual void Clear()
        {
            Background = null;
            Pattern = null;
            Edge = null;
            Padding = null;
        }

        /// <summary>Takes each property <paramref name="layer" /> sets that this does not, so this ends up setting whatever any layer gathered sets.</summary>
        public virtual void Gather(ElementState layer)
        {
            if (!Background.HasValue)
                Background = layer.Background;

            if (Pattern == null)
                Pattern = layer.Pattern;

            if (!Edge.HasValue)
                Edge = layer.Edge;

            if (!Padding.HasValue)
                Padding = layer.Padding;
        }

        /// <summary>Writes what this state sets onto <paramref name="element" />, and nothing else.</summary>
        public virtual void Lay(Element element)
        {
            if (Background.HasValue)
                element.Background = Background.Value;

            if (Pattern != null)
                element.Pattern = Pattern;

            if (Edge.HasValue)
                element.Edge = Edge.Value;

            var padded = element as IPadded;
            if (padded != null && Padding.HasValue)
                padded.Padding = Padding.Value;
        }
    }
}
