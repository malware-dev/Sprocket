using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Look
{
    /// <summary>
    /// What Sprocket looks like on this target.
    /// </summary>
    /// <remarks>
    /// Core hands over intent - heading, muted, slot, problem - and never colours or sizes, so every
    /// value here is this target's own opinion. The <b>roles</b> are a target-agnostic vocabulary; the
    /// <b>values</b> are not, and should not be. This is a screen in a game with a look of its own, and
    /// a panel that ignored it would read as something bolted on.
    /// <para>
    /// So: a neutral slate turned cold and blue. Space Engineers is steel and glass lit blue, with hard
    /// corners, hairlines rather than borders, and colour saved for the few things that mean something.
    /// </para>
    /// <para>
    /// The values below are taken out of the game's own textures rather than guessed at, and each one
    /// says where it came from. Guessing does not work here: the eye remembers Space Engineers as dark
    /// navy and it is not - its panels are a light, almost grey-green steel, and its highlight is a pale
    /// ice blue rather than a saturated one.
    /// </para>
    /// </remarks>
    public static class Palette
    {
        /// <summary>Darker than a panel, so panels read as sitting on it. Derived from <see cref="Panel" />.</summary>
        /// <remarks>
        /// Opaque, along with everything else here. How much of the world shows through a screen is not
        /// this target's business and never was: the game draws the screen's own background and multiplies
        /// it by <c>UIBkOpacity</c>, which is a slider in the player's options. Sprocket draws content into
        /// a screen somebody else opened, so a veil applied here would be a second one over the top of
        /// the player's, in a quantity nobody asked for.
        /// </remarks>
        public static readonly Color Page = new Color(20, 27, 32);

        /// <summary>Header and footer. A step darker again, so the screen's edges settle.</summary>
        public static readonly Color Chrome = new Color(27, 36, 42);

        /// <summary>
        /// DarkBlueBackground.png, which is flat and entirely this colour - and also button_default,
        /// textbox_center and rectangle_button_center. <b>Every control in the game starts from this
        /// tone.</b>
        /// </summary>
        /// <remarks>
        /// It becomes one of two things when a pointer arrives, because the game keeps the same four
        /// states twice over. The <b>panel</b> family is DarkBlueBackground and its variants -
        /// <see cref="PanelHot" />, <see cref="Accent" />, <see cref="Line" />. The <b>control</b> family is
        /// button_default and its variants - <see cref="ControlHot" />, <see cref="ControlFocus" />,
        /// <see cref="SwitchLit" />. They start together and part company under the pointer: a panel is a
        /// surface and lifts towards it, a control is a well and deepens under it.
        /// </remarks>
        public static readonly Color Panel = new Color(41, 54, 62);

        /// <summary>DarkBlueBackground_highlight.png. What the game does to a panel under the pointer.</summary>
        public static readonly Color PanelHot = new Color(60, 76, 82);

        /// <summary>
        /// DarkBlueBackground_active.png, the panel family pressed - which is also the game's mid tone and
        /// reads as a hairline at one pixel, which is what most of this project uses it for.
        /// </summary>
        public static readonly Color Line = new Color(91, 115, 123);

        /// <summary>
        /// button_default_highlight.dds, and textbox_center_highlight, and every other control's - all of
        /// them exactly this. <b>A control under the pointer goes darker in this game.</b>
        /// </summary>
        /// <remarks>
        /// Surprising, and worth not overriding on instinct. Nearly every desktop lightens what the
        /// pointer is on; this game darkens a control and lightens a panel, and the two together are what
        /// make its chrome read as pressed metal rather than as a web page.
        /// <para>
        /// <b>Rows are not controls for this purpose, whatever they behave like.</b> A row that can be
        /// pressed reads as a control, which is not what the game does.
        /// <c>MyGuiConstants.HIGHLIGHT_BACKGROUND_COLOR</c> is (60, 76, 82), which is
        /// <see cref="PanelHot" /> exactly, and <c>CONTROL_MOUSE_OVER_BACKGROUND_COLOR_MULTIPLIER</c> is
        /// 1.2 - the game brightens what the pointer is on, by a named colour or by a fifth. Deepening is
        /// what its button textures do and nothing else.
        /// </para>
        /// <para>
        /// It costs something: the smallest text reads 3.3:1 on a lifted row against 6.9 on a
        /// deepened one. That is the price of looking like the game rather than being easiest to read on,
        /// and it is the game's own price - its lists have the same problem and it has not bothered anybody.
        /// </para>
        /// </remarks>
        public static readonly Color ControlHot = new Color(12, 18, 22);

        /// <summary>button_default_focus.dds. A control with the keyboard in it.</summary>
        public static readonly Color ControlFocus = new Color(69, 128, 157);

        /// <summary>
        /// The colour the game writes in: every label, list row and field in the terminal is this.
        /// </summary>
        /// <remarks>
        /// Not white, which is what it looks like next to the panels and is the mistake worth avoiding.
        /// <para>
        /// Sampled from the terminal's own text, not from Textures/GUI/Icons/*.png, which are
        /// single-colour and a few steps darker. The game reserves plain white for the selected row
        /// and writes everything else in this.
        /// </para>
        /// </remarks>
        public static readonly Color Ink = new Color(220, 245, 253);

        /// <summary>A step down from <see cref="Ink" />, toward the focus blue.</summary>
        public static readonly Color Muted = new Color(162, 195, 205);

        /// <summary>
        /// A step down again, and the bottom of the ramp. Nothing quieter than this carries a word.
        /// </summary>
        /// <remarks>
        /// Set by the floor rather than by eye: this reads 4.5:1 against <see cref="Panel" />, which is
        /// what the smallest text on these screens needs to be read at all. Anything quieter looks right
        /// in isolation and falls under 3:1, at which a step number is decoration rather than
        /// information.
        /// </remarks>
        public static readonly Color Faint = new Color(133, 161, 169);

        /// <summary>
        /// DarkBlueBackground_focus.png. What the game fills a control with when it is the one in hand.
        /// </summary>
        /// <remarks>Pale, and filled rather than outlined, which is why anything wearing it takes dark text.</remarks>
        public static readonly Color Accent = new Color(142, 188, 207);

        public static readonly Color SlotEdge = new Color(142, 188, 207);
        public static readonly Color SlotFill = new Color(55, 72, 81);



        /// <summary>checkbox_green_checked.png. Vivid, and nothing like a desktop's mint.</summary>
        public static readonly Color Live = new Color(58, 204, 0);

        /// <summary>
        /// The other half of <see cref="Live" />: a light that is off, a border that is not in force, a
        /// control with nothing to do.
        /// </summary>
        /// <remarks>
        /// Deliberately not <see cref="PanelHot" />, which would put an unlit light and the panel behind
        /// it within a rounding error of each other - an unlit light would not read as a light at all,
        /// only as a green one sometimes appearing. Off is a state and has to be drawn.
        /// <para>
        /// Lighter than the panel rather than darker, because nothing can be darker than these panels by
        /// enough to matter: the page is already near black and the arithmetic has no room left below it.
        /// </para>
        /// </remarks>
        public static readonly Color Dim = new Color(95, 114, 120);

        /// <summary>
        /// The grounds that say what kind of thing a row is - running, wrong, worth reading.
        /// </summary>
        /// <remarks>
        /// All three are darker than the panel they interrupt, and that is what has to hold. Mid-toned,
        /// they land within a few percent of <see cref="Panel" />'s own brightness, which leaves the hue
        /// carrying the entire signal: correct on this monitor, absent for the one player in twelve who
        /// cannot separate a green wash from a blue one. Depth costs nothing here - the text on them is
        /// the brightest thing in the palette - and gives every eye a second way to see it.
        /// </remarks>
        public static readonly Color LiveWash = new Color(14, 40, 10);

        public static readonly Color Problem = new Color(232, 74, 58);

        /// <summary>
        /// What a field is filled with while what is in it cannot be accepted.
        /// </summary>
        /// <remarks>
        /// It stands in for <see cref="PanelHot" />, which is a blue-grey well; this is a red-brown one,
        /// and the hue is most of what does the work. Brightness cannot: red is dark, and the very
        /// lightest wash that still reads as red is a third of a step off the chrome behind it where the
        /// ordinary well is most of a step the other way.
        /// <para>
        /// Which is why the border matters more than the fill. <see cref="Problem" /> against the chrome
        /// is 4.1:1 and unmistakable, and text stays <see cref="Ink" /> on top at 12:1 - a field being
        /// refused is still a field somebody is reading what they typed in.
        /// </para>
        /// </remarks>
        public static readonly Color WrongFill = new Color(74, 34, 31);
        public static readonly Color ProblemWash = new Color(36, 12, 10);
        public static readonly Color Notice = new Color(240, 172, 40);
        public static readonly Color NoticeWash = new Color(40, 30, 9);

        /// <summary>
        /// A scrollbar's thumb, and the same held.
        /// </summary>
        /// <remarks>
        /// The game's own is (78, 87, 101) drawn against its window ground with no groove behind it, which
        /// is a shade under 2:1 - quiet enough to read as furniture rather than as content. Ours is the
        /// unlit tone for the same reason it draws an unlit light: it is a thing that is present and not
        /// doing anything. It brightens one step while it is being held, which is the only feedback a bar
        /// has room for.
        /// </remarks>
        public static readonly Color Thumb = Dim;

        public static readonly Color ThumbHeld = Faint;

        public static readonly Color None = new Color(0, 0, 0, 0);

        /// <summary>
        /// switch_on_off_*_checked.dds, which is also button_default_active - the control family pressed.
        /// A checked half of a switch and a held-down button are the same thing to this game.
        /// </summary>
        public static readonly Color SwitchLit = new Color(27, 44, 51);

        /// <summary>
        /// switch_on_off_*.dds. The half that is not.
        /// </summary>
        /// <remarks>
        /// Darker than anything else in this palette, and deliberately so in the game as well: the two
        /// halves are told apart by the difference between them rather than by either one being marked.
        /// </remarks>
        public static readonly Color SwitchDark = new Color(6, 10, 12);
    }
}
