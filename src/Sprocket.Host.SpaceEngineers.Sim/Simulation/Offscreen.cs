using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRageMath;
using Lumel.Drawing;
// Supplies Lettering, which the inspection does not see from every configuration it runs in.
// ReSharper disable once RedundantUsingDirective
using Sprocket.Sim.Content;
using Sprocket.Sim.Machine;
using Lumel.Sim;

namespace Sprocket.Sim.Simulation
{
    /// <summary>
    /// The editor drawn with no window and nobody watching, and what it drew.
    /// </summary>
    /// <remarks>
    /// A frame is a list. Everything that wants a screen without eyes wants the same three things put
    /// together the same way - a substitute device, the graphics that write to it, and the scene - so
    /// they are put together here once. A dump, a suite entry and a test are then the same frame read
    /// three ways rather than three arrangements that can drift apart.
    /// </remarks>
    public sealed class Offscreen
    {
        private readonly ScreenSpaceDevice _device;
        private readonly ScreenSpaceGraphics _graphics;
        private readonly EditorScene _scene;
        private readonly float _width;
        private readonly float _height;

        /// <summary>A frame of the editor on a block holding <paramref name="holding" />, or nothing for null.</summary>
        public Offscreen(float windowWidth, float windowHeight, string holding = null)
            : this(windowWidth, windowHeight, () => holding == null ? EditorScene.Nowhere() : EditorScene.Holding(holding))
        {
        }

        /// <summary>A frame of the editor on a construct of <paramref name="rig" />'s blocks that remembers nothing.</summary>
        public Offscreen(float windowWidth, float windowHeight, Rig rig)
            : this(windowWidth, windowHeight, () => EditorScene.On(rig))
        {
        }

        /// <summary>A frame of the editor on the scene <paramref name="scene" /> makes, made once the device it draws to is in place.</summary>
        Offscreen(float windowWidth, float windowHeight, System.Func<EditorScene> scene)
        {
            _width = windowWidth;
            _height = windowHeight;

            _device = new ScreenSpaceDevice(SprocketContent.Instance);

            // The letters are a stand-in, because the game's own widths need the game. The mod's own
            // faces are not: they are in the build output, so they are measured exactly and a frame
            // reproduces on a machine that has never had Space Engineers.
            var fonts = new FontSet(new FallbackFonts(_device, "Segoe UI"));
            foreach (var font in SprocketContent.Instance.Fonts)
                fonts = fonts.And(font.Name, font.Metrics(_device));

            _device.Fonts = fonts;

            ScreenSession.Install(_device);

            float safeWidth = windowHeight * 4f / 3f;
            _device.SafeArea = new RectangleF((windowWidth - safeWidth) / 2f, 0f, safeWidth, windowHeight);
            _device.ViewportSize = new Vector2(windowWidth, windowHeight);

            _graphics = new ScreenSpaceGraphics(_device, MyScreenSpaceLayer.AboveHud, SprocketContent.Instance.ImagesFor(_device), () => _scene == null ? 0 : _scene.CurrentTick);
            _scene = scene();
        }

        /// <summary>The editor being drawn.</summary>
        public EditorScene Scene => _scene;

        /// <summary>The last frame's calls. Beginning a frame empties it, so this is never a history.</summary>
        public IList<DrawCommand> Commands => _device.Commands;

        /// <summary>How many times anything has asked for the width of a string. See the device.</summary>
        public int Measured => _device.Measured;

        /// <summary>How tall one line of <paramref name="font" /> is in the font's own units.</summary>
        public int LineUnits(string font)
        {
            return _device.LineUnits(font);
        }

        /// <summary>How tall a line of this font stands at a scale the game was handed, in the units the screen lays out in.</summary>
        public float LineHeight(string font, float scale)
        {
            return _device.LineHeight(font, scale) / (_height / ScreenSpaceGraphics.GameReferenceHeight);
        }

        /// <summary>Draws one frame of the editor, the last frame's calls forgotten.</summary>
        public void Draw()
        {
            _device.BeginFrame();
            _graphics.Begin();
            _scene.Draw(_graphics);
        }

        /// <summary>Draws, then runs the script, drawing after every step.</summary>
        public void Play(Script script)
        {
            Draw();

            if (script != null)
                script.Play(_scene, Draw);
        }

        /// <summary>The frame as text, in the shape a person or a diff can read.</summary>
        public string Describe()
        {
            var text = new StringBuilder();
            text.AppendLine("window        " + Shown.Say(_width) + " x " + Shown.Say(_height));
            text.AppendLine("safe gui area " + Shown.Say(_device.SafeArea));
            text.AppendLine("UI size       " + Shown.Say(_graphics.Size));
            text.AppendLine("screen bounds " + Shown.Say(_graphics.ScreenBounds));
            text.AppendLine("on screen     " + _scene.OnScreen);
            text.AppendLine();

            IList<DrawCommand> commands = _device.Commands;

            for (int i = 0; i < commands.Count; i++)
            {
                DrawCommand command = commands[i];
                text.Append(i.ToString("000")).Append("  ").Append(command.Kind.ToString().PadRight(9)).Append("  ");

                switch (command.Kind)
                {
                    case DrawKind.SetClip:
                        text.Append(Shown.Say(command.Clip));
                        break;
                    case DrawKind.ClearClip:
                        break;
                    case DrawKind.Sprite:
                        text.Append(command.Material.String.PadRight(22))
                            .Append(Shown.Say(command.Destination))
                            .Append("  ").Append(Ink(command.Color));
                        break;
                    case DrawKind.Text:
                        text.Append("at ").Append(Shown.Say(command.Position))
                            .Append(" scale ").Append(command.Scale.ToString("0.###"))
                            .Append("  ").Append(Ink(command.Color))
                            .Append("  \"").Append(command.Text).Append('"');
                        break;
                }

                text.AppendLine();
            }

            return text.ToString();
        }

        /// <summary>A colour as eight hex digits, so a tint that changed shows up in a diff.</summary>
        private static string Ink(Color color)
        {
            return "#"
                + color.R.ToString("X2") + color.G.ToString("X2")
                + color.B.ToString("X2") + color.A.ToString("X2");
        }
    }
}
