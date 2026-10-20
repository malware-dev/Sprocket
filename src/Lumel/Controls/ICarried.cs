namespace Lumel.Controls
{
    /// <summary>A row of a <see cref="CarryList" />: taken by its grip, drawn as carried while it is, and carrying the rows under it that belong to it.</summary>
    public interface ICarried
    {
        /// <summary>What the row is taken and carried by.</summary>
        Grip Grip { get; }

        /// <summary>Whether the row is being carried, which it may draw as it likes.</summary>
        bool IsCarried { get; set; }

        /// <summary>How many of the rows under this one go with it when it is carried, none for a row that goes alone.</summary>
        int Carries { get; }
    }
}
