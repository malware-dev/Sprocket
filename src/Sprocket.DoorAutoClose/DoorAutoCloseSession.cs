using System.IO;
using Sandbox.ModAPI;
using Mal.MdkModMixin.Network;
using VRage;
using VRage.Game.Components;

namespace Sprocket.DoorAutoClose
{
    /// <summary>The mod in one world: its texts, its channel, and the slider's place among a door's controls.</summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class DoorAutoCloseSession : MySessionComponentBase
    {
        // Static because the doors and their slider are made by the game and reach the world's auto-close only through it; set
        // while a world is loaded.
        static DoorAutoCloseSession _current;

        NetworkChannel _channel;
        AutoClose _doors;
        MyLanguagesEnum _language;

        /// <summary>Every door's auto-close delay, or null outside a world.</summary>
        public static AutoClose Doors => _current?._doors;

        /// <inheritdoc />
        public override void LoadData()
        {
            _current = this;
            _doors = new AutoClose(door => door.GameLogic?.GetAs<DoorBlock>()?.Closer);
            _channel = AutoClose.Carry(NetworkChannel.Create(GetType())).Build();
            _doors.Listen(_channel);
            Speak();
        }

        /// <inheritdoc />
        public override void BeforeStart()
        {
            // MyAPIGateway.TerminalControls does not exist yet in LoadData.
            MyAPIGateway.TerminalControls.CustomControlGetter += DoorControls.Arrange;
        }

        /// <inheritdoc />
        protected override void UnloadData()
        {
            if (MyAPIGateway.TerminalControls != null)
                MyAPIGateway.TerminalControls.CustomControlGetter -= DoorControls.Arrange;

            if (MyAPIGateway.Gui != null)
                MyAPIGateway.Gui.GuiControlRemoved -= Closed;

            _channel?.Dispose();
            _channel = null;
            _doors = null;
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
