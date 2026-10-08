using System;
using Lumel;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>Where one value of a form is given, whatever it is given with: a box, a switch, a few words, a color or a dial.</summary>
    public interface IField
    {
        /// <summary>Raised when somebody has changed the value, and never when <see cref="Text" /> is set.</summary>
        event Action Edited;

        /// <summary>The value as the field spells it, which is the way a program writes it.</summary>
        string Text { get; set; }

        /// <summary>Whether the field is marked as holding something it will not take.</summary>
        bool IsWrong { set; }

        /// <summary>What takes the keyboard for this field, or null for a field given by pressing alone.</summary>
        Element Keyboard { get; }
    }
}
