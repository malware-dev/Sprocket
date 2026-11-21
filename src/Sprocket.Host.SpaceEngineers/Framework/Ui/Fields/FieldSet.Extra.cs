using Lumel;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Fields
{
    public partial class FieldSet
    {
        /// <summary>An element shown after a value, under a label of its own, that is not one of the values.</summary>
        sealed class Extra
        {
            /// <summary>The element <paramref name="element" /> under <paramref name="label" />, after the value at <paramref name="at" />.</summary>
            public Extra(int at, string label, Element element)
            {
                At = at;
                Label = label;
                Element = element;
            }

            /// <summary>Which value it comes after.</summary>
            public int At { get; }

            /// <summary>What its label says.</summary>
            public string Label { get; }

            /// <summary>The element itself.</summary>
            public Element Element { get; }
        }
    }
}
