using System;

namespace Lumel.Text
{
    partial class EditBox
    {
        /// <summary>A place between two characters: a line, and how far into it.</summary>
        struct Spot : IEquatable<Spot>
        {
            public readonly int Line;
            public readonly int Column;

            public Spot(int line, int column)
            {
                Line = line;
                Column = column;
            }

            public bool Before(Spot other)
            {
                return Line < other.Line || (Line == other.Line && Column < other.Column);
            }

            public bool Equals(Spot other)
            {
                return Line == other.Line && Column == other.Column;
            }
        }
    }
}
