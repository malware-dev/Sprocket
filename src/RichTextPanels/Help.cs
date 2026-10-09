using Lumel;
using Lumel.Controls;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Screens;
using Lumel.Text;
using Lumel.Viewing;
using VRage.Utils;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace RichTextPanels
{
    /// <summary>The guide to writing a panel's text, opened over the terminal as a screen of its own, drawn as the rich text it teaches.</summary>
    public static class Help
    {
        /// <summary>How big the screen is, in the game's normalized GUI units.</summary>
        static readonly Vector2 Size = new Vector2(0.55f, 0.85f);

        /// <summary>A flat white the screen tints to every fill it draws.</summary>
        /// <remarks>Declared in <c>Content/Data/TransparentMaterials.sbc</c> over the game's own blank texture.</remarks>
        static readonly MyStringId Fill = MyStringId.GetOrCompute("MalwareRichText_Fill");

        static readonly Color Page = new Color(22, 28, 33);
        static readonly Color Ink = new Color(220, 230, 235);
        static readonly Color Muted = new Color(162, 180, 188);
        static readonly Color Line = new Color(60, 72, 80);

        /// <summary>Opens the guide, its code drawn in the face code takes on <paramref name="surface" />.</summary>
        public static void Open(IMyTextSurface surface)
        {
            var close = new Button
            {
                Text = "Close",
                FontSize = 20f,
                Color = Ink,
                Background = Line,
                Padding = new Thickness(16f, 4f)
            };

            var root = new VStack
            {
                Background = Page,
                Padding = new Thickness(24f),
                Spacing = 16f,
                Children =
                {
                    new HStack
                    {
                        Spacing = 16f,
                        Children =
                        {
                            new Label { Text = "Writing rich text", FontSize = 32f, Font = "SpaceEngineersBold", Color = Ink, Placement = Flex.Grow },
                            close
                        }
                    },
                    new ScrollView { Placement = Flex.Grow, Content = Text(CodeFace.Of(surface)) }
                }
            };

            var stage = new Stage(root);
            close.Command = new Relay(stage.Close);

            var screen = new GameScreenSpace();
            new GameScreen("RichTextHelp", Size, stage, screen, Fill, new ScreenSpaceImages(screen)).Open();
        }

        static RichText Text(string code)
        {
            return new RichText
            {
                Markdown = Guide.Text,
                Font = "White",
                StrongFont = "SpaceEngineersBold",
                EmphasisFont = "SpaceEngineersItalic",
                StrongEmphasisFont = "SpaceEngineersBoldItalic",
                CodeFont = code,
                FontSize = 20f,
                Heading1Size = 32f,
                Heading2Size = 24f,
                Heading3Size = 20f,
                BlockSpacing = 12f,
                Indent = 24f,
                Color = Muted,
                StrongColor = Ink,
                EmphasisColor = Ink,
                HeadingColor = Ink,
                RuleColor = Line,
                Padding = new Thickness(0f, 0f, 16f, 0f)
            };
        }
    }
}
