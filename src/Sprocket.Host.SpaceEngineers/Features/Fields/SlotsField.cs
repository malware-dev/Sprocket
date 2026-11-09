using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>A value that is slots of a logic system, chosen on a selector: the chip a sentence names them with, spelled as their names with commas between.</summary>
    public class SlotsField : VStack, IField
    {
        readonly SlotChip _select;
        readonly string _unselected;
        readonly string _mark;
        string _text = "";

        /// <summary>A chip that says <paramref name="unselected" /> until slots are given and then names them under <paramref name="mark" />, and raises <see cref="Selecting" /> when pressed.</summary>
        public SlotsField(string unselected, string mark)
        {
            _unselected = unselected;
            _mark = mark;
            Align = Align.Right;
            _select = new SlotChip { Focusable = true };
            _select.Command = new RelayCommand(Select);
            Children.Add(_select);
            Text = "";
        }

        /// <summary>What heads the selector it opens.</summary>
        public string Label { get; set; } = "";

        /// <summary>Raised when somebody asks to choose the slots.</summary>
        public event Action<SlotsField> Selecting;

        /// <inheritdoc />
        public event Action Edited;

        /// <inheritdoc />
        public string Text
        {
            get { return _text; }
            set
            {
                _text = value ?? "";
                _select.IsEmpty = _text.Length == 0;
                if (_select.IsEmpty)
                {
                    _select.Say(_unselected);
                    return;
                }

                var names = new List<string>();
                var marks = new List<string>();
                foreach (var name in _text.Split(','))
                {
                    names.Add(name.Trim());
                    marks.Add(_mark);
                }

                var pieces = new List<Mention>();
                SentenceText.Slots(names, marks, pieces);
                _select.Say(pieces);
            }
        }

        /// <inheritdoc />
        public bool IsWrong
        {
            set { Base.Border = value ? Palette.Problem : Palette.None; }
        }

        /// <inheritdoc />
        public Element Keyboard => _select;

        /// <summary>Asks to choose the slots, as pressing the button does.</summary>
        public void Select()
        {
            Selecting?.Invoke(this);
        }

        /// <summary>Holds the slots <paramref name="names" />, as somebody having chosen them.</summary>
        public void Give(string names)
        {
            Text = names;
            Edited?.Invoke();
        }
    }
}
