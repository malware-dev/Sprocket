using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Look
{
    /// <summary>The colours a screen is drawn in.</summary>
    /// <remarks>A value that names a texture was sampled from that texture in the game's content.</remarks>
    public static class Palette
    {
        /// <summary>The ground panels sit on.</summary>
        /// <remarks>
        ///     Opaque, like everything here: the game draws the screen's own background underneath, at the
        ///     player's <c>UIBkOpacity</c>.
        /// </remarks>
        public static readonly Color Page = new Color(20, 27, 32);

        /// <summary>Header and footer.</summary>
        public static readonly Color Chrome = new Color(27, 36, 42);

        /// <summary>DarkBlueBackground.png, button_default, textbox_center and rectangle_button_center, which are all this colour.</summary>
        /// <remarks>
        ///     Under the pointer the game lifts a panel to <see cref="PanelHot" /> and deepens a control to
        ///     <see cref="ControlHot" />.
        /// </remarks>
        public static readonly Color Panel = new Color(41, 54, 62);

        /// <summary>DarkBlueBackground_highlight.png: a panel under the pointer.</summary>
        public static readonly Color PanelHot = new Color(60, 76, 82);

        /// <summary>DarkBlueBackground_active.png: a panel pressed, and the hairline colour.</summary>
        public static readonly Color Line = new Color(91, 115, 123);

        /// <summary>button_default_highlight.dds, textbox_center_highlight and every other control's highlight: a control under the pointer.</summary>
        /// <remarks>
        ///     Not for a row, which takes <see cref="PanelHot" />: the game's
        ///     <c>MyGuiConstants.HIGHLIGHT_BACKGROUND_COLOR</c> is (60, 76, 82).
        /// </remarks>
        public static readonly Color ControlHot = new Color(12, 18, 22);

        /// <summary>button_default_focus.dds: a control with the keyboard in it.</summary>
        public static readonly Color ControlFocus = new Color(69, 128, 157);

        /// <summary>The colour the game writes in.</summary>
        /// <remarks>
        ///     Sampled from the terminal's own text, where every label, list row and field is this and only the
        ///     selected row is white.
        /// </remarks>
        public static readonly Color Ink = new Color(220, 245, 253);

        /// <summary>A step down from <see cref="Ink" />, toward the focus blue.</summary>
        public static readonly Color Muted = new Color(162, 195, 205);

        /// <summary>The quietest colour that carries a word.</summary>
        /// <remarks>Contrast 4.5:1 against <see cref="Panel" />, the floor for the smallest text on these screens.</remarks>
        public static readonly Color Faint = new Color(133, 161, 169);

        /// <summary>DarkBlueBackground_focus.png: the fill of the control in hand.</summary>
        /// <remarks>Pale, so anything wearing it takes dark text.</remarks>
        public static readonly Color Accent = new Color(142, 188, 207);

        /// <summary>The line under a filled slot in a sentence.</summary>
        public static readonly Color SlotEdge = new Color(142, 188, 207);

        /// <summary>The fill of a slot in a sentence, also worn by notes and by buttons waiting to be ready.</summary>
        public static readonly Color SlotFill = new Color(55, 72, 81);


        /// <summary>checkbox_green_checked.png.</summary>
        public static readonly Color Live = new Color(58, 204, 0);

        /// <summary>The other half of <see cref="Live" />: a light that is off, a border not in force, a control with nothing to do.</summary>
        public static readonly Color Dim = new Color(95, 114, 120);

        /// <summary>The grounds that say what kind of thing a row is: running, wrong, worth reading.</summary>
        /// <remarks>All three stay darker than <see cref="Panel" />, so a row's kind never rests on hue alone.</remarks>
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

        /// <summary>A scrollbar's thumb.</summary>
        public static readonly Color Thumb = Dim;

        /// <summary>A scrollbar's thumb while it is held.</summary>
        public static readonly Color ThumbHeld = Faint;

        /// <summary>No colour at all, for a fill, ink or highlight that is off.</summary>
        public static readonly Color None = new Color(0, 0, 0, 0);

        /// <summary>switch_on_off_*_checked.dds and button_default_active: a control pressed.</summary>
        public static readonly Color SwitchLit = new Color(27, 44, 51);

        /// <summary>switch_on_off_*.dds: the half of a switch that is not checked.</summary>
        public static readonly Color SwitchDark = new Color(6, 10, 12);
    }
}