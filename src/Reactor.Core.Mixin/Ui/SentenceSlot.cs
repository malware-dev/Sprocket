using System.Collections.Generic;

namespace Reactor.Ui
{
    /// <summary>
    /// A fillable part of a sentence.
    /// </summary>
    /// <remarks>
    /// The only thing on screen that changes what a sequence means. A target must render
    /// these as unmistakably fillable, and must not give anything else the same look.
    /// </remarks>
    public sealed class SentenceSlot
    {
        public SentenceSlot(string text, bool isEmpty, int command)
        {
            Text = text;
            IsEmpty = isEmpty;
            Command = command;
        }

        public string Text { get; set; }

        /// <summary>Nothing chosen yet, so it should read as an invitation.</summary>
        public bool IsEmpty { get; set; }

        public int Command { get; set; }
    }
}
