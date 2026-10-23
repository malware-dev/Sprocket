using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Look
{
    /// <summary>The colors a screen is drawn in.</summary>
    /// <remarks>A value that names a texture was sampled from that texture in the game's content.</remarks>
    public static class Palette
    {
        /// <summary>The ground panels sit on.</summary>
        /// <remarks>Opaque: the game draws the screen's own background underneath, at the player's <c>UIBkOpacity</c>.</remarks>
        public static readonly Color Page = new Color(20, 27, 32);

        /// <summary>Header and footer.</summary>
        public static readonly Color Chrome = new Color(27, 36, 42);

        /// <summary>DarkBlueBackground.png, button_default, textbox_center and rectangle_button_center, which are all this color.</summary>
        /// <remarks>Under the pointer the game lifts a panel and a control alike to <see cref="PanelHot" />.</remarks>
        public static readonly Color Panel = new Color(41, 54, 62);

        /// <summary>DarkBlueBackground_highlight.png: a panel under the pointer.</summary>
        public static readonly Color PanelHot = new Color(60, 76, 82);

        /// <summary>DarkBlueBackground_active.png, button_default_active.dds and switch_on_off_*_checked.dds: a panel or control pressed, the half of a switch in force, and the hairline color.</summary>
        public static readonly Color Line = new Color(91, 115, 123);

        /// <summary>button_default.dds: the hairline round a control at rest.</summary>
        public static readonly Color ControlEdge = new Color(63, 71, 79);

        /// <summary>button_default_highlight.dds: the hairline round a control under the pointer.</summary>
        public static readonly Color ControlEdgeHot = new Color(112, 117, 122);

        /// <summary>The ground an on/off switch's halves sit in, and any other well a control is sunk into.</summary>
        /// <remarks>Sampled from the terminal's own on/off switch, which draws it without a texture of its own.</remarks>
        public static readonly Color Well = new Color(32, 39, 45);

        /// <summary>button_default_focus.dds: a control with the keyboard in it.</summary>
        public static readonly Color ControlFocus = new Color(69, 128, 157);

        /// <summary>The color the game writes in.</summary>
        /// <remarks>Sampled from the terminal's own text.</remarks>
        public static readonly Color Ink = new Color(220, 245, 253);

        /// <summary>A step down from <see cref="Ink" />, toward the focus blue.</summary>
        public static readonly Color Muted = new Color(162, 195, 205);

        /// <summary>The quietest color that carries a word.</summary>
        /// <remarks>Contrast 4.5:1 against <see cref="Panel" />, the floor for the smallest text on these screens.</remarks>
        public static readonly Color Faint = new Color(133, 161, 169);

        /// <summary>How dark a floating window's shadow lies at its darkest: black, about two parts in three opaque.</summary>
        /// <remarks>Chosen rather than sampled.</remarks>
        public static readonly Color Shadow = new Color(0, 0, 0, 170);

        /// <summary>What a dialog lays over the screen behind it: <see cref="Page" />, three parts in five opaque.</summary>
        /// <remarks>Chosen rather than sampled.</remarks>
        public static readonly Color Veil = new Color(20, 27, 32, 153);

        /// <summary>The words of a link, wherever one stands.</summary>
        /// <remarks>Chosen rather than sampled.</remarks>
        public static readonly Color Link = new Color(79, 195, 247);

        /// <summary>DarkBlueBackground_focus.png: the fill of the control in hand.</summary>
        /// <remarks>Pale, so anything wearing it takes dark text.</remarks>
        public static readonly Color Accent = new Color(142, 188, 207);

        /// <summary>The edge of a toolbar slot that holds something of the game's.</summary>
        public static readonly Color SlotEdge = new Color(142, 188, 207);

        /// <summary>A fill a notch above the page: a toolbar slot that holds something of the game's, a note, a block's picture, a step being carried, and a button waiting to be ready.</summary>
        public static readonly Color SlotFill = new Color(55, 72, 81);


        /// <summary>checkbox_green_checked.png.</summary>
        public static readonly Color Live = new Color(58, 204, 0);

        /// <summary>The other half of <see cref="Live" />: a light that is off, a border not in force, a control with nothing to do.</summary>
        public static readonly Color Dim = new Color(95, 114, 120);

        /// <summary>The ground of a row that is running.</summary>
        /// <remarks>Every wash stays darker than <see cref="Panel" />, so a row's kind never rests on hue alone.</remarks>
        public static readonly Color LiveWash = new Color(14, 40, 10);

        /// <summary>The ink of something wrong: its words, its mark, and the edge of a refusing box.</summary>
        public static readonly Color Problem = new Color(232, 74, 58);

        /// <summary>What a field is filled with while what is in it cannot be accepted.</summary>
        public static readonly Color WrongFill = new Color(74, 34, 31);

        /// <summary>The ground of a row or notice that is wrong, and the highlight of a delete control.</summary>
        public static readonly Color ProblemWash = new Color(36, 12, 10);

        /// <summary>The ink of a warning, and the line under a slot still to be filled.</summary>
        public static readonly Color Notice = new Color(240, 172, 40);

        /// <summary>The ground of a row or notice that carries a warning.</summary>
        public static readonly Color NoticeWash = new Color(40, 30, 9);

        /// <summary>The hazard stripes laid over a warning's wash, a step brighter than it so they show without crowding the words.</summary>
        public static readonly Color NoticeStripe = new Color(58, 43, 13);

        /// <summary>The hazard stripes laid over a problem's wash, a step brighter than it so they show without crowding the words.</summary>
        public static readonly Color ProblemStripe = new Color(54, 18, 15);

        /// <summary>A scrollbar's thumb.</summary>
        public static readonly Color Thumb = Dim;

        /// <summary>A scrollbar's thumb while it is held.</summary>
        public static readonly Color ThumbHeld = Faint;

        /// <summary>No color at all, for a fill, ink or highlight that is off.</summary>
        public static readonly Color None = new Color(0, 0, 0, 0);
    }
}