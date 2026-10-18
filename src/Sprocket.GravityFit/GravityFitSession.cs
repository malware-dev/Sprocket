using System.IO;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.Components;

namespace Sprocket.GravityFit
{
    /// <summary>The mod in one world: its texts, the buttons' place among a generator's controls, and the boxes a short fit shows.</summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class GravityFitSession : MySessionComponentBase
    {
        // Static because the buttons are made by the game and reach the world's boxes only through it; set while a world is
        // loaded.
        static GravityFitSession _current;

        readonly ShortfallMarks _marks = new ShortfallMarks();

        MyLanguagesEnum _language;

        /// <summary>The boxes a short fit is showing, or null outside a world.</summary>
        public static ShortfallMarks Marks => _current?._marks;

        /// <inheritdoc />
        public override void LoadData()
        {
            _current = this;
            Speak();
        }

        /// <inheritdoc />
        public override void BeforeStart()
        {
            // MyAPIGateway.TerminalControls does not exist yet in LoadData.
            MyAPIGateway.TerminalControls.CustomControlGetter += GravityFieldButtons.Arrange;
        }

        /// <summary>Draws the boxes a gravity generator's fit is showing, while there are any.</summary>
        public override void Draw()
        {
            if (MyAPIGateway.Session != null)
                _marks.Draw(MyAPIGateway.Session.ElapsedPlayTime);
        }

        /// <inheritdoc />
        protected override void UnloadData()
        {
            if (MyAPIGateway.TerminalControls != null)
                MyAPIGateway.TerminalControls.CustomControlGetter -= GravityFieldButtons.Arrange;

            if (MyAPIGateway.Gui != null)
                MyAPIGateway.Gui.GuiControlRemoved -= Closed;

            _current = null;
        }

        /// <summary>Loads the mod's texts in the player's language, and listens for the language to change.</summary>
        /// <remarks>The game loads no mod's texts itself. On a dedicated server the texts are never shown.</remarks>
        void Speak()
        {
            if (MyAPIGateway.Utilities == null || MyAPIGateway.Utilities.IsDedicated || MyAPIGateway.Gui == null
                || MyAPIGateway.Session == null || MyAPIGateway.Session.Config == null)
                return;

            Load(MyAPIGateway.Session.Config.Language);
            MyAPIGateway.Gui.GuiControlRemoved += Closed;
        }

        /// <summary>Loads the texts again where the language has changed.</summary>
        /// <remarks>Called whenever any screen closes, since the game announces no change of language; costs one comparison.</remarks>
        void Closed(object screen)
        {
            if (MyAPIGateway.Session != null && MyAPIGateway.Session.Config != null && MyAPIGateway.Session.Config.Language != _language)
                Load(MyAPIGateway.Session.Config.Language);
        }

        /// <summary>Lays the texts for <paramref name="language" /> over the English ones in the game's table of texts.</summary>
        void Load(MyLanguagesEnum language)
        {
            var folder = Path.Combine(ModContext.ModPath, "Data", "Localization");
            MyTexts.MyLanguageDescription description;
            if (!MyTexts.Languages.TryGetValue(language, out description) || description == null)
                MyTexts.LoadTexts(folder);
            else
                MyTexts.LoadTexts(folder, description.CultureName, description.SubcultureName);

            _language = language;
        }
    }
}
