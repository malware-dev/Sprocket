namespace Lumel.Text
{
    /// <summary>One column of a table, as its delimiter cell says: <c>:---</c>, <c>:---:</c> or <c>---:</c> for where its cells sit, and a star for each share of the spare width it takes.</summary>
    public struct Column
    {
        /// <summary>A column aligned as <paramref name="align" /> says, taking <paramref name="shares" /> of the spare width.</summary>
        public Column(Alignment align, int shares)
        {
            Align = align;
            Shares = shares;
        }

        /// <summary>Where the column's cells sit across it.</summary>
        public Alignment Align { get; }

        /// <summary>How many shares of the spare width it takes; zero for a column as wide as its widest cell.</summary>
        public int Shares { get; }
    }
}
