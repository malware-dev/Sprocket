using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>A value that is one of more words than fit on a line: a drop-down of the words, showing the one given.</summary>
    public class ListField : DropDown, IField
    {
        const float Mark = 12f;

        // Measured on the alignment page.
        static readonly Thickness Level = new Thickness(0f, -0.5f, 0f, 0.5f);

        readonly List<DropDownItem> _items = new List<DropDownItem>();

        string _text = "";

        /// <summary>A drop-down of <paramref name="words" />, giving none of them yet.</summary>
        public ListField(IList<string> words)
        {
            Style = Styles.ListBox;
            Focusable = true;

            foreach (var word in words)
            {
                var item = new DropDownItem { Style = Styles.MenuItem, Text = word };
                _items.Add(item);
                Items.Add(item);
            }

            ChoiceChanged += delegate
            {
                _text = Chosen == null ? "" : Chosen.Text;
                Hint = "";

                var edited = Edited;
                if (edited != null)
                    edited();
            };
        }

        /// <summary>The mark a line that opens a list ends with, placed as a drop-down's arrow is.</summary>
        public static Glyph Fold()
        {
            return new Glyph
            {
                Symbol = Symbols.Down, Style = Styles.ListMark, Placement = FixedSize.Size(Mark, Mark), Base = { Padding = Level }
            };
        }

        /// <inheritdoc />
        public event Action Edited;

        /// <inheritdoc />
        /// <remarks>A text that is none of the words is shown as it is, with none of them chosen.</remarks>
        public string Text
        {
            get { return _text; }
            set
            {
                _text = value ?? "";
                Chosen = Find(_text);
                Hint = Chosen == null ? _text : "";
            }
        }

        /// <inheritdoc />
        public bool IsWrong
        {
            set { Set(LookStates.Wrong, value); }
        }

        /// <inheritdoc />
        public Element Keyboard => this;

        /// <summary>Spells the word at <paramref name="index" /> as <paramref name="word" />, the box too where that word is the one given.</summary>
        public void Respell(int index, string word)
        {
            var item = _items[index];
            if (item.Text == word)
                return;

            if (Chosen == item)
                _text = word;

            item.Text = word;
            InvalidateMeasure();
        }

        /// <summary>Has the word at <paramref name="index" /> say <paramref name="about" /> under the pointer, in the list.</summary>
        public void Explain(int index, Tip about)
        {
            _items[index].Tip = about;
        }

        /// <summary>The item whose word is <paramref name="word" />, or null where none is.</summary>
        DropDownItem Find(string word)
        {
            foreach (var item in _items)
            {
                if (item.Text == word)
                    return item;
            }

            return null;
        }
    }
}
