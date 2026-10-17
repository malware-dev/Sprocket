namespace Lumel.Windows
{
    /// <summary>An element a window layer covers or holds open, whose layer only the layer sets.</summary>
    /// <remarks>Implemented explicitly by <see cref="Element" />.</remarks>
    public interface ILayered
    {
        /// <summary>The layer the element is the top of, or null once it is taken away.</summary>
        WindowLayer Layer { set; }
    }
}
