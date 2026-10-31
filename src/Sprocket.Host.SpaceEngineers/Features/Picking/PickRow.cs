using System;
using Lumel;
using Lumel.Controls;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>One thing that can be picked in a column: a block, something done to blocks, a rate, a sequence, or a heading over a group of them.</summary>
    /// <remarks>
    /// Whoever holds it sets what it shows and calls <see cref="Show" />.
    /// </remarks>
    public partial class PickRow : HStack
    {

        HeldModifier _held;
        int _presses;
        string _art;

        /// <summary>A row saying nothing yet.</summary>
        public PickRow()
        {
            Text = "";
            Secondary = "";
            Grid = "";
            Status = "";
            CreateView();
        }

        /// <summary>Raised when the row is pressed, with what was held down and how many presses in quick succession it was.</summary>
        public event Action<HeldModifier, int> Picked;

        /// <summary>What it is called.</summary>
        public string Text { get; set; }

        /// <summary>Something quieter about it, such as a block's kind.</summary>
        public string Secondary { get; set; }

        /// <summary>The name of the grid <see cref="Secondary" /> says the block is on, drawn a step brighter than the rest of it, or empty where it names none.</summary>
        public string Grid { get; set; }

        /// <summary>What it is doing now, in the player's words, or empty for a thing that does nothing.</summary>
        public string Status { get; set; }

        /// <summary>The picture of what it is, as its kind names one, or empty for none.</summary>
        public string Icon { get; set; }

        /// <summary>Whether the column keeps room for pictures, which it does for every row or for none.</summary>
        public bool HasPicture { get; set; }

        /// <summary>Whether it is one of any number being collected, headed by a box rather than by nothing.</summary>
        public bool Many { get; set; }

        /// <summary>Whether it has been chosen.</summary>
        public bool IsChosen { get; set; }

        /// <summary>Whether it names the group under it rather than being one of the things in it.</summary>
        public bool IsHeading { get; set; }

        /// <summary>Whether it is listed to be read about rather than chosen.</summary>
        public bool IsWithheld { get; set; }

        /// <summary>Why it is listed only to be read about, said while the pointer rests on it.</summary>
        public string Why { get; set; }

        /// <summary>What choosing it does, said while the pointer rests on it; null where it goes without saying.</summary>
        public string About { get; set; }

        /// <summary>Whether it stands in under the row before it, as one of the things that row chooses between.</summary>
        public bool Indented { get; set; }

        /// <summary>Whether it is the last of the rows standing in under the row before them, where their spine turns in.</summary>
        public bool EndsSpine { get; set; }

        /// <summary>The mark of what kind of member it is, one of the symbols, or empty for none.</summary>
        public string Mark { get; set; } = "";

        /// <summary>What pressing the box at its head does, which takes it as well as whatever else is taken.</summary>
        public ICommand Ticks
        {
            get { return _mark.Command; }
            set { _mark.Command = value; }
        }

        /// <summary>Whether pressing it does anything, which it does unless it is a heading or listed only to be read about.</summary>
        public bool IsPickable => !IsHeading && !IsWithheld;

        /// <inheritdoc />
        protected override bool TakesPointer => IsPickable;

        /// <summary>Draws the row, then its stretch of the spine where it stands in under the row before it.</summary>
        protected override void DrawContent(IGraphics g)
        {
            base.DrawContent(g);

            if (!Indented)
                return;

            // Down the middle of the indent, and up across the column's gap to the row above, so the stretches join.
            var x = _content.Bounds.X + (_mark.IsVisible ? 0f : MarkInset) + (Sizes.Indent - Sizes.Spine) / 2f;
            var top = Bounds.Y - Gaps.Within;
            if (EndsSpine)
                TreeLine.Turning(g, x, top, Bounds.Y + Bounds.Height * 0.5f, Sizes.Spine, Sizes.SpineTurn, Palette.Line);
            else
                TreeLine.Down(g, x, top, Bounds.Y + Bounds.Height, Sizes.Spine, Palette.Line);
        }

        /// <summary>Shows what it has been told.</summary>
        public void Show()
        {
            var pressable = IsPickable;

            _mark.IsVisible = Many && !IsHeading;

            // The room the row's style leaves out: the left edge where no box holds it, and above and below. A heading has more
            // above it and none under, so it stands apart from the group before and against the first row of its own.
            _content.Base.Padding = new Thickness((_mark.IsVisible ? 0f : MarkInset) + (Indented ? Sizes.Indent : 0f), IsHeading ? Gaps.Sections : Gaps.Parts, 0f, IsHeading ? 0f : Gaps.Parts);
            _mark.Many = Many;
            _mark.IsChecked = IsChosen;
            _mark.IsPressed = Many && pressable;

            _picture.IsVisible = HasPicture;
            if (_art != Icon)
            {
                _art = Icon;
                _picture.Source = Icons.PictureFor(Icon ?? "");
                _picture.Base.Tint = string.IsNullOrEmpty(Icon) ? Palette.None : Color.White;
            }

            _name.Name = IsHeading ? (Text ?? "").ToUpperInvariant() : Text;
            _name.Set(LookStates.Heading, IsHeading);
            _name.Set(LookStates.Off, IsWithheld);
            _name.Mark = Mark;

            var detail = Detail(Secondary, Status);
            var at = string.IsNullOrEmpty(Grid) ? -1 : detail.LastIndexOf(Grid, StringComparison.Ordinal);
            _detail.Text = at < 0 ? detail : "";
            _detail.IsVisible = at < 0 && detail.Length != 0;
            _onGrid.IsVisible = at >= 0;
            if (at >= 0)
                _onGrid.Markdown = Markdown.Escape(detail.Substring(0, at)) + "**" + Markdown.Escape(Grid) + "**" + Markdown.Escape(detail.Substring(at + Grid.Length));

            Set(LookStates.Still, IsHeading || !pressable);
            Set(LookStates.Chosen, IsChosen && !Many);
            Tip = IsWithheld ? KeptTip.For(Tip, Why) : CodexTip.For(Tip, About);
        }

        /// <inheritdoc />
        protected override void OnPointerDown(Vector2 point, PointerButton button, int presses, bool shift, bool ctrl)
        {
            if (button != PointerButton.Left)
                return;

            _held = shift ? HeldModifier.Through : ctrl ? HeldModifier.Also : HeldModifier.Alone;
            _presses = presses;
        }

        /// <inheritdoc />
        protected override void OnPointerUp(Vector2 point, PointerButton button)
        {
            if (button != PointerButton.Left || !IsPickable || !Holds(point))
                return;

            var picked = Picked;
            if (picked != null)
                picked(_held, _presses);
        }

        /// <summary>Presses the row the way a pointer does, with <paramref name="held" /> held down.</summary>
        public void Pick(HeldModifier held, int presses)
        {
            var picked = Picked;
            if (picked != null && IsPickable)
                picked(held, presses);
        }

        static string Detail(string secondary, string status)
        {
            if (string.IsNullOrEmpty(secondary))
                return status ?? "";

            if (string.IsNullOrEmpty(status))
                return secondary;

            return $"{secondary}   {status}";
        }
    }
}
