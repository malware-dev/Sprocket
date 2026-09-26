using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>What an element looks like in one state: only what that state sets, the rest left to the layers beneath.</summary>
    public class ElementState
    {
        /// <summary>What is painted behind the element, or null to leave it to the layers beneath.</summary>
        public Color? Background { get; set; }

        /// <summary>The hairline round the element, or null to leave it to the layers beneath.</summary>
        public Color? Edge { get; set; }

        /// <summary>The room held clear inside the element, or null to leave it to the layers beneath.</summary>
        public Thickness? Padding { get; set; }

        /// <summary>Writes the values every style starts from onto <paramref name="element" />.</summary>
        public virtual void Neutral(Element element)
        {
            element.Background = Color.Transparent;
            element.Edge = Color.Transparent;
            var padded = element as IPadded;
            if (padded != null)
                padded.Padding = new Thickness(0f);
        }

        /// <summary>Writes what this state sets onto <paramref name="element" />, and nothing else.</summary>
        public virtual void Lay(Element element)
        {
            if (Background.HasValue)
                element.Background = Background.Value;

            if (Edge.HasValue)
                element.Edge = Edge.Value;

            var padded = element as IPadded;
            if (padded != null && Padding.HasValue)
                padded.Padding = Padding.Value;
        }
    }
}
