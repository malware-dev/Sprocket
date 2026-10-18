using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage;
using VRage.Utils;

namespace Sprocket.DoorAutoClose
{
    /// <summary>The slider on a door that sets how long it stays open before it closes by itself.</summary>
    public static class DoorControls
    {
        /// <summary>The slider's id, which is the id of the value it sets.</summary>
        /// <remarks>Persisted in every program that sets it, so it cannot change without changing what those programs say.</remarks>
        public const string Id = "Sprocket_AutoClose";

        const string Open = "Open";

        // Keys into the game's table of texts, which the session fills from Content/Data/Localization.
        static readonly MyStringId Title = MyStringId.GetOrCompute("Sprocket.AutoClose.Slider");
        static readonly MyStringId About = MyStringId.GetOrCompute("Sprocket.AutoClose.SliderAbout");
        static readonly MyStringId Off = MyStringId.GetOrCompute("Sprocket.AutoClose.Off");

        /// <summary>Makes the slider for doors of type <typeparamref name="TBlock" /> once, which the game keeps for the session.</summary>
        public static void DoOnce<TBlock>()
        {
            if (Made<TBlock>.Done)
                return;

            Made<TBlock>.Done = true;

            var slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, TBlock>(Id);
            slider.Title = Title;
            slider.Tooltip = About;
            slider.SupportsMultipleBlocks = true;
            slider.Visible = _ => true;
            slider.Enabled = _ => true;
            slider.SetLogLimits(CloseDelay.Least, CloseDelay.Most);
            slider.Getter = block => CloseDelay.Stored(block);
            slider.Setter = Set;
            slider.Writer = (block, text) => text.Append(CloseDelay.Written(CloseDelay.Stored(block), MyTexts.GetString(Off)));

            MyAPIGateway.TerminalControls.AddControl<TBlock>(slider);
        }

        /// <summary>Moves the slider to stand under the door's open switch, in the list of controls a door is about to show.</summary>
        /// <remarks>Called as the terminal's <c>CustomControlGetter</c>.</remarks>
        public static void Arrange(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (!(block is IMyDoor))
                return;

            IMyTerminalControl slider = null;
            for (var i = 0; i < controls.Count; i++)
            {
                if (controls[i].Id == Id)
                {
                    slider = controls[i];
                    controls.RemoveAt(i);
                    break;
                }
            }

            if (slider == null)
                return;

            var at = controls.Count;
            for (var i = 0; i < controls.Count; i++)
            {
                if (controls[i].Id == Open)
                {
                    at = i + 1;
                    break;
                }
            }

            controls.Insert(at, slider);
        }

        static void Set(IMyTerminalBlock block, float value)
        {
            var door = block as IMyDoor;
            var doors = DoorAutoCloseSession.Doors;
            if (door != null && doors != null)
                doors.Set(door, CloseDelay.FromSlider(value));
        }

        /// <summary>Whether the slider has been made for doors of type <typeparamref name="TBlock" />.</summary>
        static class Made<TBlock>
        {
            // Static because every door of the type asks on its first frame and its slider is added once for the world;
            // the game clears every control as a world unloads, and the mod is compiled again for the next.
            // ReSharper disable once StaticMemberInGenericType
            public static bool Done;
        }
    }
}
