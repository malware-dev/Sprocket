using System;
using System.Collections.Generic;
using Lumel;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace RichTextPanels
{
    /// <summary>The panel's own text settings, offered on its script page while it runs the Rich Text app.</summary>
    /// <remarks>
    ///     The game offers Edit text, Font size and Padding only in text mode. Its controls are the game's for
    ///     as long as it runs, across every world, so they are only read, never changed: the app offers controls of its
    ///     own, made for this world and put into a panel's list as the terminal lists it, each doing its work through the
    ///     game's control of the same name. They change the panel's own settings, which the game saves and shares. The
    ///     fitting and vertical alignment are the app's own, which the block keeps and <see cref="Sharing" /> shares;
    ///     the font size hides while the text is fitted, since it has no say then.
    ///     <para>
    ///         A block with several screens tells a mod which mode its selected screen is in, through the Content
    ///         control, but not which screen is selected or which script it runs. There the settings show when the
    ///         selected screen runs a script and any of the block's screens runs this one.
    ///     </para>
    /// </remarks>
    public class PanelControls
    {
        readonly Dictionary<MyObjectBuilderType, Game> _games = new Dictionary<MyObjectBuilderType, Game>();
        readonly List<IMyTerminalControl> _functional = new List<IMyTerminalControl>();
        readonly List<IMyTerminalControl> _cockpit = new List<IMyTerminalControl>();
        readonly Sharing _sharing;

        /// <summary>The settings, with the vertical alignment kept and shared by <paramref name="sharing" />.</summary>
        public PanelControls(Sharing sharing)
        {
            _sharing = sharing;
        }

        /// <summary>Starts offering the settings, wherever the terminal lists a block's controls.</summary>
        public void Attach()
        {
            MyAPIGateway.TerminalControls.CustomControlGetter += Listing;
        }

        /// <summary>Stops offering them.</summary>
        public void Detach()
        {
            MyAPIGateway.TerminalControls.CustomControlGetter -= Listing;
        }

        /// <summary>Makes the terminal list <paramref name="block" />'s controls again while it is open, so each one's reason to show is asked afresh.</summary>
        /// <remarks>
        ///     Switching whether the block shows in the toolbar setup off and back on is what has the terminal rebuild a
        ///     block's controls. It is a setting the game shares, so it is only switched while somebody is looking.
        /// </remarks>
        public static void Refresh(IMyTerminalBlock block)
        {
            if (block == null || MyAPIGateway.Gui == null || MyAPIGateway.Gui.GetCurrentScreen != MyTerminalPageEnum.ControlPanel)
                return;

            block.ShowInToolbarConfig = !block.ShowInToolbarConfig;
            block.ShowInToolbarConfig = !block.ShowInToolbarConfig;
        }

        /// <summary>Puts the app's settings straight under the script list, before its colors, on a block whose screen runs it.</summary>
        void Listing(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (!(block is IMyTextSurfaceProvider))
                return;

            var game = Learn(block, controls);
            if (game == null || !Runs(block, game))
                return;

            var ours = block is IMyCockpit ? Ours<IMyCockpit>(_cockpit, game)
                : block is IMyFunctionalBlock ? Ours<IMyFunctionalBlock>(_functional, game)
                : null;
            if (ours == null || ours.Count == 0)
                return;

            // A plain text panel lists its screen controls twice, the first set hidden straight under Custom Data, so the
            // script list is the last one showing.
            var after = controls.FindLastIndex(c => c.Id == "Script" && c.Visible(block));
            controls.InsertRange(after < 0 ? controls.Count : after + 1, ours);
        }

        /// <summary>The game's own text controls for <paramref name="block" />'s kind, found in <paramref name="controls" /> the first time it is listed; null where it has none.</summary>
        Game Learn(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            Game game;
            var kind = block.BlockDefinition.TypeId;
            if (_games.TryGetValue(kind, out game))
                return game;

            // The last of each name, which on a plain text panel listing them twice is the set it shows.
            game = new Game();
            foreach (var control in controls)
            {
                switch (control.Id)
                {
                    case "Content":
                        game.Content = control as IMyTerminalValueControl<long>;
                        break;
                    case "ShowTextPanel":
                        game.Edit = control as IMyTerminalControlButton;
                        break;
                    case "FontSize":
                        game.Size = control as IMyTerminalControlSlider;
                        break;
                    case "TextPaddingSlider":
                        game.Padding = control as IMyTerminalControlSlider;
                        break;
                }
            }

            if (game.Content == null || game.Edit == null || game.Size == null || game.Padding == null)
                game = null;

            _games.Add(kind, game);
            return game;
        }

        /// <summary>The app's controls for one family of blocks, made the first time they are wanted, titled as the game's own.</summary>
        List<IMyTerminalControl> Ours<TBlock>(List<IMyTerminalControl> ours, Game game)
        {
            if (ours.Count > 0)
                return ours;

            var terminal = MyAPIGateway.TerminalControls;

            var edit = terminal.CreateControl<IMyTerminalControlButton, TBlock>(RichTextScript.Id + ".EditText");
            var help = terminal.CreateControl<IMyTerminalControlButton, TBlock>(RichTextScript.Id + ".Help");
            var fit = terminal.CreateControl<IMyTerminalControlCheckbox, TBlock>(RichTextScript.Id + ".Fit");
            var size = terminal.CreateControl<IMyTerminalControlSlider, TBlock>(RichTextScript.Id + ".FontSize");
            var vertical = terminal.CreateControl<IMyTerminalControlCombobox, TBlock>(RichTextScript.Id + ".Vertical");
            var padding = terminal.CreateControl<IMyTerminalControlSlider, TBlock>(RichTextScript.Id + ".Padding");
            if (edit == null || help == null || fit == null || size == null || vertical == null || padding == null)
                return ours;

            edit.Title = game.Edit.Title;
            edit.Tooltip = game.Edit.Tooltip;
            edit.Visible = Runs;
            edit.Enabled = block =>
            {
                var own = Of(block);
                return own != null && own.Edit.Enabled(block);
            };
            edit.Action = block =>
            {
                var own = Of(block);
                if (own != null)
                    own.Edit.Action(block);
            };

            help.Title = MyStringId.GetOrCompute("Markdown help");
            help.Tooltip = MyStringId.GetOrCompute("Opens a guide to what can be written in the panel's text, and how it looks.");
            help.Visible = Runs;
            help.Action = block => Help.Open(SurfaceOf(block));

            fit.Title = MyStringId.GetOrCompute("Fit to screen");
            fit.Tooltip = MyStringId.GetOrCompute("Grows or shrinks the text to fill the screen, in place of the font size.");
            fit.Visible = Runs;
            fit.Getter = block => Fitting.Stored(block);
            fit.Setter = (block, on) =>
            {
                _sharing.SetFitting(block, on);
                Refresh(block);
            };

            Slider(size, game.Size, 0.1f, 10f, 1f, own => own.Size);
            size.Visible = block => Runs(block) && !Fitting.Stored(block);
            Slider(padding, game.Padding, 0f, 50f, 0f, own => own.Padding);

            vertical.Title = MyStringId.GetOrCompute("Vertical alignment");
            vertical.Tooltip = MyStringId.GetOrCompute("Where the text sits on the screen when it does not fill it.");
            vertical.Visible = Runs;
            vertical.ComboBoxContent = items =>
            {
                items.Add(new MyTerminalControlComboBoxItem { Key = 0, Value = MyStringId.GetOrCompute("Top") });
                items.Add(new MyTerminalControlComboBoxItem { Key = 1, Value = MyStringId.GetOrCompute("Middle") });
                items.Add(new MyTerminalControlComboBoxItem { Key = 2, Value = MyStringId.GetOrCompute("Bottom") });
            };
            vertical.Getter = block =>
            {
                var align = Vertical.Stored(block);
                return align == Align.Center ? 1 : align == Align.Bottom ? 2 : 0;
            };
            vertical.Setter = (block, key) => _sharing.SetVertical(block, key == 1 ? Align.Center : key == 2 ? Align.Bottom : Align.Top);

            ours.Add(edit);
            ours.Add(help);
            ours.Add(fit);
            ours.Add(size);
            ours.Add(vertical);
            ours.Add(padding);
            return ours;
        }

        /// <summary>Makes <paramref name="slider" /> stand for the game's slider <paramref name="game" />, as <paramref name="pick" /> finds it for a block, between the game's own limits.</summary>
        void Slider(IMyTerminalControlSlider slider, IMyTerminalControlSlider game, float least, float most, float otherwise, Func<Game, IMyTerminalControlSlider> pick)
        {
            slider.Title = game.Title;
            slider.Tooltip = game.Tooltip;
            slider.SetLimits(least, most);
            slider.Visible = Runs;
            slider.Getter = block =>
            {
                var own = Of(block);
                return own == null ? otherwise : pick(own).Getter(block);
            };
            slider.Setter = (block, value) =>
            {
                var own = Of(block);
                if (own != null)
                    pick(own).Setter(block, value);
            };
            slider.Writer = (block, words) =>
            {
                var own = Of(block);
                if (own != null)
                    pick(own).Writer(block, words);
            };
        }

        /// <summary>The game's text controls for <paramref name="block" />'s kind, or null where none were found.</summary>
        Game Of(IMyTerminalBlock block)
        {
            Game game;
            return _games.TryGetValue(block.BlockDefinition.TypeId, out game) ? game : null;
        }

        bool Runs(IMyTerminalBlock block)
        {
            var game = Of(block);
            return game != null && Runs(block, game);
        }

        /// <summary>Whether <paramref name="block" />'s selected screen runs the app, as near as can be told.</summary>
        static bool Runs(IMyTerminalBlock block, Game game)
        {
            var single = block as IMyTextSurface;
            if (single != null)
                return RunsOn(single);

            var provider = block as IMyTextSurfaceProvider;
            if (provider == null || game.Content.Getter(block) != (long)ContentType.SCRIPT)
                return false;

            for (var i = 0; i < provider.SurfaceCount; i++)
            {
                if (RunsOn(provider.GetSurface(i)))
                    return true;
            }

            return false;
        }

        /// <summary>A screen of <paramref name="block" />: the block itself where it is one, or its first.</summary>
        static IMyTextSurface SurfaceOf(IMyTerminalBlock block)
        {
            var single = block as IMyTextSurface;
            if (single != null)
                return single;

            var provider = block as IMyTextSurfaceProvider;
            return provider == null || provider.SurfaceCount == 0 ? null : provider.GetSurface(0);
        }

        static bool RunsOn(IMyTextSurface surface)
        {
            return surface != null && surface.ContentType == ContentType.SCRIPT && surface.Script == RichTextScript.Id;
        }

        /// <summary>One kind of block's own text controls, which the app's controls work through.</summary>
        sealed class Game
        {
            public IMyTerminalValueControl<long> Content;
            public IMyTerminalControlButton Edit;
            public IMyTerminalControlSlider Size;
            public IMyTerminalControlSlider Padding;
        }
    }
}
