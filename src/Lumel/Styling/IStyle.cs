
namespace Lumel.Styling
{
    /// <summary>How an element looks, applied onto the element's own properties.</summary>
    public interface IStyle
    {
        /// <summary>Writes this style onto <paramref name="element" /> as it looks in the states it is in now.</summary>
        void Apply(Element element);

        /// <summary>Puts back to its neutral value everything this style sets on <paramref name="element" />, for an element taking another style.</summary>
        void Withdraw(Element element);
    }
}
