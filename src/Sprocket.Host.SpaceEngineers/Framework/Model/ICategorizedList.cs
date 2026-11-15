using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>A logic system's things of one kind as a screen lists them: in their order, each under its category, and the categories with none.</summary>
    public interface ICategorizedList
    {
        /// <summary>Every one of them, in their order.</summary>
        IReadOnlyList<ICategorized> Items { get; }

        /// <summary>The categories with nothing under them.</summary>
        IList<EmptyCategory> Empty { get; }

        /// <summary>Puts them in the order of <paramref name="order" />, which holds every one of them.</summary>
        void Arrange(IList<ICategorized> order);
    }
}
